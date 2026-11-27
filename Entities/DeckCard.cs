namespace JavaMaster.Api.Entities;

public class DeckCard
{
    public int Id { get; set; }

    public int DeckId { get; set; }
    public Deck? Deck { get; set; }

    public int Position { get; set; }               // order inside the deck, starts at 0
    public string Type { get; set; } = "";          // multiple-choice, true-false, matching, fill-blank-code, drag-drop-code
    public string Question { get; set; } = "";

    // JSON: the part that depends on the type (options, pairs, parts and answers, decoys)
    public string Data { get; set; } = "{}";
}