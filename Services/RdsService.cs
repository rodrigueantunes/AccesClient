using System.Diagnostics;

namespace AccesClientWPF.Services
{
    public class RdsService
    {
        public static void StartRds(string ip, bool multiMonitor = false)
        {
            string arguments = $"/v:{ip}" + (multiMonitor ? " /multimon" : "");
            Process.Start(new ProcessStartInfo("mstsc.exe", arguments) { UseShellExecute = true });
        }
    }
}