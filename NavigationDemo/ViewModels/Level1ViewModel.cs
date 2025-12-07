using NavigationDemo.Common;
using NavigationLib.Entities;

namespace NavigationDemo.ViewModels
{
    /// <summary>
    /// ViewModel for Level1 (second layer with two branches: L1-A and L1-B).
    /// </summary>
    public class Level1ViewModel : ViewModelBase, INavigableViewModel
    {
        private int _selectedTabIndex;
        private string _currentSegment;

        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set => SetProperty(ref _selectedTabIndex, value);
        }

        public string CurrentSegment
        {
            get => _currentSegment;
            set => SetProperty(ref _currentSegment, value);
        }

        public Level1ViewModel()
        {
            CurrentSegment = "Level1 initialized";
        }

        public void OnNavigation(NavigationContext context)
        {
            CurrentSegment = $"Level1: Segment {context.SegmentIndex + 1}/{context.AllSegments.Length} - {context.SegmentName}";

            // Switch tab based on next segment
            if (!context.IsLastSegment && context.SegmentIndex + 1 < context.AllSegments.Length)
            {
                var nextSegment = context.AllSegments[context.SegmentIndex + 1];
                
                if (nextSegment == "Level2")
                {
                    SelectedTabIndex = 0; // L1-A tab (contains Level2)
                }
                else if (nextSegment == "Level2Alt")
                {
                    SelectedTabIndex = 1; // L1-B tab (contains Level2Alt)
                }
            }
        }
    }
}
