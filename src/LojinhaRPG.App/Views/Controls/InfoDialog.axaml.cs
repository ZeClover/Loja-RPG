using Avalonia.Controls;
using Avalonia.Interactivity;

namespace LojinhaRPG.App.Views.Controls;

public partial class InfoDialog : Window
{
    public InfoDialog()
    {
        InitializeComponent();
    }

    private void OnOkClicked(object? sender, RoutedEventArgs e) => Close();

    public static async Task ShowAsync(Window owner, string title, string message)
    {
        var dialog = new InfoDialog { Title = title };
        dialog.MessageText.Text = message;
        await dialog.ShowDialog(owner);
    }
}
