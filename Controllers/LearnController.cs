using System.Text.Json;
using JavaMaster.Api.Data;
using JavaMaster.Api.Entities;
using JavaMaster.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JavaMaster.Api.Controllers;

[ApiController]
[Route("api/learn")]
public class LearnController : ControllerBase
{
    private readonly AppDbContext _db;
    public LearnController(AppDbContext db) => _db = db;

    public record AnswerRequest(int NodeId, JsonElement Answer);

    // TEMPORARY: until Microsoft login exists, everyone is the seeded test user.
    private async Task<User> CurrentUser()
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.MicrosoftObjectId == "test-user");
        if (user == null)
        {
            user = new User
            {
                MicrosoftObjectId = "test-user",
                Email = "test.student@example.com",
                FullName = "Test Student",
                Coins = 80
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
        }
        return user;
    }

    // Finds a node, whether this user may play it, and which node comes next.
    private async Task<(LearnNode? node, bool unlocked, LearnNode? next)> Locate(int userId, int nodeId)
    {
        var all = await _db.LearnNodes
            .OrderBy(n => n.LearnSection!.SortOrder).ThenBy(n => n.SortOrder)
            .ToListAsync();
        var i = all.FindIndex(n => n.Id == nodeId);
        if (i < 0) return (null, false, null);

        var unlocked = i == 0 || await _db.UserNodeProgress.AnyAsync(p =>
            p.UserId == userId && p.LearnNodeId == all[i - 1].Id && p.Status == ProgressStatus.Completed);
        return (all[i], unlocked, i + 1 < all.Count ? all[i + 1] : null);
    }

    [HttpGet("path")]
    public async Task<IActionResult> GetPath()
    {
        var user = await CurrentUser();
        var done = await _db.UserNodeProgress
            .Where(p => p.UserId == user.Id && p.Status == ProgressStatus.Completed)
            .Select(p => p.LearnNodeId).ToListAsync();

        var sections = await _db.LearnSections
            .OrderBy(s => s.SortOrder)
            .Select(s => new
            {
                s.Id, s.Title, s.IsPublished,
                Nodes = s.Nodes.OrderBy(n => n.SortOrder)
                    .Select(n => new { n.Id, n.Title, n.Subtitle, n.NodeType, n.XpReward, n.CoinReward })
                    .ToList()
            })
            .ToListAsync();

        var prevDone = true;   // the very first node starts unlocked
        var output = new List<object>();
        foreach (var s in sections)
        {
            var nodes = new List<object>();
            foreach (var n in s.Nodes)
            {
                var isDone = done.Contains(n.Id);
                var status = isDone ? "Completed" : prevDone ? "Unlocked" : "Locked";
                nodes.Add(new { n.Id, n.Title, n.Subtitle, n.NodeType, status, n.XpReward, n.CoinReward });
                prevDone = isDone;
            }
            output.Add(new { s.Id, s.Title, s.IsPublished, nodes });
        }
        return Ok(output);
    }

    [HttpGet("nodes/{nodeId}")]
    public async Task<IActionResult> GetNode(int nodeId)
    {
        var user = await CurrentUser();
        var (node, unlocked, _) = await Locate(user.Id, nodeId);
        if (node == null) return NotFound();
        if (!unlocked) return StatusCode(403, new { error = "This node is locked." });

        // AnswerKey, Explanation and Hint are never selected here.
        var questions = await _db.NodeQuestions
            .Where(nq => nq.LearnNodeId == nodeId)
            .OrderBy(nq => nq.SortOrder)
            .Select(nq => new
            {
                nq.Question!.Id, nq.Question!.Type, nq.Question!.Prompt,
                nq.Question!.CodeSnippet, nq.Question!.Payload
            })
            .ToListAsync();

        return Ok(new
        {
            node.Id, node.Title, node.IntroText, node.OutroText,
            questions = questions.Select(q => new
            {
                q.Id, q.Type, q.Prompt, q.CodeSnippet,
                payload = JsonSerializer.Deserialize<JsonElement>(q.Payload)
            })
        });
    }

    [HttpPost("questions/{questionId}/check")]
    public async Task<IActionResult> Check(int questionId, [FromBody] AnswerRequest req)
    {
        if (req.Answer.ValueKind == JsonValueKind.Undefined)
            return BadRequest(new { error = "Missing answer." });

        var user = await CurrentUser();
        var (node, unlocked, _) = await Locate(user.Id, req.NodeId);
        if (node == null) return NotFound();
        if (!unlocked) return StatusCode(403, new { error = "This node is locked." });

        var inNode = await _db.NodeQuestions
            .AnyAsync(nq => nq.LearnNodeId == req.NodeId && nq.QuestionId == questionId);
        if (!inNode) return BadRequest(new { error = "Question is not part of this node." });

        var q = await _db.Questions.FindAsync(questionId);
        if (q == null) return NotFound();

        var correct = AnswerChecker.IsCorrect(q, req.Answer);
        _db.QuestionAttempts.Add(new QuestionAttempt
        {
            UserId = user.Id, QuestionId = q.Id, LearnNodeId = req.NodeId,
            IsCorrect = correct, SubmittedAnswer = req.Answer.GetRawText()
        });
        await _db.SaveChangesAsync();

        var key = JsonSerializer.Deserialize<JsonElement>(q.AnswerKey);
        var praise  = key.TryGetProperty("correctMessage", out var m) ? m.GetString() : null;
        var output  = key.TryGetProperty("output", out var o) ? o.GetString() : null;

        return Ok(new
        {
            correct,
            message = correct ? praise : q.Explanation,
            output  = correct ? output : null
        });
    }

    [HttpPost("nodes/{nodeId}/complete")]
    public async Task<IActionResult> Complete(int nodeId)
    {
        var user = await CurrentUser();
        var (node, unlocked, next) = await Locate(user.Id, nodeId);
        if (node == null) return NotFound();
        if (!unlocked) return StatusCode(403, new { error = "This node is locked." });

        var questionIds = await _db.NodeQuestions
            .Where(nq => nq.LearnNodeId == nodeId).Select(nq => nq.QuestionId).ToListAsync();
        if (questionIds.Count == 0)
            return BadRequest(new { error = "This node has no questions yet." });

        // Only answers given since the last completion count, so a replay must be played again.
        var progress = await _db.UserNodeProgress.FindAsync(user.Id, nodeId);
        var since = progress?.LastPlayedAt ?? DateTime.MinValue;

        var correctIds = await _db.QuestionAttempts
            .Where(a => a.UserId == user.Id && a.LearnNodeId == nodeId && a.IsCorrect && a.CreatedAt > since)
            .Select(a => a.QuestionId).Distinct().ToListAsync();
        if (questionIds.Any(id => !correctIds.Contains(id)))
            return BadRequest(new { error = "Answer every question correctly first." });

        var ranks = await _db.Ranks.ToListAsync();
        var xpBefore = user.TotalXp;
        var rankBefore = RankCalculator.For(xpBefore, ranks);

        // Repeat plays earn a quarter of the reward, so nobody can farm one lesson.
        var firstTime = progress == null || progress.Status != ProgressStatus.Completed;
        var xp = firstTime ? node.XpReward : Math.Max(1, node.XpReward / 4);
        var coins = firstTime ? node.CoinReward : Math.Max(1, node.CoinReward / 4);

        user.TotalXp += xp;
        user.XpUpdatedAt = DateTime.UtcNow;
        user.Coins += coins;

        // Streak counts calendar days in Philippine time (UTC+8).
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(8));
        if (user.LastActivityDate != today)
        {
            user.CurrentStreak = user.LastActivityDate == today.AddDays(-1) ? user.CurrentStreak + 1 : 1;
            user.LongestStreak = Math.Max(user.LongestStreak, user.CurrentStreak);
            user.LastActivityDate = today;
        }

        if (progress == null)
        {
            progress = new UserNodeProgress { UserId = user.Id, LearnNodeId = nodeId };
            _db.UserNodeProgress.Add(progress);
        }
        progress.Status = ProgressStatus.Completed;
        progress.TimesCompleted++;
        progress.LastPlayedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var rankAfter = RankCalculator.For(user.TotalXp, ranks);
        return Ok(new
        {
            xpBefore, xpAfter = user.TotalXp, xpEarned = xp,
            rankBefore = rankBefore.Name, rankAfter = rankAfter.Name,
            rankIcon = "/rank titles/" + rankAfter.IconFile,
            rankMinXp = rankAfter.RankMinXp, rankMaxXp = rankAfter.RankMaxXp,
            coinsEarned = coins, coinsTotal = user.Coins,
            streak = user.CurrentStreak,
            nextNodeId = next?.Id,
            outroText = node.OutroText
        });
    }
}