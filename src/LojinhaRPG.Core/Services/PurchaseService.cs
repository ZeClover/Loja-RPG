using LojinhaRPG.Core.Models;

namespace LojinhaRPG.Core.Services;

public enum PurchaseResult
{
    Success,
    InvalidQuantity,
    InsufficientStock,
    ItemNotFound,
}

/// <summary>Regras de compra: valida quantidade/estoque e só desconta após confirmação.</summary>
public static class PurchaseService
{
    public static PurchaseResult TryPurchase(ShopSet set, Guid itemId, int quantity, out int totalCost)
    {
        totalCost = 0;
        var item = set.Items.FirstOrDefault(i => i.Id == itemId);
        if (item is null) return PurchaseResult.ItemNotFound;
        if (quantity <= 0) return PurchaseResult.InvalidQuantity;
        if (quantity > item.Stock) return PurchaseResult.InsufficientStock;

        item.Stock -= quantity;
        totalCost = quantity * item.Price;
        return PurchaseResult.Success;
    }
}
