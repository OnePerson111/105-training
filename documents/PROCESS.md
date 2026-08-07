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

**結果頁數字：**

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

練習 2 — ✅ 完成（用 MCP Inspector 手動測工具）

```powershell
npx @modelcontextprotocol/inspector dotnet run --project src/OrderHub.Mcp
```

裝的是 Inspector **v2.0.0**，啟動後印出帶 token 的網址（`http://localhost:6274?MCP_INSPECTOR_API_TOKEN=...`）。
UI 跟文件寫的「Connect → Tools → List Tools」略有不同：v2 是**左側 server 卡片上的開關**連線，
上方切 `Servers` / `Tools` 分頁；工具列表在連上後自動載入，不用手動按 List Tools。

Inspector 是網頁 UI，所以我讓 agent 用 Playwright MCP 去點——**兩個 MCP 疊在一起用**：
Playwright 開瀏覽器操作 Inspector，Inspector 再去跟我自己寫的 OrderHub MCP 講話。

#### 驗證方式

- ✅ **三個工具都列得出來，description、參數說明如我所寫**
  `customer_orders` / `get_order` / `low_stock` 都在。
  `low_stock` 的說明顯示「列出庫存低於門檻且仍在販售的商品，依庫存量升冪排序」，
  參數 `threshold` 顯示「庫存門檻，預設 10」，而且**表單預先填了 10**——
  C# 的 `int threshold = 10` 預設值被 SDK 寫進 schema 了。
  `get_order` 的 `id` 標成 `id *`（必填、沒有預設），也對。

- ✅ **`low_stock(10)` 回傳的商品和 `/Products/LowStock` 頁面一致**
  六筆、順序與庫存數字完全相同：

  | # | Inspector `low_stock(10)` | `/Products/LowStock` 頁面 |
  |---|---|---|
  | 1 | SKU-1048 晨光 行動電源 2 | SKU-1048 晨光 行動電源 2 |
  | 2 | SKU-1005 極光 筆電支架 3 | SKU-1005 極光 筆電支架 3 |
  | 3 | SKU-1023 雲峰 27吋螢幕 3 | SKU-1023 雲峰 27吋螢幕 3 |
  | 4 | SKU-1032 曜石 機械鍵盤 4 | SKU-1032 曜石 機械鍵盤 4 |
  | 5 | SKU-1014 星河 USB-C 集線器 4 | SKU-1014 星河 USB-C 集線器 4 |
  | 6 | SKU-1001 極光 無線滑鼠 5 | SKU-1001 極光 無線滑鼠 5 |

  唯一差異是**欄位**不是資料：頁面多一欄「近 30 天售出」，工具沒吐。
  那個欄位要走 `IProductService.GetLowStockAsync`（會 join 訂單）；
  我的工具走 `IProductRepository.GetLowStockActiveAsync`，只到商品層。
  商品清單一致，但如果哪天 agent 需要「賣得快又快缺貨」的判斷，這欄就得補。

- ✅ **`get_order(999999)` 回清楚訊息，不是 exception dump**
  回傳 `找不到訂單 999999`，而且 server log 寫 `"get_order" completed. IsError = False.`
  ——它是一個**正常的文字結果**，不是協定層的錯誤。這是刻意的：
  查無資料不是「壞掉」，不該讓 agent 收到一坨 stack trace。

#### Inspector 的 Server Console 給了兩個 build／單測拿不到的證據

Inspector 有個 **Server Console** 分頁，專門收 server 的 **stderr**。

**證據一：地雷 1 真的沒踩到。** 從啟動到三次呼叫，所有 `info:` log（Hosting、StdioServerTransport、
McpServer、EF Core）**全部出現在 Console 分頁**，協定通道一路乾淨，連線正常。
這比我自己 grep「有沒有 Console.WriteLine」強——這是**執行時**的證明。

**證據二：`low_stock` 的門檻篩選確實在 SQL 裡。** Console 印出 EF 產生的真實 SQL：

```sql
SELECT ... FROM [Products] AS [p]
WHERE [p].[IsActive] = CAST(1 AS bit) AND [p].[StockQuantity] < @__threshold_0
ORDER BY [p].[StockQuantity]
```

我在練習 1 把範本的「拉全部商品再記憶體篩選」改成呼叫現成的 repository 方法，
這行 SQL 就是那個改動有生效的**直接證據**（`WHERE` 和 `ORDER BY` 都下到資料庫了）。
光看程式碼只能說「我改了」，看到這行才能說「它真的這樣跑」。

