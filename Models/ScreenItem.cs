namespace AccesClientWPF.Models
{
    public sealed class ScreenItem
    {
        public int Index { get; }
        public string DeviceName { get; }

        // Coordonnées/tailles renvoyées par GetMonitorInfo (peuvent être virtualisées si process pas per-monitor)
        public int Left { get; }
        public int Top { get; }
        public int Width { get; }     // "logical" (process space)
        public int Height { get; }    // "logical" (process space)

        // Résolution physique (EnumDisplaySettings)
        public int PhysicalWidth { get; }
        public int PhysicalHeight { get; }

        public uint DpiX { get; }
        public uint DpiY { get; }
        public double ScaleX => DpiX / 96.0;
        public double ScaleY => DpiY / 96.0;

        public bool IsPrimary { get; }
        public string DisplayName { get; }

        public ScreenItem(
            int index,
            string deviceName,
            int left,
            int top,
            int width,
            int height,
            bool isPrimary,
            int physicalWidth,
            int physicalHeight,
            uint dpiX,
            uint dpiY)
        {
            Index = index;
            DeviceName = deviceName ?? "";
            Left = left;
            Top = top;
            Width = width;
            Height = height;

            PhysicalWidth = physicalWidth > 0 ? physicalWidth : width;
            PhysicalHeight = physicalHeight > 0 ? physicalHeight : height;

            DpiX = dpiX == 0 ? 96u : dpiX;
            DpiY = dpiY == 0 ? 96u : dpiY;

            IsPrimary = isPrimary;

            var p = isPrimary ? " (Principal)" : "";
            var dpiSuffix = (System.Math.Abs(ScaleX - 1.0) > 0.01) ? $" ({ScaleX * 100:0}% DPI)" : "";
            DisplayName = $"Écran {index + 1} - {PhysicalWidth}x{PhysicalHeight}{p}{dpiSuffix}";
        }

        public override string ToString() => DisplayName;
    }
}