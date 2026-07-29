using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using mlm.Models;
using mlm.Security;
using mlm.Services;

namespace mlm.Endpoints;

public static class ExternalAuthEndpoints
{
    private const string Provider = "Google";

    public static IEndpointRouteBuilder MapExternalAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/google", async (
            GoogleLoginRequest request,
            UserManager<AppUser> users,
            JwtTokenService tokens,
            IConfiguration configuration,
            HttpResponse response,
            BinaryService binary) =>
        {
            if (string.IsNullOrWhiteSpace(request.IdToken))
            {
                return Results.BadRequest("Missing Google credential.");
            }

            // Verify the Google ID token server-side (audience must be our client id).
            GoogleJsonWebSignature.Payload payload;
            try
            {
                var settings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = [configuration["Authentication:Google:ClientId"]]
                };
                payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, settings);
            }
            catch
            {
                return Results.Unauthorized();
            }

            // 1) Returning Google user.
            var user = await users.FindByLoginAsync(Provider, payload.Subject);

            if (user is null)
            {
                // 2) Email already belongs to another account — do not link (keep separate).
                if (await users.FindByEmailAsync(payload.Email) is not null)
                {
                    return Results.Conflict(
                        "An account with this email already exists. Please log in with your password.");
                }

                // 3) New user — optional sponsor from referral code, else root.
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

                string referralCode;
                do
                {
                    referralCode = ReferralCodeGenerator.Generate();
                }
                while (await users.Users.AnyAsync(u => u.ReferralCode == referralCode));

                user = new AppUser
                {
                    FirstName = payload.GivenName ?? string.Empty,
                    LastName = payload.FamilyName ?? string.Empty,
                    Email = payload.Email,
                    UserName = payload.Email,
                    EmailConfirmed = payload.EmailVerified,
                    ReferralCode = referralCode,
                    SponsorId = sponsorId
                };

                var createResult = await users.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    return Results.BadRequest(string.Join(" ", createResult.Errors.Select(e => e.Description)));
                }

                var linkResult = await users.AddLoginAsync(
                    user, new UserLoginInfo(Provider, payload.Subject, Provider));
                if (!linkResult.Succeeded)
                {
                    return Results.BadRequest(string.Join(" ", linkResult.Errors.Select(e => e.Description)));
                }

                // Binary placement: if there's a sponsor, place in binary tree.
                if (sponsorId.HasValue)
                {
                    await binary.PlaceMemberAsync(sponsorId.Value, user.Id);
                }
            }

            var roles = await users.GetRolesAsync(user);
            var (token, expiresAt) = tokens.CreateToken(user, roles);
            AuthCookie.Append(response, token, expiresAt);

            return Results.Ok(new AuthResponse(token, expiresAt, user.ToDto()));
        })
        .WithName("GoogleLogin")
        .WithTags("Auth");

        return app;
    }
}
