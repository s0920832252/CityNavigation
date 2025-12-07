
# WPF 導航函式庫 規格書

版本: 1.0

最後更新: 2025-12-07

概覽
----
本文件定義一個 WPF 導航函式庫，支援在巢狀 UI 結構（例如巢狀的 TabControl、以及使用 DataTemplate/ControlTemplate 的 ContentControl）中，以程式化方式執行多層導覽。設計採用類似 Prism 的事件驅動 RequestNavigate 模式：非阻塞、以 callback 回報結果，並會等待 region 與 DataContext 準備完成。

目標（要做什麼 & 為什麼）
-------------------------
- 提供一個簡潔的 API，讓使用者可以透過路徑字串（例如 "Shell/Level1/Level2"）發出導覽請求。
- 保持 MVVM 的分離：NavigationService 負責找到 View 與其 ViewModel，但不直接改動 View 的狀態；實際的切換或初始化由 ViewModel 透過介面（INavigableViewModel.OnNavigation）來執行。
- 支援因 DataTemplate/ControlTemplate 而延遲建立的 View：若 region 尚未註冊或 DataContext 尚未就緒，服務會排隊等待並在就緒或失敗時以 callback 通知呼叫者。
- 避免記憶體洩漏：region 登記使用弱參考，並與 View 的生命週期（Loaded/Unloaded）綁定，Unloaded 時以視覺樹檢查避免誤解除註冊。
- 若設定錯誤（同名 region、無效路徑、缺少 region、ViewModel 未實作所需介面等），以明確錯誤回報失敗，讓開發者可以快速定位問題。

高階概念
--------
- Region：一個具名的 UI 容器（透過 XAML 的附加屬性註冊），將字串名稱對應到 FrameworkElement。
- RegionStore：集中式註冊中心，將 region 名稱對應到 FrameworkElement 的弱參考，並在註冊/反註冊時發出事件（RegionRegistered / RegionUnregistered），供導航服務等待動態註冊。
- INavigableViewModel：欲接收導覽請求的 ViewModel 所實作的介面，契約為 void OnNavigation(NavigationContext context)。
- NavigationService（RequestNavigate）：非阻塞、事件驅動的 API，嘗試逐段處理路徑，必要時等待 region 與 DataContext，就緒後以 callback 回報結果。

設計決策（為什麼）
-------------------
- 採用事件驅動且以 callback 回報的導航（類 Prism）而非強制同步或強制 async/await，原因包括：
  - 避免強迫呼叫端使用 async，並避免阻塞 UI 執行緒；
  - 自然符合動態註冊的模型（region 於視圖載入時註冊）；
  - 與 Prism 在 DataTemplate 與延遲建立 region 時的成熟做法一致。
- RequestNavigate 為非阻塞：呼叫方可提供 callback 以接收成功/失敗通知，或傳入 null 表示 fire-and-forget。
- 每一段的 ViewModel 都會接收 OnNavigation，以便各層可以準備子視圖（例如設定 SelectedIndex）並執行初始化。
- RegionStore 在預設（global）範圍內維持每一個名稱對應至單一的「活躍」 element；若註冊時發現同名情況，會採取寬鬆處理：
  - 若新註冊的 element 與已登記的 element 為同一實例，視為 idempotent（忽略重複註冊）；
  - 若為不同的 element，將更新註冊到新的 element（可記錄 debug/warning 以協助診斷可能的配置問題）；
  - 因此不再於此情況下拋出例外，避免因快速切換或 Template 重建導致不必要的錯誤。
- RegionStore 使用弱參考以避免讓註冊表保持對 UI 元件的強參考，導致 GC 無法回收。

API 概述
--------

1) 附加屬性（Attached property）

`nav:Region.Name="MyRegion"`

- 放置於 XAML 中的 FrameworkElement。於 Loaded 時註冊；在 Unloaded 時若確定離開視覺樹（使用 PresentationSource.FromVisual 檢查）則解除註冊。

2) INavigableViewModel

```csharp
public interface INavigableViewModel
{
    void OnNavigation(NavigationContext context);
}
```

3) NavigationContext（不可變）

屬性：
- FullPath (string)
- SegmentIndex (int)
- SegmentName (string)
- AllSegments (string[])
- IsLastSegment (bool)
- Parameter (object?)

4) NavigationService.RequestNavigate

```csharp
public static void RequestNavigate(
    string path,
    object? parameter = null,
    Action<NavigationResult>? callback = null,
    int timeoutMs = 10000);
```

- 非阻塞。解析以 '/' 分隔的路徑。對於每個段落，嘗試尋找已註冊的 region；若尚未註冊則訂閱 RegionRegistered 並等待至 timeoutMs。找到 element 後，若 DataContext 為 null，則同樣等待直至設定或逾時。確認 DataContext 實作 INavigableViewModel，並在 UI 執行緒呼叫 OnNavigation。如果某段失敗（逾時未註冊、DataContext 不在或未實作介面等），導覽停止並透過 callback 回報失敗細節。

NavigationResult

屬性：
- Success (bool)
- FailedAtSegment (string? - region name)
- ErrorMessage (string?)
- Exception (Exception?)

