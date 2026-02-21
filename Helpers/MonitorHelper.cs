using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AccesClientWPF.Models;
using System.Linq;
using System.Text.RegularExpressions;

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
                        bool isPrimary = (info.dwFlags & 1) != 0;

                        int width = b.Right - b.Left;
                        int height = b.Bottom - b.Top;

                        result.Add(new ScreenItem(
                            index,
                            info.szDevice ?? "",
                            b.Left,
                            b.Top,
                            width,
                            height,
                            isPrimary
                        ));

                        index++;
                    }

                    return true;
                },
                IntPtr.Zero
            );

            return result;
        }

        private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

        [DllImport("user32.dll")]
        private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

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

        public static List<ScreenItem> OrderByWindowsLayout(IEnumerable<ScreenItem> screens)
        {
            // Ordre "rangement Windows" : simple et efficace
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

            // Map: ScreenIndex -> position dans l'ordre Windows (0..n-1)
            var pos = new Dictionary<int, int>();
            for (int i = 0; i < orderedAll.Count; i++)
                pos[orderedAll[i].Index] = i;

            // Positions des écrans sélectionnés dans l’ordre Windows
            var positions = new List<int>();
            foreach (var s in selected)
            {
                if (!pos.TryGetValue(s.Index, out var p))
                    continue; // si pas trouvé, on ignore (ultra simple)
                positions.Add(p);
            }

            if (positions.Count <= 1)
                return true;

            positions.Sort();

            // ✅ Test ULTRA simple: les positions doivent être contiguës (bloc)
            // ex: [0,1] OK ; [1,2] OK ; [0,2] KO
            int min = positions[0];
            int max = positions[^1];
            bool contiguousBlock = (max - min + 1) == positions.Count;

            if (!contiguousBlock)
            {
                reason = "Multi-écran désactivé : sélectionne des écrans consécutifs dans l’ordre Windows (ex: 1-4 ou 4-3).";
                return false;
            }

            return true;
        }
    }
}
