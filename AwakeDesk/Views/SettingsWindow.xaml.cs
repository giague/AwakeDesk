using AwakeDesk.Helpers;
using AwakeDesk.Models;
using FontAwesome.Sharp;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Threading;

namespace AwakeDesk.Views
{
    public partial class SettingsWindow : Window, INotifyPropertyChanged
    {
        private const string PANEL_TIME = "TIME";
        private const string PANEL_PREFERENCES = "SETTINGS";
        private const string PANEL_CREDITS = "INFO";

        private string _currentSettingPanel;
        private string _caption;
        private IconChar _panelIcon;
        private SolidColorBrush _titleColor;

        private bool capturingMouse = false;
        private bool mouseAreaToggled = false;
        private int catchCoundDownCounter = 3;
        private bool areSettingsValid;
        private MediaPlayer demoPlayer;
        private bool isDemoPlaying = false;
        private readonly DispatcherTimer mouseCaptureTimer;

        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler WindowClosed;

        private MouseCaptureWindow captureWindow;

        public SettingsWindow()
        {
            InitializeComponent();
            DataContext = this;

            captureWindow = new MouseCaptureWindow();

            //Initialize commands
            ShowTimePanelCommand = new ViewModelCommand(ExecuteShowTimePanelCommand);
            ShowSettingsPanelCommand = new ViewModelCommand(ExecuteShowSettingsPanelCommand);
            ShowCreditsPanelCommand = new ViewModelCommand(ExecuteShowCretitsPanelCommand);

            //Default panel
            ExecuteShowTimePanelCommand(null);

            SoftwareVersion.Text = App.AwakeDeskSettings.CurrentVersion;
            AlarmDelayMinutesSetting.Text = App.AwakeDeskSettings.AlarmDelayMinutes.ToString();
            Preset1Setting.Text = App.AwakeDeskSettings.Preset1;
            Preset2Setting.Text = App.AwakeDeskSettings.Preset2;
            if (App.AwakeDeskSettings.AvailableAlarmRingtones.Count > 0)
            {
                RingtoneComboBox.Items.Clear();
                foreach (var ringtoneItem in App.AwakeDeskSettings.AvailableAlarmRingtones)
                {
                    RingtoneComboBox.Items.Add(ringtoneItem);
                }

                foreach (var item in RingtoneComboBox.Items)
                {
                    if (item is RingtoneItem ringtoneItem && ringtoneItem.Path == App.AwakeDeskSettings.ActiveAlarmRingtone)
                    {
                        RingtoneComboBox.SelectedItem = item;
                        break;
                    }
                }
            }
            mouseCaptureTimer = new DispatcherTimer();
            mouseCaptureTimer.Interval = TimeSpan.FromSeconds(1);
            mouseCaptureTimer.Tick += MouseCatchTimer_Tick;
            CapturingMouse = false;
            CheckSettings();
        }

        private void CheckSettings()
        {
            var alarmDelayValid = IsAlarmDelayMinutesValid();
            var preset1Valid = CheckPreset(Preset1Setting, Preset1Error);
            var preset2Valid = CheckPreset(Preset2Setting, Preset2Error);
            var ringValid = RingtoneComboBox.SelectedItem != null;
            AreSettingsValid = alarmDelayValid
                                && preset1Valid
                                && preset2Valid
                                && ringValid;
        }

        private void CloseWindow()
        {
            if (isDemoPlaying)
            {
                demoPlayer.Stop();
            }
            this.Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            // Scatena l'evento quando la finestra viene chiusa
            WindowClosed?.Invoke(this, EventArgs.Empty);
        }

        private void DemoPlayerToggle()
        {
            if (!isDemoPlaying)
            {
                StartDemoPlayer();
                return;
            }
            isDemoPlaying = false;
            PlayPauseIcon.Icon = IconChar.Play;
            demoPlayer.Stop();
        }

        private bool IsAlarmDelayMinutesValid()
        {
            var isValid = int.TryParse(AlarmDelayMinutesSetting.Text, out int result);
            if (isValid)
            {
                isValid = result >= 1 && result <= 30;

            }
            if (isValid)
            {
                AlarmDelayMinutesError.Visibility = Visibility.Collapsed;
                AlarmDelayMinutesSetting.BorderBrush = System.Windows.Media.Brushes.Green;
                return true;
            }
            AlarmDelayMinutesError.Text = "Invalid (1-30)";
            AlarmDelayMinutesError.Visibility = Visibility.Visible;
            AlarmDelayMinutesSetting.BorderBrush = System.Windows.Media.Brushes.Red;
            return false;

        }

        private void SetCaptureMouseText()
        {
            var counterText = catchCoundDownCounter.ToString();
            captureWindow.UpdateCounterText(counterText);
        }

