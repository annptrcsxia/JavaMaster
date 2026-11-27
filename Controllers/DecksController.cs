using System.Security.Cryptography;
using System.Text.Json;
using JavaMaster.Api.Data;
using JavaMaster.Api.Entities;
using JavaMaster.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JavaMaster.Api.Controllers;

[ApiController]
[Route("api/decks")]
public class DecksController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _me;

    public DecksController(AppDbContext db, ICurrentUserService me)
    {
        _db = db;
        _me = me;
    }

    public record DeckRequest(string? Name, string? Share, int Cover, JsonElement[]? Cards);
    public record ShareRequest(string? Share);

    private sealed class ValidationError : Exception
    {
        public ValidationError(string message) : base(message) { }
    }

    // ---------- Endpoints ----------

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var user = await _me.GetAsync();

        var rows = await _db.Decks
            .Where(d => d.OwnerUserId == user.Id)
            .OrderByDescending(d => d.UpdatedAt)
            .Select(d => new
            {
                d.Id, d.Name, d.Share, d.CoverNumber, d.ShareCode, d.CreatedAt, d.UpdatedAt,
                CardTypes = d.Cards.Select(c => c.Type).ToList()
            })
            .ToListAsync();

        return Ok(rows.Select(r => new
        {
            id = r.Id,
            name = r.Name,
            share = r.Share,
            cover = r.CoverNumber,
            shareCode = r.ShareCode,
            items = r.CardTypes.Count,
            types = OrderTypes(r.CardTypes),
            createdAt = Utc(r.CreatedAt),
            updatedAt = Utc(r.UpdatedAt)
        }));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var user = await _me.GetAsync();
        var deck = await _db.Decks.Include(d => d.Cards)
            .FirstOrDefaultAsync(d => d.Id == id && d.OwnerUserId == user.Id);
        if (deck == null) return NotFound(new { error = "Deck not found." });

        return Ok(ToDetail(deck, deck.Cards));
    }

