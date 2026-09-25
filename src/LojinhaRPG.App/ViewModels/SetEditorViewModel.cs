using System.Collections.ObjectModel;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LojinhaRPG.Core.Models;
using LojinhaRPG.Core.Services;

namespace LojinhaRPG.App.ViewModels;

/// <summary>Edita um ShopSet completo: dados, mídia, falas, itens, layout visual e preset.</summary>
public partial class SetEditorViewModel : ViewModelBase
{
    private readonly SetLibraryService _library = AppServices.Sets;

    public ShopSet Set { get; private set; }

    [ObservableProperty] private string _shopName = string.Empty;
    [ObservableProperty] private string _vendorName = string.Empty;

    [ObservableProperty] private Bitmap? _vendorImage;
    [ObservableProperty] private Bitmap? _reactionImage;
    [ObservableProperty] private Bitmap? _farewellImage;
    [ObservableProperty] private Bitmap? _backgroundImage;

    [ObservableProperty] private string _arrivalCaption = string.Empty;
    [ObservableProperty] private string _arrivalAudioLabel = "(nenhum áudio)";
    [ObservableProperty] private string _saleCaption = string.Empty;
    [ObservableProperty] private string _saleAudioLabel = "(nenhum áudio)";
    [ObservableProperty] private string _exitCaption = string.Empty;
    [ObservableProperty] private string _exitAudioLabel = "(nenhum áudio)";

    public ObservableCollection<ShopItem> Items { get; } = new();
    [ObservableProperty] private ShopItem? _selectedItem;

    public ObservableCollection<ShopPreset> Presets { get; } = new();
    [ObservableProperty] private ShopPreset? _selectedPreset;

    public VisualLayout Layout => Set.Layout;

    [ObservableProperty] private string _statusMessage = string.Empty;

    public event Action? SetSavedExternally;

    public SetEditorViewModel(ShopSet set)
    {
        Set = set;
        LoadFromSet();
        ReloadPresets();
    }

    private void ReloadPresets()
    {
        Presets.Clear();
        foreach (var p in AppServices.Presets.LoadAll()) Presets.Add(p);
        SelectedPreset = Presets.FirstOrDefault(p => p.Id == Set.PresetId) ?? Presets.FirstOrDefault();
    }

    public void RefreshPresetList() => ReloadPresets();

    private void LoadFromSet()
    {
        ShopName = Set.ShopName;
        VendorName = Set.VendorName;

        VendorImage = LoadBitmap(Set.VendorImageFile);
        ReactionImage = LoadBitmap(Set.ReactionImageFile);
        FarewellImage = LoadBitmap(Set.FarewellImageFile);
        BackgroundImage = LoadBitmap(Set.BackgroundImageFile);

        ArrivalCaption = Set.Arrival.Caption;
        ArrivalAudioLabel = LabelFor(Set.Arrival.AudioFile);
        SaleCaption = Set.Sale.Caption;
        SaleAudioLabel = LabelFor(Set.Sale.AudioFile);
        ExitCaption = Set.Exit.Caption;
        ExitAudioLabel = LabelFor(Set.Exit.AudioFile);

        Items.Clear();
        foreach (var item in Set.Items) Items.Add(item);
    }

    private static string LabelFor(string file) => string.IsNullOrWhiteSpace(file) ? "(nenhum áudio)" : file;

    private Bitmap? LoadBitmap(string relativeFile)
    {
        var path = _library.ResolveMediaPath(Set.Id, relativeFile);
        if (path is null) return null;
        try { return new Bitmap(path); } catch { return null; }
    }

    /// <summary>Grava todos os campos simples (nome, falas, layout) de volta no Set e persiste.</summary>
    [RelayCommand]
    public void SaveAll()
    {
        Set.ShopName = string.IsNullOrWhiteSpace(ShopName) ? Set.ShopName : ShopName;
        Set.VendorName = VendorName;
        Set.Arrival.Caption = ArrivalCaption;
        Set.Sale.Caption = SaleCaption;
        Set.Exit.Caption = ExitCaption;
        Set.PresetId = SelectedPreset?.Id ?? Guid.Empty;
        _library.Save(Set);
        StatusMessage = $"Salvo às {DateTime.Now:HH:mm:ss}";
        SetSavedExternally?.Invoke();
    }

