namespace JavaMaster.Api.Entities;

public class LearnSection
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public int SortOrder { get; set; }
    public bool IsPublished { get; set; } = true;

    public List<LearnNode> Nodes { get; set; } = new();
}