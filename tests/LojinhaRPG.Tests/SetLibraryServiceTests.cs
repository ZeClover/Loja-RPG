using LojinhaRPG.Core.Models;
using LojinhaRPG.Core.Services;
using Xunit;

namespace LojinhaRPG.Tests;

/// <summary>Cada teste isola os dados numa pasta temporária própria via AppPaths.OverrideRoot.</summary>
public class SetLibraryServiceTests : IDisposable
{
    private readonly string _root;

    public SetLibraryServiceTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "LojinhaRPG.Tests." + Guid.NewGuid());
        AppPaths.OverrideRoot(_root);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public void CreateNew_ThenLoad_ReturnsSamePersistedData()
    {
        var library = new SetLibraryService();
        var created = library.CreateNew("Loja de Teste", "Vendedor Teste");
        created.Items.Add(new ShopItem { Name = "Corda", Price = 8, Stock = 10 });
        library.Save(created);

        var reloaded = library.Load(created.Id);

        Assert.NotNull(reloaded);
        Assert.Equal("Loja de Teste", reloaded!.ShopName);
        Assert.Single(reloaded.Items);
        Assert.Equal(10, reloaded.Items[0].Stock);
    }

    [Fact]
    public void StockChange_PersistsAcrossReload_SimulatingAppRestart()
    {
        var library = new SetLibraryService();
        var set = library.CreateNew("Loja", "Vendedor");
        set.Items.Add(new ShopItem { Name = "Tocha", Price = 5, Stock = 20 });
        library.Save(set);

        // simula uma compra e o fechamento/reabertura do app
        var freshLoad = library.Load(set.Id)!;
        PurchaseService.TryPurchase(freshLoad, freshLoad.Items[0].Id, 5, out _);
        library.Save(freshLoad);

        var reloadedAfterRestart = new SetLibraryService().Load(set.Id);

        Assert.Equal(15, reloadedAfterRestart!.Items[0].Stock);
    }

    [Fact]
    public void Duplicate_CreatesIndependentCopyWithNewId()
    {
        var library = new SetLibraryService();
        var original = library.CreateNew("Original", "Vendedor");
        original.Items.Add(new ShopItem { Name = "Item", Price = 1, Stock = 1 });
        library.Save(original);

        var clone = library.Duplicate(original.Id);
        clone.Items[0].Stock = 99;
        library.Save(clone);

        var reloadedOriginal = library.Load(original.Id)!;
        Assert.NotEqual(original.Id, clone.Id);
        Assert.Equal(1, reloadedOriginal.Items[0].Stock);
    }

    [Fact]
    public void LoadAll_ListsAllSavedSets()
    {
        var library = new SetLibraryService();
        library.CreateNew("Loja A", "V1");
        library.CreateNew("Loja B", "V2");

        var all = library.LoadAll();

        Assert.Equal(2, all.Count);
    }

    [Fact]
    public void ExportThenImport_RoundTripsDataAndMedia()
    {
        var library = new SetLibraryService();
        var exportService = new SetImportExportService(library);

        var set = library.CreateNew("Exportável", "Vendedor");
        set.Items.Add(new ShopItem { Name = "Mapa", Price = 18, Stock = 1 });
        library.Save(set);

        var mediaDir = AppPaths.SetMediaDir(set.Id);
        var fakeImagePath = Path.Combine(mediaDir, "vendor.png");
        File.WriteAllBytes(fakeImagePath, new byte[] { 1, 2, 3, 4 });
        set.VendorImageFile = "vendor.png";
        library.Save(set);

        var zipPath = Path.Combine(_root, "export-test.zip");
        exportService.Export(set.Id, zipPath);

        Assert.True(File.Exists(zipPath));

        var imported = exportService.Import(zipPath);

        Assert.NotEqual(set.Id, imported.Id);
        Assert.Equal("Exportável", imported.ShopName);
        Assert.Single(imported.Items);
        var importedImagePath = library.ResolveMediaPath(imported.Id, imported.VendorImageFile);
        Assert.NotNull(importedImagePath);
        Assert.Equal(new byte[] { 1, 2, 3, 4 }, File.ReadAllBytes(importedImagePath!));
    }
}
