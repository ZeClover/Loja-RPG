namespace LojinhaRPG.Core.Models;

/// <summary>Um item à venda na loja. Sem imagem, por decisão de design.</summary>
public class ShopItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;

    /// <summary>Preço em Sucata.</summary>
    public int Price { get; set; }

    public int Stock { get; set; }

    /// <summary>Informações adicionais exibidas ao passar o mouse / nos detalhes.</summary>
    public string Info { get; set; } = string.Empty;
}