// Play a Shared Deck: finds a Public deck by its code. Any user can look up a Public deck.
    [HttpGet("code/{code}")]
    public async Task<IActionResult> FindByCode(string code)
    {
        var clean = (code ?? "").Trim().ToUpperInvariant();
        var found = await _db.Decks
            .Where(d => d.ShareCode == clean && d.Share == "public")
            .Select(d => new { d.Id })
            .FirstOrDefaultAsync();
        if (found == null) return NotFound(new { error = "No public deck matches that code." });

        return Ok(new { id = found.Id });
    }

    // Publish
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] DeckRequest req)
    {
        var user = await _me.GetAsync();

        (string name, string share, List<DeckCard> cards) v;
        try { v = Validate(req); }
        catch (ValidationError ex) { return BadRequest(new { error = ex.Message }); }

        var now = DateTime.UtcNow;
        var deck = new Deck
        {
            OwnerUserId = user.Id,
            Name = v.name,
            Share = v.share,
            CoverNumber = req.Cover,
            ShareCode = await NewShareCodeAsync(),
            CreatedAt = now,
            UpdatedAt = now,
            Cards = v.cards
        };

        _db.Decks.Add(deck);
        await _db.SaveChangesAsync();

        return StatusCode(201, ToDetail(deck, v.cards));
    }

    // Save: replaces all cards, moves UpdatedAt
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] DeckRequest req)
    {
        var user = await _me.GetAsync();
        var deck = await _db.Decks.Include(d => d.Cards)
            .FirstOrDefaultAsync(d => d.Id == id && d.OwnerUserId == user.Id);
        if (deck == null) return NotFound(new { error = "Deck not found." });

        (string name, string share, List<DeckCard> cards) v;
        try { v = Validate(req); }
        catch (ValidationError ex) { return BadRequest(new { error = ex.Message }); }

        await using var tx = await _db.Database.BeginTransactionAsync();

        // Delete old cards first, so the unique (DeckId, Position) index never clashes.
        _db.DeckCards.RemoveRange(deck.Cards);
        await _db.SaveChangesAsync();

        foreach (var c in v.cards)
        {
            c.DeckId = deck.Id;
            _db.DeckCards.Add(c);
        }
        deck.Name = v.name;
        deck.Share = v.share;
        deck.CoverNumber = req.Cover;
        deck.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await tx.CommitAsync();

        return Ok(ToDetail(deck, v.cards));
    }

    // Public / Private: does NOT move UpdatedAt
    [HttpPatch("{id:int}/share")]
    public async Task<IActionResult> SetShare(int id, [FromBody] ShareRequest req)
    {
        var share = req.Share ?? "";
        if (!GameRules.DeckShareValues.Contains(share))
            return BadRequest(new { error = "Share must be public or private." });

        var user = await _me.GetAsync();
        var deck = await _db.Decks.FirstOrDefaultAsync(d => d.Id == id && d.OwnerUserId == user.Id);
        if (deck == null) return NotFound(new { error = "Deck not found." });

        deck.Share = share;
        await _db.SaveChangesAsync();

        return Ok(new { id = deck.Id, share = deck.Share, updatedAt = Utc(deck.UpdatedAt) });
    }

    // ---------- Output ----------

    private static DateTime Utc(DateTime d) => DateTime.SpecifyKind(d, DateTimeKind.Utc);

    private static string[] OrderTypes(IEnumerable<string> types)
    {
        var set = types.ToHashSet();
        return GameRules.DeckQuestionTypes.Where(set.Contains).ToArray();
    }

    private static object ToDetail(Deck d, IEnumerable<DeckCard> cards)
    {
        var list = cards.OrderBy(c => c.Position).ToList();
        return new
        {
            id = d.Id,
            name = d.Name,
            share = d.Share,
            cover = d.CoverNumber,
            shareCode = d.ShareCode,
            items = list.Count,
            types = OrderTypes(list.Select(c => c.Type)),
            cards = list.Select(ToCard).ToList(),
            createdAt = Utc(d.CreatedAt),
            updatedAt = Utc(d.UpdatedAt)
        };
    }

    private static Dictionary<string, object> ToCard(DeckCard c)
    {
        var card = new Dictionary<string, object>
        {
            ["type"] = c.Type,
            ["question"] = c.Question
        };
        var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(c.Data) ?? new();
        foreach (var kv in data) card[kv.Key] = kv.Value;
        return card;
    }

    // ---------- Share code ----------

    private const string CodeChars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";   // no 0, O, 1, I

    private async Task<string> NewShareCodeAsync()
    {
        while (true)
        {
            var code = new string(Enumerable.Range(0, GameRules.DeckShareCodeLength)
                .Select(_ => CodeChars[RandomNumberGenerator.GetInt32(CodeChars.Length)])
                .ToArray());
            if (!await _db.Decks.AnyAsync(d => d.ShareCode == code)) return code;
        }
    }

    // ---------- Validation ----------

    private static (string name, string share, List<DeckCard> cards) Validate(DeckRequest req)
    {
        var name = req.Name?.Trim() ?? "";
        if (name.Length == 0)
            throw new ValidationError("Enter a deck name.");
        if (name.Length > GameRules.DeckNameMaxLength)
            throw new ValidationError($"Deck name can be at most {GameRules.DeckNameMaxLength} characters.");

        if (req.Cover < 1 || req.Cover > GameRules.DeckCoverCount)
            throw new ValidationError("Choose a cover image.");

        var share = req.Share ?? "";
        if (!GameRules.DeckShareValues.Contains(share))
            throw new ValidationError("Share must be public or private.");

        var raw = req.Cards ?? Array.Empty<JsonElement>();
        if (raw.Length < GameRules.DeckMinCards)
            throw new ValidationError("Add at least 1 card.");
        if (raw.Length > GameRules.DeckMaxCards)
            throw new ValidationError($"A deck can have at most {GameRules.DeckMaxCards} cards.");

        var cards = new List<DeckCard>();
        for (var i = 0; i < raw.Length; i++)
            cards.Add(BuildCard(raw[i], i));

        return (name, share, cards);
    }

    private static DeckCard BuildCard(JsonElement el, int index)
    {
        var label = $"Card {index + 1}: ";
        if (el.ValueKind != JsonValueKind.Object)
            throw new ValidationError(label + "invalid card.");

        var type = Str(el, "type");
        if (!GameRules.DeckQuestionTypes.Contains(type))
            throw new ValidationError(label + "unknown question type.");

        var question = Str(el, "question").Trim();
        if (question.Length == 0)
            throw new ValidationError(label + "enter a question.");
        if (question.Length > GameRules.DeckQuestionMaxLength)
            throw new ValidationError(label + $"question can be at most {GameRules.DeckQuestionMaxLength} characters.");

        var data = new Dictionary<string, object>();
        switch (type)
        {
            case "multiple-choice": ReadChoice(el, label, data); break;
            case "true-false":      ReadTrueFalse(el, label, data); break;
            case "matching":        ReadMatching(el, label, data); break;
            case "fill-blank-code": ReadCode(el, label, false, data); break;
            case "drag-drop-code":  ReadCode(el, label, true, data); break;
        }

        return new DeckCard
        {
            Position = index,
            Type = type,
            Question = question,
            Data = JsonSerializer.Serialize(data)
        };
    }

    private static void ReadChoice(JsonElement el, string label, Dictionary<string, object> data)
    {
        var options = Arr(el, "options").Select(Trimmed).ToList();
        if (options.Count != 4 || options.Any(o => o.Length == 0))
            throw new ValidationError(label + "enter all 4 options.");
        if (options.Any(o => o.Length > GameRules.DeckOptionMaxLength))
            throw new ValidationError(label + $"each option can be at most {GameRules.DeckOptionMaxLength} characters.");
        if (options.Distinct(StringComparer.OrdinalIgnoreCase).Count() != 4)
            throw new ValidationError(label + "options must all be different.");

        if (!el.TryGetProperty("correct", out var c) || c.ValueKind != JsonValueKind.Number
            || !c.TryGetInt32(out var correct) || correct < 0 || correct > 3)
            throw new ValidationError(label + "choose the correct option.");

        data["options"] = options;
        data["correct"] = correct;
    }

    private static void ReadTrueFalse(JsonElement el, string label, Dictionary<string, object> data)
    {
        if (!el.TryGetProperty("answer", out var a)
            || (a.ValueKind != JsonValueKind.True && a.ValueKind != JsonValueKind.False))
            throw new ValidationError(label + "choose True or False.");

        data["answer"] = a.GetBoolean();
    }

    private static void ReadMatching(JsonElement el, string label, Dictionary<string, object> data)
    {
        var raw = Arr(el, "pairs");
        if (raw.Length < GameRules.DeckMinPairs || raw.Length > GameRules.DeckMaxPairs)
            throw new ValidationError(label + $"use {GameRules.DeckMinPairs} to {GameRules.DeckMaxPairs} pairs.");

        var pairs = new List<Dictionary<string, string>>();
        foreach (var p in raw)
        {
            var item = Str(p, "item").Trim();
            var answer = Str(p, "answer").Trim();
            if (item.Length == 0 || answer.Length == 0)
                throw new ValidationError(label + "fill in every pair.");
            if (item.Length > GameRules.DeckMatchTextMaxLength || answer.Length > GameRules.DeckMatchTextMaxLength)
                throw new ValidationError(label + $"pair text can be at most {GameRules.DeckMatchTextMaxLength} characters.");
            pairs.Add(new Dictionary<string, string> { ["item"] = item, ["answer"] = answer });
        }

        if (pairs.Select(p => p["item"]).Distinct(StringComparer.OrdinalIgnoreCase).Count() != pairs.Count)
            throw new ValidationError(label + "items must all be different.");
        if (pairs.Select(p => p["answer"]).Distinct(StringComparer.OrdinalIgnoreCase).Count() != pairs.Count)
            throw new ValidationError(label + "answers must all be different.");

        data["pairs"] = pairs;
    }

    private static void ReadCode(JsonElement el, string label, bool withDecoys, Dictionary<string, object> data)
    {
        var answers = Arr(el, "answers").Select(Trimmed).ToList();
        if (answers.Count < GameRules.DeckMinBlanks || answers.Count > GameRules.DeckMaxBlanks)
            throw new ValidationError(label + $"use {GameRules.DeckMinBlanks} to {GameRules.DeckMaxBlanks} blanks.");
        if (answers.Any(a => a.Length == 0))
            throw new ValidationError(label + "fill in every blank answer.");
        if (answers.Any(a => a.Length > GameRules.DeckAnswerMaxLength))
            throw new ValidationError(label + $"each answer can be at most {GameRules.DeckAnswerMaxLength} characters.");

        var parts = new List<Dictionary<string, object>>();
        var usedBlanks = new List<int>();
        var codeLength = 0;
        foreach (var p in Arr(el, "parts"))
        {
            if (p.ValueKind != JsonValueKind.Object)
                throw new ValidationError(label + "invalid code.");

            if (p.TryGetProperty("blank", out var b))
            {
                if (b.ValueKind != JsonValueKind.Number || !b.TryGetInt32(out var idx))
                    throw new ValidationError(label + "invalid blank.");
                usedBlanks.Add(idx);
                parts.Add(new Dictionary<string, object> { ["blank"] = idx });
            }
            else
            {
                var text = Str(p, "text");
                codeLength += text.Length;
                parts.Add(new Dictionary<string, object> { ["text"] = text });
            }
        }

        if (usedBlanks.Count != answers.Count
            || !usedBlanks.OrderBy(x => x).SequenceEqual(Enumerable.Range(0, answers.Count)))
            throw new ValidationError(label + "each blank needs exactly one answer.");
        if (codeLength > GameRules.DeckCodeMaxLength)
            throw new ValidationError(label + $"code can be at most {GameRules.DeckCodeMaxLength} characters.");

        data["parts"] = parts;
        data["answers"] = answers;

        if (!withDecoys) return;

        // Empty decoys are dropped
        var decoys = Arr(el, "decoys").Select(Trimmed).Where(d => d.Length > 0).ToList();
        if (decoys.Count > GameRules.DeckMaxDecoys)
            throw new ValidationError(label + $"use at most {GameRules.DeckMaxDecoys} decoys.");
        if (decoys.Any(d => d.Length > GameRules.DeckAnswerMaxLength))
            throw new ValidationError(label + $"each decoy can be at most {GameRules.DeckAnswerMaxLength} characters.");
        if (decoys.Distinct(StringComparer.Ordinal).Count() != decoys.Count)
            throw new ValidationError(label + "decoys must all be different.");
        if (decoys.Any(d => answers.Contains(d, StringComparer.Ordinal)))
            throw new ValidationError(label + "a decoy cannot equal an answer.");

        data["decoys"] = decoys;
    }

    // ---------- JSON helpers ----------

    private static string Str(JsonElement el, string prop) =>
        el.ValueKind == JsonValueKind.Object
        && el.TryGetProperty(prop, out var v)
        && v.ValueKind == JsonValueKind.String
            ? v.GetString() ?? ""
            : "";

    private static JsonElement[] Arr(JsonElement el, string prop) =>
        el.ValueKind == JsonValueKind.Object
        && el.TryGetProperty(prop, out var v)
        && v.ValueKind == JsonValueKind.Array
            ? v.EnumerateArray().ToArray()
            : Array.Empty<JsonElement>();

    private static string Trimmed(JsonElement o) =>
        o.ValueKind == JsonValueKind.String ? (o.GetString() ?? "").Trim() : "";
}