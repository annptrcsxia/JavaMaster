namespace JavaMaster.Api.Entities;

public class UserDailyChallenge
{
    public int UserId { get; set; }
    public DateOnly Date { get; set; }      // Philippine date
    public int QuestionId { get; set; }
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
}