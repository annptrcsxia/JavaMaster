namespace JavaMaster.Api.Entities;

public class Question
{
    public int Id { get; set; }
    public int TopicId { get; set; }
    public Topic? Topic { get; set; }

    public QuestionType Type { get; set; }
    public QuestionPurpose Purpose { get; set; } = QuestionPurpose.Lesson;
    public string Prompt { get; set; } = "";
    public string? CodeSnippet { get; set; }

    // JSON text. Payload is what the browser sees (options/tokens).
    public string Payload { get; set; } = "{}";
    // JSON text. AnswerKey is NEVER sent to the browser.
    public string AnswerKey { get; set; } = "{}";

    public string? Hint { get; set; }
    public string? Explanation { get; set; }
    public int Difficulty { get; set; } = 1;

    public int? OwnerUserId { get; set; }   // null = official question
    public bool IsActive { get; set; } = true;
}