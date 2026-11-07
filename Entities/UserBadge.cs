namespace JavaMaster.Api.Entities;

/// <summary>
/// A badge the player has earned by finishing a lesson for the first time.
/// </summary>
public class UserBadge
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }

    public int LearnNodeId { get; set; }
    public LearnNode? LearnNode { get; set; }

    public DateTime EarnedAt { get; set; } = DateTime.UtcNow;
}