using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LojinhaRPG.Core.Models;

namespace LojinhaRPG.App.ViewModels;

public partial class PresetEditDialogViewModel : ViewModelBase
{
    public ShopPreset Working { get; }
    private readonly bool _isNew;

    [ObservableProperty] private string _name;
    [ObservableProperty] private string _description;
    [ObservableProperty] private string _panelBackground;
    [ObservableProperty] private string _panelBackgroundSecondary;
    [ObservableProperty] private string _borderColor;
    [ObservableProperty] private double _borderThickness;
    [ObservableProperty] private double _cornerRadius;
    [ObservableProperty] private string _accentColor;
    [ObservableProperty] private string _textColor;
    [ObservableProperty] private string _itemsPanelBackground;
    [ObservableProperty] private string _errorMessage = string.Empty;

    public string[] TextureStyles { get; } = { "Flat", "Wood", "Cloth", "Moss", "Stone" };
    [ObservableProperty] private string _selectedTextureStyle;

    [ObservableProperty] private Bitmap? _frameTexturePreview;
    [ObservableProperty] private string _frameTextureLabel = "(nenhuma textura)";

    public string RecommendedFrameTextureSize { get; } = ItemGridMetrics.DescribeRecommendedFrameTextureSize();

    public bool Confirmed { get; private set; }
    public Action? RequestClose { get; set; }

    public PresetEditDialogViewModel(ShopPreset source, bool isNew)
    {
        Working = source;
        _isNew = isNew;
        _name = source.Name;
        _description = source.Description;
        _panelBackground = source.PanelBackground;
        _panelBackgroundSecondary = source.PanelBackgroundSecondary;
        _borderColor = source.BorderColor;
        _borderThickness = source.BorderThickness;
        _cornerRadius = source.CornerRadius;
        _accentColor = source.AccentColor;
        _textColor = source.TextColor;
        _itemsPanelBackground = source.ItemsPanelBackground;
        _selectedTextureStyle = source.TextureStyle;

        LoadFrameTexturePreview();
    }

    private void LoadFrameTexturePreview()
    {
        var path = AppServices.Presets.ResolveMediaPath(Working.Id, Working.FrameTextureFile);
        FrameTextureLabel = path is null ? "(nenhuma textura)" : Working.FrameTextureFile;
        try { FrameTexturePreview = path is null ? null : new Bitmap(path); }
        catch { FrameTexturePreview = null; }
    }

    public void ImportFrameTexture(string sourcePath)
    {
        Working.FrameTextureFile = AppServices.Presets.ImportMediaFile(Working.Id, sourcePath, "frame");
        LoadFrameTexturePreview();
    }

    public void ClearFrameTexture()
    {
        Working.FrameTextureFile = string.Empty;
        LoadFrameTexturePreview();
    }

    [RelayCommand]
    private void Confirm()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            ErrorMessage = "Informe um nome para o preset.";
            return;
        }

        Working.Name = Name;
        Working.Description = Description;
        Working.PanelBackground = PanelBackground;
        Working.PanelBackgroundSecondary = PanelBackgroundSecondary;
        Working.BorderColor = BorderColor;
        Working.BorderThickness = BorderThickness;
        Working.CornerRadius = CornerRadius;
        Working.AccentColor = AccentColor;
        Working.TextColor = TextColor;
        Working.ItemsPanelBackground = ItemsPanelBackground;
        Working.TextureStyle = SelectedTextureStyle;

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
