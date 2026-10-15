namespace JavaMaster.Api.Entities;

public class LearnNode
{
    public int Id { get; set; }
    public int LearnSectionId { get; set; }
    public LearnSection? LearnSection { get; set; }

    public int? TopicId { get; set; }
    public Topic? Topic { get; set; }

    public string Title { get; set; } = "";
    public string? Subtitle { get; set; }        // e.g. "Your journey starts here."
    public string? IntroText { get; set; }
    public string? OutroText { get; set; }
    public NodeType NodeType { get; set; } = NodeType.Lesson;
    public int SortOrder { get; set; }
    public int XpReward { get; set; } = 10;
    public int CoinReward { get; set; } = 5;

    public List<NodeQuestion> NodeQuestions { get; set; } = new();
}