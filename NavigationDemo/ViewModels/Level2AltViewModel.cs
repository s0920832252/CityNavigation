using NavigationDemo.Common;
using NavigationLib.Entities;

namespace NavigationDemo.ViewModels
{
    /// <summary>
    /// ViewModel for Level2Alt (created via ControlTemplate in L1-B).
    /// </summary>
    public class Level2AltViewModel : ViewModelBase, INavigableViewModel
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

        public Level2AltViewModel()
        {
            CurrentSegment = "Level2Alt initialized via ControlTemplate";
        }

        public void OnNavigation(NavigationContext context)
        {
            CurrentSegment = $"Level2Alt: Segment {context.SegmentIndex + 1}/{context.AllSegments.Length} - {context.SegmentName}";

            // Switch tab based on next segment
            if (!context.IsLastSegment && context.SegmentIndex + 1 < context.AllSegments.Length)
            {
                var nextSegment = context.AllSegments[context.SegmentIndex + 1];
                
                if (nextSegment == "Level3C")
                {
                    SelectedTabIndex = 0; // L2-C tab
                }
                else if (nextSegment == "Level3D")
                {
                    SelectedTabIndex = 1; // L2-D tab
                }
            }
        }
    }
}
