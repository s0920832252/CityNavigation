using NavigationDemo.ViewModels;
using System.Windows.Controls;

namespace NavigationDemo.Views
{
    public partial class Level1View : UserControl
    {
        public Level1View()
        {
            InitializeComponent();
            DataContext = new Level1ViewModel();
        }
    }
}
