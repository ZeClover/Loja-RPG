using System.IO.Compression;
using System.Text.Json;
using LojinhaRPG.Core.Models;

namespace LojinhaRPG.Core.Services;

/// <summary>
/// Exporta/importa um preset visual (cores + textura de moldura opcional) como um único
/// arquivo .zip, independente de qualquer set, preservando a imagem da textura se houver.
/// </summary>
public class PresetImportExportService
{
    private readonly PresetLibraryService _library;

    public PresetImportExportService(PresetLibraryService library)
    {
        _library = library;
    }

    public void Export(Guid presetId, string destinationZipPath)
    {
        var preset = _library.Load(presetId) ?? throw new InvalidOperationException("Preset não encontrado.");
        var presetDir = AppPaths.PresetDir(presetId);

        if (File.Exists(destinationZipPath)) File.Delete(destinationZipPath);

        var stagingDir = Path.Combine(AppPaths.TempDir, "preset-export-" + Guid.NewGuid());
        Directory.CreateDirectory(stagingDir);
        try
        {
            CopyDirectory(presetDir, stagingDir);
            ZipFile.CreateFromDirectory(stagingDir, destinationZipPath, CompressionLevel.Optimal, includeBaseDirectory: false);
        }
        finally
        {
            Directory.Delete(stagingDir, recursive: true);
        }
    }

    public ShopPreset Import(string sourceZipPath)
    {
        var stagingDir = Path.Combine(AppPaths.TempDir, "preset-import-" + Guid.NewGuid());
        Directory.CreateDirectory(stagingDir);
        try
        {
            ZipFile.ExtractToDirectory(sourceZipPath, stagingDir);

            var jsonPath = Path.Combine(stagingDir, "preset.json");
            if (!File.Exists(jsonPath))
                throw new InvalidOperationException("Arquivo .zip inválido: preset.json não encontrado.");

            var preset = JsonSerializer.Deserialize<ShopPreset>(File.ReadAllText(jsonPath), JsonStorage.Options)
                         ?? throw new InvalidOperationException("Não foi possível ler o preset.");

            preset.Id = Guid.NewGuid();

            var destMediaDir = AppPaths.PresetMediaDir(preset.Id);
            var sourceMediaDir = Path.Combine(stagingDir, "media");
            if (Directory.Exists(sourceMediaDir))
                CopyDirectory(sourceMediaDir, destMediaDir);

            _library.Save(preset);
            return preset;
        }
        finally
        {
            Directory.Delete(stagingDir, recursive: true);
        }
    }

    private static void CopyDirectory(string source, string dest)
    {
        Directory.CreateDirectory(dest);
        foreach (var file in Directory.EnumerateFiles(source))
            File.Copy(file, Path.Combine(dest, Path.GetFileName(file)), overwrite: true);
        foreach (var dir in Directory.EnumerateDirectories(source))
            CopyDirectory(dir, Path.Combine(dest, Path.GetFileName(dir)));
    }
}
