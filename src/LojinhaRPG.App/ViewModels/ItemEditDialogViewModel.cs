using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace LojinhaRPG.App.ViewModels;

public partial class ItemEditDialogViewModel : ViewModelBase
{
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private int _price;
    [ObservableProperty] private int _stock;
    [ObservableProperty] private string _info = string.Empty;
    [ObservableProperty] private string _errorMessage = string.Empty;

    public bool Confirmed { get; private set; }
    public Action? RequestClose { get; set; }

    [RelayCommand]
    private void Confirm()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            ErrorMessage = "Informe um nome para o item.";
            return;
        }
        if (Price < 0)
        {
            ErrorMessage = "O preço não pode ser negativo.";
            return;
        }
        if (Stock < 0)
        {
            ErrorMessage = "O estoque não pode ser negativo.";
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