順帶記下三個耗時：`tools/list` 17ms、`low_stock` 首呼 2,284ms（含 EF 暖機）、`get_order` 246ms。

#### 關於「練習 3 的 `dotnet run` 會踩地雷 1」——這次沒爆，但原因要記清楚

我在練習 1 標了這個風險。實測**連線成功**，原因是：**專案已經 build 過了，所以 `dotnet run` 沒有東西可印到 stdout**。
但連線花了 **15.3 秒**，那就是 `dotnet run` 的檢查／啟動成本。

所以這個地雷沒有消失，只是這次剛好沒引爆。**第一次跑、或改完程式碼還沒 build 時風險最高**。
真的出事的話，改用 `dotnet run --no-build`（或直接指向編好的 dll）就能繞開。

#### 為什麼這個練習要排在「接給 agent」之前

Inspector 是**不含模型**的測試通道：我直接對工具送參數、直接看回傳。
所以工具有問題時，我不會誤判成「模型很笨、不會用工具」。

先確定工具是對的，再去評斷 agent 用得好不好——這跟修 bug 時「先在頁面重現、再找程式」是同一個順序。

---

練習 3 — ✅ 完成（接給 Claude Code，並做「關掉 MCP／開啟 MCP」對照）

#### before：沒有 orderhub 工具，「哪些商品庫存低於 5?」到底怎麼讀

這節記的是**這次真的重跑一遍**的兩條路，不是拿練習 1、2 的舊紀錄湊。
兩條都不經過 MCP，答案都必須從系統外部拿到（DB 或 HTTP），不是從我的 context 拿。

⚠️ **一個必須講清楚的限制**：我的 context 已經知道答案（同 session 前面呼叫過 `low_stock`）。
所以下面量到的是**機械成本**——幾次呼叫、踩幾個坑、要先知道什麼；
**不是盲測的探索成本**，因為我知道要往哪裡瞄。真正的盲測必須開新 session。
但機械成本本身就足以看出差距，而且每個錯誤訊息都是真的。

**路徑 A：繞過應用程式，直接下 SQL** —— 六次呼叫，兩次失敗

| # | 動作 | 結果 |
|---|---|---|
| 1 | 讀 `src/OrderHub.Web/appsettings.json` | 拿到 `Server=localhost;Database=OrderHubTraining;Trusted_Connection=True` |
| 2 | grep `Product.cs` | 確認欄位是 `Sku` / `Name` / `StockQuantity` / `IsActive`（不是 `SKU`、不是 `Stock`）；表名還得猜是複數 `Products` |
| 3 | `sqlcmd -E -i <絕對路徑>` | ❌ `Sqlcmd: -E 与 -U/-P 选项互斥`（環境裡並沒有 `SQLCMDUSER`，我印出來確認過是空的） |
| 4 | 去掉 `-E`，`-i` 仍給絕對路徑 | ❌ `Sqlcmd: 打开文件 C: 时出错（原因: 拒绝访问）`——它把 `C:` 當成檔名 |
| 5 | `cd` 進目錄 + `-Q` 內嵌 SQL + `-o` 導檔 + `-f 65001` | ✅ exit=0 |
| 6 | 讀輸出檔 | 才看到答案 |

我下的 SQL：

```sql
SELECT Sku, Name, StockQuantity FROM Products
WHERE StockQuantity < 5 AND IsActive = 1
ORDER BY StockQuantity, Sku;
```

**路徑 B：打現成的網頁，從 HTML 裡挖** —— 三次呼叫，一次判斷失誤

| # | 動作 | 結果 |
|---|---|---|
| 1 | `curl "http://localhost:5150/Products/LowStock?threshold=5"` | 5,249 bytes HTML |
| 2 | `grep -c "<tr>"` 想數筆數 | ❌ 回 **1**。因為資料列是 `<tr class="table-danger">`，只有表頭是裸 `<tr>`——**差點誤判成「只有一筆」** |
| 3 | `sed` 切出 `<main>` + `perl` 反解 `&#xXXXX;` | ✅ 5 筆，連「近 30 天售出」都有 |

路徑 B 明顯比 A 短，但代價換了位置：

- **網站必須跑著**（我這次剛好還開著 5150；不然要先 `dotnet run`，又是一輪）
- **答案埋在 HTML 裡**：商品名稱是 `&#x6668;&#x5149;` 這種 escape，要反解才讀得懂
- **編碼是混的**：Razor 樣板裡的靜態中文（`低庫存警示`）是原生 UTF-8，而變數輸出是 `&#x..;` escape。
  我用 `perl -CO` 反解時，把原生那段弄成亂碼（`ä½åº«å­è­¦ç¤º`）——**同一份 HTML 兩種編碼狀態**
