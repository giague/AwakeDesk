using System.Runtime.InteropServices;
using System.Windows;
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
        #endregion

        #region Consts 
        private const uint SPI_GETSCREENSAVETIMEOUT = 14;
        private const int INPUT_MOUSE = 0;
        private const uint MOUSEEVENTF_MOVE = 0x0001;
        private const uint MOUSEEVENTF_ABSOLUTE = 0x8000;

        private static readonly nint HWND_TOPMOST = new nint(-1);
        private static readonly uint SWP_NOMOVE = 0x0002;
        private static readonly uint SWP_NOSIZE = 0x0001;
        #endregion 

        #region Structures
        public struct Point
        {
            public int X { get; set; }
            public int Y { get; set; }
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

        public static Point GetCursorPosition()
        {
            Point point;
            GetCursorPos(out point);
            return point;
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
    }
}
