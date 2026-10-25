using JavaMaster.Api.Data;
using JavaMaster.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace JavaMaster.Api.Services;

public interface ICurrentUserService
{
    Task<User> GetAsync();
}

// TEMPORARY: everyone is the test user until Google login is built.
// When login exists, this is the only file that changes.
public class CurrentUserService : ICurrentUserService
{
    private readonly AppDbContext _db;
    public CurrentUserService(AppDbContext db) => _db = db;

    public async Task<User> GetAsync()
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.MicrosoftObjectId == "test-user");
        if (user != null) return user;

        user = new User
        {
            MicrosoftObjectId = "test-user",
            Email = "test.student@example.com",
            FullName = "Test Student",
            Coins = GameRules.StartingCoins
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        await StartingKit.GiveAsync(_db, user);
        return user;
    }
}