        private void SetClosingTime()
        {
            ClosingTime = App.AwakeVariables.ClosingDateTime.ToString("HH:mm");
        }

        private void StartDemoPlayer()
        {
            isDemoPlaying = true;
            PlayPauseIcon.Icon = IconChar.Pause;
            demoPlayer = new();
            demoPlayer.Open(new Uri(((RingtoneItem)RingtoneComboBox.SelectedItem).Path, UriKind.RelativeOrAbsolute));
            demoPlayer.Play();
            demoPlayer.MediaEnded += (sender, e) =>
            {
                demoPlayer.Position = TimeSpan.FromSeconds(0);
            };
        }

        #region UI Handlers
        private void AddRemoveTime_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button clickedButton)
            {
                switch (clickedButton.Name)
                {
                    case "Minus1Hour":
                        App.AwakeVariables.ClosingDateTime = App.AwakeVariables.ClosingDateTime.AddHours(-1);
                        break;
                    case "Minus5Minutes":
                        App.AwakeVariables.ClosingDateTime = App.AwakeVariables.ClosingDateTime.AddMinutes(-5);
                        break;
                    case "Minus1Minute":
                        App.AwakeVariables.ClosingDateTime = App.AwakeVariables.ClosingDateTime.AddMinutes(-1);
                        break;
                    case "Plus1Hour":
                        App.AwakeVariables.ClosingDateTime = App.AwakeVariables.ClosingDateTime.AddHours(1);
                        break;
                    case "Plus5Minutes":
                        App.AwakeVariables.ClosingDateTime = App.AwakeVariables.ClosingDateTime.AddMinutes(5);
                        break;
                    case "Plus1Minute":
                        App.AwakeVariables.ClosingDateTime = App.AwakeVariables.ClosingDateTime.AddMinutes(1);
                        break;
                }
                SetClosingTime();
            }
        }

        private void ShowCaptureWindow(bool toggleOnly)
        {
            mouseAreaToggled = toggleOnly;
            captureWindow.Close();
            captureWindow = new MouseCaptureWindow();
            captureWindow.Init(mouseAreaToggled);
            captureWindow.Show();
        }

        private void CaptureMouse_Click(object sender, RoutedEventArgs e)
        {
            ShowCaptureWindow(false);
            SetCaptureMouseText();
            mouseCaptureTimer.Start();
            CapturingMouse = true;
        }

        private void ToggleMouseArea_Click(object sender, RoutedEventArgs e)
        {
            if (!mouseAreaToggled)
            {
                ShowCaptureWindow(true);
                return;
            }
            mouseAreaToggled = false;
            captureWindow.Close();
        }
        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }

        private void ControlBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void Preset_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button clickedButton)
            {
                string[]? closeTimeParts;
                switch (clickedButton.Name)
                {
                    case "Preset1":
                        closeTimeParts = App.AwakeDeskSettings.Preset1.Split(":");
                        break;
                    case "Preset2":
                        closeTimeParts = App.AwakeDeskSettings.Preset2.Split(":");
                        break;
                    default:
                        return;
                }

                var dataAttuale = DateTime.Now;
                App.AwakeVariables.ClosingDateTime = new(dataAttuale.Year, dataAttuale.Month, dataAttuale.Day, int.Parse(closeTimeParts[0]), int.Parse(closeTimeParts[1]), 0, DateTimeKind.Local);
                SetClosingTime();
            }
        }

        private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            DemoPlayerToggle();
        }

        private void RingtoneComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            CheckSettings();
            if (RingtoneComboBox.SelectedItem is RingtoneItem selectedRingtone)
            {
                if (isDemoPlaying)
                {
                    demoPlayer.Stop();
                    StartDemoPlayer();
                }
            }
        }

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            CheckSettings();
            if (!AreSettingsValid)
            {
                return;
            }
            App.AwakeDeskSettings.AlarmDelayMinutes = int.Parse(AlarmDelayMinutesSetting.Text);
            App.AwakeDeskSettings.Preset1 = Preset1Setting.Text;
            App.AwakeDeskSettings.Preset2 = Preset2Setting.Text;
            App.AwakeDeskSettings.ActiveAlarmRingtone = ((RingtoneItem)RingtoneComboBox.SelectedItem).Path;
            App.AwakeDeskSettings.SaveConfiguration();
            CloseWindow();
        }

        private void Settings_GotFocus(object sender, RoutedEventArgs e)
        {
            var tb = sender as TextBox;
            tb.SelectAll();
        }

        private void Settings_LostFocus(object sender, RoutedEventArgs e)
        {
            CheckSettings();
        }
        #endregion

        private static bool CheckPreset(TextBox presetTextBox, TextBlock presetErrorTextBlock)
        {
            string timePattern = @"^([01][0-9]|2[0-3]):[0-5][0-9]$";
            string value = presetTextBox.Text;
            if (int.TryParse(value, out int intTime))
            {
                var isValid = intTime >= 0 && intTime <= 23;
                if (isValid)
                {
                    presetTextBox.Text = $"{intTime.ToString("D2")}:00";
                }
            }

            if (Regex.IsMatch(presetTextBox.Text, timePattern))
            {
                presetErrorTextBlock.Visibility = Visibility.Collapsed;
                presetTextBox.BorderBrush = System.Windows.Media.Brushes.Green;
                return true;
            }
            presetErrorTextBlock.Text = "Invalid format (HH:mm)";
            presetErrorTextBlock.Visibility = Visibility.Visible;
            presetTextBox.BorderBrush = System.Windows.Media.Brushes.Red;
            return false;
        }

        #region Timers

        private void MouseCatchTimer_Tick(object? sender, EventArgs e)
        {
            catchCoundDownCounter--;
            SetCaptureMouseText();
            if (catchCoundDownCounter <= 0)
            {
                App.AwakeVariables.MouseDestinationAreaPoint = AwakeDeskHelpers.GetCursorPosition();
                CapturingMouse = false;
                mouseCaptureTimer.Stop();
                captureWindow.Close();
                catchCoundDownCounter = 3;
            }
        }
        #endregion

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #region Properties

        public bool AreSettingsValid
        {
            get { return areSettingsValid; }
            set
            {
                if (areSettingsValid != value)
                {
                    areSettingsValid = value;
                    OnPropertyChanged(nameof(AreSettingsValid));
                }
            }
        }

        public bool CapturingMouse
        {
            get { return capturingMouse; }
            set
            {
                if (capturingMouse != value)
                {
                    capturingMouse = value;
                    CaptureMouseButton.IsEnabled = !capturingMouse;
                    ToggleMouseArea.IsEnabled = !capturingMouse;
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

        public string Preset1Text
        {
            get { return App.AwakeDeskSettings.Preset1; }
            set
            {
                if (App.AwakeDeskSettings.Preset1 != value)
                {
                    App.AwakeDeskSettings.Preset1 = value;
                    OnPropertyChanged(nameof(Preset1Text));
                }
            }
        }

        public string Preset2Text
        {
            get { return App.AwakeDeskSettings.Preset2; }
            set
            {
                if (App.AwakeDeskSettings.Preset2 != value)
                {
                    App.AwakeDeskSettings.Preset2 = value;
                    OnPropertyChanged(nameof(Preset2Text));
                }
            }
        }

        public string CurrentSettingPanel
        {
            get => _currentSettingPanel;
            set
            {
                _currentSettingPanel = value;
                pnlTime.Visibility = (_currentSettingPanel == PANEL_TIME) ? Visibility.Visible : Visibility.Collapsed;
                pnlPreferences.Visibility = (_currentSettingPanel == PANEL_PREFERENCES) ? Visibility.Visible : Visibility.Collapsed;
                pnlCredits.Visibility = (_currentSettingPanel == PANEL_CREDITS) ? Visibility.Visible : Visibility.Collapsed;
                OnPropertyChanged(nameof(CurrentSettingPanel));
            }
        }
        public string Caption
        {
            get => _caption;
            set
            {
                _caption = value;
                OnPropertyChanged(nameof(Caption));
            }
        }
        public IconChar PanelIcon
        {
            get => _panelIcon;
            set
            {
                _panelIcon = value;
                OnPropertyChanged(nameof(PanelIcon));
            }
        }
        public SolidColorBrush TitleColor
        {
            get => _titleColor;
            set
            {
                _titleColor = value;
                OnPropertyChanged(nameof(TitleColor));
            }
        }

        #endregion

        #region Commands
        public ICommand ShowTimePanelCommand { get; }
        public ICommand ShowSettingsPanelCommand { get; }
        public ICommand ShowCreditsPanelCommand { get; }


        private void ExecuteShowTimePanelCommand(object obj)
        {
            CurrentSettingPanel = PANEL_TIME;
            Caption = "Time & Mouse settings";
            PanelIcon = IconChar.Clock;
            TitleColor = (SolidColorBrush)this.FindResource("color1");
        }
        private void ExecuteShowSettingsPanelCommand(object obj)
        {
            CurrentSettingPanel = PANEL_PREFERENCES;
            Caption = "Preferences";
            PanelIcon = IconChar.Gear;
            TitleColor = (SolidColorBrush)this.FindResource("color2");
        }
        private void ExecuteShowCretitsPanelCommand(object obj)
        {
            CurrentSettingPanel = PANEL_CREDITS;
            Caption = "Credits";
            PanelIcon = IconChar.Info;
            TitleColor = (SolidColorBrush)this.FindResource("color3");
        }

        #endregion

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            CloseWindow();
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
    }
}