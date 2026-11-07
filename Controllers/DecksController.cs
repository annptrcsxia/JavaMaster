using System.Text.Json;
using JavaMaster.Api.Data;
using JavaMaster.Api.Entities;
using JavaMaster.Api.Services;
using Microsoft.AspNetCore.Mvc;

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

    public record CreateDeckRequest(string? Name, string? Description, int Cover, int Items, string[]? Types);

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDeckRequest req)
    {
        var name = req.Name?.Trim() ?? "";
        if (name.Length == 0)
            return BadRequest(new { error = "Enter a deck name." });
        if (name.Length > GameRules.DeckNameMaxLength)
            return BadRequest(new { error = $"Deck name can be at most {GameRules.DeckNameMaxLength} characters." });

        var description = req.Description?.Trim();
        if (string.IsNullOrEmpty(description)) description = null;
        if (description != null && description.Length > GameRules.DeckDescriptionMaxLength)
            return BadRequest(new { error = $"Description can be at most {GameRules.DeckDescriptionMaxLength} characters." });

        if (req.Cover < 1 || req.Cover > GameRules.DeckCoverCount)
            return BadRequest(new { error = "Choose a cover image." });

        if (!GameRules.DeckItemOptions.Contains(req.Items))
            return BadRequest(new { error = "Select the number of items." });

        var chosen = req.Types ?? Array.Empty<string>();
        if (chosen.Length == 0 || chosen.Any(t => !GameRules.DeckQuestionTypes.Contains(t)))
            return BadRequest(new { error = "Keep at least one question type." });

        // Store in the official order, without duplicates
        var types = GameRules.DeckQuestionTypes.Where(t => chosen.Contains(t)).ToArray();

        var user = await _me.GetAsync();

        var deck = new Deck
        {
            OwnerUserId = user.Id,
            Name = name,
            Description = description,
            CoverNumber = req.Cover,
            ItemCount = req.Items,
            QuestionTypes = JsonSerializer.Serialize(types)
        };

        _db.Decks.Add(deck);
        await _db.SaveChangesAsync();

        return StatusCode(201, new { id = deck.Id });
    }
}