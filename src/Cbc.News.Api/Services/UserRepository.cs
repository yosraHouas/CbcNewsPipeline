using Cbc.News.Api.Models;
using MongoDB.Driver;

namespace Cbc.News.Api.Services;

public class UserRepository
{
    private readonly IMongoCollection<AppUser> _col;

    public UserRepository(IMongoDatabase db)
    {
        _col = db.GetCollection<AppUser>("users");
    }

    public async Task EnsureIndexesAsync()
    {
        await _col.Indexes.CreateOneAsync(
            new CreateIndexModel<AppUser>(
                Builders<AppUser>.IndexKeys.Ascending(x => x.Username),
                new CreateIndexOptions { Unique = true, Name = "uq_username" }));
    }

    public async Task<AppUser?> GetByUsernameAsync(string username, CancellationToken ct = default)
    {
        return await _col.Find(x => x.Username == username).FirstOrDefaultAsync(ct);
    }

    public async Task SeedIfEmptyAsync(CancellationToken ct = default)
    {
        var count = await _col.CountDocumentsAsync(Builders<AppUser>.Filter.Empty, cancellationToken: ct);
        if (count > 0) return;

        var users = new[]
        {
            new AppUser
            {
                Username = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                Role = "Admin"
            },
            new AppUser
            {
                Username = "user",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("User123!"),
                Role = "User"
            }
        };

        await _col.InsertManyAsync(users, cancellationToken: ct);
    }
}