namespace JavaMaster.Api.Entities;

public class Deck
{
    public int Id { get; set; }

    public int OwnerUserId { get; set; }
    public User? Owner { get; set; }

    public string Name { get; set; } = "";
    public int CoverNumber { get; set; }          // 1 to 6

    public string Share { get; set; } = "public";   // "public" or "private"
    public string ShareCode { get; set; } = "";     // random 8 characters, for the Copy link

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<DeckCard> Cards { get; set; } = new();
}