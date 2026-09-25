using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace LojinhaRPG.App.ViewModels;

public partial class ConfirmDialogViewModel : ViewModelBase
{
    [ObservableProperty] private string _message = string.Empty;
    [ObservableProperty] private string _title = "Confirmar";

    public bool Confirmed { get; private set; }
    public Action? RequestClose { get; set; }

    [RelayCommand]
    private void Yes()
    {
        Confirmed = true;
        RequestClose?.Invoke();
    }

    [RelayCommand]
    private void No()
    {
        Confirmed = false;
        RequestClose?.Invoke();
    }
}
