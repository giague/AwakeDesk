using AwakeDesk.Helpers;
using AwakeDesk.Models;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace AwakeDesk.Views
{
    public partial class MouseCaptureWindow : Window
    {
        private int mousePosX;
        private int mousePosY;
        private bool toggleMode;
        DispatcherTimer? timer;
        public MouseCaptureWindow()
        {
            InitializeComponent();
        }

        public void Init(bool toggleOnly)
        {
            toggleMode = toggleOnly;
            this.Width = AwakeVariables.NEXT_POS_AREA_WIDTH;
            this.Height = AwakeVariables.NEXT_POS_AREA_HEIGHT;
            this.Left = App.ADVariables.MouseDestinationAreaPoint.X;
            this.Top = App.ADVariables.MouseDestinationAreaPoint.Y;
            this.Loaded += Window_Loaded;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            // Window "click-through"
            var hWnd = new WindowInteropHelper(this).Handle;
            int extendedStyle = AwakeDeskHelpers.GetWindowLongExtendedStyle(hWnd);
            AwakeDeskHelpers.SetWindowLongExtendedStyle(hWnd, extendedStyle);
            if (!toggleMode)
            {
                UpdateRectanglePosition(null, null);
                timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromMilliseconds(5);
                timer.Tick += UpdateRectanglePosition;
                timer.Start();
            }
            if (toggleMode)
            {
                UpdateCounterText(string.Empty);
            }
        }
        protected override void OnClosed(EventArgs e)
        {
            if (!toggleMode && timer != null && timer.IsEnabled)
            {
                timer.Stop();
            }
            base.OnClosed(e);
        }

        private void UpdateRectanglePosition(object? sender, EventArgs? e)
        {
            var mousePos = AwakeDeskHelpers.GetCursorPosition();
            mousePosX = mousePos.X;
            mousePosY = mousePos.Y;
            this.Top = mousePosY;
            this.Left = mousePosX;
        }

        public void UpdateCounterText(string text)
        {
            counterText.Text = text;
        }

    }
}
