namespace JavaMaster.Api.Entities;

public class PracticePass
{
    public int UserId { get; set; }
    public int LearnNodeId { get; set; }
    public int ItemId { get; set; }
    public DateTime PassedAt { get; set; } = DateTime.UtcNow;
}