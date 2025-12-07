# CityNavigation 專案

這是一個 WPF Navigation Library，使用 Clean Architecture 設計。

## 專案結構

- **NavigationLib** - 核心導覽函式庫 ✅ 已完成並成功建置
- **NavigationLib.Tests** - 單元測試專案（使用 NUnit）⚠️ 需要調整 API 使用方式

## 建置專案

### 使用 Visual Studio 2019/2022（推薦）

1. 開啟 `CityNavigation.sln`
2. Visual Studio 會自動還原 NuGet 套件（已使用 PackageReference）
3. 建置方案 (Ctrl+Shift+B)

### 使用命令列（MSBuild）

```bash
# 還原 NuGet 套件
"/c/Program Files (x86)/Microsoft Visual Studio/2019/Preview/MSBuild/Current/Bin/MSBuild.exe" -t:Restore CityNavigation.sln

# 建置 NavigationLib（主函式庫）
"/c/Program Files (x86)/Microsoft Visual Studio/2019/Preview/MSBuild/Current/Bin/MSBuild.exe" NavigationLib/NavigationLib.csproj

# 建置測試專案（需先修正測試程式碼）
"/c/Program Files (x86)/Microsoft Visual Studio/2019/Preview/MSBuild/Current/Bin/MSBuild.exe" NavigationLib.Tests/NavigationLib.Tests.csproj
```

## 目前狀態

### ✅ NavigationLib - 已完成
# CityNavigation

CityNavigation 是一個以 WPF 為範例的導航（navigation）範本專案，包含一個可重用的核心函式庫 (`NavigationLib`)、一個示範用的 WPF 應用 (`NavigationDemo`) 與對應的單元測試專案 (`NavigationLib.Tests`)。

此專案目標是將導航概念（region、view model 導覽、參數與結果回傳）抽象化，使 UI 層可以交換不同實作同時保有一致的導覽 API。

## 目錄概覽

- `NavigationLib/`：核心函式庫
  - `Adapters/`：介面/適配器（例如 `IDispatcher`, `IRegionElement`）
  - `Entities/`：Domain/Entity 類別（如 `NavigationContext`, `NavigationResult`, `INavigableViewModel`）
  - `UseCases/`：業務邏輯（如 `NavigationService`, `PathValidator`, `RegionStore`）
  - `FrameworksAndDrivers/`：平台實作（如 `DispatcherAdapter`, `Region`, `RegionElementAdapter`）

- `NavigationDemo/`：WPF 範例專案（Views、ViewModels、Demo UI）

- `NavigationLib.Tests/`：單元測試專案（驗證 `NavigationContext`, `NavigationResult` 與 UseCases 行為）

- 文件：`README.md`, `Plan.md`, `Spec.md`

## 快速開始（開發者）

1. 使用 Visual Studio 打開 `CityNavigation.sln`（推薦 Visual Studio 2019 或 2022）。
2. Visual Studio 會自動還原 NuGet 套件（使用 `PackageReference`）。
3. 建置解決方案：`Build Solution`（或 Ctrl+Shift+B）。
4. 執行 `NavigationDemo` 專案以手動測試 UI 導覽流程。

命令列範例（僅在需要時）：
```bash
# 還原並建置整個方案（Windows + VS/MSBuild，可調整路徑）
msbuild /t:Restore CityNavigation.sln
msbuild CityNavigation.sln /p:Configuration=Debug
```

## 測試

專案包含 `NavigationLib.Tests`，使用 NUnit（請透過 Visual Studio 或 dotnet test 執行）。若測試無法通過，請先檢查 API 變更或調整 `InternalsVisibleTo` 設定。

```bash
# 使用 Visual Studio Test Explorer 或：
dotnet test NavigationLib.Tests/NavigationLib.Tests.csproj
```

## 重要設計要點

- Navigation 為核心 concern；`NavigationService` 提供簡潔的靜態入口，實作可在 `UseCases` 中替換。  
- `RegionStore` 管理可註冊的 region，UI 元件（views）以 `IRegionElement`/adapter 形式注入。  
- `PathValidator` 用於解析與驗證路徑字串（例如 `Shell/Level1`），確保路徑安全且格式正確。  

## 如何貢獻

- 建立 issue 描述 bug 或改進建議。  
- 若要提交程式碼，請建立 feature branch 並發送 Pull Request（PR），PR 應包含說明、如何驗證改動與相關測試。  

## 開發與除錯提示

- 若遇到 `.git` 或版本控制相關問題，請不要手動把 `.git` 資料夾複製到其他地方，改用 `git clone`。  
- 對於測試或範例修改，務求不改變公開 API（如需變動，請在 PR 中描述遷移步驟）。

## 授權

本專案尚未指定正式授權（請在需要時加入 `LICENSE` 檔案）。

---

若你要我把說明再擴充為英文版本或加入範例截圖、API 範例程式碼檔案，我可以接著產生並 commit。 
