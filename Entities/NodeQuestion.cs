namespace JavaMaster.Api.Entities;

public class NodeQuestion
{
    public int LearnNodeId { get; set; }
    public LearnNode? LearnNode { get; set; }

    public int QuestionId { get; set; }
    public Question? Question { get; set; }

    public int SortOrder { get; set; }   // fixed order, tutorial style
}