- **`<tr>` 那次失誤最值得記**：我對 HTML 結構的假設是錯的，而錯法是「靜靜回一個看起來合理的數字」。
  如果我沒有繼續往下挖，就會回報「只有 1 筆商品低於 5」——**錯得毫無徵兆**

**兩條路的共同點**：答案拿得到，但都得先自己架一個管道（連上 DB／起網站），
而且**都要自己把「低庫存」的定義重寫一次**——A 寫在 SQL 的 `WHERE`，B 寫在「`?threshold=5` 這個參數該填什麼」。

#### after：有 orderhub 工具時，同一個問題

問題原文：「哪些商品庫存低於 5?」

**一次工具呼叫就結束**：`low_stock(threshold=5)` → 5 筆，庫存升冪。
沒有前置條件、沒有失敗重試、沒有 HTML、沒有編碼問題。
「低於門檻」和「只算販售中」兩條規則都在工具裡（走 `GetLowStockActiveAsync`），我不用重寫。

#### 對照表：過程指標（重點在這裡）

| 指標 | before A：sqlcmd 直查 DB | before B：curl 網頁挖 HTML | after：`low_stock(5)` |
|---|---|---|---|
| 工具呼叫次數 | **6** | **3** | **1** |
| 失敗／誤判 | 2 次失敗 | 1 次誤判（`<tr>` 數成 1 筆） | 0 |
| 我得先知道什麼 | 連線字串在哪個檔、表名、四個欄位拼法、sqlcmd 參數、UTF-8 要 `-f 65001` | 網址與參數名 `threshold`、HTML 結構、怎麼反解 `&#x..;` | 工具名 + `threshold` |
| 前置條件 | SQL Server 要在跑、要有 sqlcmd | **網站要跑著** | client 自己 spawn server |
| 「低庫存」規則 | **自己在 SQL 重寫** `StockQuantity < 5 AND IsActive = 1` | 靠頁面實作，但門檻參數要自己給對 | 工具內建，走 repository |
| 中文 | 直接印會亂碼，要導檔 + 指定 codepage | escape 與原生 UTF-8 混在同一份 HTML | 直接可讀 |
| 排序 | 自己寫 `ORDER BY` | 靠頁面 | 內建升冪 |
| 資料形狀 | 表格文字，要自己切欄 | HTML，要自己剝標籤 | 已經是結構化 JSON |

**多出來的一欄**：路徑 B 拿得到「近 30 天售出」（2/0/17/22/18），工具沒有——
這跟練習 2 記過的原因一樣（頁面走 `IProductService.GetLowStockAsync` 會 join 訂單，
我的工具走 `IProductRepository.GetLowStockActiveAsync` 只到商品層）。
所以「工具比較好」不是全面的：**工具只吐它被設計要吐的東西**。

三條路答案完全一致（也對得上練習 1 stdio 實測與練習 2 Inspector 的紀錄）：

| Sku | Name | Stock |
|---|---|---|
| SKU-1048 | 晨光 行動電源 | 2 |
| SKU-1005 | 極光 筆電支架 | 3 |
| SKU-1023 | 雲峰 27吋螢幕 | 3 |
| SKU-1032 | 曜石 機械鍵盤 | 4 |
| SKU-1014 | 星河 USB-C 集線器 | 4 |

唯一差別是**同分排序**：兩筆庫存 4 的先後不同。
工具和網頁都是 1032 在前，我的 SQL 多加了 `, Sku` 當 tiebreak 所以 1014 在前。
資料相同，不是 bug——但順帶驗到一件事：`ORDER BY StockQuantity` 沒有 tiebreak 時，**同分順序不保證**。

#### 這次對照最值得記的一點

不是「6 次 / 3 次 vs 1 次」，是對照表的**「低庫存」規則**那列：
沒有工具時，我為了回答問題，在一句臨時 SQL 裡把「低庫存」的定義（`< 門檻` 且 `IsActive`）**又寫了一遍**。

這跟練習 1 我批評範本 `LowStock` 記憶體篩選、跟地雷 3「金額別自己算」是**同一種錯**。
差別只在：那兩次是寫進 repo 的重複，這次是寫在一次性查詢裡的重複——
**後者更難發現，因為它不留在程式碼裡**，沒有人會 review 它，也沒有測試蓋它。

所以 MCP 工具省的不只是呼叫次數，是「**每個想問這個問題的人都得重新猜一次規則**」。

