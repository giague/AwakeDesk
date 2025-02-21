using AwakeDesk.Models;
using AwakeDesk.Utils;
using AwakeDesk.Utils.Models;
using System.Configuration;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Navigation;
using System.Windows.Threading;

namespace AwakeDesk.Helpers
{
    public static class AwakeDeskHelpers
    {

        #region DllImport
        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out Point lpPoint);

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromPoint(Point pt, uint dwFlags);

        [DllImport("Shcore.dll")]
        private static extern int GetDpiForMonitor(IntPtr hmonitor, int dpiType, out uint dpiX, out uint dpiY);


        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, out int pvParam, uint fWinIni);

        [DllImport("user32.dll")]
        private static extern bool GetLastInputInfo(ref LastInputInfo plii);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, [MarshalAs(UnmanagedType.LPArray), In] Input[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowPos(nint hWnd, nint hWndInsertAfter, int X, int Y, int width, int height, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        #endregion

        #region Consts 
        private const uint SPI_GETSCREENSAVETIMEOUT = 14;
        private const int INPUT_MOUSE = 0;
        private const uint MOUSEEVENTF_MOVE = 0x0001;
        private const uint MOUSEEVENTF_ABSOLUTE = 0x8000;

        private static readonly nint HWND_TOPMOST = new nint(-1);
        private static readonly uint SWP_NOMOVE = 0x0002;
        private static readonly uint SWP_NOSIZE = 0x0001;

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_LAYERED = 0x80000;
        private const int WS_EX_TRANSPARENT = 0x20;
        private const int MDT_EFFECTIVE_DPI = 0;
        private const double DEFAULT_DPI = 96.0;
        private const int MONITOR_DEFAULTTONEAREST = 2;

        private const string KOFI_URL = "https://ko-fi.com/giague";
        #endregion 

        #region Structures
        public struct Point
        {
            public Point()
            {

            }
            public Point(double x, double y)
            {
                X = (int)x;
                Y = (int)y;
            }
            public int X { get; set; }
            public int Y { get; set; }
        }
        public struct AreaRect
        {
            public int startX { get; set; }
            public int startY { get; set; }
            public int endX { get; set; }
            public int endY { get; set; }
            public AreaRect(Point point)
            {
                startX = point.X;
                startY = point.Y;
                endX = startX + AwakeVariables.NEXT_POS_AREA_WIDTH;
                endY = startY + AwakeVariables.NEXT_POS_AREA_HEIGHT;
            }

            public Point PointInRect(Point point)
            {
                if (point.X < startX)
                { point.X = startX; }
                else if (point.X > endX)
                { point.X = endX; }
                if (point.Y < startY)
                { point.Y = startY; }
                else if (point.Y > endY)
                { point.Y = endY; }
                return point;
            }

        }

        [StructLayout(LayoutKind.Sequential)]
        private struct LastInputInfo
        {
            public uint cbSize;
            public uint dwTime;
        }


        [StructLayout(LayoutKind.Sequential)]
        public struct Input
        {
            public int type;
            public InputUnion u;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct InputUnion
        {
            [FieldOffset(0)]
            public MouseInput mi;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MouseInput
        {
            public int dx;
            public int dy;
            public int mouseData;
            public uint dwFlags;
            public uint time;
            public nint dwExtraInfo;
        }
        #endregion

        public static int GetScreenSaverTimeout()
        {
            int timeout;
            SystemParametersInfo(SPI_GETSCREENSAVETIMEOUT, 0, out timeout, 0);
            return timeout;
        }

        public static int GetWindowLongExtendedStyle(IntPtr hWnd)
        {
            return GetWindowLong(hWnd, GWL_EXSTYLE);
        }

        public static void SetWindowLongExtendedStyle(IntPtr hWnd, int extendedStyle)
        {
            SetWindowLong(hWnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT | WS_EX_LAYERED);
        }

        public static Point GetCursorPosition()
        {
            Point mousePos;
            GetCursorPos(out mousePos);
            IntPtr monitor = MonitorFromPoint(mousePos, 2);
            uint dpiX, dpiY;
            double scaleFactor = 1.0;
            if (GetDpiForMonitor(monitor, MDT_EFFECTIVE_DPI, out dpiX, out dpiY) == 0)
            {
                scaleFactor = dpiX / DEFAULT_DPI;
            }
            return new Point(mousePos.X / scaleFactor, mousePos.Y / scaleFactor);
        }

        public static int GetIdleTime()
        {
            LastInputInfo lastInputInfo = new LastInputInfo();
            lastInputInfo.cbSize = (uint)Marshal.SizeOf(lastInputInfo);

            if (!GetLastInputInfo(ref lastInputInfo))
            {
                return 0;
            }
            int elapsedMilliseconds = Environment.TickCount - (int)lastInputInfo.dwTime;
            return elapsedMilliseconds / 1000;
        }
        public static void SetCursorPosition(Point point)
        {
            Input[] inputs = new Input[1];
            inputs[0].type = INPUT_MOUSE; // INPUT_MOUSE
            inputs[0].u.mi.dwFlags = MOUSEEVENTF_MOVE;
            inputs[0].u.mi.dx = point.X * (65536 / GetSystemMetrics(0));
            inputs[0].u.mi.dy = point.Y * (65536 / GetSystemMetrics(1));
            inputs[0].u.mi.dwFlags = MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_MOVE;
            SendInput(1, inputs, Marshal.SizeOf(typeof(Input)));
        }

        public static void MakeWindowAlwaysOnTop(Window window)
        {
            nint hwnd = new System.Windows.Interop.WindowInteropHelper(window).Handle;

            SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
        }

        public static void OpenDonatePage()
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = KOFI_URL,
                UseShellExecute = true
            });
        }

        public static void OpenWebPage(string url)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }

        public async static Task<NewReleaseInfo?> CheckUpdates()
        {
            var updater = new UpdateChecker();
            return await updater.RetrieveNewRelease(App.AwakeDeskSettings.CurrentVersion);
        }
    }
}
