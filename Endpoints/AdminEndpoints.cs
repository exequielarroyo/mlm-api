using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using mlm.Models;

namespace mlm.Endpoints;

public static class AdminEndpoints
{
    private static readonly AuthorizeAttribute AdminOnly = new() { Roles = "Admin" };

    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/admin/users/search", async (string? q, UserManager<AppUser> users) =>
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
                return Results.Ok(Array.Empty<UserSearchResultDto>());

            var results = await users.Users
                .Where(u =>
                    u.FirstName.Contains(q) ||
                    u.LastName.Contains(q) ||
                    (u.UserName != null && u.UserName.Contains(q)) ||
                    u.ReferralCode.Contains(q))
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .Take(20)
                .Select(u => new UserSearchResultDto(
                    u.Id,
                    u.FirstName,
                    u.LastName,
                    u.UserName,
                    u.ReferralCode,
                    u.Email))
                .ToListAsync();

            return Results.Ok(results);
        })
        .RequireAuthorization(AdminOnly)
        .WithName("AdminSearchUsers")
        .WithTags("Admin");

        return app;
    }
}

public record UserSearchResultDto(
    Guid Id,
    string FirstName,
    string LastName,
    string? UserName,
    string ReferralCode,
    string? Email);