#### 又一次「不熱插拔」——這次是反方向

前面記過兩次「加了 server 要重啟才生效」。這次是同一條規則的反面：
**把 `.mcp.json` 改名成 `.mcp.json.off`，正在跑的 session 依然叫得動 `low_stock`。**

（時間點我無法完全確定：我發現檔案已經是 `.off` 時，成功那次呼叫已經發生了。
但結論與已知規則一致——連線在 CLI 啟動時建立，之後動檔案不影響它。）

**所以要做乾淨的 before，改名不夠，必須開新 session。** 這也是為什麼上面那條 before 只能算機械成本。

還有一個新證據：**我手動殺掉的 MCP server，client 會自己再 spawn 一個。**

| 時間點 | `dotnet run` PID | `OrderHub.Mcp.exe` PID |
|---|---|---|
| 我殺之前 | 416 | 18272 |
| 殺掉後、`low_stock` 成功時 | 22728 | 10048 |

跟前面那條「client 死掉、server 不一定死」正好配成一對：**server 死掉，client 會補一個。**
兩邊的生命週期都不是直覺以為的那樣，所以「現在在講話的是哪個實例」要用 PID 確認，不能靠推測。

#### 附帶踩到的：MSB3021 又來了一次，這次鎖檔的是 `.mcp.json` spawn 的 server

前面「停掉 Inspector 沒殺掉子程序」那節記過一次。這次一模一樣的錯誤訊息再出現：

```
error MSB3021: 無法將 OrderHub.Core.dll 複製到 bin\Debug\net10.0\...
The process cannot access the file ... because it is being used by another process.
```

差別是這次鎖檔的不是 Inspector 的殘影，而是 **`.mcp.json` 裡 `dotnet run` 起來的那個 server 本體**——
它是 Claude Code 的子程序，會活整個 session，所以**只要 session 開著就不能 build `OrderHub.Mcp`**。

| PID | 程序 |
|---|---|
| 416 | `dotnet run --project src/OrderHub.Mcp` |
| 18272 | **`OrderHub.Mcp.exe`** ← 鎖著 `bin\Debug\net10.0\OrderHub.Core.dll` |

殺掉兩支後 build 立刻成功（3.72 秒）。
但代價是**工具當場斷線**，而且如上所述，client 稍後又 spawn 了一個新的（22728 / 10048）。

這是練習 1 標的那個 `dotnet run` 風險**換一種方式引爆**：
地雷 1（建置訊息汙染 stdout）沒爆，但同一個 `dotnet run` 造成了 build 鎖檔。
真要避開，`.mcp.json` 應該指向已編好的 exe／dll，而不是 `dotnet run`——
這樣 server 跟 build 就不會共用同一個輸出目錄。

#### 驗證方式

- ✅ orderhub server 連上、工具可用 —— 證據是**真的呼叫成功**（`low_stock(5)` 回 5 筆），
  比 `/mcp` 畫面列出來更強。（`/mcp` 的 UI 我沒看，那要互動視窗；三個工具「列得出來」在練習 2 已用 Inspector 驗過。）
- ✅ 對照實驗完成且記錄（before 兩條路徑實測 + after 一條，過程指標見上表）
- ✅ `.mcp.json` 進 git，一個獨立 commit（`3f5e34f`）

---

練習 4 — ✅ 完成（第一個會改資料的工具：`cancel_order`）

前三個工具全是唯讀的，agent 用錯頂多答錯。這個會**真的改資料庫**。

工具本體只有 4 行——狀態檢查與庫存回補全在 `OrderService.CancelOrderAsync`（`OrderService.cs:92`），
工具只轉接，一行規則都不重寫。這跟練習 1 把 `LowStock` 改成呼叫現成 repository 是同一條原則。

#### 標註的預設值：範本裡有兩個是 no-op

範本寫 `[McpServerTool(Destructive = true, Idempotent = false)]`。
照抄之前我去翻了 SDK 2.0.0 的 `ModelContextProtocol.Core.xml`，查每個屬性的**實際預設值**：

| 屬性 | 預設 | 標了會怎樣 |
|---|---|---|
| `Destructive` | **`true`** | 範本這個標註**等於沒標**——預設就是 true |
| `Idempotent` | `false` | 同上，也是預設值 |
| `ReadOnly` | `false` | ⚠️ **這個才有用**：唯讀工具不標＝向 client 宣告「我可能會改東西」 |
| `OpenWorld` | `true` | 預設宣告「會碰不可預測的外部實體」（我們四個都只打自家 DB，其實不符） |

