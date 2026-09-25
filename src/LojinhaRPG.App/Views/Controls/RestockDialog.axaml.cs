using Avalonia.Controls;
using LojinhaRPG.App.ViewModels;

namespace LojinhaRPG.App.Views.Controls;

public partial class RestockDialog : Window
{
    public RestockDialog()
    {
        InitializeComponent();
    }

    /// <summary>Retorna a quantidade a adicionar, ou null se cancelado.</summary>
    public static async Task<int?> AskAsync(Window owner, string itemName, int currentStock)
    {
        var vm = new RestockDialogViewModel { ItemName = itemName, CurrentStock = currentStock };
        var dialog = new RestockDialog { DataContext = vm };
        vm.RequestClose = () => dialog.Close();
        await dialog.ShowDialog(owner);
        return vm.Confirmed ? vm.AmountToAdd : null;
    }
}
