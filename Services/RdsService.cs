using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace AccesClientWPF.Services
{
    public class RdsService
    {
        public static void StartRds(string ip, string username, string password, bool multiMonitor = false, string connectionName = "RDS Connection")
        {
            try
            {
                // Vérifier si le mot de passe est vide
                if (string.IsNullOrEmpty(password))
                {
                    throw new Exception($"Aucun mot de passe n'a été renseigné pour la connexion RDS '{connectionName}'.");
                }

                // Création du fichier RDP avec le titre personnalisé
                string rdpFilePath = Path.Combine(Path.GetTempPath(), $"{connectionName.Replace(' ', '_')}_{Guid.NewGuid().ToString().Substring(0, 8)}.rdp");

                using (StreamWriter sw = new StreamWriter(rdpFilePath))
                {
                    // Format RDP standard
                    sw.WriteLine("screen mode id:i:2");
                    sw.WriteLine($"full address:s:{ip}");
                    sw.WriteLine($"username:s:{username}");
                    sw.WriteLine("prompt for credentials:i:0");
                    sw.WriteLine("desktopwidth:i:0");
                    sw.WriteLine("desktopheight:i:0");
                    sw.WriteLine("session bpp:i:32");
                    sw.WriteLine($"use multimon:i:{(multiMonitor ? "1" : "0")}");
                    sw.WriteLine("connection type:i:7");
                    sw.WriteLine("networkautodetect:i:1");
                    sw.WriteLine("bandwidthautodetect:i:1");
                    sw.WriteLine("authentication level:i:2");
                    sw.WriteLine("redirectsmartcards:i:1");
                    sw.WriteLine("redirectclipboard:i:1");
                    sw.WriteLine("audiomode:i:0");
                    sw.WriteLine("autoreconnection enabled:i:1");

                    // Paramètres pour définir le titre de la session
                    sw.WriteLine($"alternate shell:s:");
                    sw.WriteLine($"shell working directory:s:");
                    sw.WriteLine($"disable wallpaper:i:0");
                    sw.WriteLine($"allow font smoothing:i:1");
                    sw.WriteLine($"allow desktop composition:i:1");

                    // Définir le titre de la connexion - ce paramètre devrait fonctionner
                    sw.WriteLine($"title:s:{connectionName}");
                    sw.WriteLine($"promptcredentialonce:i:1");
                    sw.WriteLine($"winposstr:s:0,3,0,0,800,600");
                }

                // Stocker les identifiants temporairement
                try
                {
                    ProcessStartInfo cmdKeyInfo = new ProcessStartInfo
                    {
                        FileName = "cmdkey.exe",
                        Arguments = $"/generic:{ip} /user:{username} /pass:{password}",
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        WindowStyle = ProcessWindowStyle.Hidden
                    };

                    using (Process cmdKeyProcess = Process.Start(cmdKeyInfo))
                    {
                        cmdKeyProcess.WaitForExit();
                    }
                }
                catch
                {
                    // Ignorer les erreurs de cmdkey et continuer
                }

                // Lancer mstsc directement
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "mstsc.exe",
                    Arguments = $"\"{rdpFilePath}\" /f",
                    UseShellExecute = true
                };

                using (Process mstscProcess = Process.Start(startInfo))
                {
                    // Ne pas attendre la fin du processus
                }

                // Supprimer le fichier RDP après un délai
                Task.Delay(5000).ContinueWith(_ =>
                {
                    try
                    {
                        if (File.Exists(rdpFilePath))
                        {
                            File.Delete(rdpFilePath);
                        }

                        // Nettoyer les identifiants après un délai plus long
                        Task.Delay(30000).ContinueWith(__ =>
                        {
                            try
                            {
                                ProcessStartInfo cleanupInfo = new ProcessStartInfo
                                {
                                    FileName = "cmdkey.exe",
                                    Arguments = $"/delete:{ip}",
                                    CreateNoWindow = true,
                                    UseShellExecute = false,
                                    WindowStyle = ProcessWindowStyle.Hidden
                                };

                                using (Process cleanupProcess = Process.Start(cleanupInfo))
                                {
                                    // Ne pas attendre
                                }
                            }
                            catch
                            {
                                // Ignorer les erreurs de nettoyage
                            }
                        });
                    }
                    catch
                    {
                        // Ignorer les erreurs de suppression
                    }
                });
            }
            catch (Exception ex)
            {
                throw new Exception($"Erreur lors du démarrage de la connexion RDS : {ex.Message}", ex);
            }
        }
    }
}