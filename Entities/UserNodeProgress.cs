namespace JavaMaster.Api.Entities;

public class UserNodeProgress
{
    public int UserId { get; set; }
    public User? User { get; set; }

    public int LearnNodeId { get; set; }
    public LearnNode? LearnNode { get; set; }

    public ProgressStatus Status { get; set; } = ProgressStatus.Locked;
    public int TimesCompleted { get; set; }
    public DateTime? LastPlayedAt { get; set; }
}