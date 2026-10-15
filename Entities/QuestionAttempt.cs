namespace JavaMaster.Api.Entities;

public class QuestionAttempt
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int QuestionId { get; set; }
    public int? LearnNodeId { get; set; }
    public bool IsCorrect { get; set; }
    public string SubmittedAnswer { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}