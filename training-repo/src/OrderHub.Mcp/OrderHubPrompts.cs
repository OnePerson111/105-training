using Microsoft.Extensions.AI;
using ModelContextProtocol.Server;
using System.ComponentModel;

/// <summary>
/// 對 agent 開放的 Prompt。Prompt 是「範本」：把採購同事每週都要問一次的那段話
/// 放在 server 上共用、進版控，client 端會變成 slash command
/// （/mcp__orderhub__low_stock_report）。
/// 範本本身不查資料，而是引導 agent 去呼叫 tool——prompt 與 tool 的合體。
/// 放在全域命名空間，配合 Program.cs 的 WithPrompts&lt;OrderHubPrompts&gt;()。
/// </summary>
[McpServerPromptType]
public class OrderHubPrompts
{
    [McpServerPrompt(Name = "low_stock_report"), Description("產生低庫存採購建議報告")]
    public static ChatMessage LowStockReport(
        [Description("庫存門檻，預設 10")] int threshold = 10) =>
        new(ChatRole.User, $"""
            請用 low_stock 工具（threshold={threshold}）查出低庫存商品，
            再用其他工具了解這些商品的近期訂單狀況，
            最後輸出採購建議表：SKU、名稱、現有庫存、建議補貨量、理由。
            """);
}
