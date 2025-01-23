using AwakeDesk.Models;
using System.Configuration;
using System.Data;
using System.Runtime.InteropServices;
using System.Windows;

namespace AwakeDesk
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern uint SetThreadExecutionState(uint esFlags);
        public static AwakeDeskSettings AwakeDeskSettings { get; private set; }
        public static AwakeVariables AwakeVariables { get; private set; }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            SetThreadExecutionState(0x80000002);
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            SetThreadExecutionState(0x80000000);
        }
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            AwakeDeskSettings = new();
            AwakeDeskSettings.LoadFromConfiguration();
            AwakeVariables = new();
        }
    }

}
