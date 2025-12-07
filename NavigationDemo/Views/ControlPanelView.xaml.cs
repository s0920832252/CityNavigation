using NavigationDemo.ViewModels;
using System.Windows.Controls;

namespace NavigationDemo.Views
{
    public partial class ControlPanelView : UserControl
    {
        public ControlPanelView()
        {
            InitializeComponent();
            DataContext = new ControlPanelViewModel();
        }
    }
}