所以這題真正改變 client 行為的**不是**範本那兩個標註，而是回頭補在三個唯讀工具上的 `ReadOnly = true`。
`OpenWorld` 我這次沒動——語意上四個工具都該是 `false`，但它不影響確認時機，留著當已知落差。

教訓跟練習 1「指令跑完不代表結果正確」同型：**照抄範本 ≠ 知道自己標了什麼。**
兩個 no-op 標註不會出錯，但如果我以為「有標就有效」，那個誤解會跟著我到下一個 server。

#### `tools/list` 實際吐出來的 annotations

| 工具 | readOnlyHint | destructiveHint | idempotentHint |
|---|---|---|---|
| `cancel_order` | (未輸出) | **True** | **False** |
| `customer_orders` | **True** | (未輸出) | (未輸出) |
| `get_order` | **True** | (未輸出) | (未輸出) |
| `low_stock` | **True** | (未輸出) | (未輸出) |

值得記：**等於預設值的欄位 SDK 根本不輸出**。所以「線上看不到 `readOnlyHint: false`」不代表沒宣告，
而是「沒宣告＝預設 false」——client 看到的結果一樣。

#### 四種情況的實測回應

```
cancel_order(1) 第一次   -> 訂單 1 已取消，庫存已回補
cancel_order(1) 第二次   -> 取消失敗：狀態為 Cancelled 的訂單不可取消
cancel_order(2) 已出貨   -> 取消失敗：狀態為 Shipped 的訂單不可取消
cancel_order(999999)    -> 取消失敗：找不到指定的訂單
```

四則的 `isError` **都是未設定**——連失敗的三則也是。這是刻意的：
「這筆不能取消」是**正常的業務結果**，不是協定層錯誤。agent 收到這句話能向使用者解釋並停手；
收到 stack trace 只會瞎猜重試。stdout 7 行全部合法 JSON-RPC，stderr 零 exception。

#### 庫存回補：SQL 與頁面兩邊對帳

訂單 1（Pending，3 個品項）取消前後：

| SKU | 訂購量 | 取消前庫存 | 取消後庫存 | 頁面 `/Products` |
|---|---|---|---|---|
| SKU-1009 | 1 | 42 | **43** | 43 |
| SKU-1032 | 1 | 4 | **5** | 5 |
| SKU-1044 | 2 | 98 | **100** | 100 |

訂單狀態 `0 → 3`（Cancelled）。訂單 2（Shipped）三個品項庫存 86/46/18 **完全沒動**——
證明失敗的那次呼叫真的沒有副作用，不是「報錯但已經改了一半」。

**一個不用自己驗自己的交叉驗證**：SKU-1032 原本庫存 4，是練習 3 那份 `low_stock(5)` 名單的成員。
回補成 5 之後 `5 < 5` 不成立，它**應該掉出名單**。取消後再打一次 `low_stock(5)`：

```
取消前 5 筆：SKU-1048(2)、SKU-1005(3)、SKU-1023(3)、SKU-1032(4)、SKU-1014(4)
取消後 4 筆：SKU-1048(2)、SKU-1023(3)、SKU-1005(3)、SKU-1014(4)
```

一個唯讀工具的輸出，因為另一個工具寫了資料而改變——**兩個工具走同一份真實狀態**，
而不是各自快取一份。這比單看 `cancel_order` 回傳的成功訊息有力。

#### 又踩一個編碼坑：PowerShell 5.1 讀 .ps1 當 ANSI

測試腳本第一版我在 label 裡寫中文，執行直接 parser error：

```
+ @{ Id = 4; Label = 'cancel_order(1)  第二�?;  Args = @{ id = 1 } },
字符串缺少终止符
```

原因：**檔案存成 UTF-8 無 BOM，PowerShell 5.1 會用系統 ANSI（GBK）去讀**，
中文字被拆爛，連帶把單引號吃掉。改成整份腳本純 ASCII 就好（server 回來的中文不受影響，
那是 runtime 的 stdout 編碼，我已經明確設 UTF-8）。

跟練習 1 那兩個坑一樣，**這也是我的測試工具壞了，不是 server 壞了**。第三次了。

#### 我做不到、要你自己做的一項

驗收清單第 2 項「對 agent 說『幫我取消訂單 X』，觀察權限確認提示」——
工具清單是 CLI 啟動時抓的，**不熱插拔**（這條規則本檔已出現第三次），
所以 `cancel_order` 要開新 session 才會進到我的工具清單；我也無法對自己觸發權限提示。

#### 驗證方式

