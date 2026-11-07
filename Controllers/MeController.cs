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

    // The avatars the app ships with. A profile can only point at one of these,
    // so a bad path can never end up in the navbar of every page.
    private static readonly string[] Avatars =
    {
        "/avatars/Avatar 1.png", "/avatars/Avatar 2.png", "/avatars/Avatar 3.png",
        "/avatars/Avatar 4.png", "/avatars/Avatar 5.png", "/avatars/Avatar 6.png",
        "/avatars/Avatar 7.png", "/avatars/Avatar 8.png", "/avatars/Avatar 9.png"
    };

    public const string DefaultAvatar = "/avatars/Avatar 1.png";

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

        // Badges earned by finishing lessons for the first time
        var badges = await _db.UserBadges.CountAsync(x => x.UserId == user.Id);

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
            school = user.School,
            grade = user.Grade,
            avatar = user.AvatarPath ?? DefaultAvatar,
            joinedDate = user.CreatedAt,
            bestStreak = user.LongestStreak,
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
            inventory = inventory.ToDictionary(x => x.Code, x => x.Quantity),
            badges
        });
    }

    public record ProfileRequest(string? FullName, string? School, string? Grade, string? Avatar);

    // Saves the profile. A field that is left out stays as it is, and a field sent
    // as an empty string is cleared - so a page can change one thing without
    // having to send the whole profile back.
    [HttpPatch("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] ProfileRequest req)
    {
        var user = await _me.GetAsync();

        if (req.FullName != null)
        {
            var name = req.FullName.Trim();
            if (name.Length == 0)
                return BadRequest(new { error = "Your name cannot be empty." });
            if (name.Length > 100)
                return BadRequest(new { error = "That name is too long." });
            user.FullName = name;
        }

        if (req.School != null)
        {
            var school = req.School.Trim();
            if (school.Length > 100)
                return BadRequest(new { error = "That school name is too long." });
            user.School = school.Length == 0 ? null : school;
        }

        if (req.Grade != null)
        {
            var grade = req.Grade.Trim();
            if (grade.Length > 50)
                return BadRequest(new { error = "That grade is too long." });
            user.Grade = grade.Length == 0 ? null : grade;
        }

        if (req.Avatar != null)
        {
            var avatar = req.Avatar.Trim();
            if (avatar.Length == 0)
            {
                user.AvatarPath = null;                     // back to the default avatar
            }
            else if (!Avatars.Contains(avatar))
            {
                return BadRequest(new { error = "That avatar does not exist." });
            }
            else
            {
                user.AvatarPath = avatar;
            }
        }

        await _db.SaveChangesAsync();

        return Ok(new
        {
            fullName = user.FullName,
            school = user.School,
            grade = user.Grade,
            avatar = user.AvatarPath ?? DefaultAvatar
        });
    }
}