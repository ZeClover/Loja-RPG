namespace LojinhaRPG.Core.Models;

/// <summary>
/// Visual (tema) da loja: cores, bordas e textura, sem depender de arquivos de imagem externos.
/// Guardado separadamente da biblioteca de sets e pode ser importado/exportado por si só.
/// </summary>
public class ShopPreset
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    /// <summary>Cor de fundo principal do painel (hex, ex: "#5B3A29").</summary>
    public string PanelBackground { get; set; } = "#5B3A29";

    /// <summary>Segunda cor de fundo, usada em gradiente/textura.</summary>
    public string PanelBackgroundSecondary { get; set; } = "#3E2718";

    public string BorderColor { get; set; } = "#2A1810";
    public double BorderThickness { get; set; } = 6;
    public double CornerRadius { get; set; } = 10;

    public string AccentColor { get; set; } = "#D8B26B";
    public string TextColor { get; set; } = "#F5E9D3";

    /// <summary>Cor do painel de itens (grade), pode diferir do painel do vendedor.</summary>
    public string ItemsPanelBackground { get; set; } = "#4A2F20";

    /// <summary>Estilo de textura simulada: Flat, Wood, Cloth, Moss, Stone.</summary>
    public string TextureStyle { get; set; } = "Wood";

    public string FontFamily { get; set; } = "Georgia";

    public static ShopPreset CreateFeiraPreset() => new()
    {
        Name = "Loja de feira",
        Description = "Visual simples de banca de feira: madeira clara, tecido e cordas.",
        PanelBackground = "#C98A4B",
        PanelBackgroundSecondary = "#8F5A2B",
        BorderColor = "#5C3A1E",
        BorderThickness = 8,
        CornerRadius = 6,
        AccentColor = "#E3B23C",
        TextColor = "#2B1B0E",
        ItemsPanelBackground = "#D9AB6B",
        TextureStyle = "Wood",
        FontFamily = "Georgia",
    };

    public static ShopPreset CreateMusgoPreset() => new()
    {
        Name = "Loja improvisada com musgo",
        Description = "Molduras malfeitas e desgastadas, pedra ou madeira cobertas de musgo.",
        PanelBackground = "#4B5D3A",
        PanelBackgroundSecondary = "#33402A",
        BorderColor = "#232B1B",
        BorderThickness = 10,
        CornerRadius = 2,
        AccentColor = "#8FA65C",
        TextColor = "#E8EFDA",
        ItemsPanelBackground = "#3A4630",
        TextureStyle = "Moss",
        FontFamily = "Georgia",
    };

    public ShopPreset Clone() => (ShopPreset)MemberwiseClone();
}
