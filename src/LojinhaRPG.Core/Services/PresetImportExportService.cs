using System.Text.Json;
using LojinhaRPG.Core.Models;

namespace LojinhaRPG.Core.Services;

/// <summary>Exporta/importa um preset visual como arquivo .json independente, sem depender de nenhum set.</summary>
public class PresetImportExportService
{
    private readonly PresetLibraryService _library;

    public PresetImportExportService(PresetLibraryService library)
    {
        _library = library;
    }

    public void Export(Guid presetId, string destinationJsonPath)
    {
        var preset = _library.Load(presetId) ?? throw new InvalidOperationException("Preset não encontrado.");
        File.WriteAllText(destinationJsonPath, JsonSerializer.Serialize(preset, JsonStorage.Options));
    }

    public ShopPreset Import(string sourceJsonPath)
    {
        var preset = JsonSerializer.Deserialize<ShopPreset>(File.ReadAllText(sourceJsonPath), JsonStorage.Options)
                     ?? throw new InvalidOperationException("Arquivo de preset inválido.");
        preset.Id = Guid.NewGuid();
        _library.Save(preset);
        return preset;
    }
}
