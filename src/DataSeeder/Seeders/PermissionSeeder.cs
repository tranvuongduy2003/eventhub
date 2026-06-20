using System.Text.Json;
using EventHub.DataSeeder.Helpers;
using EventHub.DataSeeder.Models;

namespace EventHub.DataSeeder.Seeders;

internal static class PermissionSeeder
{
    internal static Task SeedAsync(string dataDirectory, JsonSerializerOptions jsonOptions)
    {
        var seeds = JsonLoader.Load<PermissionSeed>(dataDirectory, "Permissions.json", jsonOptions);
        if (seeds.Count == 0)
        {
            Console.Error.WriteLine("No permissions loaded — skipping.");
            return Task.CompletedTask;
        }

        Console.WriteLine($"Permissions ({seeds.Count}):");
        foreach (var seed in seeds)
        {
            Console.WriteLine($"  {seed.Name} — {seed.Description}");
        }

        return Task.CompletedTask;
    }
}
