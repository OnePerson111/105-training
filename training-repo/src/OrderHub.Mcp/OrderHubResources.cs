using ModelContextProtocol.Server;
using System.ComponentModel;

/// <summary>
/// 對 agent 開放的 Resource。Resource 是「資料」而不是「動作」：
/// 折扣規則不需要參數、不用打 DB，是 agent 判讀金額問題時該有的背景知識，
/// 由 client 決定何時放進 context（Claude Code 用 @ 選取）。
/// 放在全域命名空間，配合 Program.cs 的 WithResources&lt;OrderHubResources&gt;()。
///
/// 注意：這裡的折扣數字是寫死的文字，和 OrderService.GetDiscountRate 是兩份真相——
/// 規則改版時兩邊都要改。
/// </summary>
[McpServerResourceType]
public class OrderHubResources
{
    [McpServerResource(UriTemplate = "orderhub://discount-rules",
        Name = "會員折扣規則", MimeType = "text/markdown")]
    [Description("目前生效的會員折扣規則與計算方式")]
    public static string DiscountRules() => """
        # OrderHub 會員折扣規則
        - Standard：不打折
        - Silver：95 折
        - Gold：9 折
        折扣在訂單總額上折抵一次，單價快照（UnitPriceSnapshot）為下單當下原價。
        """;
}
