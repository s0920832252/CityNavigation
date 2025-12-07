using NavigationDemo.ViewModels;
using System.Windows.Controls;

namespace NavigationDemo.Views
{
    public partial class Level2View : UserControl
    {
        public Level2View()
        {
            InitializeComponent();
            DataContext = new Level2ViewModel();
        }
    }
}
