using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LojinhaRPG.Core.Models;

namespace LojinhaRPG.App.ViewModels;

/// <summary>ViewModel raiz do Painel do Mestre: biblioteca de sets + editor do set selecionado.</summary>
public partial class MainWindowViewModel : ViewModelBase
{
    public ObservableCollection<ShopSet> Library { get; } = new();

    [NotifyCanExecuteChangedFor(nameof(DuplicateSetCommand))]
    [NotifyCanExecuteChangedFor(nameof(OpenShopCommand))]
    [ObservableProperty] private ShopSet? _selectedSetSummary;

    [ObservableProperty] private SetEditorViewModel? _editor;

    [NotifyCanExecuteChangedFor(nameof(OpenShopCommand))]
    [NotifyCanExecuteChangedFor(nameof(CloseShopCommand))]
    [ObservableProperty] private bool _shopOpen;

    [ObservableProperty] private string _statusMessage = "Nenhum set carregado.";

    public event Action<ShopSet>? OpenShopRequested;
    public event Action? CloseShopRequested;

    public MainWindowViewModel()
    {
        AppServices.Presets.EnsureBuiltInPresets();
        ReloadLibrary();
    }

    public void ReloadLibrary(Guid? preferSelect = null)
    {
        var keepId = preferSelect ?? SelectedSetSummary?.Id;
        Library.Clear();
        foreach (var s in AppServices.Sets.LoadAll()) Library.Add(s);
        SelectedSetSummary = Library.FirstOrDefault(s => s.Id == keepId) ?? Library.FirstOrDefault();
        if (Library.Count == 0) StatusMessage = "Nenhuma loja criada ainda. Clique em \"Nova loja\".";
    }

    partial void OnSelectedSetSummaryChanged(ShopSet? value)
    {
        if (value is null)
        {
            Editor = null;
            return;
        }

        var fresh = AppServices.Sets.Load(value.Id) ?? value;
        Editor = new SetEditorViewModel(fresh);
        StatusMessage = $"Editando \"{fresh.ShopName}\".";
    }

    [RelayCommand]
    private void NewSet()
    {
        var set = AppServices.Sets.CreateNew();
        ReloadLibrary(set.Id);
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void DuplicateSet()
    {
        if (SelectedSetSummary is null) return;
        var clone = AppServices.Sets.Duplicate(SelectedSetSummary.Id);
        ReloadLibrary(clone.Id);
    }

    public void DeleteSelected()
    {
        if (SelectedSetSummary is null) return;
        if (ShopOpen)
        {
            StatusMessage = "Encerre a loja antes de excluir o set.";
            return;
        }
        AppServices.Sets.Delete(SelectedSetSummary.Id);
        ReloadLibrary();
    }

    private bool HasSelection() => SelectedSetSummary is not null;

    [RelayCommand(CanExecute = nameof(HasSelectionAndClosed))]
    private void OpenShop()
    {
        if (Editor is null) return;
        Editor.SaveAll();
        ShopOpen = true;
        OpenShopRequested?.Invoke(Editor.Set);
        StatusMessage = $"Loja \"{Editor.Set.ShopName}\" aberta. Selecione a janela no OBS.";
        OpenShopCommand.NotifyCanExecuteChanged();
        CloseShopCommand.NotifyCanExecuteChanged();
    }

    private bool HasSelectionAndClosed() => SelectedSetSummary is not null && !ShopOpen;

    [RelayCommand(CanExecute = nameof(ShopOpen))]
    private void CloseShop()
    {
        ShopOpen = false;
        CloseShopRequested?.Invoke();
        StatusMessage = "Loja encerrada.";
        OpenShopCommand.NotifyCanExecuteChanged();
        CloseShopCommand.NotifyCanExecuteChanged();
    }

    /// <summary>Chamado pela janela da loja quando o estoque muda, para refletir no editor aberto.</summary>
    public void NotifyStockChangedExternally()
    {
        if (Editor is null) return;
        var fresh = AppServices.Sets.Load(Editor.Set.Id);
        if (fresh is null) return;
        var reopened = new SetEditorViewModel(fresh);
        Editor = reopened;
    }
}
