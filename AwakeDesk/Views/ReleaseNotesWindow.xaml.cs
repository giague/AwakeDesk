using AwakeDesk.Models;
using Markdig;
using Microsoft.Web.WebView2.Core;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace AwakeDesk.Views
{
    public partial class ReleaseNotesWindow : Window, INotifyPropertyChanged
    {

        private string softwareName = string.Empty;
        private string softwareVersion = string.Empty;
        private bool confirmed = false;

        private string? releaseNoteMarkdown = null;
        private string? releaseVersion = null;


        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler? WindowClosed;


        public ReleaseNotesWindow(string? releaseNoteToShow = null, string? versionToShow = null)
        {
            InitializeComponent();
            DataContext = this;
            SoftwareName = AwakeDeskSettings.SOFTWARE_NAME;
            SoftwareVersion = App.ADSettings.CurrentVersionLabel;
            releaseNoteMarkdown = releaseNoteToShow;
            releaseVersion = versionToShow;
            _ = InitializeWebView();

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
        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            CloseWindow();
        }

        private async Task InitializeWebView()
        {
            await webView.EnsureCoreWebView2Async();
            LoadReleaseNotes();
        }

        private void LoadReleaseNotes()
        {
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "release_notes.txt");
            string? markdown = releaseNoteMarkdown;
            if (string.IsNullOrEmpty(markdown) && File.Exists(filePath))
            {
                markdown = File.ReadAllText(filePath);
                releaseVersion = App.ADSettings.CurrentVersion;
            }
            if (!string.IsNullOrEmpty(markdown))
            {
                markdown = string.Concat("# Awake Desk v.", releaseVersion, "\n", markdown);
                string html = Markdown.ToHtml(markdown);

                string fullHtml = $@"
                <html>
                <head>
                    <meta charset='utf-8'>
                    <style>
                        body {{ font-family: Arial, sans-serif; padding: 20px; line-height: 1.6; background-color: white;}}
                        h1, h2, h3 {{ color: #333; }}
                    </style>
                </head>
                <body>{html}</body>
                </html>";

                webView.NavigateToString(fullHtml);
            }
            else
            {
                webView.NavigateToString("<html><body><h2>Nessuna release note disponibile.</h2></body></html>");
            }
        }

        private void webView_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            if (Uri.TryCreate(e.Uri, UriKind.Absolute, out Uri? uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                // Annulla la navigazione nel WebView2
                e.Cancel = true;

                // Apri il link nel browser predefinito
                Process.Start(new ProcessStartInfo
                {
                    FileName = uri.AbsoluteUri,
                    UseShellExecute = true
                });
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
    }
}