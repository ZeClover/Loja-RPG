using System.Collections.ObjectModel;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LojinhaRPG.Core.Models;
using LojinhaRPG.Core.Services;

namespace LojinhaRPG.App.ViewModels;

/// <summary>ViewModel da janela da loja (visual para o OBS): grade de itens, compra e falas.</summary>
public partial class ShopWindowViewModel : ViewModelBase
{
    private const int ItemsPerPage = 9;

    private readonly SetLibraryService _library = AppServices.Sets;
    private readonly IAudioPlayer _audio = AppServices.Audio;

    private DispatcherTimer? _indicatorTimer;
    private DispatcherTimer? _reactionTimer;
    private DispatcherTimer? _captionTimer;
    private DispatcherTimer? _exitTimer;

    public ShopSet Set { get; }
    public string ShopName => Set.ShopName;
    public string VendorName => Set.VendorName;

    [ObservableProperty] private Bitmap? _vendorDisplayImage;
    [ObservableProperty] private Bitmap? _backgroundImage;
    [ObservableProperty] private ShopPreset _preset;

    /// <summary>Fundo de cada moldura de item: a textura do preset, se houver, senão a cor lisa configurada.</summary>
    [ObservableProperty] private IBrush _itemCardBackground = Brushes.SaddleBrown;
    [ObservableProperty] private IBrush _itemCardBorderBrush = Brushes.Black;
    [ObservableProperty] private double _itemCardBorderThickness = 2;
    [ObservableProperty] private double _itemCardCornerRadius = 8;
    [ObservableProperty] private IBrush _itemCardTextColor = Brushes.White;

    public VisualLayout Layout => Set.Layout;

    public ObservableCollection<ShopItem> CurrentPageItems { get; } = new();
    [ObservableProperty] private int _currentPage;
    [ObservableProperty] private int _totalPages = 1;
    [ObservableProperty] private string _pageLabel = "Página 1/1";

    [ObservableProperty] private bool _detailOverlayVisible;
    [ObservableProperty] private ShopItem? _selectedItem;
    [ObservableProperty] private int _purchaseQuantity = 1;
    [ObservableProperty] private string _purchaseError = string.Empty;
    [ObservableProperty] private int _purchaseTotal;

    [ObservableProperty] private bool _indicatorVisible;
    [ObservableProperty] private string _indicatorText = string.Empty;

    [ObservableProperty] private bool _captionVisible;
    [ObservableProperty] private string _captionText = string.Empty;

    /// <summary>Disparado depois que a despedida (fala/áudio) terminou de tocar; a View deve fechar/ocultar a janela.</summary>
    public event Action? ExitSequenceCompleted;

    /// <summary>Disparado sempre que o estoque muda, para o Painel do Mestre atualizar sua cópia.</summary>
    public event Action? StockChanged;

    public ShopWindowViewModel(ShopSet set)
    {
        Set = set;
        _preset = ResolvePreset(set);
        RefreshVendorAndBackground();
        RefreshPage();
        ApplyItemCardStyle();
    }

    private static ShopPreset ResolvePreset(ShopSet set)
    {
        var fromSet = set.PresetId != Guid.Empty ? AppServices.Presets.Load(set.PresetId) : null;
        return fromSet ?? AppServices.Presets.LoadAll().FirstOrDefault() ?? ShopPreset.CreateFeiraPreset();
    }

    partial void OnPresetChanged(ShopPreset value) => ApplyItemCardStyle();

    /// <summary>Calcula o visual de cada quadro de item a partir do preset: usa a textura de
    /// moldura como imagem de fundo se o preset tiver uma, senão usa a cor lisa configurada.</summary>
    private void ApplyItemCardStyle()
    {
        var preset = Preset;
        var texturePath = AppServices.Presets.ResolveMediaPath(preset.Id, preset.FrameTextureFile);

        if (texturePath is not null)
        {
            try
            {
                ItemCardBackground = new ImageBrush(new Bitmap(texturePath)) { Stretch = Stretch.UniformToFill };
            }
            catch
            {
                ItemCardBackground = new SolidColorBrush(ParseColor(preset.ItemsPanelBackground));
            }
        }
        else
        {
            ItemCardBackground = new SolidColorBrush(ParseColor(preset.ItemsPanelBackground));
        }

        ItemCardBorderBrush = new SolidColorBrush(ParseColor(preset.BorderColor));
        ItemCardBorderThickness = Math.Max(2, preset.BorderThickness / 2);
        ItemCardCornerRadius = Math.Max(0, preset.CornerRadius - 2);
        ItemCardTextColor = new SolidColorBrush(ParseColor(preset.TextColor));
    }

    private static Color ParseColor(string hex)
    {
        try { return Color.Parse(hex); } catch { return Colors.Gray; }
    }

    private Bitmap? LoadBitmap(string relativeFile)
    {
        var path = _library.ResolveMediaPath(Set.Id, relativeFile);
        if (path is null) return null;
        try { return new Bitmap(path); } catch { return null; }
    }

    private void RefreshVendorAndBackground()
    {
        VendorDisplayImage = LoadBitmap(Set.VendorImageFile);
        BackgroundImage = LoadBitmap(Set.BackgroundImageFile);
    }

