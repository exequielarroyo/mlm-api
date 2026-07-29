namespace mlm.Models;

public class BinaryPair
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>The user who earned this commission.</summary>
    public Guid UserId { get; set; }

    /// <summary>Number of pairs matched in this record.</summary>
    public int PairsMatched { get; set; }

    /// <summary>Commission amount earned.</summary>
    public decimal CommissionAmount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// DTOs

public record BinaryTreeNodeDto(
    Guid Id,
    string FirstName,
    string LastName,
    string? Username,
    int LeftCount,
    int RightCount,
    LegPosition? Position);

public record BinaryTreeDto(
    BinaryTreeNodeDto? Root,
    BinaryTreeNodeDto? Left,
    BinaryTreeNodeDto? Right);

public record BinaryStatsDto(
    int LeftCount,
    int RightCount,
    int MatchedPairs,
    decimal TotalEarned);

public record BinaryPairDto(
    Guid Id,
    int PairsMatched,
    decimal CommissionAmount,
    DateTime CreatedAt);

public record PlaceMemberRequest(
    Guid SponsorId,
    Guid NewUserId,
    LegPosition Position);
