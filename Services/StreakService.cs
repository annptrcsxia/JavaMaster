using JavaMaster.Api.Entities;

namespace JavaMaster.Api.Services;

public static class StreakService
{
    // Today's date in Philippine time (UTC+8)
    public static DateOnly Today() => DateOnly.FromDateTime(DateTime.UtcNow.AddHours(8));

    // Counts today as an active day and returns the streak bonus coins,
    // or 0 if today was already counted. The caller saves the changes.
    public static int RecordActivity(User user)
    {
        var today = Today();
        if (user.LastActivityDate == today) return 0;

        user.CurrentStreak = user.LastActivityDate == today.AddDays(-1) ? user.CurrentStreak + 1 : 1;
        user.LongestStreak = Math.Max(user.LongestStreak, user.CurrentStreak);
        user.LastActivityDate = today;
        user.Coins += GameRules.StreakBonusCoins;
        return GameRules.StreakBonusCoins;
    }
}