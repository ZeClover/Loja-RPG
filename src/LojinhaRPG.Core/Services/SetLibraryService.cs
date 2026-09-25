using LojinhaRPG.Core.Models;

namespace LojinhaRPG.Core.Services;

/// <summary>CRUD de sets de loja na biblioteca local (pasta AppData/LojinhaRPG/Sets).</summary>
public class SetLibraryService
{
    public IReadOnlyList<ShopSet> LoadAll()
    {
        var result = new List<ShopSet>();
        foreach (var dir in Directory.EnumerateDirectories(AppPaths.SetsDir))
        {
            var jsonPath = Path.Combine(dir, "set.json");
            var set = JsonStorage.Load<ShopSet>(jsonPath);
            if (set is not null) result.Add(set);
        }
        return result.OrderBy(s => s.ShopName, StringComparer.CurrentCultureIgnoreCase).ToList();
    }

    public ShopSet? Load(Guid id) => JsonStorage.Load<ShopSet>(AppPaths.SetJsonPath(id));

    public ShopSet CreateNew(string shopName = "Nova loja", string vendorName = "Vendedor")
    {
        var set = new ShopSet { ShopName = shopName, VendorName = vendorName };
        Save(set);
        return set;
    }

    public void Save(ShopSet set)
    {
        set.UpdatedAt = DateTimeOffset.UtcNow;
        AppPaths.SetDir(set.Id); // garante que a pasta existe
        JsonStorage.Save(AppPaths.SetJsonPath(set.Id), set);
    }

    public void Delete(Guid id)
    {
        var dir = AppPaths.SetDir(id);
        if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
    }

    /// <summary>Duplica um set (dados + mídia) com um novo Id.</summary>
    public ShopSet Duplicate(Guid sourceId, string? newName = null)
    {
        var source = Load(sourceId) ?? throw new InvalidOperationException("Set não encontrado.");
        var clone = System.Text.Json.JsonSerializer.Deserialize<ShopSet>(
            System.Text.Json.JsonSerializer.Serialize(source, JsonStorage.Options), JsonStorage.Options)!;
        clone.Id = Guid.NewGuid();
        clone.ShopName = newName ?? source.ShopName + " (cópia)";
        clone.CreatedAt = DateTimeOffset.UtcNow;

        var sourceMedia = AppPaths.SetMediaDir(sourceId);
        var destMedia = AppPaths.SetMediaDir(clone.Id);
        if (Directory.Exists(sourceMedia))
        {
            foreach (var file in Directory.EnumerateFiles(sourceMedia))
                File.Copy(file, Path.Combine(destMedia, Path.GetFileName(file)), overwrite: true);
        }

        Save(clone);
        return clone;
    }

    /// <summary>Copia um arquivo externo (imagem ou áudio) para a pasta de mídia do set,
    /// usando um nome canônico (ex: "vendor", "sale") + a extensão original.</summary>
    public string ImportMediaFile(Guid setId, string sourceFilePath, string canonicalName)
    {
        var ext = Path.GetExtension(sourceFilePath);
        var destFileName = canonicalName + ext;
        var destPath = Path.Combine(AppPaths.SetMediaDir(setId), destFileName);
        File.Copy(sourceFilePath, destPath, overwrite: true);
        return destFileName;
    }

    public string? ResolveMediaPath(Guid setId, string? relativeFileName)
    {
        if (string.IsNullOrWhiteSpace(relativeFileName)) return null;
        var path = Path.Combine(AppPaths.SetMediaDir(setId), relativeFileName);
        return File.Exists(path) ? path : null;
    }
}
