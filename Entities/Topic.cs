namespace JavaMaster.Api.Entities;

public class Topic
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public bool IsActive { get; set; } = true;
}