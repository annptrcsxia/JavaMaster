using JavaMaster.Api.Data;
using JavaMaster.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JavaMaster.Api.Controllers;

[ApiController]
[Route("api/me")]
public class MeController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _me;
    public MeController(AppDbContext db, ICurrentUserService me) { _db = db; _me = me; }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var user = await _me.GetAsync();
        var ranks = await _db.Ranks.ToListAsync();
        var rank = RankCalculator.For(user.TotalXp, ranks);

        var inventory = await _db.UserInventory
            .Where(i => i.UserId == user.Id)
            .Select(i => new { i.ShopItem!.Code, i.Quantity })
            .ToListAsync();
        var repairs = inventory.FirstOrDefault(i => i.Code == "STREAK_REPAIR")?.Quantity ?? 0;

        // Streak days are counted in Philippine time (UTC+8)
        var today = StreakService.Today();
        var last = user.LastActivityDate;
        var alive = last != null && last >= today.AddDays(-1);   // played today or yesterday

        // Days missed since the last activity (0 while the streak is fine or already gone)
        var missedDays = 0;
        if (last != null && user.CurrentStreak > 0 && last < today.AddDays(-1))
            missedDays = today.DayNumber - last.Value.DayNumber - 1;

        return Ok(new
        {
            fullName = user.FullName,
            coins = user.Coins,
            totalXp = user.TotalXp,
            rank = new
            {
                name = rank.Name,
                minXp = rank.RankMinXp,
                maxXp = rank.RankMaxXp,
                icon = "/rank titles/" + rank.IconFile
            },
            streak = alive ? user.CurrentStreak : 0,
            activeToday = last == today,
            missedDays,                                            // 0 = fine, 1 = repairable, 2+ = too late
            lostStreak = missedDays > 0 ? user.CurrentStreak : 0,  // the "14" in "Repair 14-day streak"
            canRepairStreak = missedDays == 1 && repairs > 0,      // missed exactly one day AND owns 1+
            inventory = inventory.ToDictionary(x => x.Code, x => x.Quantity)
        });
    }
}