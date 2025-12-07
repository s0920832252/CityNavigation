using NavigationDemo.Common;
using NavigationLib.Entities;

namespace NavigationDemo.ViewModels
{
    /// <summary>
    /// ViewModel for Level2 (created via ControlTemplate in L1-A).
    /// </summary>
    public class Level2ViewModel : ViewModelBase, INavigableViewModel
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

        public Level2ViewModel()
        {
            CurrentSegment = "Level2 initialized via ControlTemplate";
        }

        public void OnNavigation(NavigationContext context)
        {
            CurrentSegment = $"Level2: Segment {context.SegmentIndex + 1}/{context.AllSegments.Length} - {context.SegmentName}";

            // Switch tab based on next segment
            if (!context.IsLastSegment && context.SegmentIndex + 1 < context.AllSegments.Length)
            {
                var nextSegment = context.AllSegments[context.SegmentIndex + 1];
                
                if (nextSegment == "Level3A")
                {
                    SelectedTabIndex = 0; // L2-A tab
                }
                else if (nextSegment == "Level3B")
                {
                    SelectedTabIndex = 1; // L2-B tab
                }
            }
        }
    }
}
