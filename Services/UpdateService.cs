using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using AccesClientWPF.Helpers;
using AccesClientWPF.Models;
using AccesClientWPF.Views;

namespace AccesClientWPF.Services
{
    public sealed class UpdateService
    {
        public const string RemoteZipPath =
            @"\\172.16.0.49\Partage\Utilisateurs\R.Antunes-Barata\Documentation\Applications Antunes\App Client.zip";

        public const string PreferredZipVersionEntry = "Acces_client/version.txt";

        public async Task<UpdateDecision> CheckAndHandleOnStartupAsync()
        {
            var local = AppVersion.Current;
            var localStr = AppVersion.CurrentString;

            string remoteStr;
            try
            {
                remoteStr = await Task.Run(ReadVersionFromZip).ConfigureAwait(true);
            }
            catch
            {
                return UpdateDecision.NoUpdateOrUnavailable;
            }

            var remote = AppVersion.ParseSafe(remoteStr);

            if (remote <= local)
                return UpdateDecision.NoUpdateOrUnavailable;

            var vm = new ViewModels.UpdatePromptViewModel(localStr, remoteStr, RemoteZipPath);
            var dlg = new UpdatePromptWindow { DataContext = vm };

            var owner = Application.Current?.MainWindow;
            if (owner != null && !ReferenceEquals(owner, dlg))
            {
                dlg.Owner = owner;
                dlg.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }
            else
            {
                dlg.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                dlg.Topmost = true;
            }

            var result = dlg.ShowDialog();
            if (result != true)
                return UpdateDecision.UserSkipped;

            // ✅ Ici on lance l'UpdaterHost (UI) et on ferme AccesClientWPF
            StartUiUpdaterAndExit(remoteStr);

            return UpdateDecision.ExitForUpdate;
        }

        private static string ReadVersionFromZip()
        {
            if (!File.Exists(RemoteZipPath))
                throw new FileNotFoundException("ZIP introuvable", RemoteZipPath);

            using var fs = new FileStream(RemoteZipPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var zip = new ZipArchive(fs, ZipArchiveMode.Read, leaveOpen: false);

            ZipArchiveEntry? entry = zip.Entries.FirstOrDefault(e =>
                string.Equals(NormPath(e.FullName), NormPath(PreferredZipVersionEntry), StringComparison.OrdinalIgnoreCase));

            entry ??= zip.Entries.FirstOrDefault(e =>
                NormPath(e.FullName).EndsWith("version.txt", StringComparison.OrdinalIgnoreCase));

            if (entry == null)
                throw new InvalidDataException("version.txt introuvable dans le ZIP.");

            using var sr = new StreamReader(entry.Open(), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            return (sr.ReadToEnd() ?? "").Trim();
        }

        private static string NormPath(string p) => p.Replace('\\', '/').TrimStart('/');

        private static void StartUiUpdaterAndExit(string remoteVersion)
        {
            var installDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\');
            var exePath = Process.GetCurrentProcess().MainModule?.FileName ?? "";
            var pid = Process.GetCurrentProcess().Id;

            var requireAdmin = !UpdateApplyService.CanWriteToDirectory(installDir);

            var pending = new UpdatePendingInfo
            {
                ZipPath = RemoteZipPath,
                InstallDir = installDir,
                TargetExePath = exePath,
                OriginalPid = pid,
                RemoteVersion = remoteVersion,
                RequireAdmin = requireAdmin
            };

            UpdateApplyService.WritePending(pending);

            var updaterExe = Path.Combine(installDir, "AccesClientUpdaterHost.exe");
            if (!File.Exists(updaterExe))
                throw new FileNotFoundException("AccesClientUpdaterHost.exe introuvable.", updaterExe);

            Process.Start(new ProcessStartInfo
            {
                FileName = updaterExe,
                UseShellExecute = true,
                WorkingDirectory = installDir
            });

            Application.Current.Shutdown();
        }
    }

    public enum UpdateDecision
    {
        NoUpdateOrUnavailable,
        UserSkipped,
        ExitForUpdate
    }
}