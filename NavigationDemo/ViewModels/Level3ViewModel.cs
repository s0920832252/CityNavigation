using NavigationDemo.Common;
using NavigationLib.Entities;

namespace NavigationDemo.ViewModels
{
    /// <summary>
    /// ViewModel for Level3 (final destination).
    /// </summary>
    public class Level3ViewModel : ViewModelBase, INavigableViewModel
    {
        private string _currentSegment;
        private string _fullPath;
        private string _parameterInfo;

        public string CurrentSegment
        {
            get => _currentSegment;
            set => SetProperty(ref _currentSegment, value);
        }

        public string FullPath
        {
            get => _fullPath;
            set => SetProperty(ref _fullPath, value);
        }

        public string ParameterInfo
        {
            get => _parameterInfo;
            set => SetProperty(ref _parameterInfo, value);
        }

        public Level3ViewModel()
        {
            CurrentSegment = "Level3 initialized (awaiting navigation)";
            FullPath = "-";
            ParameterInfo = "-";
        }

        public void OnNavigation(NavigationContext context)
        {
            CurrentSegment = $"Level3: Segment {context.SegmentIndex + 1}/{context.AllSegments.Length} - {context.SegmentName}";
            FullPath = context.FullPath;
            
            if (context.Parameter != null)
            {
                ParameterInfo = context.Parameter.ToString();
            }
            else
            {
                ParameterInfo = "(No parameter)";
            }
        }
    }
}
