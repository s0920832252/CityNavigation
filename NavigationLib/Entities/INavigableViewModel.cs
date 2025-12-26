namespace NavigationLib.Entities
{
    /// <summary>
    /// ViewModels implement this interface to receive navigation requests.
    /// When NavigationService processes a path, it calls OnNavigation on the ViewModel corresponding to each segment.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interface is the core contract of the navigation system. ViewModels implementing this interface will be called during navigation flow
    /// to prepare child views (e.g., setting TabControl's SelectedIndex) and perform initialization logic.
    /// </para>
    /// <para>
    /// <strong>Important Notes:</strong>
    /// <list type="bullet">
    /// <item><description>The OnNavigation method should be lightweight and execute synchronously.</description></item>
    /// <item><description>For long-running operations (such as loading data), start asynchronous operations within OnNavigation rather than blocking the method.</description></item>
    /// <item><description>This method is called on the UI thread, so it is safe to update UI-related properties.</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public class ShellViewModel : INavigableViewModel
    /// {
    ///     public void OnNavigation(NavigationContext context)
    ///     {
    ///         // Prepare child views based on path segment
    ///         if (context.SegmentIndex == 0)
    ///         {
    ///             // This is the first segment, may need to set up main menu
    ///             PrepareMainView();
    ///         }
    ///         
    ///         // If this is the last segment, process parameters
    ///         if (context.IsLastSegment &amp;&amp; context.Parameter != null)
    ///         {
    ///             ProcessParameter(context.Parameter);
    ///         }
    ///     }
    /// }
    /// </code>
    /// </example>
    public interface INavigableViewModel
    {
        /// <summary>
        /// Called when navigating to the region that this ViewModel belongs to.
        /// </summary>
        /// <param name="context">
        /// Navigation context containing path information, segment index, parameters, etc.
        /// See <see cref="NavigationContext"/> for available properties.
        /// </param>
        /// <remarks>
        /// <para>
        /// This method executes synchronously on the UI thread. Implementations should remain lightweight to avoid blocking.
        /// </para>
        /// <para>
        /// If this method throws an exception, the navigation flow will be interrupted and failure will be reported via NavigationResult.
        /// </para>
        /// </remarks>
        void OnNavigation(NavigationContext context);
    }
}
