using System;
using System.Diagnostics;
using System.Threading;
using NavigationLib.Adapters;
using NavigationLib.Entities;
using NavigationLib.Entities.Exceptions;

namespace NavigationLib.UseCases
{
    /// <summary>
    ///     導航服務的內部實作（Use Case 層）。
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         負責協調路徑解析、region 查找、ViewModel 呼叫等導航流程。
    ///     </para>
    ///     <para>
    ///         採用 Prism 風格的事件驅動、非阻塞導航模型。
    ///         RequestNavigate 方法立即返回，並透過 callback 回報結果。
    ///     </para>
    ///     <para>
    ///         支援延遲建立的 region（DataTemplate/ControlTemplate 情境）：
    ///         若 region 尚未註冊或 DataContext 尚未就緒，服務會等待直至就緒或 timeout。
    ///     </para>
    ///     <para>
    ///         <strong>外部使用者請透過 NavigationLib.Adapters.NavigationHost (OHS) 呼叫導航功能。</strong>
    ///     </para>
    /// </remarks>
    internal static class NavigationService
    {
        private const int DefaultTimeoutMs = 10000; // 10 seconds

        /// <summary>
        ///     發出非阻塞的導航請求。
        /// </summary>
        /// <param name="path">導航路徑（例如："Shell/Level1/Level2"）。</param>
        /// <param name="parameter">導航參數（可選）。</param>
        /// <param name="callback">完成時的回呼（可選，null 表示 fire-and-forget）。</param>
        /// <param name="timeoutMs">每個段落的等待超時時間（毫秒），預設為 10000 (10秒)。</param>
        /// <remarks>
        ///     <para>
        ///         此方法立即返回，不會阻塞呼叫執行緒。
        ///         導航流程在背景執行，完成時會透過 <paramref name="callback" /> 回報結果。
        ///     </para>
        ///     <para>
        ///         若路徑驗證失敗，會立即透過 callback 回報失敗（在呼叫執行緒上同步執行 callback）。
        ///     </para>
        /// </remarks>
        public static void RequestNavigate(
            string path,
            object parameter = null,
            Action<NavigationResult> callback = null,
            int timeoutMs = DefaultTimeoutMs)
        {
            // 驗證路徑
            string[] segments;

            try
            {
                segments = PathValidator.ValidateAndParse(path);
            }
            catch (InvalidPathException ex)
            {
                // 路徑無效，立即回報失敗
                InvokeCallback(callback, NavigationResult.CreateFailure(null, ex.Message, ex));
                return;
            }

            // 初始化導航狀態並啟動非阻塞處理
            var state = new NavigationState
            {
                FullPath            = path,
                Segments            = segments,
                CurrentSegmentIndex = 0,
                Parameter           = parameter,
                Callback            = callback,
                TimeoutMs           = timeoutMs,
            };

            // 啟動第一個段落的處理
            ProcessNextSegment(state);
        }

        /// <summary>
        ///     處理下一個段落（或完成導航）。
        /// </summary>
        private static void ProcessNextSegment(NavigationState state)
        {
            if (state.CurrentSegmentIndex >= state.Segments.Length)
            {
                // 所有段落都已成功處理，導航完成
                InvokeCallback(state.Callback, NavigationResult.CreateSuccess());
                return;
            }

            var segmentName = state.Segments[state.CurrentSegmentIndex];

            // 嘗試取得 region
            if (RegionStore.Instance.TryGetRegion(segmentName, out var element))
            {
                // Region 已存在，繼續處理
                ProcessSegment(state, element);
            }
            else
            {
                // Region 尚未註冊，等待註冊
                WaitForRegionRegistration(state, segmentName);
            }
        }

        /// <summary>
        ///     等待 region 註冊（使用事件訂閱）。
        /// </summary>
        private static void WaitForRegionRegistration(NavigationState state, string segmentName)
        {
            var handlerInvoked = 0; // 0 = false, 1 = true
            EventHandler<RegionEventArgs> handler = null;
            Timer timer = null;

            // 建立事件處理器
            handler = (sender, e) =>
            {
                if (string.Equals(e.RegionName, segmentName, StringComparison.OrdinalIgnoreCase))
                {
                    // 找到目標 region
                    if (Interlocked.Exchange(ref handlerInvoked, 1) == 0)
                    {
                        // 清理
                        RegionStore.Instance.RegionRegistered -= handler;
                        timer?.Dispose();

                        // 繼續處理
                        ProcessSegment(state, e.Element);
                    }
                }
            };

            // 建立 timeout timer
            timer = new Timer(_ =>
                {
                    if (Interlocked.Exchange(ref handlerInvoked, 1) == 0)
                    {
                        // Timeout
                        RegionStore.Instance.RegionRegistered -= handler;
                        timer?.Dispose();

                        var errorMsg = $"Region '{segmentName}' was not registered within timeout ({state.TimeoutMs}ms).";

                        InvokeCallback(state.Callback,
                            NavigationResult.CreateFailure(segmentName, errorMsg));
                    }
                },
                null,
                state.TimeoutMs,
                Timeout.Infinite);

            // 訂閱事件
            RegionStore.Instance.RegionRegistered += handler;

            // 再次檢查（避免競爭條件：region 可能在訂閱前已註冊）
            if (RegionStore.Instance.TryGetRegion(segmentName, out var element))
            {
                if (Interlocked.Exchange(ref handlerInvoked, 1) == 0)
                {
                    // 清理
                    RegionStore.Instance.RegionRegistered -= handler;
                    timer.Dispose();

                    // 繼續處理
                    ProcessSegment(state, element);
                }
            }
        }

