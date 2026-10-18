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
        private const int SW_RESTORE = 9;
        private static Mutex _mutex;

        protected override void OnStartup(StartupEventArgs e)
        {
            _mutex = new Mutex(true, "AOSharp_MainWindow_Instance", out bool createdNew);

            if (!createdNew)
            {
                var current = Process.GetCurrentProcess();
                var existing = Process.GetProcessesByName(current.ProcessName)
                    .FirstOrDefault(p => p.Id != current.Id);

                if (existing != null && existing.MainWindowHandle != IntPtr.Zero)
                {
                    ShowWindow(existing.MainWindowHandle, SW_RESTORE);
                    SetForegroundWindow(existing.MainWindowHandle);
                }

                Shutdown();
                return;
            }

            base.OnStartup(e);
        }
    }
}