    public void ImportImage(string sourcePath, ImageSlot slot)
    {
        var canonical = slot switch
        {
            ImageSlot.Vendor => "vendor",
            ImageSlot.Reaction => "reaction",
            ImageSlot.Farewell => "farewell",
            ImageSlot.Background => "background",
            _ => "img",
        };
        var fileName = _library.ImportMediaFile(Set.Id, sourcePath, canonical);
        switch (slot)
        {
            case ImageSlot.Vendor: Set.VendorImageFile = fileName; break;
            case ImageSlot.Reaction: Set.ReactionImageFile = fileName; break;
            case ImageSlot.Farewell: Set.FarewellImageFile = fileName; break;
            case ImageSlot.Background: Set.BackgroundImageFile = fileName; break;
        }
        _library.Save(Set);
        LoadFromSet();
    }

    public void ClearImage(ImageSlot slot)
    {
        switch (slot)
        {
            case ImageSlot.Reaction: Set.ReactionImageFile = string.Empty; break;
            case ImageSlot.Farewell: Set.FarewellImageFile = string.Empty; break;
            case ImageSlot.Background: Set.BackgroundImageFile = string.Empty; break;
        }
        _library.Save(Set);
        LoadFromSet();
    }

    public void ImportAudio(string sourcePath, VoiceSlot slot)
    {
        var canonical = slot switch
        {
            VoiceSlot.Arrival => "arrival",
            VoiceSlot.Sale => "sale",
            VoiceSlot.Exit => "exit",
            _ => "audio",
        };
        var fileName = _library.ImportMediaFile(Set.Id, sourcePath, canonical);
        VoiceLineFor(slot).AudioFile = fileName;
        _library.Save(Set);
        LoadFromSet();
    }

    public void ClearAudio(VoiceSlot slot)
    {
        VoiceLineFor(slot).AudioFile = string.Empty;
        _library.Save(Set);
        LoadFromSet();
    }

    public void PreviewAudio(VoiceSlot slot)
    {
        var relative = VoiceLineFor(slot).AudioFile;
        var path = _library.ResolveMediaPath(Set.Id, relative);
        if (path is not null) AppServices.Audio.Play(path);
    }

    private VoiceLine VoiceLineFor(VoiceSlot slot) => slot switch
    {
        VoiceSlot.Arrival => Set.Arrival,
        VoiceSlot.Sale => Set.Sale,
        VoiceSlot.Exit => Set.Exit,
        _ => throw new ArgumentOutOfRangeException(nameof(slot)),
    };

    public void AddItem(ShopItem item)
    {
        Set.Items.Add(item);
        Items.Add(item);
        _library.Save(Set);
    }

    public void UpdateSelectedItem(string name, int price, int stock, string info)
    {
        if (SelectedItem is null) return;
        SelectedItem.Name = name;
        SelectedItem.Price = price;
        SelectedItem.Stock = stock;
        SelectedItem.Info = info;
        _library.Save(Set);
        // força atualização visual da lista
        var idx = Items.IndexOf(SelectedItem);
        if (idx >= 0)
        {
            var item = Items[idx];
            Items.RemoveAt(idx);
            Items.Insert(idx, item);
            SelectedItem = item;
        }
    }

    public void DeleteSelectedItem()
    {
        if (SelectedItem is null) return;
        Set.Items.Remove(SelectedItem);
        Items.Remove(SelectedItem);
        SelectedItem = null;
        _library.Save(Set);
    }

    public void RestockSelectedItem(int amount)
    {
        if (SelectedItem is null) return;
        SelectedItem.Stock += amount;
        _library.Save(Set);
        var idx = Items.IndexOf(SelectedItem);
        if (idx >= 0)
        {
            var item = Items[idx];
            Items.RemoveAt(idx);
            Items.Insert(idx, item);
            SelectedItem = item;
        }
    }

    public void SaveLayout()
    {
        _library.Save(Set);
    }
}

public enum ImageSlot { Vendor, Reaction, Farewell, Background }
public enum VoiceSlot { Arrival, Sale, Exit }
