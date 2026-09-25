using LojinhaRPG.Core.Models;

namespace LojinhaRPG.Core.Services;

/// <summary>CRUD de presets visuais (tema da loja) na biblioteca local. Cada preset tem sua própria
/// pasta (como os sets), com um "preset.json" e uma subpasta "media" para a textura de moldura opcional.</summary>
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
        foreach (var dir in Directory.EnumerateDirectories(AppPaths.PresetsDir))
        {
            var jsonPath = Path.Combine(dir, "preset.json");
            var preset = JsonStorage.Load<ShopPreset>(jsonPath);
            if (preset is not null) result.Add(preset);
        }
        return result.OrderBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
    }

    public ShopPreset? Load(Guid id) => JsonStorage.Load<ShopPreset>(AppPaths.PresetJsonPath(id));

    public void Save(ShopPreset preset)
    {
        AppPaths.PresetDir(preset.Id); // garante que a pasta existe
        JsonStorage.Save(AppPaths.PresetJsonPath(preset.Id), preset);
    }

    public void Delete(Guid id)
    {
        var dir = AppPaths.PresetDir(id);
        if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
    }

    public ShopPreset Duplicate(Guid sourceId, string? newName = null)
    {
        var source = Load(sourceId) ?? throw new InvalidOperationException("Preset não encontrado.");
        var clone = source.Clone();
        clone.Id = Guid.NewGuid();
        clone.Name = newName ?? source.Name + " (cópia)";

        var sourceMedia = AppPaths.PresetMediaDir(sourceId);
        var destMedia = AppPaths.PresetMediaDir(clone.Id);
        if (Directory.Exists(sourceMedia))
        {
            foreach (var file in Directory.EnumerateFiles(sourceMedia))
                File.Copy(file, Path.Combine(destMedia, Path.GetFileName(file)), overwrite: true);
        }

        Save(clone);
        return clone;
    }

    /// <summary>Copia um arquivo de imagem externo para a pasta de mídia do preset, usando um
    /// nome canônico + a extensão original, e retorna o nome do arquivo salvo.</summary>
    public string ImportMediaFile(Guid presetId, string sourceFilePath, string canonicalName)
    {
        var ext = Path.GetExtension(sourceFilePath);
        var destFileName = canonicalName + ext;
        var destPath = Path.Combine(AppPaths.PresetMediaDir(presetId), destFileName);
        File.Copy(sourceFilePath, destPath, overwrite: true);
        return destFileName;
    }

    public string? ResolveMediaPath(Guid presetId, string? relativeFileName)
    {
        if (string.IsNullOrWhiteSpace(relativeFileName)) return null;
        var path = Path.Combine(AppPaths.PresetMediaDir(presetId), relativeFileName);
        return File.Exists(path) ? path : null;
    }
}