        /// <summary>
        ///     處理單一段落：等待 DataContext、驗證 ViewModel、呼叫 OnNavigation。
        /// </summary>
        private static void ProcessSegment(NavigationState state, IRegionElement element)
        {
            var segmentName = state.Segments[state.CurrentSegmentIndex];

            // 檢查 DataContext
            var dataContext = element.GetDataContext();

            if (dataContext == null)
            {
                // DataContext 尚未設定，等待
                WaitForDataContext(state, element, segmentName);
                return;
            }

            // 驗證是否實作 INavigableViewModel
            if (!(dataContext is INavigableViewModel navigableVm))
            {
                var errorMsg = string.Format("DataContext of region '{0}' does not implement INavigableViewModel. Type: {1}",
                    segmentName,
                    dataContext.GetType().FullName);
                InvokeCallback(state.Callback, NavigationResult.CreateFailure(segmentName, errorMsg));
                return;
            }

            // 建立 NavigationContext
            var context = new NavigationContext(state.FullPath,
                state.CurrentSegmentIndex,
                segmentName,
                state.Segments,
                state.CurrentSegmentIndex == state.Segments.Length - 1,
                state.Parameter);

            // 在 UI 執行緒上呼叫 OnNavigation
            var dispatcher = element.GetDispatcher();

            try
            {
                dispatcher.Invoke(() =>
                {
                    try
                    {
                        navigableVm.OnNavigation(context);

                        // 成功，處理下一段
                        state.CurrentSegmentIndex++;
                        ProcessNextSegment(state);
                    }
                    catch (Exception ex)
                    {
                        // OnNavigation 拋出例外
                        var errorMsg = $"OnNavigation threw an exception at segment '{segmentName}': {ex.Message}";

                        InvokeCallback(state.Callback,
                            NavigationResult.CreateFailure(segmentName, errorMsg, ex));
                    }
                });
            }
            catch (Exception ex)
            {
                // Dispatcher.Invoke 失敗
                var errorMsg = $"Failed to invoke OnNavigation on UI thread for segment '{segmentName}': {ex.Message}";

                InvokeCallback(state.Callback,
                    NavigationResult.CreateFailure(segmentName, errorMsg, ex));
            }
        }

        /// <summary>
        ///     等待 DataContext 設定（使用 DataContextChanged 事件）。
        /// </summary>
        private static void WaitForDataContext(NavigationState state, IRegionElement element, string segmentName)
        {
            var handlerInvoked = 0; // 0 = false, 1 = true
            EventHandler handler = null;
            Timer timer = null;

            // 建立事件處理器
            handler = (sender, e) =>
            {
                if (Interlocked.Exchange(ref handlerInvoked, 1) == 0)
                {
                    // 清理
                    element.RemoveDataContextChangedHandler(handler);
                    timer?.Dispose();

                    // 重新處理此段落
                    ProcessSegment(state, element);
                }
            };

            // 建立 timeout timer
            timer = new Timer(_ =>
                {
                    if (Interlocked.Exchange(ref handlerInvoked, 1) == 0)
                    {
                        // Timeout
                        element.RemoveDataContextChangedHandler(handler);
                        timer?.Dispose();

                        var errorMsg = string.Format("DataContext of region '{0}' was not set within timeout ({1}ms).",
                            segmentName,
                            state.TimeoutMs);

                        InvokeCallback(state.Callback,
                            NavigationResult.CreateFailure(segmentName, errorMsg));
                    }
                },
                null,
                state.TimeoutMs,
                Timeout.Infinite);

            // 訂閱事件
            element.AddDataContextChangedHandler(handler);

            // 再次檢查（避免競爭條件）
            var dataContext = element.GetDataContext();

            if (dataContext != null)
            {
                if (Interlocked.Exchange(ref handlerInvoked, 1) == 0)
                {
                    // 清理
                    element.RemoveDataContextChangedHandler(handler);
                    timer.Dispose();

                    // 重新處理此段落
                    ProcessSegment(state, element);
                }
            }
        }

        /// <summary>
        ///     安全地呼叫 callback。
        /// </summary>
        private static void InvokeCallback(Action<NavigationResult> callback, NavigationResult result)
        {
            if (callback == null)
            {
                return;
            }

            try
            {
                callback(result);
            }
            catch (Exception ex)
            {
                // Callback 拋出例外，記錄但不中斷流程
                Debug.WriteLine("[NavigationService] Callback threw exception: {0}", ex);
            }
        }

        /// <summary>
        ///     導航狀態（內部使用）。
        /// </summary>
        private class NavigationState
        {
            public string FullPath { get; set; }
            public string[] Segments { get; set; }
            public int CurrentSegmentIndex { get; set; }
            public object Parameter { get; set; }
            public Action<NavigationResult> Callback { get; set; }
            public int TimeoutMs { get; set; }
        }
    }
}