- ✅ `dotnet build src/OrderHub.Mcp` 0 errors / 0 warnings；`dotnet test` **35 綠**
- ✅ annotations 如上表（三個唯讀 `readOnlyHint: true`，`cancel_order` destructive/idempotent 如標）
- ✅ 取消一筆待處理訂單成功，庫存回補：SQL 與 `/Products` 頁面兩邊數字一致
- ✅ 重複取消／已出貨／查無訂單都回清楚中文訊息，非 exception dump，且失敗無副作用
- ⬜ **（待你做）** 新 session 說「幫我取消訂單 X」，確認按允許前資料沒被動到
- ⬜ （選做）Inspector UI 看 annotations——文字證據已用 `tools/list` 取得

---

練習 5 — ✅ 完成（Resource 與 Prompt：MCP 不只有 tools）

新增兩個檔案、Program.cs 接兩行：

```csharp
    .WithTools<OrderHubTools>()
    .WithResources<OrderHubResources>()   // orderhub://discount-rules
    .WithPrompts<OrderHubPrompts>();      // low_stock_report
```

`ChatMessage` 來自 `Microsoft.Extensions.AI`，是 `ModelContextProtocol` 2.0.0 帶進來的**遞移依賴**，
`.csproj` 一個字都不用改。這點先確認再動手，因為 CLAUDE.md 寫著「不要未經同意就加 NuGet 套件」——
範本裡多一個 `using` 不代表要多一個 `PackageReference`。

#### 三個原語的分工

| 原語 | 是什麼 | 誰決定何時用 | 這次的例子 |
|---|---|---|---|
| Tool | **動作**（查、算、改） | agent 自己決定呼叫 | `low_stock`、`cancel_order` |
| Resource | **資料**（讀進 context） | **client／使用者**（`@` 選取） | 會員折扣規則 |
| Prompt | **範本**（替使用者說話） | 使用者（slash command） | `low_stock_report` |

「什麼都做成 tool」是最常見的 MCP 設計臭味。折扣規則沒有參數、不打 DB，
它不是動作而是**背景知識**——做成 tool 就是要 agent 猜「我該不該查一下折扣規則」，
做成 resource 才是「使用者知道這題要用，主動掛上去」。

#### 不靠 Inspector 的文字證據

`/mcp` 看得到清單但看不到 payload，所以我直接餵 6 筆 JSON-RPC 到 stdio（initialize →
resources/list → resources/read → prompts/list → prompts/get），實際回應：

```
capabilities: {"logging":{},"prompts":{"listChanged":true},
               "resources":{"listChanged":true},"tools":{"listChanged":true}}
resources/list  -> 會員折扣規則 / orderhub://discount-rules / text/markdown
resources/read  -> "# OrderHub 會員折扣規則\n- Standard：不打折\n- Silver：95 折\n- Gold：9 折…"
prompts/list    -> low_stock_report，arguments: threshold（required: false）
prompts/get(threshold=5) -> "請用 low_stock 工具（threshold=5）查出低庫存商品…"
```

最後一則是重點：**參數真的被代進範本了**（`threshold=5`，不是預設的 10）。
prompt 展開後的內容是「叫 agent 去用 low_stock」——prompt 引導 tool，兩個原語在這裡合體。

capabilities 也從只有 `tools` 變成三個都在。這是 server 對 client 的自我宣告，
少接一行 `WithResources`，client 就永遠不會來問 `resources/list`。

#### 5c 第 3 點：為什麼不讓 agent 自己去讀程式碼？

**折扣規則用 Resource 給 vs. 讓 agent 自己讀 `OrderService.cs`：**
自己讀要花好幾輪工具呼叫（grep → 讀檔 → 推論 `0.10m` 是「折掉 10%」還是「收 10%」），
而且每個人、每個 session 都要重跑一次，答案還可能不一樣。Resource 是**一次寫好、全隊同一份**，
還附上「折抵一次」、「UnitPriceSnapshot 是原價」這種**程式碼裡看不出來的意圖**。

**Prompt 範本放 server vs. 每個人自己打一段話：**
自己打，十個人有十種問法，報表欄位每次都不一樣；規則改版時要通知十個人各自改自己的筆記。
放 server 就是**進版控、code review、改一個地方全隊生效**——跟前面幾個練習「單一真實來源」是同一堂課。

#### 地雷確認：我自己就製造了兩份真相

文件的地雷區點名這件事，我照範本寫死了折扣文字，所以動手前先去對帳
`OrderService.GetDiscountRate`（`OrderService.cs:119`）：`Gold => 0.10m`、`Silver => 0.05m`、
其他 `0m`——和 resource 寫的 9 折／95 折／不打折**目前一致**。

