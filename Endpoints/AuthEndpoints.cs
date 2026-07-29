using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using mlm.Models;
using mlm.Security;

using mlm.Services;

namespace mlm.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/register", async (RegisterRequest request, UserManager<AppUser> users, BinaryService binary) =>
        {
            if (string.IsNullOrWhiteSpace(request.FirstName) ||
                string.IsNullOrWhiteSpace(request.LastName) ||
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Username))
            {
                return Results.BadRequest("First name, last name, email and username are required.");
            }

            // Resolve the sponsor from the referral code (optional — the first member has none).
            Guid? sponsorId = null;
            if (!string.IsNullOrWhiteSpace(request.ReferralCode))
            {
                var sponsor = await users.Users
                    .FirstOrDefaultAsync(u => u.ReferralCode == request.ReferralCode);
                if (sponsor is null)
                {
                    return Results.BadRequest("Invalid referral code.");
                }

                sponsorId = sponsor.Id;
            }

            // Generate a unique referral code for the new member.
            string referralCode;
            do
            {
                referralCode = ReferralCodeGenerator.Generate();
            }
            while (await users.Users.AnyAsync(u => u.ReferralCode == referralCode));

            var user = new AppUser
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                UserName = request.Username,
                ReferralCode = referralCode,
                SponsorId = sponsorId
            };

            var result = await users.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                return Results.BadRequest(string.Join(" ", result.Errors.Select(e => e.Description)));
            }

            // Binary placement: if there's a sponsor, place in binary tree.
            if (sponsorId.HasValue)
            {
                await binary.PlaceMemberAsync(sponsorId.Value, user.Id);
            }

            return Results.Ok(user.ToDto());
        })
        .WithName("Register")
        .WithTags("Auth");

        app.MapPost("/login", async (LoginRequest request, UserManager<AppUser> users, JwtTokenService tokens, HttpResponse response) =>
        {
            if (string.IsNullOrWhiteSpace(request.UsernameOrEmail) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return Results.BadRequest("Username/email and password are required.");
            }

            var user = await users.FindByNameAsync(request.UsernameOrEmail)
                       ?? await users.FindByEmailAsync(request.UsernameOrEmail);

            if (user is null || !await users.CheckPasswordAsync(user, request.Password))
            {
                return Results.Unauthorized();
            }

            var roles = await users.GetRolesAsync(user);
            var (token, expiresAt) = tokens.CreateToken(user, roles);
            // Keep setting the cookie (works same-origin) and also return the token so
            // the SPA can use bearer auth cross-origin (no proxy).
            AuthCookie.Append(response, token, expiresAt);

            return Results.Ok(new AuthResponse(token, expiresAt, user.ToDto()));
        })
        .WithName("Login")
        .WithTags("Auth");

        app.MapPost("/logout", (HttpResponse response) =>
        {
            response.Cookies.Delete(AuthCookie.Name);
            return Results.Ok();
        })
        .WithName("Logout")
        .WithTags("Auth");

        app.MapGet("/me", async (ClaimsPrincipal principal, UserManager<AppUser> users) =>
        {
            var user = await users.GetUserAsync(principal);
            return user is null ? Results.NotFound() : Results.Ok(user.ToDto());
        })
        .RequireAuthorization()
        .WithName("Me")
        .WithTags("Auth");

        return app;
    }
}
