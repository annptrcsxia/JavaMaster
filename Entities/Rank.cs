namespace JavaMaster.Api.Entities;

public class Rank
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int MinXp { get; set; }
    public int MaxXp { get; set; }
    public string IconFile { get; set; } = "";
}