namespace OrderHub.Core.Services;

/// <summary>
/// 低庫存查詢結果：商品基本資訊加上近 30 天售出數量（排除 Cancelled 訂單）。
/// </summary>
public record LowStockProduct(string Sku, string Name, int StockQuantity, int SoldLast30Days);
