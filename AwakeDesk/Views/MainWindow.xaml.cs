
using AwakeDesk.Helpers;
using AwakeDesk.Models;
using NLog;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using static AwakeDesk.Helpers.AwakeDeskHelpers;

namespace AwakeDesk.Views
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private const int LOWER_BOUND_FOR_RANDOM_SCREENSAVER_PREVENT = 5;
        private const int UPPER_BOUND_FOR_RANDOM_SCREENSAVER_PREVENT = 20;
        private const int SCREENSAVER_TIMEOUT_OVERRIDE = 300;
        private const string ACTUAL_TIME_FORMAT = "HH:mm:ss";
        private const string CLOCK_TRAY_ICON_BASE = "../Media/img/ClockTray.png";
        private const string CLOCK_TRAY_ICON_HIGHLITED = "../Media/img/ClockTrayHighlighted.png";

        private SettingsWindow settingsWindow;
        private bool isSettingWindowOpen = false;
        private string actualTime;
        private int elapsedIdle;
        private int screenSaverPreventTimeout;
        private readonly int screenSaverTimeout;
        private readonly DispatcherTimer mainTimer;
        private readonly DispatcherTimer moveTimer;
        private AwakeDeskHelpers.Point currentPos = new AwakeDeskHelpers.Point() { X = 0, Y = 0 };
        private AwakeDeskHelpers.Point nextPos = new AwakeDeskHelpers.Point() { X = 0, Y = 0 };
        private AwakeDeskHelpers.Point movingPos = new AwakeDeskHelpers.Point() { X = 0, Y = 0 };
        private System.Drawing.Size PrimaryScreenSize;
        private readonly Random rnd = new();
        private double moverDelay = 0;
        private MediaPlayer alarmPlayer;
        private int alarmPlayngTime = 0;
        private int alarmStartedMinute = -1;
        private bool isClockIconLighting = false;
        private bool isClockIconHighlighted;
        private DateTime lastOnTopForcing;

        public event PropertyChangedEventHandler? PropertyChanged;

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public MainWindow()
        {
            InitializeComponent();
            isClockIconHighlighted = false;
            lastOnTopForcing = DateTime.Now.AddMinutes(-1);
            AwakeDeskHelpers.MakeWindowAlwaysOnTop(this);
            DataContext = this;
            this.MouseLeftButtonDown += (sender, e) =>
            {
                if (this.alarmPlayngTime > 0)
                {
                    isClockIconLighting = false;
                    alarmPlayngTime = 100;
                }
                if (e.ButtonState == MouseButtonState.Pressed)
                {
                    this.DragMove();
                }
            };

            double screenWidth = SystemParameters.PrimaryScreenWidth;
            double screenHeight = SystemParameters.PrimaryScreenHeight;
            PrimaryScreenSize = new System.Drawing.Size((int)screenWidth, (int)screenHeight + 2);
            this.Left = PrimaryScreenSize.Width - (this.Width + 73);
            this.Top = PrimaryScreenSize.Height - (this.Height + 26);


            GetAppConfig();
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
            var closeTimeParts = App.AwakeDeskSettings.Preset1.Split(":");
            App.AwakeVariables.ClosingDateTime = new(dataAttuale.Year, dataAttuale.Month, dataAttuale.Day, int.Parse(closeTimeParts[0]), int.Parse(closeTimeParts[1]), 0, DateTimeKind.Local);
            SetClosingTime();
        }

        private void Donate_Click(object sender, RoutedEventArgs e)
        {
            string koFiUrl = "https://ko-fi.com/giague";
            Process.Start(new ProcessStartInfo
            {
                FileName = koFiUrl,
                UseShellExecute = true
            });
        }
        private void OpenSettings_Click(object sender, RoutedEventArgs e)
        {
            if (!IsSettingWindowOpen)
            {
                IsSettingWindowOpen = true;
                settingsWindow = new();
                settingsWindow.Show();
                settingsWindow.WindowClosed += SettingsWindow_WindowClosed;

            }
        }
        private void SettingsWindow_WindowClosed(object sender, EventArgs e)
        {
            IsSettingWindowOpen = false;
            GetAppConfig();
            SetClosingTime();
            AwakeDeskHelpers.MakeWindowAlwaysOnTop(this);
        }

        private void QuitApplication_Click(object sender, RoutedEventArgs e)
        {
            var confirmWindow = new ConfirmWindow();
            if (confirmWindow.ShowDialog().GetValueOrDefault())
            {
                Application.Current.Shutdown();
            }
        }

        private void CalculateScreenSaverPreventTimeout()
        {
            elapsedIdle = AwakeDeskHelpers.GetIdleTime();
            screenSaverPreventTimeout = screenSaverTimeout - rnd.Next(LOWER_BOUND_FOR_RANDOM_SCREENSAVER_PREVENT, UPPER_BOUND_FOR_RANDOM_SCREENSAVER_PREVENT);
        }

        private void ChangeImageSource(string imagePath)
        {
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(imagePath, UriKind.Relative);
            bitmap.EndInit();

            ClockIcon.Source = bitmap;
        }

        private void GenerateNextPoint()
        {
            currentPos = AwakeDeskHelpers.GetCursorPosition();
            nextPos = new AwakeDeskHelpers.Point()
            {
                X = rnd.Next(App.AwakeVariables.MouseDestinationAreaPoint.X, App.AwakeVariables.MouseDestinationAreaPoint.X + AwakeVariables.NEXT_POS_AREA_WIDTH),
                Y = rnd.Next(App.AwakeVariables.MouseDestinationAreaPoint.Y, App.AwakeVariables.MouseDestinationAreaPoint.Y + AwakeVariables.NEXT_POS_AREA_HEIGHT)
            };
        }

        private void GetAppConfig()
        {
            App.AwakeDeskSettings.LoadFromConfiguration();
            alarmPlayer = new();
            alarmPlayer.Open(new Uri(App.AwakeDeskSettings.ActiveAlarmRingtone, UriKind.RelativeOrAbsolute));
        }

        private void SetClosingTime()
        {
            ClosingTime = App.AwakeVariables.ClosingDateTime.ToString("HH:mm");
            OnPropertyChanged(nameof(ClosingTime));
            alarmStartedMinute = -1;
        }

        private void ToggleClockTextBlockLighting()
        {
            if (isClockIconLighting)
            {
                isClockIconHighlighted = !isClockIconHighlighted;
                if (isClockIconHighlighted)
                {
                    ChangeImageSource(CLOCK_TRAY_ICON_HIGHLITED);
                    return;
                }
                ChangeImageSource(CLOCK_TRAY_ICON_BASE);
            }
        }

        #region Timers
        private void MainTimer_Tick(object? sender, EventArgs e)
        {
            var now = DateTime.Now;
            ActualTime = now.ToString(ACTUAL_TIME_FORMAT);

            if (!isSettingWindowOpen && (now - lastOnTopForcing).TotalSeconds > 10)
            {
                // Updates ontop only after 10 seconds since last time, to prevent glitches
                lastOnTopForcing = now;
                AwakeDeskHelpers.MakeWindowAlwaysOnTop(this);
            }

            if (elapsedIdle >= screenSaverPreventTimeout && !moveTimer.IsEnabled)
            {
                GenerateNextPoint();
                moverDelay = 0;
                moveTimer.Start();
            }
            else
            {
                elapsedIdle = AwakeDeskHelpers.GetIdleTime();
                int secsToNextMove = screenSaverPreventTimeout - elapsedIdle;
            }

            if (alarmPlayngTime != 0)
            {
                alarmPlayngTime++;
                if (alarmPlayngTime > 60)
                {
                    alarmPlayngTime = 0;
                    isClockIconLighting = false;
                    isClockIconHighlighted = false;
                    ChangeImageSource(CLOCK_TRAY_ICON_BASE);
                    alarmPlayer.Stop();
                }

            }


            if (DateTime.Now.Hour == App.AwakeVariables.AlarmDateTime.Hour
                && DateTime.Now.Minute == App.AwakeVariables.AlarmDateTime.Minute
                && alarmStartedMinute != DateTime.Now.Minute
                && alarmPlayngTime == 0)
            {
                alarmStartedMinute = DateTime.Now.Minute;
                isClockIconLighting = true;
                alarmPlayngTime++;
                alarmPlayer.Position = TimeSpan.FromSeconds(0);
                alarmPlayer.Play();
            }

            if (DateTime.Now.Hour == App.AwakeVariables.ClosingDateTime.Hour && DateTime.Now.Minute == App.AwakeVariables.ClosingDateTime.Minute)
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
                movingPos = App.AwakeVariables.MouseDestinationAreaRect.PointInRect(movingPos);
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
            get { return App.AwakeVariables.ClosingTime; }
            set
            {
                if (App.AwakeVariables.ClosingTime != value)
                {
                    App.AwakeVariables.ClosingTime = value;
                    OnPropertyChanged(nameof(ClosingTime));
                }
            }
        }

        public bool IsSettingWindowOpen
        {
            get => isSettingWindowOpen; set
            {
                isSettingWindowOpen = value;
                SettingContextMenuItem.IsEnabled = !isSettingWindowOpen;
            }
        }
        #endregion

    }
}