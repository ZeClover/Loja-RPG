namespace LojinhaRPG.Core.Models;

/// <summary>
/// Um set completo de loja: dados, imagens, falas, itens e layout visual.
/// Persistido em Sets/&lt;Id&gt;/set.json, com arquivos de mídia em Sets/&lt;Id&gt;/media/.
/// </summary>
public class ShopSet
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string ShopName { get; set; } = "Nova loja";
    public string VendorName { get; set; } = "Vendedor";

    /// <summary>Caminhos relativos (dentro de media/) para as imagens do set. Vazio = não definido.</summary>
    public string VendorImageFile { get; set; } = string.Empty;
    public string ReactionImageFile { get; set; } = string.Empty;
    public string FarewellImageFile { get; set; } = string.Empty;
    public string BackgroundImageFile { get; set; } = string.Empty;

    public VoiceLine Arrival { get; set; } = new();
    public VoiceLine Sale { get; set; } = new();
    public VoiceLine Exit { get; set; } = new();

    public List<ShopItem> Items { get; set; } = new();

    public VisualLayout Layout { get; set; } = new();

    /// <summary>Id do ShopPreset (visual) usado por este set. Guid.Empty = usar o padrão do app.</summary>
    public Guid PresetId { get; set; } = Guid.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
