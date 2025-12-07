using NavigationDemo.ViewModels;
using System.Windows.Controls;

namespace NavigationDemo.Views
{
    public partial class Level3View : UserControl
    {
        public Level3View()
        {
            InitializeComponent();
            DataContext = new Level3ViewModel();
        }
    }
}
