using Avalonia.Controls;
using LojinhaRPG.App.ViewModels;

namespace LojinhaRPG.App.Views.Controls;

public partial class ConfirmDialog : Window
{
    public ConfirmDialog()
    {
        InitializeComponent();
    }

    public static async Task<bool> AskAsync(Window owner, string title, string message)
    {
        var vm = new ConfirmDialogViewModel { Title = title, Message = message };
        var dialog = new ConfirmDialog { DataContext = vm };
        vm.RequestClose = () => dialog.Close();
        await dialog.ShowDialog(owner);
        return vm.Confirmed;
    }
}
