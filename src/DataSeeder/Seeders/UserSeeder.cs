using System.Text.Json;
using EventHub.DataSeeder.Models;
using EventHub.Domain.Users;
using EventHub.Infrastructure.Persistence;
using EventHub.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventHub.DataSeeder.Seeders;

internal static class UserSeeder
{
    internal static async Task SeedAsync(
        ApplicationDatabaseContext dbContext,
        PasswordHash passwordHash,
        DateTimeOffset now,
        string dataDirectory,
        JsonSerializerOptions jsonOptions)
    {
        if (await dbContext.Users.AnyAsync())
        {
            Console.WriteLine("Users already exist — skipping.");
            return;
        }

        var filePath = Path.Combine(dataDirectory, "Users.json");
        if (!File.Exists(filePath))
        {
            Console.Error.WriteLine($"Users.json not found: {filePath}");
            return;
        }

        var json = await File.ReadAllTextAsync(filePath);
        var seeds = JsonSerializer.Deserialize<List<UserSeed>>(json, jsonOptions);

        if (seeds is null or { Count: 0 })
        {
            Console.Error.WriteLine("Users.json is empty or invalid.");
            return;
        }

        var records = seeds.Select(seed => new UserRecord
        {
            Id = seed.Id,
            DisplayName = seed.DisplayName,
            Email = seed.Email.ToLowerInvariant(),
            PasswordHash = passwordHash.Value,
            Role = Enum.Parse<UserRole>(seed.Role),
            CreatedAt = now,
            UpdatedAt = now,
            RowVersion = 1,
        }).ToList();

        dbContext.Users.AddRange(records);
        await dbContext.SaveChangesAsync();

        Console.WriteLine($"Seeded {records.Count} users.");
    }
}
