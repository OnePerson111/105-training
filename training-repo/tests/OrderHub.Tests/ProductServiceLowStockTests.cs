using OrderHub.Core.Domain;

namespace OrderHub.Tests;

public class ProductServiceLowStockTests
{
    [Fact]
    public async Task GetLowStock_FiltersByThresholdAndSortsByStockAscending()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        TestSetup.AddProduct(db, sku: "SKU-S08", stock: 8);
        TestSetup.AddProduct(db, sku: "SKU-S03", stock: 3);
        TestSetup.AddProduct(db, sku: "SKU-S12", stock: 12);
        TestSetup.AddProduct(db, sku: "SKU-S20", stock: 20);

        var result = await service.GetLowStockAsync(10);

        Assert.Equal(new[] { "SKU-S03", "SKU-S08" }, result.Select(r => r.Sku).ToArray());
    }

    [Fact]
    public async Task GetLowStock_ExcludesInactiveProducts()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        TestSetup.AddProduct(db, sku: "SKU-ACTIVE", stock: 4);
        TestSetup.AddProduct(db, sku: "SKU-INACTIVE", stock: 2, isActive: false);

        var result = await service.GetLowStockAsync(10);

        Assert.Single(result);
        Assert.Equal("SKU-ACTIVE", result[0].Sku);
    }

    [Fact]
    public async Task GetLowStock_SoldQuantity_ExcludesCancelledAndOlderThan30Days()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        var customer = TestSetup.AddCustomer(db);
        var product = TestSetup.AddProduct(db, sku: "SKU-SOLD", stock: 4);

        db.Orders.AddRange(
            // 近期、非 Cancelled → 計入（2）
            new Order
            {
                CustomerId = customer.Id,
                Status = OrderStatus.Confirmed,
                CreatedAt = DateTime.UtcNow,
                Items = { new OrderItem { ProductId = product.Id, Quantity = 2, UnitPriceSnapshot = 100m } }
            },
            // 近期、Cancelled → 排除
            new Order
            {
                CustomerId = customer.Id,
                Status = OrderStatus.Cancelled,
                CreatedAt = DateTime.UtcNow,
                Items = { new OrderItem { ProductId = product.Id, Quantity = 5, UnitPriceSnapshot = 100m } }
            },
            // 40 天前、非 Cancelled → 排除（超出 30 天視窗）
            new Order
            {
                CustomerId = customer.Id,
                Status = OrderStatus.Confirmed,
                CreatedAt = DateTime.UtcNow.AddDays(-40),
                Items = { new OrderItem { ProductId = product.Id, Quantity = 7, UnitPriceSnapshot = 100m } }
            });
        db.SaveChanges();

        var result = await service.GetLowStockAsync(10);

        var row = Assert.Single(result);
        Assert.Equal(2, row.SoldLast30Days);
    }
}
