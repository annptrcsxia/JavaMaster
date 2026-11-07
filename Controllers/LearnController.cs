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
    private readonly ICurrentUserService _me;
    public LearnController(AppDbContext db, ICurrentUserService me) { _db = db; _me = me; }

    public record AnswerRequest(int NodeId, JsonElement Answer);

    // Finds a node, whether this user may play it, and which node comes next.
    private async Task<(LearnNode? node, bool unlocked, LearnNode? next)> Locate(int userId, int nodeId)
    {
        var all = await _db.LearnNodes
            .OrderBy(n => n.LearnSection!.SortOrder).ThenBy(n => n.SortOrder)
            .ToListAsync();
        var i = all.FindIndex(n => n.Id == nodeId);
        if (i < 0) return (null, false, null);

        // Walk back to the nearest previous node that actually has questions.
        // Placeholder nodes with no questions do not lock the ones behind them.
        var previous = -1;
        for (var p = i - 1; p >= 0; p--)
        {
            if (await _db.NodeQuestions.AnyAsync(nq => nq.LearnNodeId == all[p].Id && nq.Question!.IsActive)) { previous = p; break; }
        }

        var unlocked = i == 0 || previous < 0 || await _db.UserNodeProgress.AnyAsync(p =>
            p.UserId == userId && p.LearnNodeId == all[previous].Id && p.Status == ProgressStatus.Completed);
        return (all[i], unlocked, i + 1 < all.Count ? all[i + 1] : null);
    }

    [HttpGet("path")]
    public async Task<IActionResult> GetPath()
    {
        var user = await _me.GetAsync();
        var done = await _db.UserNodeProgress
            .Where(p => p.UserId == user.Id && p.Status == ProgressStatus.Completed)
            .Select(p => p.LearnNodeId).ToListAsync();

        var sections = await _db.LearnSections
            .Where(s => s.IsPublished)
            .OrderBy(s => s.SortOrder)
            .Select(s => new
            {
                s.Id, s.Title, s.IsPublished,
                Nodes = s.Nodes.OrderBy(n => n.SortOrder)
                    .Select(n => new { n.Id, n.Title, n.Subtitle, n.NodeType, n.XpReward, n.CoinReward })
                    .ToList()
            })
            .ToListAsync();

        // Nodes that have no questions yet are placeholders. They must not block
        // the node after them, otherwise an unfinished placeholder would lock the
        // rest of the path for good.
        var withQuestions = await _db.NodeQuestions
            .Where(nq => nq.Question!.IsActive)
            .Select(nq => nq.LearnNodeId).Distinct().ToListAsync();

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
                if (withQuestions.Contains(n.Id)) prevDone = isDone;
            }
            output.Add(new { s.Id, s.Title, s.IsPublished, nodes });
        }
        return Ok(output);
    }

    [HttpGet("nodes/{nodeId}")]
    public async Task<IActionResult> GetNode(int nodeId)
    {
        var user = await _me.GetAsync();
        var (node, unlocked, _) = await Locate(user.Id, nodeId);
        if (node == null) return NotFound();
        if (!unlocked) return StatusCode(403, new { error = "This node is locked." });

        // AnswerKey, Explanation and Hint are never selected here.
        var questions = await _db.NodeQuestions
            .Where(nq => nq.LearnNodeId == nodeId && nq.Question!.IsActive)
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

        var user = await _me.GetAsync();
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
        var user = await _me.GetAsync();
        var (node, unlocked, next) = await Locate(user.Id, nodeId);
        if (node == null) return NotFound();
        if (!unlocked) return StatusCode(403, new { error = "This node is locked." });

        var questionIds = await _db.NodeQuestions
            .Where(nq => nq.LearnNodeId == nodeId && nq.Question!.IsActive).Select(nq => nq.QuestionId).ToListAsync();
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

        // Rewards are only ever paid once per node. A repeat play still has to answer
        // every question correctly again, but it earns no XP and no coins, so a learner
        // cannot farm the same lesson for unlimited money.
        var firstTime = progress == null || progress.Status != ProgressStatus.Completed;
        var xp = firstTime ? node.XpReward : 0;
        var coins = firstTime ? node.CoinReward : 0;

        user.TotalXp += xp;
        user.XpUpdatedAt = DateTime.UtcNow;
        user.Coins += coins;

        var streakBonus = StreakService.RecordActivity(user);

        // A badge is earned the first time a lesson is finished, and only then.
        var alreadyHadBadge = await _db.UserBadges
            .AnyAsync(x => x.UserId == user.Id && x.LearnNodeId == nodeId);
        var badgeEarned = false;
        if (firstTime && !alreadyHadBadge)
        {
            _db.UserBadges.Add(new UserBadge { UserId = user.Id, LearnNodeId = nodeId });
            badgeEarned = true;
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
            coinsEarned = coins, streakBonus, coinsTotal = user.Coins,
            badgeEarned, firstTime,
            title = node.Title,
            streak = user.CurrentStreak,
            nextNodeId = next?.Id,
            outroText = node.OutroText
        });
    }
}