using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using mlm.Models;
using mlm.Services;

namespace mlm.Endpoints;

public static class BinaryEndpoints
{
    private static readonly AuthorizeAttribute AdminOnly = new() { Roles = "Admin" };

    public static IEndpointRouteBuilder MapBinaryEndpoints(this IEndpointRouteBuilder app)
    {
        // Get user's binary tree
        app.MapGet("/binary/tree", async (ClaimsPrincipal principal, UserManager<AppUser> users, BinaryService binary) =>
        {
            var me = await users.GetUserAsync(principal);
            if (me is null) return Results.NotFound();

            var tree = await binary.GetTreeAsync(me.Id);
            return tree is null ? Results.NotFound() : Results.Ok(tree);
        })
        .RequireAuthorization()
        .WithName("BinaryTree")
        .WithTags("Binary");

        // Get user's binary stats
        app.MapGet("/binary/stats", async (ClaimsPrincipal principal, UserManager<AppUser> users, BinaryService binary) =>
        {
            var me = await users.GetUserAsync(principal);
            if (me is null) return Results.NotFound();

            var stats = await binary.GetStatsAsync(me.Id);
            return stats is null ? Results.NotFound() : Results.Ok(stats);
        })
        .RequireAuthorization()
        .WithName("BinaryStats")
        .WithTags("Binary");

        // Get user's pair commission history
        app.MapGet("/binary/pairs", async (ClaimsPrincipal principal, UserManager<AppUser> users, BinaryService binary) =>
        {
            var me = await users.GetUserAsync(principal);
            if (me is null) return Results.NotFound();

            var pairs = await binary.GetPairsAsync(me.Id);
            return Results.Ok(pairs);
        })
        .RequireAuthorization()
        .WithName("BinaryPairs")
        .WithTags("Binary");

        // Admin: manually place member
        app.MapPost("/admin/binary/place", async (PlaceMemberRequest request, BinaryService binary) =>
        {
            var (success, error) = await binary.AdminPlaceMemberAsync(request.SponsorId, request.NewUserId, request.Position);
            return success ? Results.Ok() : Results.BadRequest(error);
        })
        .RequireAuthorization(AdminOnly)
        .WithName("AdminPlaceBinary")
        .WithTags("Binary");

        // Auto-place member under sponsor (called during registration)
        app.MapPost("/binary/place", async (PlaceMemberRequest request, BinaryService binary) =>
        {
            var (success, error) = await binary.PlaceMemberAsync(request.SponsorId, request.NewUserId, null);
            return success ? Results.Ok() : Results.BadRequest(error);
        })
        .RequireAuthorization()
        .WithName("PlaceBinary")
        .WithTags("Binary");

        return app;
    }
}
