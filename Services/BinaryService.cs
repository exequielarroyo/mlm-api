using Microsoft.EntityFrameworkCore;
using mlm.Data;
using mlm.Models;

namespace mlm.Services;

public class BinaryService
{
    private const decimal PairCommissionRate = 100m;

    private readonly AppDbContext _db;

    public BinaryService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<(bool Success, string Error)> PlaceMemberAsync(Guid sponsorId, Guid newUserId, LegPosition? forcePosition = null)
    {
        var sponsor = await _db.Users.FindAsync(sponsorId);
        if (sponsor is null) return (false, "Sponsor not found.");

        var newUser = await _db.Users.FindAsync(newUserId);
        if (newUser is null) return (false, "User not found.");

        if (newUser.BinaryParentId is not null)
            return (false, "User is already placed in the binary tree.");

        LegPosition position;
        if (forcePosition.HasValue)
        {
            position = forcePosition.Value;
        }
        else
        {
            if (sponsor.LeftLegId is null)
                position = LegPosition.Left;
            else if (sponsor.RightLegId is null)
                position = LegPosition.Right;
            else
                position = sponsor.LeftCount <= sponsor.RightCount ? LegPosition.Left : LegPosition.Right;
        }

        newUser.BinaryParentId = sponsorId;
        newUser.Position = position;

        if (position == LegPosition.Left)
            sponsor.LeftLegId = newUserId;
        else
            sponsor.RightLegId = newUserId;

        // Bulk tree update
        await UpdateTreeBulkAsync(sponsorId, position);

        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> AdminPlaceMemberAsync(Guid sponsorId, Guid newUserId, LegPosition position)
    {
        return await PlaceMemberAsync(sponsorId, newUserId, position);
    }

    public async Task<BinaryTreeDto?> GetTreeAsync(Guid userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user is null) return null;

        BinaryTreeNodeDto? ToNode(AppUser? u) => u is null ? null : new BinaryTreeNodeDto(
            u.Id, u.FirstName, u.LastName, u.UserName, u.LeftCount, u.RightCount, u.Position);

        var root = user;
        while (root.BinaryParentId is not null)
            root = await _db.Users.FindAsync(root.BinaryParentId) ?? root;

        return new BinaryTreeDto(
            ToNode(root),
            root.LeftLegId.HasValue ? ToNode(await _db.Users.FindAsync(root.LeftLegId)) : null,
            root.RightLegId.HasValue ? ToNode(await _db.Users.FindAsync(root.RightLegId)) : null);
    }

    public async Task<BinaryStatsDto?> GetStatsAsync(Guid userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user is null) return null;

        var totalEarned = await _db.BinaryPairs
            .Where(p => p.UserId == userId)
            .SumAsync(p => p.CommissionAmount);

        var matchedPairs = await _db.BinaryPairs
            .Where(p => p.UserId == userId)
            .SumAsync(p => p.PairsMatched);

        return new BinaryStatsDto(
            user.LeftCount,
            user.RightCount,
            matchedPairs,
            totalEarned);
    }

    public async Task<List<BinaryPairDto>> GetPairsAsync(Guid userId)
    {
        return await _db.BinaryPairs
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new BinaryPairDto(p.Id, p.PairsMatched, p.CommissionAmount, p.CreatedAt))
            .ToListAsync();
    }

    private async Task UpdateTreeBulkAsync(Guid startUserId, LegPosition addedPosition)
    {
        var ancestors = await _db.Users.FromSqlRaw(@$"
            WITH RECURSIVE AncestorTree AS (
                SELECT ""Id"", ""BinaryParentId"", ""LeftCount"", ""RightCount"" FROM ""AspNetUsers"" WHERE ""Id"" = {{0}}
                UNION ALL
                SELECT u.""Id"", u.""BinaryParentId"", u.""LeftCount"", u.""RightCount"" FROM ""AspNetUsers"" u
                INNER JOIN AncestorTree a ON u.""Id"" = a.""BinaryParentId""
            )
            SELECT * FROM AncestorTree
        ", startUserId).ToListAsync();

        foreach (var user in ancestors)
        {
            if (addedPosition == LegPosition.Left) user.LeftCount++;
            else user.RightCount++;

            var matchedPairs = Math.Min(user.LeftCount, user.RightCount);
            var existingPairs = await _db.BinaryPairs
                .Where(p => p.UserId == user.Id)
                .SumAsync(p => p.PairsMatched);

            var newPairs = matchedPairs - existingPairs;
            if (newPairs > 0)
            {
                _db.BinaryPairs.Add(new BinaryPair
                {
                    UserId = user.Id,
                    PairsMatched = newPairs,
                    CommissionAmount = newPairs * PairCommissionRate
                });
            }
        }
    }
}
