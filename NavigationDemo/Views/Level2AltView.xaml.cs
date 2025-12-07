using NavigationDemo.ViewModels;
using System.Windows.Controls;

namespace NavigationDemo.Views
{
    public partial class Level2AltView : UserControl
    {
        public Level2AltView()
        {
            InitializeComponent();
            DataContext = new Level2AltViewModel();
        }
    }
}
