using System;
using System.Runtime.InteropServices;

namespace AccesClientWPF.Helpers
{
    public static class DpiHelper
    {
        // Shcore: GetProcessDpiAwareness / GetDpiForMonitor (Win 8.1+)
        private static readonly IntPtr _shcore;
        private static readonly GetDpiForMonitorDelegate? _getDpiForMonitor;
        private static readonly GetProcessDpiAwarenessDelegate? _getProcessDpiAwareness;

        static DpiHelper()
        {
            if (NativeLibrary.TryLoad("Shcore.dll", out _shcore))
            {
                if (NativeLibrary.TryGetExport(_shcore, "GetDpiForMonitor", out var p1))
                    _getDpiForMonitor = Marshal.GetDelegateForFunctionPointer<GetDpiForMonitorDelegate>(p1);

                if (NativeLibrary.TryGetExport(_shcore, "GetProcessDpiAwareness", out var p2))
                    _getProcessDpiAwareness = Marshal.GetDelegateForFunctionPointer<GetProcessDpiAwarenessDelegate>(p2);
            }
        }

        public enum ProcessDpiAwareness
        {
            Unaware = 0,
            SystemAware = 1,
            PerMonitorAware = 2,
            Unknown = -1
        }

        public static ProcessDpiAwareness GetProcessDpiAwarenessSafe()
        {
            try
            {
                if (_getProcessDpiAwareness is null)
                    return ProcessDpiAwareness.Unknown;

                var hr = _getProcessDpiAwareness(GetCurrentProcess(), out var awareness);
                return hr == 0 ? awareness : ProcessDpiAwareness.Unknown;
            }
            catch
            {
                return ProcessDpiAwareness.Unknown;
            }
        }

        public static bool IsPerMonitorAwareProcess()
            => GetProcessDpiAwarenessSafe() == ProcessDpiAwareness.PerMonitorAware;

        public static bool TryGetMonitorEffectiveDpi(IntPtr hMonitor, out uint dpiX, out uint dpiY)
        {
            dpiX = 96;
            dpiY = 96;

            try
            {
                if (_getDpiForMonitor is null || hMonitor == IntPtr.Zero)
                    return false;

                // MDT_EFFECTIVE_DPI = 0
                return _getDpiForMonitor(hMonitor, 0, out dpiX, out dpiY) == 0 && dpiX > 0 && dpiY > 0;
            }
            catch
            {
                dpiX = 96;
                dpiY = 96;
                return false;
            }
        }

        public static (double scaleX, double scaleY) GetScaleForPoint(int xPx, int yPx)
        {
            // 1) DPI monitor (si dispo)
            var hMon = MonitorFromPoint(new POINT { x = xPx, y = yPx }, 2 /*MONITOR_DEFAULTTONEAREST*/);
            if (TryGetMonitorEffectiveDpi(hMon, out var dx, out var dy))
                return (dx / 96.0, dy / 96.0);

            // 2) Fallback : DPI système (GDI)
            var (sx, sy) = GetSystemScaleFallback();
            return (sx, sy);
        }

        private static (double sx, double sy) GetSystemScaleFallback()
        {
            IntPtr hdc = IntPtr.Zero;
            try
            {
                hdc = GetDC(IntPtr.Zero);
                if (hdc == IntPtr.Zero) return (1.0, 1.0);

                int dpiX = GetDeviceCaps(hdc, LOGPIXELSX);
                int dpiY = GetDeviceCaps(hdc, LOGPIXELSY);

                if (dpiX <= 0) dpiX = 96;
                if (dpiY <= 0) dpiY = 96;

                return (dpiX / 96.0, dpiY / 96.0);
            }
            catch
            {
                return (1.0, 1.0);
            }
            finally
            {
                if (hdc != IntPtr.Zero)
                    ReleaseDC(IntPtr.Zero, hdc);
            }
        }

        // Delegates
        private delegate int GetDpiForMonitorDelegate(IntPtr hmonitor, int dpiType, out uint dpiX, out uint dpiY);
        private delegate int GetProcessDpiAwarenessDelegate(IntPtr hProcess, out ProcessDpiAwareness awareness);

        // P/Invoke minimal ultra stable
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetCurrentProcess();

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int x; public int y; }

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll")]
        private static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

        private const int LOGPIXELSX = 88;
        private const int LOGPIXELSY = 90;
    }
}