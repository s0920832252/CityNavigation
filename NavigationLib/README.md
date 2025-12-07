# NavigationLib - WPF 導航函式庫

一個基於 Clean Architecture 設計的 WPF 導航函式庫，支援多層巢狀導航、DataTemplate 延遲建立、以及記憶體洩漏防護。

## 🎯 特色功能

- ✅ **Clean Architecture** - 四層架構 (Entities, UseCases, Adapters, FrameworksAndDrivers)
- ✅ **事件驅動導航** - Prism 風格的非阻塞 RequestNavigate API
- ✅ **DataTemplate 支援** - 處理動態建立的 View
- ✅ **記憶體安全** - WeakReference + WeakEventManager 防止記憶體洩漏
- ✅ **路徑導航** - 使用 slash-path 格式（例如 "Shell/Level1/Level2"）
- ✅ **MVVM 友善** - ViewModel 透過 INavigableViewModel 接收導航請求
- ✅ **超時處理** - 每個段落可設定 timeout（預設 10 秒）
- ✅ **完整 XML 註解** - IntelliSense 友善

## 📦 專案結構

```
NavigationLib/
├── Entities/                       # 領域模型
│   ├── INavigableViewModel.cs     # ViewModel 契約介面
│   ├── NavigationContext.cs       # 導航上下文（不可變）
│   ├── NavigationResult.cs        # 導航結果
│   └── Exceptions/
│       ├── InvalidPathException.cs
│       └── NavigationException.cs
├── UseCases/                       # 應用邏輯
│   ├── NavigationService.cs       # 導航服務（核心）
│   ├── RegionStore.cs              # Region 註冊中心
│   └── PathValidator.cs            # 路徑驗證器
├── Adapters/                       # 介面抽象
│   ├── IRegionElement.cs           # 隔離 FrameworkElement
│   └── IDispatcher.cs              # 隔離 Dispatcher
└── FrameworksAndDrivers/           # WPF 實作
    ├── Region.cs                   # Region.Name 附加屬性
    ├── RegionElementAdapter.cs     # IRegionElement 實作
    └── DispatcherAdapter.cs        # IDispatcher 實作
```

## 🚀 快速開始

### 1. 在 XAML 中標記 Region

```xaml
<Window xmlns:nav="http://schemas.citynavigation.com/navigationlib">
    <TabControl nav:Region.Name="Shell">
        <TabItem Header="Home">
            <local:HomeView nav:Region.Name="Home" />
        </TabItem>
        <TabItem Header="Settings">
            <local:SettingsView nav:Region.Name="Settings" />
        </TabItem>
    </TabControl>
</Window>
```

### 2. ViewModel 實作 INavigableViewModel

```csharp
using NavigationLib.Entities;

public class ShellViewModel : INavigableViewModel
{
    public void OnNavigation(NavigationContext context)
    {
        // 根據導航上下文準備子視圖
        if (context.IsLastSegment && context.Parameter != null)
        {
            ProcessParameter(context.Parameter);
        }
    }
}
```

### 3. 發起導航請求

```csharp
using NavigationLib.UseCases;

// 基本導航
NavigationService.RequestNavigate("Shell/Settings");

// 帶參數的導航
NavigationService.RequestNavigate(
    path: "Shell/Home",
    parameter: myData,
    callback: result =>
    {
        if (result.Success)
        {
            Console.WriteLine("導航成功！");
        }
        else
        {
            Console.WriteLine($"導航失敗：{result.ErrorMessage}");
            // result.FailedAtSegment 指示失敗的段落
            // result.Exception 包含詳細例外資訊
        }
    },
    timeoutMs: 5000  // 可選：自訂 timeout
);
```

## 🏗️ 設計決策

### Clean Architecture 分層

- **Entities (領域模型)** - 核心業務概念，不依賴任何外部框架
- **UseCases (應用邏輯)** - 導航協調流程，透過介面隔離 WPF 依賴
- **Adapters (介面層)** - 定義抽象介面 (IRegionElement, IDispatcher)
- **FrameworksAndDrivers (實作層)** - WPF 具體實作

### 事件驅動模型

採用 Prism 風格的非阻塞 RequestNavigate：
- ✅ 不強制使用 async/await
- ✅ 自然處理 DataTemplate 延遲建立
- ✅ callback 模式報告結果

### 記憶體管理

- **ConditionalWeakTable** - 使用 .NET 內建的 `ConditionalWeakTable<FrameworkElement, RegionElementAdapter>` 管理元素與 Adapter 的關聯，確保 Adapter 隨 Element 生命週期自動回收
- **WeakReference** - RegionStore 使用弱參考儲存 Region，當元素被移除後允許 GC 回收
- **WeakEventManager** - Region 附加屬性使用弱事件管理 Loaded/Unloaded，避免事件處理器造成記憶體洩漏
- **PresentationSource 檢查** - Unloaded 時確認元素真正離開視覺樹（避免 TabControl 切換時誤解除註冊）
- **自動回收** - 當 FrameworkElement 被 GC 回收時，ConditionalWeakTable 中的 Adapter 也會自動被回收，無需手動清理

### 重複註冊策略

- 相同實例：忽略（idempotent）
- 不同實例：更新註冊並記錄警告

## 📋 技術規格

- **目標框架**: .NET Framework 4.7.2
- **C# 版本**: 7.2（不使用 `init` 存取子或 nullable reference types）
- **不可變物件**: 使用唯讀屬性 + 建構子初始化
- **執行緒安全**: RegionStore 使用 `lock` 保護
- **UI 執行緒**: OnNavigation 自動調度到 UI 執行緒

## 🧪 驗收標準（已完成）

- ✅ Region.Name 附加屬性在 Loaded 時註冊，Unloaded 時解除註冊
- ✅ RequestNavigate 依序觸發各段落的 OnNavigation
- ✅ 等待 Region 註冊（支援 DataTemplate 延遲建立）
- ✅ 等待 DataContext 設定
- ✅ 重複註冊不拋出例外（更新註冊）
- ✅ Timeout 處理與事件清理
- ✅ 所有 handler 在完成或失敗後被清理

## 🔧 建置專案

```bash
# 使用 dotnet CLI
dotnet build NavigationLib.csproj

# 或使用 MSBuild
msbuild NavigationLib.csproj /t:Build /p:Configuration=Release
```

## 📄 授權

Copyright © 2025

## 🤝 貢獻

本專案遵循 Clean Architecture 和 SOLID 原則。貢獻時請確保：
- 程式碼符合 C# 7.2 語法限制
- 新增完整 XML 註解
- 維護 Clean Architecture 分層
- 避免記憶體洩漏

---

**版本**: 1.0.2  
**最後更新**: 2025-12-08

## 🔄 版本歷史

### v1.0.2 (2025-12-08)
- ✨ 改進：使用 `ConditionalWeakTable` 取代附加屬性管理 Adapter 生命週期
- 🎯 優化：更符合 .NET 最佳實踐的記憶體管理機制
- ✅ 驗證：所有測試通過，實際應用運作正常

### v1.0.1 (2025-12-08)
- 🐛 修正：添加 Adapter 強引用機制，防止 RegionElementAdapter 過早被 GC 回收
- 📝 更新：記憶體管理機制說明

### v1.0.0 (2025-12-07)
- 🎉 初始版本發布
- ✅ 完整 Clean Architecture 實作
- ✅ 事件驅動導航機制
- ✅ WeakReference 記憶體管理
