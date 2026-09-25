using Avalonia.Controls;
using LojinhaRPG.App.ViewModels;
using LojinhaRPG.Core.Models;

namespace LojinhaRPG.App.Views.Controls;

public partial class PresetEditDialog : Window
{
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
}
