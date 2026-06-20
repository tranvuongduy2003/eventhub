using System.Text.Json;

namespace EventHub.DataSeeder.Helpers;

internal static class JsonLoader
{
    internal static List<T> Load<T>(string dataDirectory, string fileName, JsonSerializerOptions options)
    {
        var filePath = Path.Combine(dataDirectory, fileName);
        if (!File.Exists(filePath))
        {
            Console.Error.WriteLine($"{fileName} not found: {filePath}");
            return [];
        }

        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<List<T>>(json, options) ?? [];
    }
}
