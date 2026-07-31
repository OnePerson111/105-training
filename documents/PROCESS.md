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

## 附錄：值得留下的對話片段

（貼 1–2 段最有代表性的 prompt 與回應**摘要**——不用貼全文，重點是「我怎麼問」和「它怎麼答」。）

**片段 1 — 用短指令換到完整驗收**
- 我問：「setup the repo and db first」
- 它答（摘要）：沒有直接 `dotnet run` 了事，而是先查 SDK/SQL 服務/連線，build 後 run 觸發 migrate+seed，再用 sqlcmd 回報 `Customers 20 / Products 50 / Orders 200 / OrderItems 501` 並 curl `/Orders` 得 HTTP 200。學到：把驗收目標（「db」）明確寫進指令，agent 會自己補上核對步驟。

**片段 2 — 先計畫、reviewer 補刀**
- 練習 3 我要它「先不要寫程式，給實作計畫並派子代理盤點既有 Products 慣例」，核對後才放行。實作完再叫它以 reviewer 角度看 diff。
- reviewer 回：架構乾淨，但「門檻測試沒有等於門檻的樣本，`<` 改 `<=` 也會過」。我照補 stock=10 樣本並證明它會紅。學到：agent 自己寫的測試也要被另一個 agent（或自己）挑，「測試有沒有真的能抓到 bug」比「測試綠不綠」重要。