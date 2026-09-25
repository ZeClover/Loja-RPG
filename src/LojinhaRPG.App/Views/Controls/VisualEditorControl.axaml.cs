using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using LojinhaRPG.Core.Models;

namespace LojinhaRPG.App.Views.Controls;

/// <summary>
/// Prévia arrastável/redimensionável da cena de 1920x1080: permite posicionar as áreas do
/// vendedor e da grade de itens, mantendo os valores sincronizados com o RegionLayout do set.
/// Também renderiza uma prévia do resultado final (fundo, vendedor sem moldura e grade de itens
/// já estilizada com o preset), para o usuário ver como a loja vai ficar antes de abri-la.
/// </summary>
public partial class VisualEditorControl : UserControl
{
    private enum DragMode { None, MoveVendor, ResizeVendor, MoveItems, ResizeItems }

    private RegionLayout? _vendor;
    private RegionLayout? _items;

    private DragMode _mode = DragMode.None;
    private Point _pointerStart;
    private double _startX, _startY, _startW, _startH;

    private readonly Border[] _itemPreviewCards = new Border[9];

    public event Action? LayoutEdited;
    public event Action? DragCompleted;

    public VisualEditorControl()
    {
        InitializeComponent();

        for (var i = 0; i < _itemPreviewCards.Length; i++)
        {
            var card = new Border
            {
                Margin = new Thickness(3),
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(4),
                Background = new SolidColorBrush(Color.Parse("#4A2F20")),
                BorderBrush = new SolidColorBrush(Color.Parse("#2A1810")),
            };
            _itemPreviewCards[i] = card;
            ItemsPreviewGrid.Children.Add(card);
        }

        VendorHandle.PointerPressed += (s, e) => BeginDrag(DragMode.MoveVendor, e);
        VendorGrip.PointerPressed += (s, e) => { BeginDrag(DragMode.ResizeVendor, e); e.Handled = true; };
        ItemsHandle.PointerPressed += (s, e) => BeginDrag(DragMode.MoveItems, e);
        ItemsGrip.PointerPressed += (s, e) => { BeginDrag(DragMode.ResizeItems, e); e.Handled = true; };

        EditorCanvas.PointerMoved += OnPointerMoved;
        EditorCanvas.PointerReleased += OnPointerReleased;

        EditorCanvas.SizeChanged += (_, _) => Redraw();
    }

    public void SetRegions(RegionLayout vendor, RegionLayout items)
    {
        if (_vendor is not null) _vendor.PropertyChanged -= OnRegionPropertyChanged;
        if (_items is not null) _items.PropertyChanged -= OnRegionPropertyChanged;

        _vendor = vendor;
        _items = items;
        _vendor.PropertyChanged += OnRegionPropertyChanged;
        _items.PropertyChanged += OnRegionPropertyChanged;

        Redraw();
    }

    /// <summary>Atualiza a prévia visual: fundo, imagem do vendedor e o estilo das molduras de item.</summary>
    public void SetPreview(Bitmap? background, Bitmap? vendorImage, ShopPreset preset, Bitmap? frameTexture)
    {
        PreviewBackgroundImage.Source = background;
        VendorPreviewImage.Source = vendorImage;

        IBrush cardBackground = frameTexture is not null
            ? new ImageBrush(frameTexture) { Stretch = Stretch.UniformToFill }
            : new SolidColorBrush(ParseColor(preset.ItemsPanelBackground));
        var cardBorder = new SolidColorBrush(ParseColor(preset.BorderColor));
        var cardRadius = new CornerRadius(Math.Max(0, preset.CornerRadius - 2));
        var cardThickness = new Thickness(Math.Max(2, preset.BorderThickness / 2));

        foreach (var card in _itemPreviewCards)
        {
            card.Background = cardBackground;
            card.BorderBrush = cardBorder;
            card.CornerRadius = cardRadius;
            card.BorderThickness = cardThickness;
        }
    }

    private static Color ParseColor(string hex)
    {
        try { return Color.Parse(hex); } catch { return Colors.Gray; }
    }

    private void OnRegionPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) => Redraw();

    private double Scale => EditorCanvas.Bounds.Width > 0 ? EditorCanvas.Bounds.Width / VisualLayout.SceneWidth : 0.25;

    private void Redraw()
    {
        var scale = Scale;

        PreviewBackgroundImage.Width = VisualLayout.SceneWidth * scale;
        PreviewBackgroundImage.Height = VisualLayout.SceneHeight * scale;

        if (_vendor is null || _items is null) return;

        Canvas.SetLeft(VendorHandle, _vendor.X * scale);
        Canvas.SetTop(VendorHandle, _vendor.Y * scale);
        VendorHandle.Width = Math.Max(10, _vendor.Width * scale);
        VendorHandle.Height = Math.Max(10, _vendor.Height * scale);

        Canvas.SetLeft(ItemsHandle, _items.X * scale);
        Canvas.SetTop(ItemsHandle, _items.Y * scale);
        ItemsHandle.Width = Math.Max(10, _items.Width * scale);
        ItemsHandle.Height = Math.Max(10, _items.Height * scale);
    }

    private void BeginDrag(DragMode mode, PointerPressedEventArgs e)
    {
        if (_vendor is null || _items is null) return;
        _mode = mode;
        _pointerStart = e.GetPosition(EditorCanvas);

        var region = mode is DragMode.MoveVendor or DragMode.ResizeVendor ? _vendor : _items;
        _startX = region.X;
        _startY = region.Y;
        _startW = region.Width;
        _startH = region.Height;

        e.Pointer.Capture((e.Source as Control) ?? EditorCanvas);
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_mode == DragMode.None || _vendor is null || _items is null) return;
        var scale = Scale;
        if (scale <= 0) return;

        var pos = e.GetPosition(EditorCanvas);
        var dx = (pos.X - _pointerStart.X) / scale;
        var dy = (pos.Y - _pointerStart.Y) / scale;

        switch (_mode)
        {
            case DragMode.MoveVendor:
                _vendor.X = Clamp(_startX + dx, 0, VisualLayout.SceneWidth - _vendor.Width);
                _vendor.Y = Clamp(_startY + dy, 0, VisualLayout.SceneHeight - _vendor.Height);
                break;
            case DragMode.ResizeVendor:
                _vendor.Width = Clamp(_startW + dx, 40, VisualLayout.SceneWidth - _vendor.X);
                _vendor.Height = Clamp(_startH + dy, 40, VisualLayout.SceneHeight - _vendor.Y);
                break;
            case DragMode.MoveItems:
                _items.X = Clamp(_startX + dx, 0, VisualLayout.SceneWidth - _items.Width);
                _items.Y = Clamp(_startY + dy, 0, VisualLayout.SceneHeight - _items.Height);
                break;
            case DragMode.ResizeItems:
                _items.Width = Clamp(_startW + dx, 40, VisualLayout.SceneWidth - _items.X);
                _items.Height = Clamp(_startH + dy, 40, VisualLayout.SceneHeight - _items.Y);
                break;
        }

        LayoutEdited?.Invoke();
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_mode == DragMode.None) return;
        _mode = DragMode.None;
        e.Pointer.Capture(null);
        DragCompleted?.Invoke();
    }

    private static double Clamp(double value, double min, double max)
    {
        if (max < min) max = min;
        return Math.Max(min, Math.Min(value, max));
    }
}
