using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using LojinhaRPG.App.ViewModels;
using LojinhaRPG.Core.Models;

namespace LojinhaRPG.App.Views;

/// <summary>
/// Janela da loja: cena fixa de 1920x1080 pensada para ser capturada no OBS via "Captura de janela".
/// Sem decorações do sistema para não aparecer nenhuma barra de título na captura; pode ser
/// arrastada clicando e segurando em uma área de fundo (fora de botões/itens).
/// </summary>
public partial class ShopWindow : Window
{
    private readonly Action? _onExitFullyClosed;
    private bool _exitAlreadyNotified;
    private bool _dragging;
    private PixelPoint _dragStartScreenPoint;
    private PixelPoint _dragStartWindowPos;

    public ShopWindowViewModel Vm { get; }

    public event Action? StockChangedExternally;

    // Construtor sem parâmetros exigido pelo carregador XAML do designer; não usado em runtime.
    public ShopWindow() : this(new ShopSet(), null) { }

    public ShopWindow(ShopSet set, Action? onExitFullyClosed)
    {
        _onExitFullyClosed = onExitFullyClosed;
        Vm = new ShopWindowViewModel(set);
        DataContext = Vm;

        InitializeComponent();

        Position = new PixelPoint(0, 0);

        Vm.ExitSequenceCompleted += () =>
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(Close);
        };
        Vm.StockChanged += () => StockChangedExternally?.Invoke();
        Vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ShopWindowViewModel.Preset)) ApplyPresetStyle();
        };

        ApplyPresetStyle();
        PositionCaptionBubble();

        Closing += (_, _) =>
        {
            Vm.StopAllTimersAndAudio();
            if (!_exitAlreadyNotified)
            {
                _exitAlreadyNotified = true;
                _onExitFullyClosed?.Invoke();
            }
        };
    }

    public void PlayArrival() => Vm.PlayArrival();

    public void BeginFarewellAndClose() => Vm.BeginExitSequence();

    private void ApplyPresetStyle()
    {
        var preset = Vm.Preset;

        var itemsBrush = new SolidColorBrush(ParseColor(preset.ItemsPanelBackground));
        var borderBrush = new SolidColorBrush(ParseColor(preset.BorderColor));
        var thickness = new Thickness(preset.BorderThickness);
        var radius = new CornerRadius(preset.CornerRadius);

        // Não há moldura atrás do vendedor de propósito: o PNG fica solto sobre o Background da cena.
        ItemsPanelBorder.Background = itemsBrush;
        ItemsPanelBorder.BorderBrush = borderBrush;
        ItemsPanelBorder.BorderThickness = thickness;
        ItemsPanelBorder.CornerRadius = radius;
    }

    private static Color ParseColor(string hex)
    {
        try { return Color.Parse(hex); } catch { return Colors.Gray; }
    }

    /// <summary>Posiciona o balão de fala perto do vendedor, com o rabicho apontando para ele.
    /// Fica acima quando há espaço; senão, logo abaixo do topo do vendedor.</summary>
    private void PositionCaptionBubble()
    {
        const double bubbleWidth = 620;
        const double estimatedBubbleHeight = 190; // corpo + rabicho, para decidir/posicionar antes do layout medir o texto
        const double margin = 20;

        var vendor = Vm.Layout.Vendor;
        var desiredLeft = vendor.X + vendor.Width / 2 - bubbleWidth / 2;
        var left = Math.Clamp(desiredLeft, margin, 1920 - bubbleWidth - margin);

        bool placeAbove = vendor.Y - estimatedBubbleHeight - margin > 0;
        double top = placeAbove
            ? Math.Max(margin, vendor.Y - estimatedBubbleHeight)
            : Math.Min(1080 - estimatedBubbleHeight - margin, vendor.Y + 24);

        Canvas.SetLeft(CaptionBubbleRoot, left);
        Canvas.SetTop(CaptionBubbleRoot, top);
        TailDown.IsVisible = placeAbove;
        TailUp.IsVisible = !placeAbove;
    }

    private void OnItemTapped(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { DataContext: ShopItem item })
            Vm.SelectItem(item);
    }

    // ---------- Arrastar a janela sem barra de título (área de fundo apenas) ----------

    private void OnBackgroundPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Source is not (Canvas or Border or Image)) return; // só inicia arraste no fundo/vendedor, não em botões de itens
        _dragging = true;
        _dragStartScreenPoint = this.PointToScreen(e.GetPosition(this));
        _dragStartWindowPos = Position;
        e.Pointer.Capture((sender as Control));
    }

    private void OnBackgroundPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_dragging) return;
        var currentScreen = this.PointToScreen(e.GetPosition(this));
        var dx = currentScreen.X - _dragStartScreenPoint.X;
        var dy = currentScreen.Y - _dragStartScreenPoint.Y;
        Position = new PixelPoint(_dragStartWindowPos.X + dx, _dragStartWindowPos.Y + dy);
    }

    private void OnBackgroundPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _dragging = false;
        e.Pointer.Capture(null);
    }
}
