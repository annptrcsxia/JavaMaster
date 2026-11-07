namespace JavaMaster.Api.Entities;

public class Deck
{
    public int Id { get; set; }

    public int OwnerUserId { get; set; }
    public User? Owner { get; set; }

    public string Name { get; set; } = "";
    public string? Description { get; set; }

    public int CoverNumber { get; set; }      // 1 to 6
    public int ItemCount { get; set; }        // 10, 15 or 20

    // JSON list of type ids, for example ["multiple-choice","fill-blank"]
    public string QuestionTypes { get; set; } = "[]";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}