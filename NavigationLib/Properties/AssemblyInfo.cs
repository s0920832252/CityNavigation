using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Markup;

// 組件的一般資訊由下列的屬性集控制。
// 變更這些屬性的值即可修改組件的相關資訊。
[assembly: AssemblyTitle("NavigationLib")]
[assembly: AssemblyDescription("WPF Navigation Library with Clean Architecture")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("NavigationLib")]
[assembly: AssemblyCopyright("Copyright © 2025")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// 將 ComVisible 設為 false 可使此組件中的類型對 COM 元件隱藏。
// 若必須從 COM 存取此組件中的類型，請在該類型上將 ComVisible 屬性設為 true。
[assembly: ComVisible(false)]

// 下列 GUID 為專案公開 (Expose) 至 COM 時所要使用的 typelib ID
[assembly: Guid("8b5e6e5a-9f5e-4c5e-9e5e-5e5e5e5e5e5e")]

// 組件的版本資訊由下列四個值組成:
//
//      主要版本
//      次要版本
//      組建編號
//      修訂
//
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

// XAML 命名空間對應
[assembly: XmlnsPrefix("http://schemas.citynavigation.com/navigationlib", "nav")]
[assembly: XmlnsDefinition("http://schemas.citynavigation.com/navigationlib", "NavigationLib.FrameworksAndDrivers")]

// 讓內部類別對測試專案可見
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("NavigationLib.Tests")]
