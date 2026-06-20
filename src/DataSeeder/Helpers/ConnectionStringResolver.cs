using System.Text.Json;

namespace EventHub.DataSeeder.Helpers;

internal static class ConnectionStringResolver
{
    internal static string? Resolve()
    {
        return Environment.GetEnvironmentVariable("ConnectionStrings__app")
               ?? ReadFromAppSettings()
               ?? Environment.GetEnvironmentVariable("ConnectionStrings__App");
    }

    private static string? ReadFromAppSettings()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (!File.Exists(path))
        {
            return null;
        }

        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        return doc.RootElement
            .GetProperty("ConnectionStrings")
            .GetProperty("App")
            .GetString();
    }
}
