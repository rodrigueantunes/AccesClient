using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Security.Principal;
using System.Text.Json;
using System.Threading.Tasks;

namespace AccesClientUpdaterHost.Services
{
    public sealed class UpdaterHostService
    {
        public event Action<double, string>? Progress;

        private static string PendingPath =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                         "AccesClient", "update.pending.json");

        public async Task RunAsync()
        {
            if (!File.Exists(PendingPath))
                throw new FileNotFoundException("Pending introuvable.", PendingPath);

            var pendingJson = await File.ReadAllTextAsync(PendingPath);
            var info = JsonSerializer.Deserialize<PendingInfo>(pendingJson)
                       ?? throw new InvalidDataException("Pending invalide.");

            // UAC si nécessaire
            if (info.RequireAdmin && !IsAdmin())
            {
                RestartSelfAsAdmin();
                Environment.Exit(0);
                return;
            }

            Report(0, "Attente fermeture application…");
            await WaitPidExit(info.OriginalPid);

            Report(5, "Extraction du ZIP…");
            var tempExtract = Path.Combine(Path.GetTempPath(), "AccesClientExtract_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempExtract);

            try
            {
                ZipFile.ExtractToDirectory(info.ZipPath, tempExtract, overwriteFiles: true);

                // Certains zips contiennent un dossier racine "Acces_client"
                var preferred = Path.Combine(tempExtract, "Acces_client");
                var sourceRoot = Directory.Exists(preferred) ? preferred : tempExtract;

                await CopyWithProgress(sourceRoot, info.InstallDir);

                TryDelete(PendingPath);

                Report(100, "Relance de l’application…");
                Process.Start(new ProcessStartInfo
                {
                    FileName = info.TargetExePath,
                    UseShellExecute = true,
                    WorkingDirectory = info.InstallDir
                });
            }
            finally
            {
                try { Directory.Delete(tempExtract, true); } catch { /* ignore */ }
            }
        }

        private async Task CopyWithProgress(string sourceRoot, string targetRoot)
        {
            var files = Directory.GetFiles(sourceRoot, "*", SearchOption.AllDirectories);

            long total = 0;
            foreach (var f in files) total += Math.Max(1, new FileInfo(f).Length);
            long done = 0;

            foreach (var src in files)
            {
                var rel = Path.GetRelativePath(sourceRoot, src);
                var dst = Path.Combine(targetRoot, rel);

                // ✅ IMPORTANT : ne jamais écraser l’UpdaterHost pendant qu’il tourne
                // (corrige ton IOException "being used by another process" sur AccesClientUpdaterHost.dll)
                var fileName = Path.GetFileName(dst);
                if (IsUpdaterSelfFile(fileName))
                {
                    Report(Map(done, total, 10, 98), $"Skip : {rel}");
                    done += Math.Max(1, new FileInfo(src).Length);
                    continue;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(dst)!);

                Report(Map(done, total, 10, 98), $"Copie : {rel}");
                await CopyFileSafeAsync(src, dst);

                done += Math.Max(1, new FileInfo(src).Length);
            }

            Report(99, "Finalisation…");
        }

        private static bool IsUpdaterSelfFile(string fileName)
        {
            // Filtre simple et efficace : tout ce qui commence par AccesClientUpdaterHost
            // => AccesClientUpdaterHost.dll/.exe/.pdb/.deps.json/.runtimeconfig.json etc.
            return fileName.StartsWith("AccesClientUpdaterHost", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Copie robuste:
        /// - copie vers dst.tmp
        /// - remplace atomiquement (File.Replace) si dst existe
        /// - retries si fichier lock
        /// </summary>
        private static async Task CopyFileSafeAsync(string src, string dst)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(dst)!);

            var tmp = dst + ".tmp";
            var bak = dst + ".bak";

            const int bufferSize = 1024 * 1024;
            const int maxRetries = 10;
            int delayMs = 120;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    TryDelete(tmp);
                    TryDelete(bak);

                    // Source: FileShare.ReadWrite (si un AV ou autre scanne le fichier)
                    using (var s = new FileStream(src, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufferSize, useAsync: true))
                    using (var d = new FileStream(tmp, FileMode.CreateNew, FileAccess.Write, FileShare.None, bufferSize, useAsync: true))
                    {
                        await s.CopyToAsync(d);
                    }

                    // Remplacement final
                    if (File.Exists(dst))
                    {
                        // Atomique (mais échoue si dst lock)
                        File.Replace(tmp, dst, bak, ignoreMetadataErrors: true);
                        TryDelete(bak);
                    }
                    else
                    {
                        File.Move(tmp, dst);
                    }

                    return;
                }
                catch (IOException) when (attempt < maxRetries)
                {
                    // lock / antivirus / indexation / process pas totalement down
                    await Task.Delay(delayMs);
                    delayMs = Math.Min(delayMs * 2, 800);
                }
                finally
                {
                    // cleanup si ça a foiré
                    TryDelete(tmp);
                }
            }

            throw new IOException($"Impossible de copier '{src}' vers '{dst}' : fichier verrouillé (retries épuisés).");
        }

        private static async Task WaitPidExit(int pid)
        {
            if (pid <= 0) return;
            try
            {
                var p = Process.GetProcessById(pid);
                await Task.Run(() => p.WaitForExit());
            }
            catch
            {
                // déjà fermé / pid invalide
            }
        }

        private static double Map(long done, long total, double start, double end)
        {
            if (total <= 0) return start;
            var ratio = (double)done / total;
            return start + (end - start) * ratio;
        }

        private void Report(double p, string s)
            => Progress?.Invoke(Math.Max(0, Math.Min(100, p)), s);

        private static bool IsAdmin()
        {
            using var id = WindowsIdentity.GetCurrent();
            var p = new WindowsPrincipal(id);
            return p.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private static void RestartSelfAsAdmin()
        {
            var exe = Process.GetCurrentProcess().MainModule?.FileName ?? "";
            var psi = new ProcessStartInfo(exe)
            {
                UseShellExecute = true,
                Verb = "runas"
            };
            Process.Start(psi);
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { /* ignore */ }
        }

        private sealed class PendingInfo
        {
            public string ZipPath { get; set; } = "";
            public string InstallDir { get; set; } = "";
            public string TargetExePath { get; set; } = "";
            public int OriginalPid { get; set; }
            public string RemoteVersion { get; set; } = "";
            public bool RequireAdmin { get; set; }
        }
    }
}