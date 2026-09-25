using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LojinhaRPG.Core.Models;

/// <summary>
/// Posição e tamanho de um elemento dentro da cena de 1920x1080 (em pixels dessa cena).
/// Implementa INotifyPropertyChanged manualmente (sem depender de nenhum framework de UI)
/// para permitir binding bidirecional em tempo real no editor visual.
/// </summary>
public class RegionLayout : INotifyPropertyChanged
{
    private double _x;
    private double _y;
    private double _width;
    private double _height;

    public double X { get => _x; set => SetField(ref _x, value); }
    public double Y { get => _y; set => SetField(ref _y, value); }
    public double Width { get => _width; set => SetField(ref _width, value); }
    public double Height { get => _height; set => SetField(ref _height, value); }

    public RegionLayout() { }

    public RegionLayout(double x, double y, double width, double height)
    {
        _x = x;
        _y = y;
        _width = width;
        _height = height;
    }

    public RegionLayout Clone() => new(X, Y, Width, Height);

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>Posições/tamanhos editáveis da cena da loja, salvos por set.</summary>
public class VisualLayout
{
    public const double SceneWidth = 1920;
    public const double SceneHeight = 1080;

    public RegionLayout Vendor { get; set; } = new(60, 140, 560, 820);
    public RegionLayout ItemsPanel { get; set; } = new(700, 120, 1160, 760);

    public VisualLayout Clone() => new()
    {
        Vendor = Vendor.Clone(),
        ItemsPanel = ItemsPanel.Clone(),
    };
}
