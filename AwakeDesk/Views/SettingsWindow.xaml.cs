using System.ComponentModel;
using System.Configuration;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Text.RegularExpressions;
using System.IO;
using System.Runtime;
using AwakeDesk.Models;
using AwakeDesk.Helpers;
using System.Diagnostics;
using System.Windows.Navigation;

namespace AwakeDesk.Views
{
    public partial class SettingsWindow : Window, INotifyPropertyChanged
    {
        private bool capturingMouse = false;
        private int catchCoundDownCounter = 3;
        private string captureMouseText = string.Empty;
        private bool areSettingsValid;
        private MediaPlayer demoPlayer;
        private bool isDemoPlaying = false;
        private readonly DispatcherTimer mouseCaptureTimer;

        public event PropertyChangedEventHandler? PropertyChanged;

        public SettingsWindow()
        {
            InitializeComponent();
            DataContext = this;
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
            AreSettingsValid = IsAlarmDelayMinutesValid()
                                && CheckPreset(Preset1Setting, Preset1Error)
                                && CheckPreset(Preset2Setting, Preset2Error)
                                && RingtoneComboBox.SelectedItem != null;
        }

        private void CloseSettings()
        {
            if (isDemoPlaying)
            {
                demoPlayer.Stop();
            }
            this.Close();
        }

        private void DemoPlayerToggle()
        {
            if (!isDemoPlaying)
            {
                StartDemoPlayer();
                return;
            }
            isDemoPlaying = false;
            PlayPauseButton.Content = "▶";
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
            CaptureMouseText = $"Mouse position will be captured in {catchCoundDownCounter.ToString()} seconds";
        }

        private void SetClosingTime()
        {
            ClosingTime = App.AwakeVariables.ClosingDateTime.ToString("HH:mm");
        }

        private void StartDemoPlayer()
        {
            isDemoPlaying = true;
            PlayPauseButton.Content = "⏸";
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

        private void CaptureMouse_Click(object sender, RoutedEventArgs e)
        {
            SetCaptureMouseText();
            mouseCaptureTimer.Start();
            CapturingMouse = true;
        }

        private void CloseApplication_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure to close Awake Desk?",
                "Confirm Action",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Yes)
            {
                Application.Current.Shutdown();
            }
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
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
            CloseSettings();
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
                App.AwakeVariables.MouseDestinationStartingPoint = AwakeDeskHelpers.GetCursorPosition();
                CapturingMouse = false;
                mouseCaptureTimer.Stop();
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

        public string CaptureMouseText
        {
            get { return captureMouseText; }
            set
            {
                if (captureMouseText != value)
                {
                    captureMouseText = value;
                    OnPropertyChanged(nameof(CaptureMouseText));
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
                    CaptureMouseButton.Visibility = capturingMouse ? Visibility.Collapsed : Visibility.Visible;
                    CaptureMouseTextBlock.Visibility = capturingMouse ? Visibility.Visible : Visibility.Collapsed;
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
       
        #endregion
    }
}