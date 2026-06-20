using System.Text.Json;
using EventHub.DataSeeder.Helpers;
using EventHub.DataSeeder.Models;

namespace EventHub.DataSeeder.Seeders;

internal static class RoleSeeder
{
    internal static Task SeedAsync(string dataDirectory, JsonSerializerOptions jsonOptions)
    {
        var seeds = JsonLoader.Load<RoleSeed>(dataDirectory, "Roles.json", jsonOptions);
        if (seeds.Count == 0)
        {
            Console.Error.WriteLine("No roles loaded — skipping.");
            return Task.CompletedTask;
        }

        Console.WriteLine($"Roles ({seeds.Count}):");
        foreach (var seed in seeds)
        {
            var permissions = string.Join(", ", seed.Permissions);
            Console.WriteLine($"  {seed.Name} — {seed.Description} [{permissions}]");
        }

        return Task.CompletedTask;
    }
}
