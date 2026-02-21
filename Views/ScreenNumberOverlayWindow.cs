using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace AccesClientWPF.Views
{
    public sealed class ScreenNumberOverlayWindow : Window
    {
        public ScreenNumberOverlayWindow(int number, Rect boundsInDips)
        {
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;
            Topmost = true;
            AllowsTransparency = true;
            Background = Brushes.Transparent;

            Left = boundsInDips.Left;
            Top = boundsInDips.Top;
            Width = boundsInDips.Width;
            Height = boundsInDips.Height;

            var root = new Grid
            {
                Background = new SolidColorBrush(Color.FromArgb(210, 0, 0, 0)) // noir semi-transparent
            };

            var txt = new TextBlock
            {
                Text = number.ToString(),
                Foreground = new SolidColorBrush(Color.FromRgb(231, 76, 60)), // rouge #E74C3C
                FontSize = 220,
                FontWeight = FontWeights.Black,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            root.Children.Add(txt);
            Content = root;

            // clic = fermer (pratique)
            MouseDown += (_, __) => Close();
            KeyDown += (_, e) => { if (e.Key == Key.Escape) Close(); };
        }

        // --- DPI helper (fiable multi-DPI, sans usine à gaz)
        public static Rect PixelsToDips(int leftPx, int topPx, int widthPx, int heightPx)
        {
            // fallback 96dpi
            uint dpiX = 96, dpiY = 96;
            try
            {
                var pt = new POINT { x = leftPx + 1, y = topPx + 1 };
                IntPtr hMon = MonitorFromPoint(pt, 2 /*MONITOR_DEFAULTTONEAREST*/);
                if (hMon != IntPtr.Zero)
                    GetDpiForMonitor(hMon, 0 /*MDT_EFFECTIVE_DPI*/, out dpiX, out dpiY);
            }
            catch { /* ignore */ }

            double scaleX = dpiX / 96.0;
            double scaleY = dpiY / 96.0;

            return new Rect(
                leftPx / scaleX,
                topPx / scaleY,
                widthPx / scaleX,
                heightPx / scaleY
            );
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int x; public int y; }

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

        [DllImport("Shcore.dll")]
        private static extern int GetDpiForMonitor(IntPtr hmonitor, int dpiType, out uint dpiX, out uint dpiY);
    }
}