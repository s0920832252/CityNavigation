using NavigationDemo.Common;
using NavigationLib.Entities;

namespace NavigationDemo.ViewModels
{
    /// <summary>
    /// ViewModel for Shell (top-level container with Home and Layer1 tabs).
    /// </summary>
    public class ShellViewModel : ViewModelBase, INavigableViewModel
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

        public ShellViewModel()
        {
            CurrentSegment = "Shell initialized";
        }

        public void OnNavigation(NavigationContext context)
        {
            CurrentSegment = $"Shell: Segment {context.SegmentIndex + 1}/{context.AllSegments.Length} - {context.SegmentName}";

            // If not the last segment, switch to Layer 1 tab
            if (!context.IsLastSegment && context.SegmentIndex + 1 < context.AllSegments.Length)
            {
                var nextSegment = context.AllSegments[context.SegmentIndex + 1];
                
                if (nextSegment == "Level1")
                {
                    SelectedTabIndex = 1; // Switch to Layer 1 tab
                }
            }
            else
            {
                // If this is the final destination, go to Home tab
                SelectedTabIndex = 0;
            }
        }
    }
}
