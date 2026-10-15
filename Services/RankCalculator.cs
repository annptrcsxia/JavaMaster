using JavaMaster.Api.Entities;

namespace JavaMaster.Api.Services;

public record RankInfo(string Name, int RankMinXp, int RankMaxXp, string IconFile);

public static class RankCalculator
{
    public static RankInfo For(int totalXp, IReadOnlyList<Rank> ranks)
    {
        var ordered = ranks.OrderBy(r => r.MinXp).ToList();
        var rank = ordered.LastOrDefault(r => totalXp >= r.MinXp) ?? ordered.First();
        return new RankInfo(rank.Name, rank.MinXp, rank.MaxXp, rank.IconFile);
    }
}