using System.Text.Json;
using EventHub.DataSeeder.Helpers;
using EventHub.DataSeeder.Models;
using EventHub.Domain.Events;
using EventHub.Infrastructure.Persistence;
using EventHub.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventHub.DataSeeder.Seeders;

internal static class EventUserRoleSeeder
{
    internal static async Task SeedAsync(
        ApplicationDatabaseContext dbContext,
        DateTimeOffset now,
        string dataDirectory,
        JsonSerializerOptions jsonOptions)
    {
        if (await dbContext.EventUserRoles.AnyAsync())
        {
            Console.WriteLine("EventUserRoles already exist — skipping.");
            return;
        }

        var seeds = JsonLoader.Load<EventUserRoleSeed>(dataDirectory, "EventUserRoles.json", jsonOptions);
        if (seeds.Count == 0)
        {
            Console.Error.WriteLine("No event user roles loaded — skipping.");
            return;
        }

        var records = seeds.Select(seed => new EventUserRoleRecord
        {
            EventId = seed.EventId,
            UserId = seed.UserId,
            Role = Enum.Parse<EventRole>(seed.Role),
            CreatedAt = now,
        }).ToList();

        dbContext.EventUserRoles.AddRange(records);
        await dbContext.SaveChangesAsync();

        var ownerCount = records.Count(r => r.Role == EventRole.Owner);
        var staffCount = records.Count(r => r.Role == EventRole.Staff);
        Console.WriteLine($"Seeded {records.Count} event user roles ({ownerCount} Owner, {staffCount} Staff).");
    }
}
