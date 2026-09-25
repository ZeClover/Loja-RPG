using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using LojinhaRPG.App.ViewModels;
using LojinhaRPG.Core.Models;

namespace LojinhaRPG.App.Views.Controls;

public partial class PresetEditDialog : Window
{
    private PresetEditDialogViewModel Vm => (PresetEditDialogViewModel)DataContext!;

    public PresetEditDialog()
    {
        InitializeComponent();
    }

    /// <summary>Edita o preset informado (in-place). Retorna true se as alterações foram confirmadas.</summary>
    public static async Task<bool> EditAsync(Window owner, ShopPreset preset, bool isNew)
    {
        var vm = new PresetEditDialogViewModel(preset, isNew);
        var dialog = new PresetEditDialog { DataContext = vm, Title = isNew ? "Novo preset" : "Editar preset" };
        vm.RequestClose = () => dialog.Close();
        await dialog.ShowDialog(owner);
        return vm.Confirmed;
    }

    private async void OnPickFrameTexture(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Escolher textura de moldura",
            AllowMultiple = false,
            FileTypeFilter = new[] { FilePickerFileTypes.ImageAll },
        });
        var file = files.FirstOrDefault();
        if (file is not null) Vm.ImportFrameTexture(file.Path.LocalPath);
    }

    private void OnClearFrameTexture(object? sender, RoutedEventArgs e) => Vm.ClearFrameTexture();
}