但「目前一致」就是問題本身：規則改版時要改兩個地方，而且沒有任何測試會抓到它們不一致。
我在檔案的 doc comment 裡把這件事寫明了。想真正解掉得讓 resource 動態組內容
（注入 `IOrderService`，把三個 tier 的 `GetDiscountRate` 跑出來組成 markdown），
這樣就退回單一真實來源——這次沒做，留成已知落差，和練習 4 的 `OpenWorld` 同一類。

#### 又一個工具坑：跑著的 server 會鎖住 bin

第一次 `dotnet build src/OrderHub.Mcp` 直接失敗：

```
error MSB3027: 无法将 OrderHub.Infrastructure.dll 复制到 bin\Debug\net10.0\…
              文件由 OrderHub.Mcp (26328) 锁定
```

Claude Code 自己拉起來的 MCP server 正抓著 `bin\Debug\net10.0` 裡的 DLL。
**要改 MCP server 的程式碼，得先讓 client 放掉那個行程**（停掉行程或 `/mcp disable`），
build 完再 reconnect。這是第四個「我的工具鏈壞了，不是 server 壞了」——
前三個是編碼問題，這個是**開發中的 server 同時是正在被使用的 server**造成的自我打結。

#### 驗證方式

- ✅ `dotnet build src/OrderHub.Mcp` 0 errors / 0 warnings；`dotnet test` **35 綠**（未受影響）
- ✅ JSON-RPC 直測：`resources/list`、`resources/read`、`prompts/list`、`prompts/get(threshold=5)` 全部正確回應（payload 如上）
- ✅ server capabilities 由 `tools` 擴為 `tools + resources + prompts`
- ✅ resource 文字與 `OrderService.GetDiscountRate` 對帳一致（並記下這是兩份真相）
- ⬜ **（待你做）** `/mcp reconnect` 後用 `@` 選 `orderhub://discount-rules`，問「Gold 會員買 1000 元應付多少」（預期 900），確認它不讀程式碼就答對
- ⬜ **（待你做）** `/mcp__orderhub__low_stock_report` 一鍵產出採購建議表，觀察它展開範本後自動呼叫 `low_stock`
- ⬜ （選做）Inspector UI 的 Resources／Prompts 分頁——文字證據已用 JSON-RPC 取得

這兩項「待你做」的理由和練習 4 一樣：resource 的 `@` 選取和 prompt 的 slash command
都是**使用者介面動作**，我無法替自己觸發。

---

### Week 3 — Gemini 免費 API

練習 1 — ✅ 完成(自然語言查訂單 API)

成品:`src/OrderHub.Core/Ai/`(`OrderSearchQuery`、`IOrderQueryTranslator`、`AiServiceUnavailableException`)+
`Services/OrderSearchService`、`src/OrderHub.Infrastructure/Gemini/`(`GeminiOptions`、`IGeminiJsonClient`、
`GeminiInteractionsClient`、`GeminiOrderQueryTranslator`)+ `OrdersApiController`(`POST /api/orders/search`)。

#### 流程(實際跑起來對應文件那條線)

```
使用者輸入一句話 (text)
  → OrdersApiController.Search 接住
  → OrderSearchService.SearchAsync
      → GeminiOrderQueryTranslator.TranslateAsync
          → GeminiInteractionsClient 打 POST /v1/interactions(structured output)
          → 解析 model_output → RawQuery → [AllowedValues] 驗證 → enum/日期轉型
      → 白名單第二道防線:HasAnyFilter / 日期範圍檢查
  → OrderRepository.SearchAsync(EF Core 產 SQL)
  → OrderService.CalculateTotal 算金額
  → 回 JSON 摘要
```

#### 地雷區:逐項對照文件

| 地雷 | 對應程式碼 | 這次有沒有踩到 |
|---|---|---|
| 今天日期要塞進 prompt,否則「上個月」算不出絕對日期 | `GeminiOrderQueryTranslator.PromptTemplate` 的 `{0}` | 沒踩到——實測「上個月金卡會員取消的訂單」回傳的兩筆訂單建立時間都落在 2026-07(當時系統日期是 2026-08-07),換算正確 |
| `Enum.TryParse` 單獨用不夠,要先過 `[AllowedValues]` 白名單再轉型 | `RawQuery` 的 `[AllowedValues]` + 轉型順序 | 程式碼依文件寫,但沒有主動誘導 Gemini 吐出 schema 外的值來驗證這條防線真的擋得住——這次沒測到,留成已知落差 |
| schema 的 `required` 只放 `intent`,其他欄位缺是正常行為 | `ResponseSchema` | 沒踩到——只帶部分條件(如只給 memberTier+status)的查詢一樣正常回結果 |

