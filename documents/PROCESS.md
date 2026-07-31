# PROCESS.md — 我的練習心得

> 一個原則：**寫「具體發生的事」，不寫感想文。**
> 貼上當時真實的 prompt、真實的數字、真實的錯誤訊息——三個月後的你（和你的同事）才用得上。

#### 使用的 agent 與模型：Claude Code Opus 4.8 High

---

## 通用四問

### 1. 我的任務拆解

（開工前你把任務拆成哪幾步？實際做的時候順序有變嗎？為什麼變？）

- 剛開始先 /init，把 CLAUDE.md 產出來，確認技術棧、慣例、禁止事項都正確。遵循reduce-token-usage.md 的建議 和 利用prompt-caching。

### 2. AI 幫上大忙的地方

（哪件事 agent 做得又快又好？**貼上當時的提問原文**，說明為什麼這樣問有效。）

- **環境設定與驗證一次到位**。提問原文：「setup the repo and db first」。這句雖短，但 agent 自己拆成：查 `dotnet --list-sdks` → 查 SQL Server 服務狀態 → 測 `localhost` 連線 → `dotnet build` → `dotnet run` 觸發自動 migrate+seed → 用 sqlcmd 核對種子筆數 → curl 打 `/Orders` 確認 HTTP 200。
- 為什麼有效：我沒有只問「幫我跑起來」，而是點名「repo **和 db**」，agent 就把「資料庫真的建好了嗎」當成獨立驗收項，主動去數 `Customers 20 / Products 50 / Orders 200 / OrderItems 501`（對得上 README 寫的 20/50/200），而不是看到 `Now listening` 就宣稱完成。

### 3. AI 誤導我的地方，與我如何發現

（agent 說錯／改錯／過度自信的時刻。你靠什麼抓到——對照程式碼？頁面實測？跑測試？）

### 4. 我會帶回日常工作的一招

（一個具體、可複製的做法，不要寫「要多驗證」這種口號——寫出**操作步驟**。）
1. 新repo一定要 `/init` 產 CLAUDE.md，**先讓 agent 讀懂專案**，再開始問它問題。這樣它才不會每次都重頭摸索，省下很多 token。
2. Plan & Review：**先讓 agent 產出計畫，再自己核對、再放行**。
3. 修bug時，**先在頁面上重現症狀、再把具體觀察告訴 agent**，而不是只貼客訴原文。這樣它才不會亂猜。
4. hooks：**把重複流程做成 skill / hook**，例如 `fix-bug`、`writing-plans`，讓 agent 自己去跑，省下 prompt token。

## 自我驗證（做到哪個階段答哪題）

### 第一階段 — Agentic Coding

練習 1 — ✅ 完成

1. ✅ 我能不看筆記說出三個專案（Web/Core/Infrastructure）各自的職責
2. ✅ 我核對過 agent 描述的建單流程，且至少找出一處不精確或過度簡化的說法
3. ✅ 我知道商業邏輯應該放在哪一層、新增頁面要動哪些地方

練習 2 — ✅ 完成

1. ✅ 三個 bug 我都先在頁面上重現過，才開始找程式
2. ✅ 我給 agent 的資訊包含具體觀察（頁碼／金額數字／庫存數字），而不是只貼客訴原文
3. ✅ 每個修復都回到頁面驗證過症狀消失
4. ✅ 每個 bug 都補了一個回歸測試，dotnet test 全綠
5. ✅ 三個獨立 commit，message 說明症狀與根因
6. ✅ （思考題）為什麼原本的測試沒抓到這三個 bug？
   - Bug 1：舊測試只斷言 `TotalCount`/`TotalPages`，從不看 `Items` 內容。
   - Bug 2：pricing 測試只單獨測 `CalculateTotal`（本身正確），沒有一條走 `CreateOrderAsync`，而第二次折扣正藏在那裡。
   - Bug 3：cancel 測試只斷言狀態轉為 Cancelled，從不斷言庫存有沒有加回。
   共同教訓：**斷言「效果」而非「摘要」**，且要走真正的 service 路徑，不要只單測純函式。

練習 3 — ✅ 完成
1. ✅ `/Products/LowStock` 不帶參數 → 門檻 10 的結果；帶 `?threshold=3` → 結果隨之改變
2. ✅ `?threshold=0`、`?threshold=-1` → 頁面顯示驗證錯誤，不是 500
3. ✅ 售出數量欄位排除了 Cancelled 訂單（可用一筆已取消的訂單驗證）
4. ✅ 停售商品不出現（測試 `GetLowStock_ExcludesInactiveProducts` + repo `Where(p.IsActive && ...)`）。
5. ✅ 程式分層與命名跟既有的 Products 功能一致（請 agent 自我 review 一次，並自己確認）。
6. ✅ 至少 3 個新測試，`dotnet test` 全綠

