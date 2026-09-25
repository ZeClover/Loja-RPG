using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using LojinhaRPG.App.ViewModels;
using LojinhaRPG.App.Views.Controls;
using LojinhaRPG.Core.Models;

namespace LojinhaRPG.App.Views;

public partial class MainWindow : Window
{
    private MainWindowViewModel Vm => (MainWindowViewModel)DataContext!;
    private ShopWindow? _shopWindow;
    private bool _suppressRegionBoxEvents;
    private SetEditorViewModel? _subscribedEditor;

    public MainWindow()
    {
        InitializeComponent();

        var vm = new MainWindowViewModel();
        DataContext = vm;

        vm.OpenShopRequested += OnOpenShopRequested;
        vm.CloseShopRequested += OnCloseShopRequested;
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainWindowViewModel.Editor)) OnEditorChanged();
        };

        VisualEditor.LayoutEdited += OnVisualEditorLayoutEdited;
        VisualEditor.DragCompleted += () => Vm.Editor?.SaveLayout();

        OnEditorChanged();

        Closing += (_, _) => _shopWindow?.Close();
    }

    // ---------- Sincronização do editor visual ----------

    private void OnEditorChanged()
    {
        if (_subscribedEditor is not null) _subscribedEditor.PropertyChanged -= OnEditorPropertyChangedForPreview;

        var editor = Vm.Editor;
        _subscribedEditor = editor;
        if (editor is null) return;

        editor.PropertyChanged += OnEditorPropertyChangedForPreview;

        VisualEditor.SetRegions(editor.Layout.Vendor, editor.Layout.ItemsPanel);
        SyncRegionBoxesFromModel();
        RefreshVisualPreview();
    }

    private void OnEditorPropertyChangedForPreview(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(SetEditorViewModel.VendorImage) or nameof(SetEditorViewModel.BackgroundImage)
            or nameof(SetEditorViewModel.SelectedPreset))
        {
            RefreshVisualPreview();
        }
    }

    /// <summary>Atualiza a prévia do "resultado final" na aba Visual: fundo, vendedor sem
    /// moldura e a grade de itens já estilizada com o preset selecionado.</summary>
    private void RefreshVisualPreview()
    {
        var editor = Vm.Editor;
        if (editor is null) return;

        var preset = editor.SelectedPreset ?? editor.Presets.FirstOrDefault();
        Bitmap? frameTexture = null;
        if (preset is not null && preset.HasFrameTexture)
        {
            var path = AppServices.Presets.ResolveMediaPath(preset.Id, preset.FrameTextureFile);
            if (path is not null)
            {
                try { frameTexture = new Bitmap(path); } catch { frameTexture = null; }
            }
        }

        VisualEditor.SetPreview(editor.BackgroundImage, editor.VendorImage, preset ?? new ShopPreset(), frameTexture);
    }

    private void SyncRegionBoxesFromModel()
    {
        var layout = Vm.Editor?.Layout;
        if (layout is null) return;
        _suppressRegionBoxEvents = true;
        VendorXBox.Value = (decimal)layout.Vendor.X;
        VendorYBox.Value = (decimal)layout.Vendor.Y;
        VendorWBox.Value = (decimal)layout.Vendor.Width;
        VendorHBox.Value = (decimal)layout.Vendor.Height;
        ItemsXBox.Value = (decimal)layout.ItemsPanel.X;
        ItemsYBox.Value = (decimal)layout.ItemsPanel.Y;
        ItemsWBox.Value = (decimal)layout.ItemsPanel.Width;
        ItemsHBox.Value = (decimal)layout.ItemsPanel.Height;
        _suppressRegionBoxEvents = false;
    }

    private void OnVisualEditorLayoutEdited() => SyncRegionBoxesFromModel();

    private void OnVendorRegionBoxChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (_suppressRegionBoxEvents) return;
        var layout = Vm.Editor?.Layout;
        if (layout is null) return;
        layout.Vendor.X = (double)(VendorXBox.Value ?? 0);
        layout.Vendor.Y = (double)(VendorYBox.Value ?? 0);
        layout.Vendor.Width = (double)(VendorWBox.Value ?? 40);
        layout.Vendor.Height = (double)(VendorHBox.Value ?? 40);
        Vm.Editor?.SaveLayout();
    }

    private void OnItemsRegionBoxChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (_suppressRegionBoxEvents) return;
        var layout = Vm.Editor?.Layout;
        if (layout is null) return;
        layout.ItemsPanel.X = (double)(ItemsXBox.Value ?? 0);
        layout.ItemsPanel.Y = (double)(ItemsYBox.Value ?? 0);
        layout.ItemsPanel.Width = (double)(ItemsWBox.Value ?? 40);
        layout.ItemsPanel.Height = (double)(ItemsHBox.Value ?? 40);
        Vm.Editor?.SaveLayout();
    }

    // ---------- Sessão da loja (abrir/fechar janela OBS) ----------

    private void OnOpenShopRequested(ShopSet set)
    {
        _shopWindow?.Close();
        _shopWindow = new ShopWindow(set, onExitFullyClosed: () =>
        {
            _shopWindow = null;
            if (Vm.ShopOpen) Vm.CloseShopCommand.Execute(null);
        });
        _shopWindow.StockChangedExternally += () => Vm.NotifyStockChangedExternally();
        _shopWindow.Show();
        _shopWindow.PlayArrival();
    }

    private void OnCloseShopRequested()
    {
        _shopWindow?.BeginFarewellAndClose();
    }

    // ---------- Biblioteca ----------

    private async void OnDeleteClicked(object? sender, RoutedEventArgs e)
    {
        if (Vm.SelectedSetSummary is null) return;
        var ok = await ConfirmDialog.AskAsync(this, "Excluir loja",
            $"Tem certeza que deseja excluir \"{Vm.SelectedSetSummary.ShopName}\"? Essa ação não pode ser desfeita.");
        if (ok) Vm.DeleteSelected();
    }

    private async void OnImportClicked(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Importar set de loja (.zip)",
            AllowMultiple = false,
            FileTypeFilter = new[] { new FilePickerFileType("Set de loja (*.zip)") { Patterns = new[] { "*.zip" } } },
        });
        var file = files.FirstOrDefault();
        if (file is null) return;

        try
        {
            var imported = AppServices.SetImportExport.Import(file.Path.LocalPath);
            Vm.ReloadLibrary(imported.Id);
            Vm.StatusMessage = $"Set \"{imported.ShopName}\" importado com sucesso.";
        }
        catch (Exception ex)
        {
            Vm.StatusMessage = $"Falha ao importar: {ex.Message}";
        }
    }

    private async void OnExportClicked(object? sender, RoutedEventArgs e)
    {
        if (Vm.SelectedSetSummary is null) return;
        var suggestedName = SanitizeFileName(Vm.SelectedSetSummary.ShopName) + ".zip";
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Exportar set de loja",
            SuggestedFileName = suggestedName,
            FileTypeChoices = new[] { new FilePickerFileType("Set de loja (*.zip)") { Patterns = new[] { "*.zip" } } },
        });
        if (file is null) return;

        try
        {
            AppServices.SetImportExport.Export(Vm.SelectedSetSummary.Id, file.Path.LocalPath);
            Vm.StatusMessage = "Set exportado com sucesso.";
        }
        catch (Exception ex)
        {
            Vm.StatusMessage = $"Falha ao exportar: {ex.Message}";
        }
    }

    private static string SanitizeFileName(string name)
    {
        foreach (var c in System.IO.Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
        return name;
    }

    // ---------- Imagens ----------

    private async Task<string?> PickImageFileAsync()
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Escolher imagem",
            AllowMultiple = false,
            FileTypeFilter = new[] { FilePickerFileTypes.ImageAll },
        });
        return files.FirstOrDefault()?.Path.LocalPath;
    }

    private async void OnPickVendorImage(object? sender, RoutedEventArgs e)
    {
        var path = await PickImageFileAsync();
        if (path is not null) Vm.Editor?.ImportImage(path, ImageSlot.Vendor);
    }

    private async void OnPickReactionImage(object? sender, RoutedEventArgs e)
    {
        var path = await PickImageFileAsync();
        if (path is not null) Vm.Editor?.ImportImage(path, ImageSlot.Reaction);
    }

    private async void OnPickFarewellImage(object? sender, RoutedEventArgs e)
    {
        var path = await PickImageFileAsync();
        if (path is not null) Vm.Editor?.ImportImage(path, ImageSlot.Farewell);
    }

    private async void OnPickBackgroundImage(object? sender, RoutedEventArgs e)
    {
        var path = await PickImageFileAsync();
        if (path is not null) Vm.Editor?.ImportImage(path, ImageSlot.Background);
    }

    private void OnClearReactionImage(object? sender, RoutedEventArgs e) => Vm.Editor?.ClearImage(ImageSlot.Reaction);
    private void OnClearFarewellImage(object? sender, RoutedEventArgs e) => Vm.Editor?.ClearImage(ImageSlot.Farewell);
    private void OnClearBackgroundImage(object? sender, RoutedEventArgs e) => Vm.Editor?.ClearImage(ImageSlot.Background);

    // ---------- Falas / áudio ----------

    private async Task<string?> PickAudioFileAsync()
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Escolher áudio",
            AllowMultiple = false,
            FileTypeFilter = new[] { new FilePickerFileType("Áudio (*.mp3, *.wav, *.ogg)") { Patterns = new[] { "*.mp3", "*.wav", "*.ogg" } } },
        });
        return files.FirstOrDefault()?.Path.LocalPath;
    }

    private async void OnPickArrivalAudio(object? sender, RoutedEventArgs e)
    {
        var path = await PickAudioFileAsync();
        if (path is not null) Vm.Editor?.ImportAudio(path, VoiceSlot.Arrival);
    }

    private async void OnPickSaleAudio(object? sender, RoutedEventArgs e)
    {
        var path = await PickAudioFileAsync();
        if (path is not null) Vm.Editor?.ImportAudio(path, VoiceSlot.Sale);
    }

    private async void OnPickExitAudio(object? sender, RoutedEventArgs e)
    {
        var path = await PickAudioFileAsync();
        if (path is not null) Vm.Editor?.ImportAudio(path, VoiceSlot.Exit);
    }

    private void OnClearArrivalAudio(object? sender, RoutedEventArgs e) => Vm.Editor?.ClearAudio(VoiceSlot.Arrival);
    private void OnClearSaleAudio(object? sender, RoutedEventArgs e) => Vm.Editor?.ClearAudio(VoiceSlot.Sale);
    private void OnClearExitAudio(object? sender, RoutedEventArgs e) => Vm.Editor?.ClearAudio(VoiceSlot.Exit);

    private void OnPreviewArrivalAudio(object? sender, RoutedEventArgs e) => Vm.Editor?.PreviewAudio(VoiceSlot.Arrival);
    private void OnPreviewSaleAudio(object? sender, RoutedEventArgs e) => Vm.Editor?.PreviewAudio(VoiceSlot.Sale);
    private void OnPreviewExitAudio(object? sender, RoutedEventArgs e) => Vm.Editor?.PreviewAudio(VoiceSlot.Exit);

    // ---------- Itens ----------

    private async void OnAddItem(object? sender, RoutedEventArgs e)
    {
        if (Vm.Editor is null) return;
        var item = await ItemEditDialog.CreateAsync(this);
        if (item is not null) Vm.Editor.AddItem(item);
    }

    private async void OnEditItem(object? sender, RoutedEventArgs e)
    {
        if (Vm.Editor?.SelectedItem is null) return;
        var result = await ItemEditDialog.EditAsync(this, Vm.Editor.SelectedItem);
        if (result.Confirmed) Vm.Editor.UpdateSelectedItem(result.Name, result.Price, result.Stock, result.Info);
    }

    private async void OnRestockItem(object? sender, RoutedEventArgs e)
    {
        if (Vm.Editor?.SelectedItem is null) return;
        var amount = await RestockDialog.AskAsync(this, Vm.Editor.SelectedItem.Name, Vm.Editor.SelectedItem.Stock);
        if (amount is > 0) Vm.Editor.RestockSelectedItem(amount.Value);
    }

    private async void OnDeleteItem(object? sender, RoutedEventArgs e)
    {
        if (Vm.Editor?.SelectedItem is null) return;
        var ok = await ConfirmDialog.AskAsync(this, "Remover item", $"Remover \"{Vm.Editor.SelectedItem.Name}\" da loja?");
        if (ok) Vm.Editor.DeleteSelectedItem();
    }

    // ---------- Presets visuais ----------

    private async void OnNewPreset(object? sender, RoutedEventArgs e)
    {
        if (Vm.Editor is null) return;
        var preset = new ShopPreset { Name = "Novo preset" };
        var ok = await PresetEditDialog.EditAsync(this, preset, isNew: true);
        if (!ok) return;
        AppServices.Presets.Save(preset);
        Vm.Editor.RefreshPresetList();
        Vm.Editor.SelectedPreset = Vm.Editor.Presets.FirstOrDefault(p => p.Id == preset.Id);
    }

    private async void OnEditPreset(object? sender, RoutedEventArgs e)
    {
        if (Vm.Editor?.SelectedPreset is null) return;
        var preset = Vm.Editor.SelectedPreset;
        var ok = await PresetEditDialog.EditAsync(this, preset, isNew: false);
        if (!ok) return;
        AppServices.Presets.Save(preset);
        Vm.Editor.RefreshPresetList();
        Vm.Editor.SelectedPreset = Vm.Editor.Presets.FirstOrDefault(p => p.Id == preset.Id);
    }

    private void OnDuplicatePreset(object? sender, RoutedEventArgs e)
    {
        if (Vm.Editor?.SelectedPreset is null) return;
        var clone = AppServices.Presets.Duplicate(Vm.Editor.SelectedPreset.Id);
        Vm.Editor.RefreshPresetList();
        Vm.Editor.SelectedPreset = Vm.Editor.Presets.FirstOrDefault(p => p.Id == clone.Id);
    }

    private async void OnDeletePreset(object? sender, RoutedEventArgs e)
    {
        if (Vm.Editor?.SelectedPreset is null) return;
        var ok = await ConfirmDialog.AskAsync(this, "Excluir preset", $"Excluir o preset \"{Vm.Editor.SelectedPreset.Name}\"?");
        if (!ok) return;
        AppServices.Presets.Delete(Vm.Editor.SelectedPreset.Id);
        Vm.Editor.RefreshPresetList();
    }

    private async void OnImportPreset(object? sender, RoutedEventArgs e)
    {
        if (Vm.Editor is null) return;

        await InfoDialog.ShowAsync(this, "Importar preset visual",
            "O preset é um arquivo .zip com as cores e, se houver, a textura de moldura de item.\n\n" +
            ItemGridMetrics.DescribeRecommendedFrameTextureSize());

        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Importar preset visual (.zip)",
            AllowMultiple = false,
            FileTypeFilter = new[] { new FilePickerFileType("Preset visual (*.zip)") { Patterns = new[] { "*.zip" } } },
        });
        var file = files.FirstOrDefault();
        if (file is null) return;

        try
        {
            var imported = AppServices.PresetImportExport.Import(file.Path.LocalPath);
            Vm.Editor.RefreshPresetList();
            Vm.Editor.SelectedPreset = Vm.Editor.Presets.FirstOrDefault(p => p.Id == imported.Id);
            Vm.StatusMessage = $"Preset \"{imported.Name}\" importado.";
        }
        catch (Exception ex)
        {
            Vm.StatusMessage = $"Falha ao importar preset: {ex.Message}";
        }
    }

    private async void OnExportPreset(object? sender, RoutedEventArgs e)
    {
        if (Vm.Editor?.SelectedPreset is null) return;
        var suggestedName = SanitizeFileName(Vm.Editor.SelectedPreset.Name) + ".zip";
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Exportar preset visual",
            SuggestedFileName = suggestedName,
            FileTypeChoices = new[] { new FilePickerFileType("Preset visual (*.zip)") { Patterns = new[] { "*.zip" } } },
        });
        if (file is null) return;

        try
        {
            AppServices.PresetImportExport.Export(Vm.Editor.SelectedPreset.Id, file.Path.LocalPath);
            Vm.StatusMessage = "Preset exportado com sucesso.";
        }
        catch (Exception ex)
        {
            Vm.StatusMessage = $"Falha ao exportar preset: {ex.Message}";
        }
    }
}