#### 開發過程本身的坑:curl 傳中文 body 直接炸

第一次跑煙霧測試,直接在指令列寫 `curl -d '{"text":"上個月金卡會員取消的訂單"}'`,回應是:

```
{"title":"One or more validation errors occurred.","status":400,
 "errors":{"request":["The request field is required."],
           "$.text":["The JSON value could not be converted to System.String. ..."]}}
```

先懷疑是 controller 的 JSON binding 壞了,換一個純 ASCII 字串 `"test"` 測,結果正常回 200 +
「無法理解的查詢」——證明 pipeline 本身是通的,問題縮小到「Git Bash 傳中文參數給 curl 的編碼」。
改成把 body 寫成 UTF-8 檔案、用 `curl --data-binary @檔案路徑` 送出,中文就正常了。

跟 MCP 階段記過的幾次一樣:**這是測試工具的問題,不是程式的問題**——但如果沒有先拿 ASCII
隔離變因,很可能會誤判成 controller 或 Gemini 翻譯器出錯。

#### 過程中的一次安全事故:金鑰差點進 git

寫程式碼之前,`documents/activities/activity-3-gemini-api.md` 裡煙霧測試範例那行
`$env:GEMINI_API_KEY = "你的key"` 被改成貼了一把看起來像真實 key 的字串。這個檔案是
git 追蹤的文件,如果 commit 下去,這把 key 會永久留在 git history 裡,就算之後刪掉也還在
舊 commit 裡查得到。發現後立刻把該行改回佔位符,`git diff` 確認檔案已還原,始終沒有進任何 commit。

教訓:**任何會進 git 的文件,金鑰一律只能是佔位符**;真的 key 只放 user-secrets 或當次終端機的環境變數。

#### 驗證方式(逐項記錄實測結果)

- ✅ `dotnet build` 全綠(Core / Infrastructure / Web / Tests 四個專案都編譯成功;`OrderHub.Mcp`
  那次失敗是它自己執行中的處理程序鎖住輸出 DLL,錯誤碼是 MSB3027/MSB3021 檔案鎖定,不是 C#
  編譯錯誤,跟這次的程式碼無關)
- ✅ 「上個月金卡會員取消的訂單」查得出結果:實測回傳 2 筆——陳志明(Gold, Cancelled, 2026-07-15)、
  劉思穎(Gold, Cancelled, 2026-07-07),與規格描述的條件(金卡、取消、上月)完全對上
- ✅ 「幫我把所有訂單刪掉」→ 實測 `HTTP 422` + `{"error":"無法理解的查詢"}`,資料毫髮無傷
- ✅ 塞一段完全無關的文字(「請告訴我番茄炒蛋怎麼做」)→ 實測 `HTTP 422` + `{"error":"無法理解的查詢"}`
- ⬜ **(待補測)** 拔掉 API key 再打,預期得到 503 而不是 500——這步要暫時移除 user-secrets
  裡的真實 key,這次先跳過沒測

---

## 附錄：值得留下的對話片段

（貼 1–2 段最有代表性的 prompt 與回應**摘要**——不用貼全文，重點是「我怎麼問」和「它怎麼答」。）

**片段 1 — 用短指令換到完整驗收**
- 我問：「setup the repo and db first」
- 它答（摘要）：沒有直接 `dotnet run` 了事，而是先查 SDK/SQL 服務/連線，build 後 run 觸發 migrate+seed，再用 sqlcmd 回報 `Customers 20 / Products 50 / Orders 200 / OrderItems 501` 並 curl `/Orders` 得 HTTP 200。學到：把驗收目標（「db」）明確寫進指令，agent 會自己補上核對步驟。

**片段 2 — 先計畫、reviewer 補刀**
- 練習 3 我要它「先不要寫程式，給實作計畫並派子代理盤點既有 Products 慣例」，核對後才放行。實作完再叫它以 reviewer 角度看 diff。
- reviewer 回：架構乾淨，但「門檻測試沒有等於門檻的樣本，`<` 改 `<=` 也會過」。我照補 stock=10 樣本並證明它會紅。學到：agent 自己寫的測試也要被另一個 agent（或自己）挑，「測試有沒有真的能抓到 bug」比「測試綠不綠」重要。