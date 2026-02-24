using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using AccesClientWPF.Models;

namespace AccesClientWPF.Helpers
{
    public static class MonitorHelper
    {
        public static List<ScreenItem> GetAllScreens()
        {
            var result = new List<ScreenItem>();
            int index = 0;

            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero,
                (IntPtr hMon, IntPtr hdc, ref RECT rc, IntPtr data) =>
                {
                    var info = new MONITORINFOEX();
                    info.cbSize = Marshal.SizeOf<MONITORINFOEX>();

                    if (GetMonitorInfo(hMon, ref info))
                    {
                        var b = info.rcMonitor;
                        bool isPrimary = (info.dwFlags & MONITORINFOF_PRIMARY) != 0;

                        int logicalW = b.Right - b.Left;
                        int logicalH = b.Bottom - b.Top;

                        // Résolution physique (fiable, non DPI-virtualisée)
                        int physicalW = logicalW;
                        int physicalH = logicalH;

                        var dm = new DEVMODE();
                        dm.dmSize = (short)Marshal.SizeOf<DEVMODE>();

                        if (!string.IsNullOrWhiteSpace(info.szDevice) &&
                            EnumDisplaySettings(info.szDevice, ENUM_CURRENT_SETTINGS, ref dm) &&
                            dm.dmPelsWidth > 0 && dm.dmPelsHeight > 0)
                        {
                            physicalW = dm.dmPelsWidth;
                            physicalH = dm.dmPelsHeight;
                        }

                        // DPI (optionnel + fallback)
                        uint dpiX = 96, dpiY = 96;
                        if (DpiHelper.TryGetMonitorEffectiveDpi(hMon, out var dx, out var dy))
                        {
                            dpiX = dx;
                            dpiY = dy;
                        }
                        else
                        {
                            // fallback "intelligent" : ratio physique/logique si différent
                            if (logicalW > 0 && physicalW > 0)
                            {
                                var sx = (double)physicalW / logicalW;
                                dpiX = (uint)Math.Round(96 * sx);
                            }
                            if (logicalH > 0 && physicalH > 0)
                            {
                                var sy = (double)physicalH / logicalH;
                                dpiY = (uint)Math.Round(96 * sy);
                            }
                        }

                        result.Add(new ScreenItem(
                            index,
                            info.szDevice ?? "",
                            b.Left,
                            b.Top,
                            logicalW,
                            logicalH,
                            isPrimary,
                            physicalW,
                            physicalH,
                            dpiX,
                            dpiY));

                        index++;
                    }

                    return true;
                },
                IntPtr.Zero);

            return result;
        }

        public static List<ScreenItem> OrderByWindowsLayout(IEnumerable<ScreenItem> screens)
        {
            return (screens ?? Enumerable.Empty<ScreenItem>())
                .OrderBy(s => s.Left)
                .ThenBy(s => s.Top)
                .ToList();
        }

        public static bool AreSelectedScreensContiguousInWindowsOrder(
            IEnumerable<ScreenItem> allScreensAlreadyInWindowsOrder,
            IEnumerable<ScreenItem> selectedScreens,
            out string reason)
        {
            reason = null;

            var orderedAll = (allScreensAlreadyInWindowsOrder ?? Enumerable.Empty<ScreenItem>()).ToList();
            var selected = (selectedScreens ?? Enumerable.Empty<ScreenItem>()).ToList();

            if (selected.Count <= 1)
                return true;

            var pos = new Dictionary<int, int>();
            for (int i = 0; i < orderedAll.Count; i++)
                pos[orderedAll[i].Index] = i;

            var positions = new List<int>();
            foreach (var s in selected)
            {
                if (pos.TryGetValue(s.Index, out var p))
                    positions.Add(p);
            }

            if (positions.Count <= 1)
                return true;

            positions.Sort();
            int min = positions[0];
            int max = positions[^1];

            if ((max - min + 1) != positions.Count)
            {
                reason = "Multi-écran désactivé : sélectionne des écrans consécutifs dans l’ordre Windows (ex: 1-4 ou 4-3).";
                return false;
            }

            return true;
        }

        private const uint MONITORINFOF_PRIMARY = 0x00000001;
        private const int ENUM_CURRENT_SETTINGS = -1;

        private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

        [DllImport("user32.dll")]
        private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool EnumDisplaySettings(string lpszDeviceName, int iModeNum, ref DEVMODE lpDevMode);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT { public int Left, Top, Right, Bottom; }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct MONITORINFOEX
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string szDevice;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        private struct DEVMODE
        {
            private const int CCHDEVICENAME = 32;
            private const int CCHFORMNAME = 32;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHDEVICENAME)]
            public string dmDeviceName;

            public short dmSpecVersion;
            public short dmDriverVersion;
            public short dmSize;
            public short dmDriverExtra;
            public int dmFields;

            public int dmPositionX;
            public int dmPositionY;
            public int dmDisplayOrientation;
            public int dmDisplayFixedOutput;

            public short dmColor;
            public short dmDuplex;
            public short dmYResolution;
            public short dmTTOption;
            public short dmCollate;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHFORMNAME)]
            public string dmFormName;

            public short dmLogPixels;
            public int dmBitsPerPel;
            public int dmPelsWidth;
            public int dmPelsHeight;
            public int dmDisplayFlags;
            public int dmDisplayFrequency;

            public int dmICMMethod;
            public int dmICMIntent;
            public int dmMediaType;
            public int dmDitherType;
            public int dmReserved1;
            public int dmReserved2;
            public int dmPanningWidth;
            public int dmPanningHeight;
        }
    }
}