練習 4 — ✅ 完成

做法：用 writing-plans skill 產出計畫（`documents/plans/2026-07-24-refactor-createorder-validation.md`），選 inline 執行——先跑基準測試 34 綠，再一次原子改動，再跑一次確認仍 34 綠。

1. ✅ 重構後 `dotnet test` 全綠
2. ✅ 我能說出這次重構「改善了什麼、沒有改變什麼」
3. ✅ 我有在 code review 的角度看過 diff（不是 agent 說好就好）

### 第二階段 — MCP

練習 0 — ✅ 完成（接現成的 Playwright MCP，讓 agent 自己建一筆訂單）

1. `browser_navigate` → `/Orders/Create`
2. `browser_snapshot` → 拿到 a11y tree，客戶 20 筆、商品 47 筆選項都在裡面，
   而且選項文字自帶庫存與單價（`SKU-1002 極光 機械鍵盤（庫存 102，NT$ 2,320.00）`）
3. 客戶選「陳志明（金卡會員）」——故意挑金卡，這樣折扣算錯的話會被看出來
4. 第一列：`SKU-1002 極光 機械鍵盤` × 2
5. 點「新增一列」→ 第二列 `SKU-1013 星河 27吋螢幕` × 1
   （順便驗到 `Create.cshtml` 那段 clone + reindex JS 是好的：submit 後 `Lines[1]` 有正確綁上）
6. 「送出訂單」→ 302 到 `/Orders/Details/204`，綠色 alert「訂單 #204 建立成功」

**結果頁數字（截圖：`documents/activities/screenshots/activity2-ex0-order-204-details.png`）：**

| 項目 | 數字 |
|---|---|
| SKU-1002 × 2 @ NT$ 2,320.00 | NT$ 4,640.00 |
| SKU-1013 × 1 @ NT$ 340.00 | NT$ 340.00 |
| 小計 | NT$ 4,980.00 |
| 會員折扣（金卡 10%） | -NT$ 498.00 |
| 應付總額 | **NT$ 4,482.00** |

我自己用下拉選項裡的單價算過一次：`2320×2 + 340×1 = 4980`、`4980×0.9 = 4482`，對得上。
另外回 `/Orders` 確認 #204 真的在第一頁（不只信 redirect），`browser_console_messages` 0 error。

**驗證方式：**

- ✅ agent 能自己開瀏覽器完成操作並回傳截圖
- ✅ 對比活動 1 練習 2（詳見下面兩張對照表）

### 對比活動 1 練習 2：人工重現 → agent 自己做

**當時三個 bug 我是怎麼人工重現的**（根因見上面練習 2 第 6 題）：

| Bug | 症狀 | 當時的人工動作 |
|---|---|---|
| 1 分頁 | `Items` 內容不對（第 2 頁重複／漏筆） | 開 `/Orders`，**手動點分頁**，肉眼比對第 1、2 頁的訂單編號有沒有重疊 |
| 2 折扣算兩次 | 建單後總額比預期少了一次折扣 | **手動在 `/Orders/Create` 建一筆單**，抄下單價×數量，自己乘一遍，跟 Details 頁總額對帳 |
| 3 取消不回庫存 | 取消訂單後庫存沒加回 | 先去 `/Products` 抄下庫存 → 建單 → Details 頁按取消 → **再回 `/Products` 看數字有沒有變回來** |

三件事的共同結構都是：**開頁面 → 操作 → 抄下畫面數字 → 換頁 → 再抄一次 → 自己比對**。

**這次建 #204 用到的工具，正好一格一格對上：**

| 人工動作 | 對應的工具呼叫 |
|---|---|
| 開頁面、換頁 | `browser_navigate` |
| 看畫面上有什麼、有哪些選項 | `browser_snapshot`（a11y tree，不是圖片——數字是**文字**，可直接比對） |
| 選客戶／選商品／填數量 | `browser_select_option`、`browser_type` |
| 按「新增一列」「送出訂單」 | `browser_click` |
| 留證據給人看 | `browser_take_screenshot` |
| 「頁面有沒有壞掉」 | `browser_console_messages`（**以前我根本不會做這步**，要自己開 F12） |

所以 bug 3 那個「抄庫存 → 建單 → 取消 → 再回去看庫存」的來回，現在是四五個工具呼叫、agent 一輪跑完。

**但便宜的只有「重現」，不是「判斷」。**
snapshot 只是忠實把畫面唸出來。頁面顯示 `NT$ 4,482.00`，agent 完全可以照抄這個數字說「總額正確」——
但那句話沒有驗證力，因為 **bug 2 的症狀本來就是「頁面顯示一個錯的數字」**。
用錯的數字驗證錯的數字，永遠會綠。