    public void RefreshPage()
    {
        TotalPages = Math.Max(1, (int)Math.Ceiling(Set.Items.Count / (double)ItemsPerPage));
        if (CurrentPage >= TotalPages) CurrentPage = TotalPages - 1;
        if (CurrentPage < 0) CurrentPage = 0;

        CurrentPageItems.Clear();
        foreach (var item in Set.Items.Skip(CurrentPage * ItemsPerPage).Take(ItemsPerPage))
            CurrentPageItems.Add(item);

        PageLabel = $"Página {CurrentPage + 1}/{TotalPages}";
        NextPageCommand.NotifyCanExecuteChanged();
        PrevPageCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanGoNext))]
    private void NextPage()
    {
        if (CurrentPage < TotalPages - 1) { CurrentPage++; RefreshPage(); }
    }
    private bool CanGoNext() => CurrentPage < TotalPages - 1;

    [RelayCommand(CanExecute = nameof(CanGoPrev))]
    private void PrevPage()
    {
        if (CurrentPage > 0) { CurrentPage--; RefreshPage(); }
    }
    private bool CanGoPrev() => CurrentPage > 0;

    public void SelectItem(ShopItem item)
    {
        SelectedItem = item;
        PurchaseQuantity = item.Stock > 0 ? 1 : 0;
        PurchaseError = string.Empty;
        RecomputeTotal();
        DetailOverlayVisible = true;
    }

    [RelayCommand]
    private void CloseDetail()
    {
        DetailOverlayVisible = false;
        SelectedItem = null;
    }

    [RelayCommand]
    private void IncreaseQty()
    {
        if (SelectedItem is not null && PurchaseQuantity < SelectedItem.Stock) PurchaseQuantity++;
        RecomputeTotal();
    }

    [RelayCommand]
    private void DecreaseQty()
    {
        if (PurchaseQuantity > 1) PurchaseQuantity--;
        RecomputeTotal();
    }

    partial void OnPurchaseQuantityChanged(int value) => RecomputeTotal();

    private void RecomputeTotal() => PurchaseTotal = SelectedItem is null ? 0 : SelectedItem.Price * Math.Max(0, PurchaseQuantity);

    [RelayCommand]
    private void ConfirmPurchase()
    {
        if (SelectedItem is null) return;

        var result = PurchaseService.TryPurchase(Set, SelectedItem.Id, PurchaseQuantity, out var totalCost);
        PurchaseError = result switch
        {
            PurchaseResult.InvalidQuantity => "Quantidade inválida.",
            PurchaseResult.InsufficientStock => "Estoque insuficiente para essa quantidade.",
            PurchaseResult.ItemNotFound => "Item não encontrado.",
            _ => string.Empty,
        };
        if (result != PurchaseResult.Success) return;

        _library.Save(Set);
        DetailOverlayVisible = false;
        SelectedItem = null;
        RefreshPage();
        ShowSpendIndicator(totalCost);
        PlaySaleReaction();
        PlayVoiceLine(Set.Sale);
        StockChanged?.Invoke();
    }

    private void ShowSpendIndicator(int cost)
    {
        IndicatorText = $"− {cost} Sucata";
        IndicatorVisible = true;
        _indicatorTimer?.Stop();
        _indicatorTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _indicatorTimer.Tick += (_, _) =>
        {
            IndicatorVisible = false;
            _indicatorTimer!.Stop();
        };
        _indicatorTimer.Start();
    }

    private void PlaySaleReaction()
    {
        var reactionBitmap = LoadBitmap(Set.ReactionImageFile);
        if (reactionBitmap is null) return;

        VendorDisplayImage = reactionBitmap;
        _reactionTimer?.Stop();
        _reactionTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _reactionTimer.Tick += (_, _) =>
        {
            RefreshVendorAndBackground();
            _reactionTimer!.Stop();
        };
        _reactionTimer.Start();
    }

    public void PlayArrival() => PlayVoiceLine(Set.Arrival);

    public void BeginExitSequence()
    {
        var farewellBitmap = LoadBitmap(Set.FarewellImageFile);
        if (farewellBitmap is not null) VendorDisplayImage = farewellBitmap;

        PlayVoiceLine(Set.Exit);

        _exitTimer?.Stop();
        _exitTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2.5) };
        _exitTimer.Tick += (_, _) =>
        {
            _exitTimer!.Stop();
            ExitSequenceCompleted?.Invoke();
        };
        _exitTimer.Start();
    }

    private void PlayVoiceLine(VoiceLine line)
    {
        if (line.HasAudio)
        {
            var path = _library.ResolveMediaPath(Set.Id, line.AudioFile);
            if (path is not null) _audio.Play(path);
        }

        if (line.HasCaption)
        {
            CaptionText = line.Caption;
            CaptionVisible = true;
            _captionTimer?.Stop();
            _captionTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
            _captionTimer.Tick += (_, _) =>
            {
                CaptionVisible = false;
                _captionTimer!.Stop();
            };
            _captionTimer.Start();
        }
        else
        {
            CaptionVisible = false;
        }
    }

    public void StopAllTimersAndAudio()
    {
        _indicatorTimer?.Stop();
        _reactionTimer?.Stop();
        _captionTimer?.Stop();
        _exitTimer?.Stop();
        _audio.Stop();
    }
}
