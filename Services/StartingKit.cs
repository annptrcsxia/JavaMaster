using JavaMaster.Api.Data;
using JavaMaster.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace JavaMaster.Api.Services;

public static class StartingKit
{
    // Gives a new player their starting items.
    public static async Task GiveAsync(AppDbContext db, User user)
    {
        var items = await db.ShopItems.ToListAsync();
        foreach (var (code, qty) in GameRules.StartingItems)
        {
            var item = items.FirstOrDefault(i => i.Code == code);
            if (item != null)
                db.UserInventory.Add(new UserInventory
                {
                    UserId = user.Id, ShopItemId = item.Id, Quantity = qty
                });
        }
        await db.SaveChangesAsync();
    }
}