這次我仍然是自己從下拉選項的單價（`2,320` / `340`）算 `2320×2+340×1=4980`、`4980×0.9=4482` 才敢說對。
這跟練習 3 那個「agent 自己寫的測試也要被挑」是同一件事：**期望值必須來自頁面以外的地方**。

一句話收斂：**操作外包給 agent，期望值留在自己手上。**

---

練習 1 — ✅ 完成（自己造一個 OrderHub MCP Server，提供 3 個唯讀工具）

成品：`src/OrderHub.Mcp`，透過 stdio 提供 `get_order`、`low_stock`、`customer_orders`。
（方法名 SDK 會自動轉 snake_case：`GetOrder` → `get_order`，所以 agent 看到的不是我寫的 PascalCase。）

#### 1a 有兩個沒發現的洞

`dotnet new console` + `dotnet add reference` 跑完後看起來沒事，但實際上：

| 問題 | 症狀 | 處理 |
|---|---|---|
| `ProjectReference` 沒進 csproj | `Program.cs` 引用 `OrderHub.Core.*` 會編不過 | 手動補兩個 `ProjectReference` |
| TFM 是 `net10.0` | 其餘專案全是 `net8.0`（CLAUDE.md 也寫 .NET 8） | 目前保留 net10.0——能 build（net10 可以參照 net8），但不一致 |

教訓：**指令跑完不代表結果正確，要打開 csproj 看一眼。**

#### 我對文件範本做的兩處調整

1. `LowStock` 改呼叫現成的 `IProductRepository.GetLowStockActiveAsync(threshold)`，
   不是範本的「拉全部商品 → 記憶體 `Where(StockQuantity < threshold)`」。
   語意一樣，但篩選在 SQL 完成，也**不用在工具裡重寫一份門檻規則**。
2. 加 `threshold < 1` 防呆，回文字訊息（對齊練習 3 那條「輸入錯誤不能變成 500」）。

#### build 過 ≠ 能跑，所以我打了真的 JSON-RPC

`OrderHubTools` 只有在工具**被呼叫時**才被 DI 容器建出來，`dotnet build` 驗不到這件事。
所以我直接對 stdio 發 `initialize` → `tools/list` → `tools/call`：

| 請求 | 結果 |
|---|---|
| `tools/list` | 三個工具都列出，description／參數說明如我所寫 |
| `low_stock(5)` | 5 筆，庫存升冪：SKU-1048(2)、SKU-1005(3)、SKU-1023(3)、SKU-1032(4)、SKU-1014(4) |
| `get_order(204)` | Subtotal `4980.00`、DiscountRate `0.10`、Total `4482.00` |
| `get_order(999999)` | `找不到訂單 999999`——清楚訊息，不是 exception dump |

stderr 零 error／exception。

**兩個交叉驗證**（重點是「不用自己驗自己」）：

- `get_order(204)` 的數字跟練習 0 瀏覽器截圖**一字不差**（4,980 / 10% / 4,482）。
  同一筆訂單、兩條路徑（Razor 頁面 vs MCP 工具）、同一組數字——如果工具偷寫一份折扣，這裡就會分岔。
- `low_stock(5)` 那 5 筆的庫存值，跟練習 0 建單頁下拉選項讀到的數字一一吻合。

#### 逐項對照文件的「地雷區」

**地雷 1：stdout 絕對不能印東西** — ✅ 符合

| 檢查 | 結果 |
|---|---|
| 工具／`Program.cs` 有 `Console.Write*` | 無 |
| **Core / Infrastructure 有嗎** | 無——這點要特地查，依賴專案裡任何一行 `Console.WriteLine` 一樣會毀掉協定 |
| log 導向 | `LogToStandardErrorThreshold = LogLevel.Trace`，全部走 stderr |
| 實測 | stdout 5 行全部 `JSON.parse` 通過，都是合法 JSON-RPC；`info:` log 全在 stderr |

後半句「**stdin 不能立刻關閉**」我實測踩到了：用 pipe 餵一則 `initialize` 後 EOF，
stderr 顯示 handler **16ms 就處理完**，但 stdout **完全空的**——回應還沒 flush，server 已經收工。
改成「stdin 保持開著、讀完回應才 close」就正常。這句警告不是理論。

⚠️ **還沒爆的風險**：練習 3 的 `.mcp.json` 用 `dotnet run`，它的建置訊息會印到 **stdout**，
正好踩回地雷 1（尤其第一次要建置時）。**如果練習 3 agent 連不上，先查這裡。**

