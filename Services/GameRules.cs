namespace JavaMaster.Api.Services;

public static class GameRules
{
    public const int StartingCoins = 80;
    public const int MaxItemStack = 30;

    public const int StreakBonusCoins = 30;
    public const int DailyXp = 10;
    public const int DailyCoins = 5;

    public static readonly (string Code, int Qty)[] StartingItems =
    {
        ("HEAL_POTION", 3),
        ("HINT", 3),
        ("STREAK_REPAIR", 1)
    };

    // Decks
    public const int DeckNameMaxLength = 30;
    public const int DeckMinCards = 1;
    public const int DeckMaxCards = 50;
    public const int DeckCoverCount = 6;
    public const int DeckShareCodeLength = 6;
    public const int DeckQuestionMaxLength = 200;
    public const int DeckOptionMaxLength = 60;       // multiple-choice options
    public const int DeckMatchTextMaxLength = 40;    // matching item and answer
    public const int DeckMinPairs = 2;
    public const int DeckMaxPairs = 8;
    public const int DeckCodeMaxLength = 1000;
    public const int DeckMinBlanks = 1;
    public const int DeckMaxBlanks = 5;
    public const int DeckAnswerMaxLength = 40;       // blank answers and decoys
    public const int DeckMaxDecoys = 5;

    public static readonly string[] DeckShareValues = { "public", "private" };
    public static readonly string[] DeckQuestionTypes =
    {
        "multiple-choice", "true-false", "matching", "fill-blank-code", "drag-drop-code"
    };
}