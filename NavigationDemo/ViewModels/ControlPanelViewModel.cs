using NavigationDemo.Common;
using NavigationLib.Adapters;
using System;
using System.Windows.Input;

namespace NavigationDemo.ViewModels
{
    /// <summary>
    /// ViewModel for the navigation control panel.
    /// </summary>
    public class ControlPanelViewModel : ViewModelBase
    {
        private string _lastResult;

        public string LastResult
        {
            get => _lastResult;
            set => SetProperty(ref _lastResult, value);
        }

        public ICommand NavigateCommand { get; }

        public ControlPanelViewModel()
        {
            NavigateCommand = new DelegateCommand(OnNavigate);
            LastResult = "Please use the buttons below to start navigation...";
        }

        private void OnNavigate(object parameter)
        {
            if (!(parameter is string path))
            {
                return;
            }

            LastResult = $"Navigating to: {path}...";
            
            var startTime = DateTime.Now;

            NavigationHost.RequestNavigate(
                path,
                parameter: $"Navigation to {path} at {DateTime.Now:HH:mm:ss}",
                callback: result =>
                {
                    var duration = (DateTime.Now - startTime).TotalMilliseconds;
                    
                    if (result.Success)
                    {
                        LastResult = $"✓ Success: {path}\nCompleted in: {duration:F0}ms";
                    }
                    else
                    {
                        LastResult = $"✗ Failed: {result.ErrorMessage}\nFailed at: '{result.FailedAtSegment}'\nDuration: {duration:F0}ms";
                    }
                },
                timeoutMs: 10000
            );
        }
    }
}
