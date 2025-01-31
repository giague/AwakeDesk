using AwakeDesk.Models;
using System.Configuration;
using System.Data;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;

namespace AwakeDesk
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static Mutex mutex = new Mutex(true, "AwakeDeskApp");

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern uint SetThreadExecutionState(uint esFlags);
        public static AwakeDeskSettings AwakeDeskSettings { get; private set; }
        public static AwakeVariables AwakeVariables { get; private set; }

        protected override void OnExit(ExitEventArgs e)
        {
            mutex.ReleaseMutex();
            SetThreadExecutionState(0x80000000);
            base.OnExit(e);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            if (!mutex.WaitOne(TimeSpan.Zero, true))
            {
                Application.Current.Shutdown();
                return;
            }
            base.OnStartup(e);
            SetThreadExecutionState(0x80000002);
            AwakeDeskSettings = new();
            AwakeDeskSettings.LoadFromConfiguration();
            AwakeVariables = new();
        }
    }

}
