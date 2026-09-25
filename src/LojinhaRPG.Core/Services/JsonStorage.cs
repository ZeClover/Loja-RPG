using System.Text.Json;

namespace LojinhaRPG.Core.Services;

internal static class JsonStorage
{
    public static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
    };

    public static void Save<T>(string path, T value)
    {
        var json = JsonSerializer.Serialize(value, Options);
        var tmp = path + ".tmp";
        File.WriteAllText(tmp, json);
        File.Move(tmp, path, overwrite: true);
    }

    public static T? Load<T>(string path)
    {
        if (!File.Exists(path)) return default;
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<T>(json, Options);
    }
}
