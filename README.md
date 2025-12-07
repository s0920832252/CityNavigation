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
主函式庫已成功建置，包含：
- Clean Architecture (4 層)
- AttachedProperty 註冊機制
- 靜態 NavigationService API
- WeakReference 記憶體管理
- 完整的 XML 文件

### ⚠️ NavigationLib.Tests - 需要調整

測試專案結構已建立，但需要修正以匹配實際 API：

**需要修正的項目：**
1. `RegionStore.GetInstance()` → `RegionStore.Instance`
2. `NavigationService` 是靜態類，測試應直接調用靜態方法
3. `PathValidator` 是 internal 類，需要在 AssemblyInfo.cs 中添加 `[assembly: InternalsVisibleTo("NavigationLib.Tests")]`
4. API 參數名稱：`timeoutMilliseconds` → `timeoutMs`
5. `PathValidator.ValidateAndSplit` → `PathValidator.ValidateAndParse`
6. `MockRegionElement` 需要實作 `DataContext` 屬性

## NuGet 套件管理

專案使用 **PackageReference** 格式（不需要 packages.config 或 NuGet.config）：

```xml
<ItemGroup>
  <PackageReference Include="NUnit">
    <Version>3.14.0</Version>
  </PackageReference>
  <PackageReference Include="Moq">
    <Version>4.20.70</Version>
  </PackageReference>
</ItemGroup>
```

MSBuild 會自動從 nuget.org 下載套件到 `%UserProfile%\.nuget\packages`。

## 技術細節

- **Framework**: .NET Framework 4.7.2
- **Language**: C# 7.2
- **Architecture**: Clean Architecture (4 層)
- **Testing**: NUnit 3.14.0, Moq 4.20.70
- **WPF**: AttachedProperty, WeakReference, WeakEventManager

## API 設計

### 靜態 API
```csharp
// 導航服務（靜態類別）
NavigationService.RequestNavigate("Shell/Level1", 
    parameter: myData,
    callback: result => { /* 處理結果 */ },
    timeoutMs: 5000);

// Region 存儲（單例）
var store = RegionStore.Instance;
store.RegisterRegion("MyRegion", element);
```

### 路徑驗證
```csharp
// PathValidator 是 internal 類別
// 路徑格式：segment1/segment2/segment3
// 每個 segment 只能包含字母、數字、底線、連字號
```
