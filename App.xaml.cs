using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using AccesClientWPF.Services;
using AccesClientWPF.Views;

namespace AccesClientWPF
{
    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // 1) Test d'accès ULTRA rapide au ZIP réseau (RemoteZipPath).
            // Si pas atteignable => on ne propose PAS la MAJ, on démarre direct.
            bool zipReachable = false;
            try
            {
                zipReachable = await CanReachRemoteZipAsync(UpdateService.RemoteZipPath, timeoutMs: 300);
            }
            catch
            {
                zipReachable = false;
            }

            // 2) Check update + prompt (ne doit JAMAIS empêcher l'ouverture)
            if (zipReachable)
            {
                try
                {
                    var updateService = new UpdateService();
                    var decision = await updateService.CheckAndHandleOnStartupAsync();

                    if (decision == UpdateDecision.ExitForUpdate)
                    {
                        Shutdown();
                        return;
                    }
                }
                catch
                {
                    // Ignorer toute erreur de MAJ (offline, partage KO, timeout, etc.)
                }
            }
            // else => ZIP pas atteignable => pas de MAJ

            // 3) Démarrage normal
            var main = new MainWindow();
            MainWindow = main;
            ShutdownMode = ShutdownMode.OnMainWindowClose;
            main.Show();
        }

        /// <summary>
        /// Test rapide d'accès au ZIP:
        /// - évite de bloquer sur un partage réseau KO
        /// - ne lit pas le zip, juste "exists + open stream"
        /// </summary>
        private static async Task<bool> CanReachRemoteZipAsync(string zipPath, int timeoutMs = 300)
        {
            if (string.IsNullOrWhiteSpace(zipPath))
                return false;

            using var cts = new CancellationTokenSource(timeoutMs);

            var task = Task.Run(() =>
            {
                try
                {
                    if (!File.Exists(zipPath))
                        return false;

                    // Ouverture simple => valide l'accès (droits, réseau, lock)
                    using var fs = new FileStream(zipPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    return fs.Length > 0; // length => force accès au metadata
                }
                catch
                {
                    return false;
                }
            }, cts.Token);

            try
            {
                var completed = await Task.WhenAny(task, Task.Delay(timeoutMs, cts.Token));
                if (completed != task) return false; // timeout
                return await task;
            }
            catch
            {
                return false;
            }
        }
    }
}