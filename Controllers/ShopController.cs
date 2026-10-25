using JavaMaster.Api.Data;
using JavaMaster.Api.Entities;
using JavaMaster.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JavaMaster.Api.Controllers;

[ApiController]
[Route("api/shop")]
public class ShopController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _me;
    public ShopController(AppDbContext db, ICurrentUserService me) { _db = db; _me = me; }

    public record BuyRequest(string Code, int PackSize);

    // The shop page: coins, items, prices, and how many the player owns
    [HttpGet]
    public async Task<IActionResult> GetShop()
    {
        var user = await _me.GetAsync();
        var owned = await _db.UserInventory
            .Where(i => i.UserId == user.Id)
            .ToDictionaryAsync(i => i.ShopItemId, i => i.Quantity);
        var items = await _db.ShopItems.Where(s => s.IsActive).OrderBy(s => s.Id).ToListAsync();

        return Ok(new
        {
            coins = user.Coins,
            maxStack = GameRules.MaxItemStack,
            items = items.Select(s => new
            {
                s.Code, s.Name, s.Description, s.Price5Pack, s.Price10Pack,
                owned = owned.TryGetValue(s.Id, out var q) ? q : 0
            })
        });
    }

    // Buying a pack. The server checks everything, never the browser.
    [HttpPost("buy")]
    public async Task<IActionResult> Buy([FromBody] BuyRequest req)
    {
        if (req.PackSize != 5 && req.PackSize != 10)
            return BadRequest(new { error = "Pick a 5 pack or a 10 pack." });

        var item = await _db.ShopItems.FirstOrDefaultAsync(s => s.Code == req.Code && s.IsActive);
        if (item == null) return NotFound(new { error = "Item not found." });

        var user = await _me.GetAsync();
        var price = req.PackSize == 5 ? item.Price5Pack : item.Price10Pack;
        if (user.Coins < price)
            return BadRequest(new { error = "Not enough coins.", needed = price, coins = user.Coins });

        var inv = await _db.UserInventory.FindAsync(user.Id, item.Id);
        var current = inv?.Quantity ?? 0;
        if (current + req.PackSize > GameRules.MaxItemStack)
            return BadRequest(new { error = $"You can hold at most {GameRules.MaxItemStack} of this item." });

        if (inv == null)
        {
            inv = new UserInventory { UserId = user.Id, ShopItemId = item.Id };
            _db.UserInventory.Add(inv);
        }
        inv.Quantity = current + req.PackSize;
        user.Coins -= price;
        _db.Purchases.Add(new Purchase
        {
            UserId = user.Id, ShopItemId = item.Id,
            PackSize = req.PackSize, Quantity = req.PackSize, TotalPrice = price
        });
        await _db.SaveChangesAsync();

        return Ok(new { code = item.Code, owned = inv.Quantity, coins = user.Coins, spent = price });
    }
}