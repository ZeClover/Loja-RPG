using System.IO.Compression;
using System.Text.Json;
using LojinhaRPG.Core.Models;

namespace LojinhaRPG.Core.Services;

/// <summary>
/// Exporta um set (dados + mídia) para um único arquivo .zip, e importa esse zip
/// em outra máquina sem perder imagens/áudios, gerando um novo Id para evitar colisão.
/// </summary>
public class SetImportExportService
{
    private readonly SetLibraryService _library;

    public SetImportExportService(SetLibraryService library)
    {
        _library = library;
    }

    public void Export(Guid setId, string destinationZipPath)
    {
        var set = _library.Load(setId) ?? throw new InvalidOperationException("Set não encontrado.");
        var setDir = AppPaths.SetDir(setId);

        if (File.Exists(destinationZipPath)) File.Delete(destinationZipPath);

        var stagingDir = Path.Combine(AppPaths.TempDir, "export-" + Guid.NewGuid());
        Directory.CreateDirectory(stagingDir);
        try
        {
            CopyDirectory(setDir, stagingDir);
            ZipFile.CreateFromDirectory(stagingDir, destinationZipPath, CompressionLevel.Optimal, includeBaseDirectory: false);
        }
        finally
        {
            Directory.Delete(stagingDir, recursive: true);
        }
    }

    public ShopSet Import(string sourceZipPath)
    {
        var stagingDir = Path.Combine(AppPaths.TempDir, "import-" + Guid.NewGuid());
        Directory.CreateDirectory(stagingDir);
        try
        {
            ZipFile.ExtractToDirectory(sourceZipPath, stagingDir);

            var jsonPath = Path.Combine(stagingDir, "set.json");
            if (!File.Exists(jsonPath))
                throw new InvalidOperationException("Arquivo .zip inválido: set.json não encontrado.");

            var set = JsonSerializer.Deserialize<ShopSet>(File.ReadAllText(jsonPath), JsonStorage.Options)
                      ?? throw new InvalidOperationException("Não foi possível ler o set.");

            // novo Id para não colidir com um set já existente nesta máquina
            set.Id = Guid.NewGuid();
            set.UpdatedAt = DateTimeOffset.UtcNow;

            var destMediaDir = AppPaths.SetMediaDir(set.Id);
            var sourceMediaDir = Path.Combine(stagingDir, "media");
            if (Directory.Exists(sourceMediaDir))
                CopyDirectory(sourceMediaDir, destMediaDir);

            _library.Save(set);
            return set;
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
