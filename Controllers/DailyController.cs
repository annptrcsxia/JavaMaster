using System.Text.Json;
using JavaMaster.Api.Data;
using JavaMaster.Api.Entities;
using JavaMaster.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JavaMaster.Api.Controllers;

[ApiController]
[Route("api/daily")]
public class DailyController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _me;
    public DailyController(AppDbContext db, ICurrentUserService me) { _db = db; _me = me; }

    public record DailyAnswer(JsonElement Answer);

    // Every player gets the same question each day, rotating through the pool.
    private async Task<Question?> TodaysQuestion(DateOnly today)
    {
        var pool = await _db.Questions
            .Where(q => q.Purpose == QuestionPurpose.Daily && q.IsActive)
            .OrderBy(q => q.Id).ToListAsync();
        return pool.Count == 0 ? null : pool[today.DayNumber % pool.Count];
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var user = await _me.GetAsync();
        var today = StreakService.Today();

        if (await _db.UserDailyChallenges.AnyAsync(d => d.UserId == user.Id && d.Date == today))
            return Ok(new { completed = true });

        var q = await TodaysQuestion(today);
        if (q == null) return NotFound(new { error = "No daily question yet." });

        // AnswerKey and Explanation are never sent
        return Ok(new
        {
            completed = false,
            question = new
            {
                q.Id, q.Type, q.Prompt,
                payload = JsonSerializer.Deserialize<JsonElement>(q.Payload)
            }
        });
    }

    [HttpPost("answer")]
    public async Task<IActionResult> Answer([FromBody] DailyAnswer req)
    {
        if (req.Answer.ValueKind == JsonValueKind.Undefined)
            return BadRequest(new { error = "Missing answer." });

        var user = await _me.GetAsync();
        var today = StreakService.Today();

        if (await _db.UserDailyChallenges.AnyAsync(d => d.UserId == user.Id && d.Date == today))
            return Conflict(new { error = "Challenge already completed today." });

        var q = await TodaysQuestion(today);
        if (q == null) return NotFound(new { error = "No daily question yet." });

        var correct = AnswerChecker.IsCorrect(q, req.Answer);
        _db.QuestionAttempts.Add(new QuestionAttempt
        {
            UserId = user.Id, QuestionId = q.Id, LearnNodeId = null,
            IsCorrect = correct, SubmittedAnswer = req.Answer.GetRawText()
        });

        if (!correct)
        {
            await _db.SaveChangesAsync();
            return Ok(new { correct = false, message = q.Explanation });
        }

        // Correct: the server grants the reward right away, so closing the tab loses nothing.
        var ranks = await _db.Ranks.ToListAsync();
        var xpBefore = user.TotalXp;
        var rankBefore = RankCalculator.For(xpBefore, ranks);

        user.TotalXp += GameRules.DailyXp;
        user.XpUpdatedAt = DateTime.UtcNow;
        user.Coins += GameRules.DailyCoins;
        var streakBonus = StreakService.RecordActivity(user);

        _db.UserDailyChallenges.Add(new UserDailyChallenge
        {
            UserId = user.Id, Date = today, QuestionId = q.Id
        });

        try { await _db.SaveChangesAsync(); }
        catch (DbUpdateException)   // a double click or a second tab got there first
        {
            return Conflict(new { error = "Challenge already completed today." });
        }

        var rankAfter = RankCalculator.For(user.TotalXp, ranks);
        var key = JsonSerializer.Deserialize<JsonElement>(q.AnswerKey);
        var praise = key.TryGetProperty("correctMessage", out var m) ? m.GetString() : "Great job!";

        return Ok(new
        {
            correct = true,
            message = praise,
            reward = new
            {
                xpBefore, xpAfter = user.TotalXp, xpEarned = GameRules.DailyXp,
                rankBefore = rankBefore.Name, rankAfter = rankAfter.Name,
                coinsEarned = GameRules.DailyCoins, streakBonus, coinsTotal = user.Coins,
                streak = user.CurrentStreak
            }
        });
    }
}