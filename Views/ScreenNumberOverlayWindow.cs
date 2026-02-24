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
            // Si le process n’est PAS per-monitor aware :
            // les valeurs de GetMonitorInfo sont déjà dans l’espace "logique" du process -> NE PAS reconvertir,
            // sinon tu sous-dimensionnes sur les écrans zoomés.
            if (!AccesClientWPF.Helpers.DpiHelper.IsPerMonitorAwareProcess())
                return new Rect(leftPx, topPx, widthPx, heightPx);

            // Process per-monitor aware : coords en pixels physiques -> conversion vers DIPs
            var (scaleX, scaleY) = AccesClientWPF.Helpers.DpiHelper.GetScaleForPoint(leftPx + 1, topPx + 1);

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