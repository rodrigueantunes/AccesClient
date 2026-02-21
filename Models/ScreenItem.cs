namespace AccesClientWPF.Models
{
    public sealed class ScreenItem
    {
        public int Index { get; }
        public string DeviceName { get; }
        public int Left { get; }
        public int Top { get; }
        public int Width { get; }
        public int Height { get; }
        public bool IsPrimary { get; }
        public string DisplayName { get; }

        public ScreenItem(int index, string deviceName, int left, int top, int width, int height, bool isPrimary)
        {
            Index = index;
            DeviceName = deviceName ?? "";
            Left = left;
            Top = top;
            Width = width;
            Height = height;
            IsPrimary = isPrimary;

            var p = isPrimary ? " (Principal)" : "";
            DisplayName = $"Écran {index + 1} - {width}x{height}{p}";
        }

        public override string ToString() => DisplayName;
    }
}