錯誤與例外（語意定義）
------------------------
- `InvalidPathException` — 當路徑為 null/空或包含無效段（段必須符合 `[a-zA-Z0-9_-]+`）。
- 註冊時若同名且有活躍實例：新版行為會視情況處理（相同 instance 忽略、不同 instance 更新），不再直接拋出 RegionAlreadyRegisteredException。
- `RegionNotFound`（透過 NavigationResult 回報）— region 未在 timeout 內註冊。
- `DataContextNotReady`（透過 NavigationResult 回報）— region 存在但 DataContext 在 timeout 內仍為 null。
- `InvalidNavigableViewModel`（透過 NavigationResult 回報）— DataContext 存在但沒有實作 `INavigableViewModel`。
- `NavigationException` — 若 ViewModel 的 OnNavigation 拋例外，該例外會包含在 NavigationResult.Exception 並回報為失敗。

行為流程（概念層級）
-------------------

RequestNavigate(path, parameter, callback, timeoutMs) 的流程：

1. 驗證路徑（非空、分段、每段符合允許模式）。
2. 依序處理每個段：
   a. 嘗試 RegionStore.TryGetRegion(segmentName)。
   b. 若找到，繼續；否則訂閱 RegionRegistered 等待該 region 註冊或逾時。
   c. 當 element 可用時，檢查 DataContext：
       - 若為 null：等待 DataContext 設定（或依 RegionRegistered 事件機制）直到逾時。
       - 若 DataContext 存在但未實作 INavigableViewModel：立即失敗並透過 callback 回報。
   d. 在 UI 執行緒上呼叫 viewModel.OnNavigation(context)。若該呼叫拋例外，停止並回報失敗。
3. 若所有段均成功，呼叫 callback 並回報 Success = true。

重要註記：
- 服務使用 RegionRegistered 事件與弱參考的 handler，以避免造成記憶體洩漏。
- 服務必須將 OnNavigation 的呼叫 marshal 到 UI 執行緒（使用 element.Dispatcher）。

為何這能解決 DataTemplate 的 apply timing 問題
-----------------------------------------------
- DataTemplate/ControlTemplate 在容器實際顯示（例如 TabItem 被選中）時才產生 view 實例。Prism 的做法是延遲 region 的建立直到 Loaded，並以事件驅動的方式處理導航：若要求的 region 尚未存在，導航請求會訂閱 region 建立事件並在建立後繼續處理。本設計複製該行為：導航請求不會立即失敗，而是等待 region 註冊（有限的 timeout），因此可以處理上層 OnNavigation 觸發子視圖建立的情況。

設計限制與權衡
-----------------
- 採用非阻塞 callback 模式而非 async/await，以避免強迫呼叫者改用 asynchronous code 並與 Prism 的既有設計保持一致。
- Region 的唯一性簡化目標解析並避免模糊行為；這也意味著無法同時對多個同名 region 廣播（可以在未來擴充）。
- ViewModel.OnNavigation 為同步且應為輕量；若需要長時間操作，ViewModel 應啟動非同步程序。
- NavigationService 不會序列化或取消並行的導航請求；並行請求被允許，協調責任交由呼叫方或 ViewModel。

驗收標準
--------
- 在 FrameworkElement 上附加 `nav:Region.Name` 會在 Loaded 時註冊並在 Unloaded（確認離開視覺樹）時解除註冊到 RegionStore。
- RequestNavigate("A/B/C") 會依序觸發對應的 INavigableViewModel.OnNavigation，或在適當時以明確結果回報失敗。
- 若所需 region 尚未可用，RequestNavigate 會非阻塞地等待 region 在 timeoutMs 內註冊，否則失敗且在 callback 中回報；等待期間 UI 必須保持回應。
- 重複註冊不會立即拋出例外：
  - 若為相同 element，則忽略（idempotent）；
  - 若為不同 element，會以新的 element 更新該 region 的註冊，並可記錄警告以便診斷。
- 所有 handler 與計時器在完成或逾時後皆會被清理，以避免記憶體洩漏。

使用範例
--------

XAML 範例：

```xaml
<TabControl nav:Region.Name="Shell">
  <TabItem Header="Home">
    <local:HomeView nav:Region.Name="Home" />
  </TabItem>
  <TabItem Header="Layer1">
    <local:Level1View nav:Region.Name="Level1" />
  </TabItem>
</TabControl>
```

C# 範例：

```csharp
NavigationService.RequestNavigate("Shell/Level1/Level2", parameter: myParam, callback: result =>
{
    if (!result.Success)
    {
        // 記錄或顯示錯誤。result.Exception 可能包含詳細例外資訊。
    }
});
```

實作時的下一步（當您允許後）
-----------------------------
1. 實作核心類型：`INavigableViewModel`、`NavigationContext`、例外類別、`NavigationResult`。
2. 實作 `RegionStore`（弱參考、使用 lock 的字典、事件）。
3. 實作 `Region` 附加屬性（Loaded/Unloaded 的生命周期處理，含 PresentationSource 檢查）。
4. 實作 `NavigationService.RequestNavigate`（解析路徑、事件驅動等待、UI 執行緒呼叫 OnNavigation、callback 呼叫、timeout 處理）。
5. 新增單元／整合測試與一個示例，示範巢狀 TabControl 與 DataTemplate 的導覽。

修訂記錄
---------
- 1.0 — 初版設計，採事件驅動 RequestNavigate 模型，能處理 DataTemplate 延遲建立情境（2025-12-07）。

