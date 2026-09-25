using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using LojinhaRPG.Core.Models;

namespace LojinhaRPG.App.Views.Controls;

/// <summary>
/// Prévia arrastável/redimensionável da cena de 1920x1080: permite posicionar as áreas do
/// vendedor e da grade de itens, mantendo os valores sincronizados com o RegionLayout do set.
/// </summary>
public partial class VisualEditorControl : UserControl
{
    private enum DragMode { None, MoveVendor, ResizeVendor, MoveItems, ResizeItems }

    private RegionLayout? _vendor;
    private RegionLayout? _items;

    private DragMode _mode = DragMode.None;
    private Point _pointerStart;
    private double _startX, _startY, _startW, _startH;

    public event Action? LayoutEdited;
    public event Action? DragCompleted;

    public VisualEditorControl()
    {
        InitializeComponent();

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

    private void OnRegionPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) => Redraw();

    private double Scale => EditorCanvas.Bounds.Width > 0 ? EditorCanvas.Bounds.Width / VisualLayout.SceneWidth : 0.25;

    private void Redraw()
    {
        if (_vendor is null || _items is null) return;
        var scale = Scale;

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
