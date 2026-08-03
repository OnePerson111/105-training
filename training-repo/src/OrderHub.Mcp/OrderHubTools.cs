using ModelContextProtocol.Server;
using OrderHub.Core.Domain;
using OrderHub.Core.Interfaces;
using OrderHub.Core.Services;
using System.ComponentModel;
using System.Text.Encodings.Web;
using System.Text.Json;

/// <summary>
/// 對 agent 開放的工具。角色等同 Controller：只轉接 service / repository 結果，
/// 不放商業邏輯、不碰 DbContext。
/// 放在全域命名空間，配合 Program.cs 的 WithTools&lt;OrderHubTools&gt;()。
///
/// 標註（annotations）說明：SDK 的 ReadOnly 預設是 false，唯讀工具不標等於向 client 宣告
/// 「我可能會改東西」，所以三個查詢工具一律明確標 ReadOnly = true。
/// 注意標註只是給 client 的提示，不是強制——真正的規則檢查在 service 層。
/// </summary>
[McpServerToolType]
public class OrderHubTools
{
    // UnsafeRelaxedJsonEscaping 讓中文不被轉成 \uXXXX，方便閱讀與除錯
    private static readonly JsonSerializerOptions Json = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true
    };

    private readonly IOrderService _orderService;
    private readonly IProductRepository _productRepository;

    public OrderHubTools(IOrderService orderService, IProductRepository productRepository)
    {
        _orderService = orderService;
        _productRepository = productRepository;
    }

    [McpServerTool(ReadOnly = true),
     Description("依訂單編號查詢訂單，含客戶、品項、單價快照、會員折扣與應付總額")]
    public async Task<string> GetOrder([Description("訂單 Id")] int id)
    {
        var order = await _orderService.GetOrderAsync(id);
        if (order is null)
            return $"找不到訂單 {id}";

        // 一律投影成匿名物件：Order ↔ Customer 是循環參照，直接序列化 entity 會在執行期炸掉
        var result = new
        {
            order.Id,
            order.CreatedAt,
            Status = order.Status.ToString(),
            Customer = order.Customer is null ? null : new
            {
                order.Customer.Id,
                order.Customer.Name,
                Tier = order.Customer.Tier.ToString()
            },
            Items = order.Items.Select(i => new
            {
                i.ProductId,
                i.Product?.Sku,
                i.Product?.Name,
                i.Quantity,
                i.UnitPriceSnapshot,
                LineTotal = i.UnitPriceSnapshot * i.Quantity
            }),
            // 金額一律問 OrderService，不在工具裡重算折扣
            Subtotal = _orderService.CalculateSubtotal(order),
            DiscountRate = _orderService.GetDiscountRate(order.Customer?.Tier ?? CustomerTier.Standard),
            Total = _orderService.CalculateTotal(order)
        };
        return JsonSerializer.Serialize(result, Json);
    }

    [McpServerTool(ReadOnly = true),
     Description("列出庫存低於門檻且仍在販售的商品，依庫存量升冪排序")]
    public async Task<string> LowStock([Description("庫存門檻，預設 10")] int threshold = 10)
    {
        if (threshold < 1)
            return $"庫存門檻必須大於 0，收到的是 {threshold}";

        // 重用 /Products/LowStock 走的同一個 repository 方法，門檻篩選在 SQL 裡完成，
        // 不要在工具裡自己 Where 一遍，否則規則改版會出現兩種答案
        var products = await _productRepository.GetLowStockActiveAsync(threshold);
        var items = products.Select(p => new { p.Sku, p.Name, p.StockQuantity });
        return JsonSerializer.Serialize(items, Json);
    }

    [McpServerTool(ReadOnly = true),
     Description("查詢某位客戶的全部訂單摘要（編號、日期、狀態、應付總額）")]
    public async Task<string> CustomerOrders([Description("客戶 Id")] int customerId)
    {
        var orders = await _orderService.GetCustomerOrdersAsync(customerId);
        var result = orders.Select(o => new
        {
            o.Id,
            o.CreatedAt,
            Status = o.Status.ToString(),
            Total = _orderService.CalculateTotal(o)
        });
        return JsonSerializer.Serialize(result, Json);
    }

    [McpServerTool(Destructive = true, Idempotent = false),
     Description("取消一筆訂單（僅限待處理/已確認狀態），品項庫存會自動回補。此操作會修改資料，無法還原")]
    public async Task<string> CancelOrder([Description("要取消的訂單 Id")] int id)
    {
        // 狀態檢查與庫存回補都在 OrderService.CancelOrderAsync 裡，這裡只做轉接。
        // 失敗是 ServiceResult 表達的預期內結果，不要包成 exception 丟給 agent。
        var result = await _orderService.CancelOrderAsync(id);
        return result.Success
            ? $"訂單 {id} 已取消，庫存已回補"
            : $"取消失敗：{result.ErrorMessage}";
    }
}
