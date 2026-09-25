using LojinhaRPG.Core.Models;
using LojinhaRPG.Core.Services;
using Xunit;

namespace LojinhaRPG.Tests;

public class PurchaseServiceTests
{
    private static ShopSet MakeSet(int stock = 5, int price = 10)
    {
        var set = new ShopSet();
        set.Items.Add(new ShopItem { Name = "Tocha", Price = price, Stock = stock });
        return set;
    }

    [Fact]
    public void Purchase_WithValidQuantity_DecrementsStockAndComputesCost()
    {
        var set = MakeSet(stock: 5, price: 10);
        var itemId = set.Items[0].Id;

        var result = PurchaseService.TryPurchase(set, itemId, 3, out var totalCost);

        Assert.Equal(PurchaseResult.Success, result);
        Assert.Equal(30, totalCost);
        Assert.Equal(2, set.Items[0].Stock);
    }

    [Fact]
    public void Purchase_WithQuantityAboveStock_FailsAndLeavesStockUnchanged()
    {
        var set = MakeSet(stock: 2);
        var itemId = set.Items[0].Id;

        var result = PurchaseService.TryPurchase(set, itemId, 5, out var totalCost);

        Assert.Equal(PurchaseResult.InsufficientStock, result);
        Assert.Equal(0, totalCost);
        Assert.Equal(2, set.Items[0].Stock);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Purchase_WithNonPositiveQuantity_IsRejected(int quantity)
    {
        var set = MakeSet(stock: 5);
        var itemId = set.Items[0].Id;

        var result = PurchaseService.TryPurchase(set, itemId, quantity, out _);

        Assert.Equal(PurchaseResult.InvalidQuantity, result);
        Assert.Equal(5, set.Items[0].Stock);
    }

    [Fact]
    public void Purchase_UnknownItem_ReturnsItemNotFound()
    {
        var set = MakeSet();

        var result = PurchaseService.TryPurchase(set, Guid.NewGuid(), 1, out _);

        Assert.Equal(PurchaseResult.ItemNotFound, result);
    }

    [Fact]
    public void Purchase_ExactRemainingStock_DepletesToZero()
    {
        var set = MakeSet(stock: 3, price: 7);
        var itemId = set.Items[0].Id;

        var result = PurchaseService.TryPurchase(set, itemId, 3, out var totalCost);

        Assert.Equal(PurchaseResult.Success, result);
        Assert.Equal(21, totalCost);
        Assert.Equal(0, set.Items[0].Stock);
    }
}
