using AwakeDesk.Helpers;
using AwakeDesk.Models;
using AwakeDesk.Utils;
using AwakeDesk.Utils.Models;
using AwakeDesk.Views;
using FontAwesome.Sharp;
using NLog;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
namespace AwakeDesk
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application, INotifyPropertyChanged
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern uint SetThreadExecutionState(uint esFlags);
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        public static AwakeDeskSettings ADSettings { get; private set; }
        public static AwakeVariables ADVariables { get; private set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

        public event PropertyChangedEventHandler? PropertyChanged;

        private const string CLOCK_TRAY_ICON_BASE = "Media/img/ClockTray.png";
        private const string CLOCK_TRAY_ICON_HIGHLIGHTED = "Media/img/ClockTrayHighlighted.png";
        private const string ACTUAL_TIME_FORMAT = "HH:mm:ss";
        private const int LOWER_BOUND_FOR_RANDOM_SCREENSAVER_PREVENT = 5;
        private const int UPPER_BOUND_FOR_RANDOM_SCREENSAVER_PREVENT = 20;
        private const int SCREENSAVER_TIMEOUT_OVERRIDE = 300;


        private string actualTime = string.Empty;
        private SettingsWindow? settingsWindow;
        private int elapsedIdle;
        private int screenSaverPreventTimeout;
        private int screenSaverTimeout;
        private DispatcherTimer mainTimer = new();
        private DispatcherTimer moveTimer = new();
        private double moverDelay = 0;
        private MediaPlayer alarmPlayer = new();
        private int alarmPlayngTime = 0;
        private int alarmStartedMinute = -1;
        private bool isClockIconLighting = false;
        private bool isClockIconHighlighted;
        private AwakeDeskHelpers.Point currentPos = new AwakeDeskHelpers.Point() { X = 0, Y = 0 };
        private AwakeDeskHelpers.Point nextPos = new AwakeDeskHelpers.Point() { X = 0, Y = 0 };
        private AwakeDeskHelpers.Point movingPos = new AwakeDeskHelpers.Point() { X = 0, Y = 0 };
        private string _appBasePath = string.Empty;
        private static Mutex mutex = new Mutex(true, "AwakeDeskApp");
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private System.Drawing.Icon trayIcon;
        private System.Drawing.Icon trayIconHighlighted;
        private readonly Random rnd = new();
        private System.Windows.Forms.NotifyIcon _notifyIcon = new();

        public App()
        {
            ADSettings = new();
            ADVariables = new();
            trayIcon = GetIconFromPng(CLOCK_TRAY_ICON_BASE);
            trayIconHighlighted = GetIconFromPng(CLOCK_TRAY_ICON_HIGHLIGHTED);
        }

        #region Events & Handlers
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
            isClockIconHighlighted = false;
            _appBasePath = AppDomain.CurrentDomain.BaseDirectory;

            this.DispatcherUnhandledException += Application_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            _ = SetThreadExecutionState(0x80000002);
            ADSettings.LoadFromConfiguration();
            GetAppConfig();
            CheckForNewVersion();
            screenSaverTimeout = AwakeDeskHelpers.GetScreenSaverTimeout();
            if (SCREENSAVER_TIMEOUT_OVERRIDE < screenSaverTimeout)
            {
                screenSaverTimeout = SCREENSAVER_TIMEOUT_OVERRIDE;
            }
            CalculateScreenSaverPreventTimeout();
            mainTimer = new DispatcherTimer();
            mainTimer.Interval = TimeSpan.FromSeconds(1);
            mainTimer.Tick += MainTimer_Tick;
            mainTimer.Start();

            moveTimer = new DispatcherTimer();
            moveTimer.Interval = TimeSpan.FromMilliseconds(10);
            moveTimer.Tick += MoveTimer_Tick;


            var actualDate = DateTime.Now;
            actualTime = actualDate.ToString(ACTUAL_TIME_FORMAT);

            var dataAttuale = DateTime.Now;
            var closeTimeParts = ADSettings.Preset1.Split(":");
            ADVariables.ClosingDateTime = new(dataAttuale.Year, dataAttuale.Month, dataAttuale.Day, int.Parse(closeTimeParts[0]), int.Parse(closeTimeParts[1]), 0, DateTimeKind.Local);
            SetClosingTime();

            _notifyIcon = new System.Windows.Forms.NotifyIcon
            {
                Icon = trayIcon,
                Visible = true
            };
            _notifyIcon.Click += (s, args) => StopAlarm();

            UpdateNotifyIconText();
            var contextMenu = new System.Windows.Forms.ContextMenuStrip();
            var settingsItem = new System.Windows.Forms.ToolStripMenuItem("Settings", FontAwesome.Sharp.IconChar.Gear.ToBitmap(48, 48, System.Drawing.Color.DarkBlue));
            settingsItem.Click += (s, args) => OpenSettings();
            var separator1Item = new System.Windows.Forms.ToolStripSeparator();
            var koFiItem = new System.Windows.Forms.ToolStripMenuItem("Donate", System.Drawing.Image.FromFile("Media\\img\\kofi_tray.png"));
            koFiItem.Click += (s, args) => AwakeDeskHelpers.OpenDonatePage();
            var separator2Item = new System.Windows.Forms.ToolStripSeparator();
            var updateItem = new System.Windows.Forms.ToolStripMenuItem("Check updates", FontAwesome.Sharp.IconChar.Repeat.ToBitmap(48, 48, System.Drawing.Color.Orange));
            updateItem.Click += (s, args) => CheckForNewVersion();
            var quitItem = new System.Windows.Forms.ToolStripMenuItem("Quit", FontAwesome.Sharp.IconChar.PowerOff.ToBitmap(48, 48, System.Drawing.Color.Red));
            quitItem.Click += (s, args) => CloseApp();

            contextMenu.Items.Add(settingsItem);
            contextMenu.Items.Add(separator1Item);
            contextMenu.Items.Add(koFiItem);
            contextMenu.Items.Add(separator2Item);
            contextMenu.Items.Add(updateItem);
            contextMenu.Items.Add(quitItem);

            _notifyIcon.ContextMenuStrip = contextMenu;
            _notifyIcon.DoubleClick += (s, args) => OpenSettings();

            ADVariables.PropertyChanged += ADVariables_PropertyChanged;
            logger.Info("Application started.");
        }

        private static void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {

            logger.Fatal(e.Exception, "Unhandled UI Exception");
            MessageBox.Show("Unexpected exception. Check log file.");
            e.Handled = true;

        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
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
            ExitApplication();
        }

        private void ADVariables_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AwakeVariables.ClosingTime))
            {
                UpdateNotifyIconText();
            }
        }

        private void SettingsWindow_WindowClosed(object? sender, EventArgs? e)
        {
            IsSettingWindowOpen = false;
            GetAppConfig();
            SetClosingTime();
        }

        #endregion

        public static void CloseApp()
        {
            var confirmWindow = new ConfirmWindow();
            confirmWindow.Text = "Are you sure to close Awake Desk?";
            if (confirmWindow.ShowDialog().GetValueOrDefault())
            {
                Application.Current.Shutdown();
            }
        }

        public void StopAlarm()
        {
            if (this.alarmPlayngTime > 0)
            {
                isClockIconLighting = false;
                alarmPlayngTime = 100;
            }
        }

        public static void CheckForNewVersion()
        {
            var newRelease = Task.Run(async () => await CheckUpdates()).Result;
            if (newRelease != null)
            {
                var confirmWindow = new ConfirmWindow();
                confirmWindow.Text = $"New version {newRelease.Version} available, download?";
                confirmWindow.NewReleaseInfo = newRelease;
                if (confirmWindow.ShowDialog().GetValueOrDefault())
                {
                    AwakeDeskHelpers.OpenWebPage(newRelease.InstallerUrl);
                }
            }
        }

        private void GetAppConfig()
        {
            ADSettings.LoadFromConfiguration();
            alarmPlayer = new();
            alarmPlayer.Open(new Uri(ADSettings.ActiveAlarmRingtone, UriKind.RelativeOrAbsolute));
        }

        private void SetClosingTime()
        {
            ClosingTime = ADVariables.ClosingDateTime.ToString("HH:mm");
            OnPropertyChanged(nameof(ClosingTime));
            alarmStartedMinute = -1;
        }

        private void OpenSettings()
        {
            if (!IsSettingWindowOpen)
            {
                IsSettingWindowOpen = true;
                settingsWindow = new();
                settingsWindow.Show();
                settingsWindow.WindowClosed += SettingsWindow_WindowClosed;

            }
        }

        private void GenerateNextPoint()
        {
            currentPos = AwakeDeskHelpers.GetCursorPosition();
            nextPos = new AwakeDeskHelpers.Point()
            {
                X = rnd.Next(ADVariables.MouseDestinationAreaPoint.X, ADVariables.MouseDestinationAreaPoint.X + AwakeVariables.NEXT_POS_AREA_WIDTH),
                Y = rnd.Next(ADVariables.MouseDestinationAreaPoint.Y, ADVariables.MouseDestinationAreaPoint.Y + AwakeVariables.NEXT_POS_AREA_HEIGHT)
            };
        }

        private void ToggleClockTextBlockLighting()
        {
            if (isClockIconLighting)
            {
                isClockIconHighlighted = !isClockIconHighlighted;
                if (isClockIconHighlighted)
                {
                    ChangeImageSource(trayIconHighlighted);
                    return;
                }
                ChangeImageSource(trayIcon);
            }
        }

        private void CalculateScreenSaverPreventTimeout()
        {
            elapsedIdle = AwakeDeskHelpers.GetIdleTime();
            screenSaverPreventTimeout = screenSaverTimeout - rnd.Next(LOWER_BOUND_FOR_RANDOM_SCREENSAVER_PREVENT, UPPER_BOUND_FOR_RANDOM_SCREENSAVER_PREVENT);
        }

        private void ExitApplication()
        {
            _notifyIcon.Dispose();
            Current.Shutdown();
        }

        public async static Task<NewReleaseInfo?> CheckUpdates()
        {
            var updater = new UpdateChecker();
            return await updater.RetrieveNewRelease(ADSettings.CurrentVersion);
        }

        private void UpdateNotifyIconText()
        {
            _notifyIcon.Text = string.Concat(AwakeDeskSettings.SOFTWARE_NAME, " - ", ADVariables.ClosingTime);
        }

        private System.Drawing.Icon GetIconFromPng(string relativePath)
        {
            System.Drawing.Icon icon;

            string absolutePath = System.IO.Path.Combine(_appBasePath, relativePath);

            using (Bitmap bitmap = (Bitmap)Image.FromFile(absolutePath))
            {
                IntPtr hIcon = bitmap.GetHicon();
                icon = System.Drawing.Icon.FromHandle(hIcon);

            }
            return icon;
        }

        private void ChangeImageSource(System.Drawing.Icon icon)
        {
            _notifyIcon.Icon = icon;
        }


        #region Timers
        private void MainTimer_Tick(object? sender, EventArgs e)
        {
            var now = DateTime.Now;
            ActualTime = now.ToString(ACTUAL_TIME_FORMAT);

            if (elapsedIdle >= screenSaverPreventTimeout && !moveTimer.IsEnabled)
            {
                GenerateNextPoint();
                moverDelay = 0;
                moveTimer.Start();
            }
            else
            {
                elapsedIdle = AwakeDeskHelpers.GetIdleTime();
            }

            if (alarmPlayngTime != 0)
            {
                alarmPlayngTime++;
                if (alarmPlayngTime > 60)
                {
                    alarmPlayngTime = 0;
                    isClockIconLighting = false;
                    isClockIconHighlighted = false;
                    ChangeImageSource(trayIcon);
                    alarmPlayer.Stop();
                }

            }


            if (DateTime.Now.Hour == ADVariables.AlarmDateTime.Hour
                && DateTime.Now.Minute == ADVariables.AlarmDateTime.Minute
                && alarmStartedMinute != DateTime.Now.Minute
                && alarmPlayngTime == 0)
            {
                alarmStartedMinute = DateTime.Now.Minute;
                isClockIconLighting = true;
                alarmPlayngTime++;
                alarmPlayer.Position = TimeSpan.FromSeconds(0);
                alarmPlayer.Play();
            }

            if (DateTime.Now.Hour == ADVariables.ClosingDateTime.Hour && DateTime.Now.Minute == ADVariables.ClosingDateTime.Minute)
            {
                Application.Current.Shutdown();
            }
            ToggleClockTextBlockLighting();
        }

        private void MoveTimer_Tick(object? sender, EventArgs e)
        {
            if (moverDelay <= 1)
            {
                movingPos.X = (int)(currentPos.X * (1 - moverDelay) + nextPos.X * moverDelay);
                movingPos.Y = (int)(currentPos.Y * (1 - moverDelay) + nextPos.Y * moverDelay);
                movingPos = ADVariables.MouseDestinationAreaRect.PointInRect(movingPos);
                AwakeDeskHelpers.SetCursorPosition(movingPos);
                moverDelay += 0.08;
            }
            else
            {
                CalculateScreenSaverPreventTimeout();
                moveTimer.IsEnabled = false;
            }
        }

        #endregion

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #region Properties

        public string ActualTime
        {
            get { return actualTime; }
            set
            {
                if (actualTime != value)
                {
                    actualTime = value;
                    OnPropertyChanged(nameof(ActualTime));
                }
            }
        }

        public string ClosingTime
        {
            get { return ADVariables.ClosingTime; }
            set
            {
                if (ADVariables.ClosingTime != value)
                {
                    ADVariables.ClosingTime = value;
                    OnPropertyChanged(nameof(ClosingTime));
                }
            }
        }

        public bool IsSettingWindowOpen { get; set; }

        #endregion

    }

}
