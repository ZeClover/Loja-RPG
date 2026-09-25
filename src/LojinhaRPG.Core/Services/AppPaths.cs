namespace LojinhaRPG.Core.Services;

/// <summary>Resolve as pastas de dados do aplicativo (%AppData%\LojinhaRPG no Windows, ~/.config/LojinhaRPG no Linux/macOS).</summary>
public static class AppPaths
{
    private static string? _rootOverride;

    /// <summary>Usado pelos testes para isolar os dados em uma pasta temporária.</summary>
    public static void OverrideRoot(string path) => _rootOverride = path;

    public static string Root
    {
        get
        {
            if (_rootOverride is not null) return _rootOverride;
            var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(baseDir, "LojinhaRPG");
        }
    }

    public static string SetsDir => EnsureDir(Path.Combine(Root, "Sets"));
    public static string PresetsDir => EnsureDir(Path.Combine(Root, "Presets"));
    public static string TempDir => EnsureDir(Path.Combine(Root, "Temp"));

    public static string SetDir(Guid setId) => EnsureDir(Path.Combine(SetsDir, setId.ToString()));
    public static string SetMediaDir(Guid setId) => EnsureDir(Path.Combine(SetDir(setId), "media"));
    public static string SetJsonPath(Guid setId) => Path.Combine(SetDir(setId), "set.json");

    public static string PresetDir(Guid presetId) => EnsureDir(Path.Combine(PresetsDir, presetId.ToString()));
    public static string PresetMediaDir(Guid presetId) => EnsureDir(Path.Combine(PresetDir(presetId), "media"));
    public static string PresetJsonPath(Guid presetId) => Path.Combine(PresetDir(presetId), "preset.json");

    private static string EnsureDir(string path)
    {
        Directory.CreateDirectory(path);
        return path;
    }
}
