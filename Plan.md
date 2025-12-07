# 實作計畫（Plan）

此檔案依據 `Spec.md` 規格，將實作工作拆解為具體任務。每個任務包含：目標、交付項目、功能描述與驗收條件，供實作與驗收時使用。

## 約束與前提
- 目標框架：.NET Framework 4.7.2（WPF）
- C# 語法限制：僅使用 C# 7.2（含）以前語法；因此不可使用 `init` 存取子或 nullable reference type 標記。
- 不可變物件實作方式：使用唯讀屬性（`get;`）並透過建構子初始化。
- 事件管理：可使用 `WeakEventManager`（適用於 .NET Framework 4.7.2），或手動註銷 handlers 以避免記憶體洩漏。
- 命名空間：`NavigationLib`。
- 專案資料夾：在工作區建立 `NavigationLib/`，並採 Clean Architecture 四層結構：
	1. `Entities`（領域模型）
	2. `UseCases`（應用流程）
	3. `Adapters`（介面/轉接）
	4. `FrameworksAndDrivers`（UI、WPF 相關實作、第三方依賴）

上述約束會反映到實作細節與檔案放置位置。

---

## 階段 1：核心型別與契約定義

### 任務 1.1：定義 `INavigableViewModel` 介面
- 目標：建立 ViewModel 必須實作的導航契約。
- 交付項目：`INavigableViewModel.cs`。
- 功能描述：定義 `void OnNavigation(NavigationContext context)`，包含 XML 文件註解與使用範例，提醒為輕量同步方法。
- 驗收條件：介面僅含一方法；具備清楚註解與範例；在 IDE 可見 IntelliSense 訊息。

### 任務 1.2：定義 `NavigationContext`
- 目標：提供導航時傳遞的不可變上下文資料。
- 交付項目：`NavigationContext.cs`。
- 功能描述：屬性包含 `FullPath`、`SegmentIndex`、`SegmentName`、`AllSegments`、`IsLastSegment`、`Parameter`，皆為唯讀；具建構子初始化。
- 驗收條件：屬性為 immutable；包含 XML 註解；可建立實例並正確讀取屬性。

### 任務 1.3：定義 `NavigationResult`
- 目標：封裝導航回呼的結果資訊。
- 交付項目：`NavigationResult.cs`。
- 功能描述：包含 `Success`、`FailedAtSegment`、`ErrorMessage`、`Exception`；提供 `CreateSuccess()` 與 `CreateFailure(...)` 靜態工廠方法。
- 驗收條件：能以簡單方式產生成功/失敗結果；callback 可正確解析內容。

### 任務 1.4：定義必要例外型別
- 目標：建立語意化例外以利除錯與測試。
- 交付項目：`InvalidPathException.cs`、其他必要例外檔案（視情況新增）。
- 功能描述：`InvalidPathException` 包含 `Path` 與 `Reason`；其他例外視 Spec 需求補齊。
- 驗收條件：例外包含可讀的訊息與上下文屬性；適當繼承自 `Exception`。

---

## 階段 2：Region 管理基礎建設

### 任務 2.1：實作 `RegionStore`
- 目標：集中註冊、查詢與事件通知 region 的中心。
- 交付項目：`RegionStore.cs` 與事件參數類別。
- 功能描述：singleton 實作，內部字典使用 `Dictionary<string, WeakReference<FrameworkElement>>`；提供 `Register`、`Unregister`、`TryGetRegion`、`RegionRegistered` 與 `RegionUnregistered`；操作以 `lock` 保護；在 Register/Unregister/TryGetRegion 時執行死引用清理；在同名註冊時若為相同 instance 則忽略，若為不同 instance 則以新 instance 覆寫並可紀錄警告。
- 驗收條件：多執行緒安全；弱參考正確回收；事件在註冊/解除註冊時觸發；重複註冊行為依 Spec 定義。

### 任務 2.2：實作 `Region` 附加屬性
- 目標：提供 XAML 附加屬性以方便在視圖上宣告 region 名稱。
- 交付項目：`Region.cs`。
- 功能描述：實作 `NameProperty`；在 PropertyChangedCallback 處理 Loaded/Unloaded 事件訂閱與移除；Loaded 時呼叫 `RegionStore.Register`；Unloaded 時以 `PresentationSource.FromVisual` 檢查是否離開視覺樹再呼叫 `Unregister`；確保 handler 使用弱參考或在必要時移除，避免記憶體洩漏。
- 驗收條件：能在 XAML 宣告 `nav:Region.Name` 並在 Loaded/Unloaded 正確註冊或解除註冊；Property 變更時 handler 被正確清理；長時間運行無記憶體洩漏。

---

## 階段 3：導航服務實作

### 任務 3.1：路徑驗證與解析
- 目標：驗證與解析輸入的路徑字串，防止非法輸入進入流程。
- 交付項目：`PathValidator` 或 `NavigationService` 內路徑驗證模組。
- 功能描述：檢查 null/空；以 `/` 分割並移除空段；驗證每段符合 `^[a-zA-Z0-9_-]+$`；驗證失敗時拋出 `InvalidPathException`。
- 驗收條件：合法路徑通過解析；非法路徑拋正確例外並包含原因。

### 任務 3.2：實作 `RequestNavigate` 骨架
- 目標：建立非阻塞的導航入口並準備狀態追蹤結構。
- 交付項目：`NavigationService.cs`（含 `RequestNavigate` 签章與基礎驗證流程）。
- 功能描述：接受 path、parameter、callback、timeoutMs；驗證路徑；若路徑不合法立即以 callback 回報失敗；初始化導航狀態並啟動非阻塞處理（排程下一步段落處理）。
- 驗收條件：方法立即返回；路徑錯誤透過 callback 回報；支持 null callback（fire-and-forget）。

