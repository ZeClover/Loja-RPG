namespace LojinhaRPG.Core.Models;

/// <summary>
/// Constantes da grade 3x3 da Janela da Loja (devem refletir o que está em ShopWindow.axaml:
/// padding do painel, margem de cada quadro de item e altura reservada para a paginação),
/// usadas para calcular o tamanho recomendado de uma textura de moldura de preset.
/// </summary>
public static class ItemGridMetrics
{
    public const int Columns = 3;
    public const int Rows = 3;
    public const double CardMargin = 6;
    public const double PanelPadding = 16;
    public const double PaginationBarHeight = 48;

    /// <summary>Tamanho aproximado (em pixels da cena 1920x1080) de cada quadro de item,
    /// dado o retângulo atual da grade de itens de um set.</summary>
    public static (double Width, double Height) EstimateCardSize(RegionLayout itemsPanel)
    {
        var innerWidth = Math.Max(0, itemsPanel.Width - PanelPadding * 2);
        var innerHeight = Math.Max(0, itemsPanel.Height - PanelPadding * 2 - PaginationBarHeight);
        var cardWidth = Math.Max(0, innerWidth / Columns - CardMargin * 2);
        var cardHeight = Math.Max(0, innerHeight / Rows - CardMargin * 2);
        return (cardWidth, cardHeight);
    }

    /// <summary>Tamanho do quadro de item no layout padrão de um set novo (usado como referência
    /// para recomendar o tamanho de uma textura de moldura de preset, que não pertence a nenhum set).</summary>
    public static (double Width, double Height) EstimateDefaultCardSize() => EstimateCardSize(new VisualLayout().ItemsPanel);

    /// <summary>Mensagem explicando o tamanho recomendado para uma imagem de textura de moldura,
    /// calculada a partir de como a grade de itens é montada (não é um número fixo arbitrário).</summary>
    public static string DescribeRecommendedFrameTextureSize()
    {
        var (w, h) = EstimateDefaultCardSize();
        var recommendedW = (int)Math.Ceiling(w * 1.6 / 10) * 10;
        var recommendedH = (int)Math.Ceiling(h * 1.6 / 10) * 10;
        return $"Cada moldura de item usa cerca de {(int)w}×{(int)h} px no layout padrão da grade 3×3. " +
               $"Para não ficar borrada ao ser esticada, use uma imagem de pelo menos {recommendedW}×{recommendedH} px " +
               "(nunca algo pequeno como 100×100). A imagem é redimensionada automaticamente para preencher cada quadro.";
    }
}
