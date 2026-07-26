using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using mlm.Data;
using mlm.Endpoints;
using mlm.Models;
using mlm.Security;

var builder = WebApplication.CreateBuilder(args);

// Render (and most PaaS) tell the app which port to listen on via the PORT env var.
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Prefer a DATABASE_URL (e.g. Render Postgres) and fall back to the configured connection string.
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
var connectionString = !string.IsNullOrWhiteSpace(databaseUrl)
    ? BuildNpgsqlConnectionString(databaseUrl)
    : builder.Configuration.GetConnectionString("Default");

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

builder.Services
    .AddIdentityCore<AppUser>(options =>
    {
        // Relaxed policy for early development; tighten later.
        options.Password.RequiredLength = 6;
        options.Password.RequireDigit = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.User.RequireUniqueEmail = true;
    })
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<AppDbContext>();

builder.Services.AddScoped<JwtTokenService>();

var jwt = builder.Configuration.GetSection("Jwt");
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwt["Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!)),
            ValidateLifetime = true
        };

        // The token is delivered as an httpOnly cookie, not an Authorization header.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (context.Request.Cookies.TryGetValue(AuthCookie.Name, out var token))
                {
                    context.Token = token;
                }

                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();

// Behind Render's TLS-terminating proxy the app receives plain http; honor
// X-Forwarded-Proto so Request.IsHttps is correct (drives the Secure cookie flag).
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// Direct browser->API calls only happen in local dev (prod is same-origin via the Netlify /api proxy).
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                  ?? ["http://localhost:4200"];
const string uiCorsPolicy = "ui";
builder.Services.AddCors(options =>
{
    options.AddPolicy(uiCorsPolicy, policy =>
        policy.WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

var app = builder.Build();

// Apply pending EF migrations at startup (safe on Render's single free instance).
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    services.GetRequiredService<AppDbContext>().Database.Migrate();
    var roles = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
    if (!await roles.RoleExistsAsync("Admin"))
    {
        await roles.CreateAsync(new IdentityRole<Guid>("Admin"));
    }

    var bootstrapEmails = builder.Configuration.GetSection("Admin:BootstrapEmails").Get<string[]>() ?? [];
    var users = services.GetRequiredService<UserManager<AppUser>>();
    foreach (var email in bootstrapEmails.Where(email => !string.IsNullOrWhiteSpace(email)))
    {
        var user = await users.FindByEmailAsync(email);
        if (user is not null && !await users.IsInRoleAsync(user, "Admin"))
        {
            await users.AddToRoleAsync(user, "Admin");
        }
    }
}

app.UseForwardedHeaders();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseHttpsRedirection();
}

app.UseCors(uiCorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
    .WithName("Health")
    .WithTags("Health");

app.MapAuthEndpoints();
app.MapExternalAuthEndpoints();
app.MapReferralEndpoints();
app.MapCommerceEndpoints();

app.Run();

// Converts a "postgres://user:pass@host:port/db" URL (Render) into an Npgsql connection string.
static string BuildNpgsqlConnectionString(string databaseUrl)
{
    var uri = new Uri(databaseUrl);
    var userInfo = uri.UserInfo.Split(':', 2);

    return new NpgsqlConnectionStringBuilder
    {
        Host = uri.Host,
        Port = uri.Port > 0 ? uri.Port : 5432,
        Username = Uri.UnescapeDataString(userInfo[0]),
        Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty,
        Database = uri.AbsolutePath.TrimStart('/'),
        SslMode = SslMode.Prefer
    }.ToString();
}