### 任務 3.3：單段處理邏輯
- 目標：實作對單一 segment 的等待、驗證與呼叫流程。
- 交付項目：`NavigationService` 內部方法（例如 `ProcessSegment` 與輔助 handler）。
- 功能描述：嘗試取得 region，若不存在則訂閱 `RegionRegistered` 並啟 timer；region 可用後檢查 DataContext，若 null 訂閱 DataContextChanged 等待；DataContext 存在但未實作介面則回報失敗；建立 `NavigationContext` 並在 UI thread 呼叫 `OnNavigation`；例外與 timeout 處理與清理 handlers。
- 驗收條件：在 region 即時可用時立即呼叫；在 delayed 情境正確等待或逾時回報；所有 handlers 在完成或失敗後被移除。

### 任務 3.4：多段串接處理
- 目標：依序處理整個路徑的各段，並在任何段失敗時中斷並回報。
- 交付項目：`NavigationService` 的協調邏輯實作。
- 功能描述：在每段成功後遞增 index 並處理下一段；若成功全部段落於 callback 回報成功；若失敗則立即回報失敗並包含失敗段資訊。
- 驗收條件：多段路徑依序執行；失敗時停止後續段處理；callback 收到正確結果。

### 任務 3.5：Timeout 與清理
- 目標：確保等待不會無限進行，且發生 timeout 時能正確清理並回報。
- 交付項目：Timer 管理邏輯整合於 `NavigationService`。
- 功能描述：為每個等待註冊或 DataContext 的步驟設定 timeout（預設 10 秒），timeout 觸發後取消等待、移除事件 subscription，並回報失敗。
- 驗收條件：timeout 情況會回報失敗並移除所有 handlers；成功完成時 timer 被取消。

---

## 階段 4：測試與示例

### 任務 4.1：RegionStore 單元測試
- 目標：驗證 RegionStore 的正確性與邊界行為。
- 交付項目：`RegionStoreTests.cs`。
- 功能描述：測試註冊、重複註冊（相同/不同 instance）、解除註冊、弱參考回收、事件觸發、並行安全。
- 驗收條件：測試通過；覆蓋邊界情況。

### 任務 4.2：Region 附加屬性測試
- 目標：驗證附加屬性的生命週期與清理。
- 交付項目：`RegionAttachedPropertyTests.cs` 與相關 helpers。
- 功能描述：模擬 Loaded/Unloaded、PresentationSource 檢查、property 變更、handler 清理。
- 驗收條件：測試通過，無 handler 殘留。

### 任務 4.3：路徑驗證測試
- 目標：驗證各種路徑格式與錯誤回報。
- 交付項目：`PathValidationTests.cs`。
- 功能描述：測試合法/非法路徑與例外內容。
- 驗收條件：測試通過，例外訊息清楚。

### 任務 4.4：NavigationService 整合測試
- 目標：驗證導覽完整流程（含延遲註冊、DataContext、timeout、例外）。
- 交付項目：`NavigationServiceIntegrationTests.cs`。
- 功能描述：模擬 DataTemplate 延遲、DataContext 延遲、OnNavigation 拋例外、並行請求等情境。
- 驗收條件：整合測試通過，callback 在各情境返回正確結果。

### 任務 4.5：示例應用
- 目標：建立可執行的示範專案展示用法。
- 交付項目：範例 WPF 專案、README 與使用說明。
- 功能描述：三層巢狀 TabControl、DataTemplate、按鈕觸發導航、顯示結果。
- 驗收條件：示例能運行並展示主要功能，能在真實 UI 下驗證 DataTemplate timing。

---

## 階段 5：文件與設計紀錄

### 任務 5.1：API 文件
- 目標：為公開 API 撰寫 XML 註解，提升可用性。
- 交付項目：補齊各檔案的 XML 註解。
- 功能描述：為每個 public 型別與方法加入 summary/param/returns/exception/example。
- 驗收條件：IDE 可顯示完整 IntelliSense 註解；可用工具產出 API 文件。

### 任務 5.2：使用者指南（README）
- 目標：提供快速上手指南與範例。
- 交付項目：`README.md` 與 `docs/` 範例文件。
- 功能描述：包含快速開始、主要 API、進階注意事項與最佳實踐。
- 驗收條件：新使用者可在 30 分鐘內完成第一個範例。

### 任務 5.3：設計決策文件
- 目標：記錄重要設計決策與理由，便於未來維護。
- 交付項目：`docs/design-decisions.md`。
- 功能描述：記錄事件驅動選擇、WeakReference 原因、允許更新註冊的考量等。
- 驗收條件：涵蓋主要設計決策與替代方案說明。

---

## 建議實作順序
1. 階段 1（任務 1.1-1.4）
2. 階段 2（任務 2.1-2.2）
3. 階段 3（任務 3.1-3.5）
4. 階段 4（測試與示例）
5. 階段 5（文件與設計紀錄）

## 時間估算（粗略）
- 階段 1：2-3 小時
- 階段 2：4-6 小時
- 階段 3：6-8 小時
- 階段 4：8-10 小時
- 階段 5：3-4 小時
- 總計：約 23-31 小時

---

如需我開始實作，請回覆「開始實作」，我將從階段 1 的任務 1.1 開始並逐步回報進度。若需要調整計畫或優先順序，也請指示。
