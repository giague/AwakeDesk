using AwakeDesk.Models;
using AwakeDesk.Utils.Models;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace AwakeDesk.Views
{
    public partial class ConfirmWindow : Window, INotifyPropertyChanged
    {

        private string softwareName = string.Empty;
        private string softwareVersion = string.Empty;
        private bool confirmed = false;
        private string text = string.Empty;
        private NewReleaseInfo? newReleaseInfo = null;


        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler WindowClosed;


        public ConfirmWindow()
        {
            InitializeComponent();
            releaseNotes.Visibility = Visibility.Hidden;
            DataContext = this;
            SoftwareName = AwakeDeskSettings.SOFTWARE_NAME;
            SoftwareVersion = App.AwakeDeskSettings.CurrentVersionLabel;

        }

        private void CloseWindow()
        {
            this.DialogResult = confirmed;
            this.Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            // Scatena l'evento quando la finestra viene chiusa
            WindowClosed?.Invoke(this, EventArgs.Empty);
        }


        private void ControlBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        #region UI Handlers
        private void ReleaseNotes_Click(object sender, RoutedEventArgs e)
        {
            if (NewReleaseInfo != null)
            {
                var releaseNotesWindow = new ReleaseNotesWindow(NewReleaseInfo.ReleaseNotes, NewReleaseInfo.Version);
                releaseNotesWindow.ShowDialog();
            }
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            confirmed = true;
            CloseWindow();
        }
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            confirmed = false;
            CloseWindow();
        }

        #endregion

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public string Text
        {
            get { return text; }
            set
            {
                if (text != value)
                {
                    text = value;
                    OnPropertyChanged(nameof(Text));
                }
            }
        }
        public string SoftwareName
        {
            get { return softwareName; }
            set
            {
                if (softwareName != value)
                {
                    softwareName = value;
                    OnPropertyChanged(nameof(SoftwareName));
                }
            }
        }

        public string SoftwareVersion
        {
            get { return softwareVersion; }
            set
            {
                if (softwareVersion != value)
                {
                    softwareVersion = value;
                    OnPropertyChanged(nameof(SoftwareVersion));
                }
            }
        }

        public NewReleaseInfo? NewReleaseInfo
        {
            get => newReleaseInfo;
            set
            {
                newReleaseInfo = value;
                if (newReleaseInfo != null)
                {
                    releaseNotes.Visibility = Visibility.Visible;
                }
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            CloseWindow();
        }
    }
}