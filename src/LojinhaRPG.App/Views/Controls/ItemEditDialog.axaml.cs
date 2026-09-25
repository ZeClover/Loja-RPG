using Avalonia.Controls;
using LojinhaRPG.App.ViewModels;
using LojinhaRPG.Core.Models;

namespace LojinhaRPG.App.Views.Controls;

public partial class ItemEditDialog : Window
{
    public ItemEditDialog()
    {
        InitializeComponent();
    }

    /// <summary>Abre o diálogo para criar um item novo. Retorna o item criado ou null se cancelado.</summary>
    public static async Task<ShopItem?> CreateAsync(Window owner)
    {
        var vm = new ItemEditDialogViewModel { Name = "Novo item", Price = 10, Stock = 1 };
        var dialog = new ItemEditDialog { DataContext = vm, Title = "Novo item" };
        vm.RequestClose = () => dialog.Close();
        await dialog.ShowDialog(owner);
        if (!vm.Confirmed) return null;
        return new ShopItem { Name = vm.Name.Trim(), Price = vm.Price, Stock = vm.Stock, Info = vm.Info };
    }

    /// <summary>Abre o diálogo para editar um item existente. Não altera o item diretamente:
    /// devolve os novos valores para o chamador decidir como aplicá-los e persistir.</summary>
    public static async Task<(bool Confirmed, string Name, int Price, int Stock, string Info)> EditAsync(Window owner, ShopItem item)
    {
        var vm = new ItemEditDialogViewModel { Name = item.Name, Price = item.Price, Stock = item.Stock, Info = item.Info };
        var dialog = new ItemEditDialog { DataContext = vm, Title = "Editar item" };
        vm.RequestClose = () => dialog.Close();
        await dialog.ShowDialog(owner);
        return (vm.Confirmed, vm.Name.Trim(), vm.Price, vm.Stock, vm.Info);
    }
}
