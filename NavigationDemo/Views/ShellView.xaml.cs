using NavigationDemo.ViewModels;
using System.Windows.Controls;

namespace NavigationDemo.Views
{
    public partial class ShellView : UserControl
    {
        public ShellView()
        {
            InitializeComponent();
            DataContext = new ShellViewModel();
        }
    }
}
