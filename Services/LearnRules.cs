using JavaMaster.Api.Data;
using JavaMaster.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace JavaMaster.Api.Services;

public static class LearnRules
{
    // One rule everywhere: a node is open only when the node before it is Completed.
    public static async Task<(LearnNode? node, bool unlocked, LearnNode? next)> LocateAsync(
        AppDbContext db, int userId, int nodeId)
    {
        var all = await db.LearnNodes
            .Where(n => n.LearnSection!.IsPublished)
            .OrderBy(n => n.LearnSection!.SortOrder).ThenBy(n => n.SortOrder)
            .ToListAsync();
        var i = all.FindIndex(n => n.Id == nodeId);
        if (i < 0) return (null, false, null);

        var next = i + 1 < all.Count ? all[i + 1] : null;
        if (i == 0) return (all[i], true, next);

        var prevId = all[i - 1].Id;
        var unlocked = await db.UserNodeProgress.AnyAsync(p =>
            p.UserId == userId && p.LearnNodeId == prevId && p.Status == ProgressStatus.Completed);
        return (all[i], unlocked, next);
    }
}