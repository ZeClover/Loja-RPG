using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace LojinhaRPG.App.ViewModels;

public partial class RestockDialogViewModel : ViewModelBase
{
    [ObservableProperty] private string _itemName = string.Empty;
    [ObservableProperty] private int _currentStock;
    [ObservableProperty] private int _amountToAdd = 1;
    [ObservableProperty] private string _errorMessage = string.Empty;

    public bool Confirmed { get; private set; }
    public Action? RequestClose { get; set; }

    [RelayCommand]
    private void Confirm()
    {
        if (AmountToAdd <= 0)
        {
            ErrorMessage = "Informe uma quantidade maior que zero.";
            return;
        }
        Confirmed = true;
        RequestClose?.Invoke();
    }

    [RelayCommand]
    private void Cancel()
    {
        Confirmed = false;
        RequestClose?.Invoke();
    }
}
