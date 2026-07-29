using Microsoft.AspNetCore.Identity;

namespace mlm.Models;

public enum LegPosition { Left, Right }

/// <summary>
/// A member of the MLM network. Extends the ASP.NET Core Identity user with the
/// referral relationship (SponsorId) that forms the network tree, and binary tree fields.
/// </summary>
public class AppUser : IdentityUser<Guid>
{
    public AppUser()
    {
        // Identity does not generate the key for a Guid PK, so seed it here.
        Id = Guid.NewGuid();
    }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    /// <summary>Unique, shareable code others use to sign up under this member.</summary>
    public string ReferralCode { get; set; } = string.Empty;

    /// <summary>The user who referred this member. Null for the root member.</summary>
    public Guid? SponsorId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Binary tree fields
    /// <summary>Parent in the binary tree. Null for root or unplaced members.</summary>
    public Guid? BinaryParentId { get; set; }

    /// <summary>Direct left child in binary tree.</summary>
    public Guid? LeftLegId { get; set; }

    /// <summary>Direct right child in binary tree.</summary>
    public Guid? RightLegId { get; set; }

    /// <summary>Position under parent (Left or Right).</summary>
    public LegPosition? Position { get; set; }

    /// <summary>Total members in left subtree.</summary>
    public int LeftCount { get; set; }

    /// <summary>Total members in right subtree.</summary>
    public int RightCount { get; set; }

    // Navigation properties for binary tree
    public AppUser? BinaryParent { get; set; }
    public AppUser? LeftLeg { get; set; }
    public AppUser? RightLeg { get; set; }
}
