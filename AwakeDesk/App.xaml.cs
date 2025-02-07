using AwakeDesk.Models;
using NLog;
using System.Runtime.InteropServices;
using System.Windows;

namespace AwakeDesk
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static Mutex mutex = new Mutex(true, "AwakeDeskApp");
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern uint SetThreadExecutionState(uint esFlags);
        public static AwakeDeskSettings AwakeDeskSettings { get; private set; }
        public static AwakeVariables AwakeVariables { get; private set; }

        protected override void OnExit(ExitEventArgs e)
        {
            mutex.ReleaseMutex();
            SetThreadExecutionState(0x80000000);
            logger.Info("Application closed.");
            base.OnExit(e);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            logger.Info("Trying start application.");
            if (!mutex.WaitOne(TimeSpan.Zero, true))
            {
                Application.Current.Shutdown();
                return;
            }
            base.OnStartup(e);
            this.DispatcherUnhandledException += Application_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            SetThreadExecutionState(0x80000002);
            AwakeDeskSettings = new();
            AwakeDeskSettings.LoadFromConfiguration();
            AwakeVariables = new();
            logger.Info("Application started.");
        }

        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {

            var logger = LogManager.GetCurrentClassLogger(); // Get logger here, because otherwise it might fail to load NLog
            logger.Fatal(e.Exception, "Unhandled UI Exception");

            MessageBox.Show("Unexpected exception. Check log file.");

            e.Handled = true;

            // Application.Current.Shutdown();
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            // Log dell'eccezione non gestita nel dominio dell'applicazione
            var logger = LogManager.GetCurrentClassLogger(); // Get logger here, because otherwise it might fail to load NLog
            var exception = e.ExceptionObject as Exception;
            if (exception != null)
            {
                logger.Fatal(exception, "Unhandled Exception in AppDomain");
            }
            else
            {
                logger.Fatal("Unhandled Exception in AppDomain - Exception object is null.");
            }

            MessageBox.Show("Unexpected exception. Check log file.");

            Application.Current.Shutdown();
        }

    }

}
