using LojinhaRPG.Core.Models;

namespace LojinhaRPG.Core.Services;

/// <summary>CRUD de presets visuais (tema da loja) na biblioteca local.</summary>
public class PresetLibraryService
{
    /// <summary>Garante que os presets padrão (feira, musgo) existam na primeira execução.</summary>
    public void EnsureBuiltInPresets()
    {
        var existing = LoadAll();
        if (existing.Any(p => p.Name == "Loja de feira") && existing.Any(p => p.Name == "Loja improvisada com musgo"))
            return;

        if (!existing.Any(p => p.Name == "Loja de feira"))
            Save(ShopPreset.CreateFeiraPreset());
        if (!existing.Any(p => p.Name == "Loja improvisada com musgo"))
            Save(ShopPreset.CreateMusgoPreset());
    }

    public IReadOnlyList<ShopPreset> LoadAll()
    {
        var result = new List<ShopPreset>();
        foreach (var file in Directory.EnumerateFiles(AppPaths.PresetsDir, "*.json"))
        {
            var preset = JsonStorage.Load<ShopPreset>(file);
            if (preset is not null) result.Add(preset);
        }
        return result.OrderBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
    }

    public ShopPreset? Load(Guid id) => JsonStorage.Load<ShopPreset>(AppPaths.PresetJsonPath(id));

    public void Save(ShopPreset preset) => JsonStorage.Save(AppPaths.PresetJsonPath(preset.Id), preset);

    public void Delete(Guid id)
    {
        var path = AppPaths.PresetJsonPath(id);
        if (File.Exists(path)) File.Delete(path);
    }

    public ShopPreset Duplicate(Guid sourceId, string? newName = null)
    {
        var source = Load(sourceId) ?? throw new InvalidOperationException("Preset não encontrado.");
        var clone = source.Clone();
        clone.Id = Guid.NewGuid();
        clone.Name = newName ?? source.Name + " (cópia)";
        Save(clone);
        return clone;
    }
}
