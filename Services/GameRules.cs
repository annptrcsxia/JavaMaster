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
}