**地雷 2：entity 直接序列化會因循環參照在執行期炸掉** — ✅ 符合

文件只講一個迴圈，實際有**兩個**：

```
Order.Customer → Customer.Orders → Order      ← 文件說的
Order.Items    → OrderItem.Order → Order      ← 文件沒提到
```

我寫了最小重現，用同樣形狀實際炸一次：

```
A) 直接序列化 entity -> JsonException: A possible object cycle was detected...
   Path: $.Customer.Orders.Customer.Orders...（堆到深度 64）
B) 投影匿名物件      -> {"Id":204,"Customer":{"Name":"陳志明"}}
```

A 那段**編譯完全沒問題**——這就是「編譯過 ≠ 能跑」的具體長相。
三個工具全部投影成匿名物件，而 `get_order(204)` 是**兩個迴圈都在場**（同時載入 Customer 和 Items）
仍然正常回傳，所以兩條路都避開了。

**地雷 3：金額別自己算** — ✅ 符合

| 檢查 | 結果 |
|---|---|
| 工具裡有折扣數字或算式（`0.1`、`* 0.9`、`/100`） | grep **零命中** |
| 三個金額來源 | 全部問 service：`CalculateSubtotal` / `GetDiscountRate` / `CalculateTotal` |
| tier 預設值 | `?? CustomerTier.Standard`，與 `OrdersController.cs:138` 一致 |
| 交叉驗證 | 4,980 / 0.10 / 4,482 與網頁完全相同（見上） |

誠實註記：`LineTotal = UnitPriceSnapshot * Quantity` 確實是工具裡的算式，
但 `OrdersController.cs:155` 是一模一樣的一行，屬既有展示層慣例，**且不是折扣規則**（單價×數量沒有版本問題）。
判斷不算違反；若要更嚴應往 service 收，但那是偏離現有慣例的改動，不該在這個練習順手做。

**地雷區沒寫、但同型的一個問題**：範本的 `LowStock` 記憶體篩選，
跟地雷 3 是**同一種錯，只是換成庫存規則**——門檻邏輯已有一份，工具再寫一份，
日後把 `<` 改成 `<=` 就會出現兩種答案。這就是我上面調整它的理由。

#### 我自己的測試腳本也踩了兩個坑

1. **stdin 一 EOF 就收不到回應**（同地雷 1 後半句）。
2. **回應會亂序**：我原本讀到「最後一個送出的 id」就 break，結果兩則比較慢的
   （`low_stock`、`get_order(204)`）被丟掉，只收到 id 1、2、5。
   server 是**並行處理**的，要按「收到幾則」收，不能假設順序。

值得記的一點：**這兩個都是我的測試工具錯了，不是 server 錯了。**
如果當時直接下結論「工具壞了」，就會跑去改沒問題的程式。

#### 一處要修正我先前的理解

`UnsafeRelaxedJsonEscaping` 只作用在**工具自己回傳的那串 JSON**（`get_order` 的中文是乾淨的）。
`tools/list` 裡的 description 仍是 `\uXXXX`——那段是 SDK 序列化協定信封，不受這個選項影響。
所以「中文不被轉義、省 token」只對工具 payload 成立，**不含工具描述**。

#### 驗證方式

- ✅ `dotnet build src/OrderHub.Mcp` 成功（0 errors, 0 warnings）
- ✅ 一個獨立 commit（訊息說明新增了哪些工具）

---

## 附錄：值得留下的對話片段

（貼 1–2 段最有代表性的 prompt 與回應**摘要**——不用貼全文，重點是「我怎麼問」和「它怎麼答」。）

**片段 1 — 用短指令換到完整驗收**
- 我問：「setup the repo and db first」
- 它答（摘要）：沒有直接 `dotnet run` 了事，而是先查 SDK/SQL 服務/連線，build 後 run 觸發 migrate+seed，再用 sqlcmd 回報 `Customers 20 / Products 50 / Orders 200 / OrderItems 501` 並 curl `/Orders` 得 HTTP 200。學到：把驗收目標（「db」）明確寫進指令，agent 會自己補上核對步驟。

**片段 2 — 先計畫、reviewer 補刀**
- 練習 3 我要它「先不要寫程式，給實作計畫並派子代理盤點既有 Products 慣例」，核對後才放行。實作完再叫它以 reviewer 角度看 diff。
- reviewer 回：架構乾淨，但「門檻測試沒有等於門檻的樣本，`<` 改 `<=` 也會過」。我照補 stock=10 樣本並證明它會紅。學到：agent 自己寫的測試也要被另一個 agent（或自己）挑，「測試有沒有真的能抓到 bug」比「測試綠不綠」重要。