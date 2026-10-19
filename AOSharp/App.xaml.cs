using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;

namespace AOSharp
{
    public partial class App : Application
    {
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        [DllImport("user32.dll")]
        private static extern bool AllowSetForegroundWindow(int processId);
        private const int SW_RESTORE = 9;
        private const string ActivationEventName = "AOSharp_MainWindow_Activate";
        private static Mutex _mutex;
        private EventWaitHandle _activationEvent;
        private RegisteredWaitHandle _activationWait;
        private WindowsTheme _windowsTheme;

        protected override void OnStartup(StartupEventArgs e)
        {
            _mutex = new Mutex(true, "AOSharp_MainWindow_Instance", out bool createdNew);

            if (!createdNew)
            {
                var current = Process.GetCurrentProcess();
                var existing = Process.GetProcessesByName(current.ProcessName)
                    .FirstOrDefault(p => p.Id != current.Id);

                if (existing != null) AllowSetForegroundWindow(existing.Id);
                bool activated = false;
                try
                {
                    using (var activation = EventWaitHandle.OpenExisting(ActivationEventName))
                        activated = activation.Set();
                }
                catch (WaitHandleCannotBeOpenedException) { }
                catch (UnauthorizedAccessException) { }

                if (!activated && existing != null && existing.MainWindowHandle != IntPtr.Zero)
                {
                    ShowWindow(existing.MainWindowHandle, SW_RESTORE);
                    SetForegroundWindow(existing.MainWindowHandle);
                }

                Shutdown();
                return;
            }

            _activationEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ActivationEventName);
            _windowsTheme = new WindowsTheme(this);
            base.OnStartup(e);
        }

        internal void RegisterActivationHandler(Action activate)
        {
            // Process.MainWindowHandle is zero while the launcher is hidden in the tray.
            _activationWait = ThreadPool.RegisterWaitForSingleObject(_activationEvent,
                (state, timedOut) =>
                {
                    if (Dispatcher.HasShutdownStarted) return;
                    try { Dispatcher.BeginInvoke(activate); }
                    catch (InvalidOperationException) { } // Closing while an activation was queued.
                }, null, Timeout.Infinite, false);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _activationWait?.Unregister(null);
            _activationEvent?.Dispose();
            _mutex?.Dispose();
            _windowsTheme?.Dispose();
            base.OnExit(e);
        }
    }
}
