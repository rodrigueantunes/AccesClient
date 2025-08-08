using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using System.Security.Cryptography;
using System.Text;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;
using System.Net;
using System.Threading.Channels;
using System.Threading;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Accès_client
{
    public partial class Form1 : Form
    {
        // Dictionnaire pour stocker les informations de connexion par fichier RDS, y compris l'IP
        private Dictionary<string, (string Ip, string User, string Password)> rdsCredentials = new Dictionary<string, (string, string, string)>();

        public Form1()
        {
            InitializeComponent();
            LoadDirectories();
           
        }
        public class FortiClientVpnConnection
        {
            // Constants for FortiClient API
            private const string FORTICLIENT_DLL = "FORTICLIENT.dll";
            private const int FCTAPI_SUCCESS = 0;

            // API Function imports
            [DllImport(FORTICLIENT_DLL)]
            private static extern int FCT_Initialize();

            [DllImport(FORTICLIENT_DLL)]
            private static extern int FCT_Connect(string vpnName, string server, string username, string password);

            [DllImport(FORTICLIENT_DLL)]
            private static extern int FCT_GetStatus();

            [DllImport(FORTICLIENT_DLL)]
            private static extern int FCT_Disconnect();

            [DllImport(FORTICLIENT_DLL)]
            private static extern void FCT_Cleanup();

            public async Task<bool> ConnectToVPN(string vpnName, string server, string username, string password)
            {
                try
                {
                    // Initialize FortiClient API
                    int result = FCT_Initialize();
                    if (result != FCTAPI_SUCCESS)
                    {
                        throw new Exception($"Failed to initialize FortiClient API. Error code: {result}");
                    }

                    // Attempt to connect
                    result = FCT_Connect(vpnName, server, username, password);
                    if (result != FCTAPI_SUCCESS)
                    {
                        throw new Exception($"Failed to connect to VPN. Error code: {result}");
                    }

                    // Wait for connection to establish
                    bool connected = await WaitForConnection();
                    return connected;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error connecting to VPN: {ex.Message}");
                    return false;
                }
            }

            private async Task<bool> WaitForConnection(int timeoutSeconds = 30)
            {
                int attempts = 0;
                while (attempts < timeoutSeconds)
                {
                    int status = FCT_GetStatus();
                    if (status == 1) // 1 typically indicates connected status
                    {
                        return true;
                    }
                    await Task.Delay(1000);
                    attempts++;
                }
                return false;
            }

            public bool DisconnectFromVPN()
            {
                try
                {
                    int result = FCT_Disconnect();
                    FCT_Cleanup();
                    return result == FCTAPI_SUCCESS;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error disconnecting from VPN: {ex.Message}");
                    return false;
                }
            }
        }
        public static class Prompt
    {
        public static string ShowDialog(string text, string caption)
        {
            Form prompt = new Form()
            {
                Width = 300,
                Height = 150,
                Text = caption,
                StartPosition = FormStartPosition.CenterScreen
            };
            Label textLabel = new Label() { Left = 20, Top = 20, Text = text };
            TextBox textBox = new TextBox() { Left = 20, Top = 50, Width = 240 };
            Button confirmation = new Button() { Text = "OK", Left = 20, Width = 100, Top = 80, DialogResult = DialogResult.OK };
            confirmation.Click += (sender, e) => { prompt.Close(); };
            prompt.Controls.Add(textLabel);
            prompt.Controls.Add(textBox);
            prompt.Controls.Add(confirmation);
            prompt.AcceptButton = confirmation;

            return prompt.ShowDialog() == DialogResult.OK ? textBox.Text : "";
        }
    }
        public class EncryptionHelper
        {
            private static readonly string key = "1234567890123456"; // Clé de 16 caractères pour AES-128
            private static readonly string iv = "1234567890123456"; // IV de 16 caractères pour AES

            // Méthode pour chiffrer un texte
            public static string Encrypt(string plainText)
            {
                using (Aes aesAlg = Aes.Create())
                {
                    aesAlg.Key = Encoding.UTF8.GetBytes(key);
                    aesAlg.IV = Encoding.UTF8.GetBytes(iv);

                    ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                    using (MemoryStream msEncrypt = new MemoryStream())
                    {
                        using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        {
                            using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                            {
                                swEncrypt.Write(plainText);
                            }
                        }
                        return Convert.ToBase64String(msEncrypt.ToArray());
                    }
                }
            }

            // Méthode pour déchiffrer un texte
            public static string Decrypt(string cipherText)
            {
                using (Aes aesAlg = Aes.Create())
                {
                    aesAlg.Key = Encoding.UTF8.GetBytes(key);
                    aesAlg.IV = Encoding.UTF8.GetBytes(iv);

                    ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                    using (MemoryStream msDecrypt = new MemoryStream(Convert.FromBase64String(cipherText)))
                    {
                        using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                        {
                            using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                            {
                                return srDecrypt.ReadToEnd();
                            }
                        }
                    }
                }
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            LoadDirectories();
            LoadRdsCredentials(); // Charger les informations de connexion RDS
        }

        private void LoadDirectories()
        {
            string directoryPath = @"C:\Application\Clients\";
            if (Directory.Exists(directoryPath))
            {
                var directories = Directory.GetDirectories(directoryPath);
                flowLayoutPanelClients.Controls.Clear();

                foreach (var dir in directories)
                {
                    string dirName = Path.GetFileName(dir);

                    Button button = new Button
                    {
                        Text = dirName,
                        Tag = dir,
                        AutoSize = true
                    };

                    button.Click += DirectoryButton_Click;
                    flowLayoutPanelClients.Controls.Add(button);
                }
            }
            else
            {
                MessageBox.Show("Le répertoire spécifié n'existe pas.");
            }
        }

        private void DirectoryButton_Click(object sender, EventArgs e)
        {
            if (sender is Button button)
            {
                string directoryPath = button.Tag as string;
                if (directoryPath != null)
                {
                    LoadFiles(directoryPath);
                }
            }
        }

        private void LoadFiles(string directoryPath)
        {
            flowLayoutPanelAny.Controls.Clear();
            flowLayoutPanelRDS.Controls.Clear();
            flowLayoutPanelVPN.Controls.Clear();

            var files = Directory.GetFiles(directoryPath);

            foreach (var file in files)
            {
                string fileName = Path.GetFileName(file);  // Récupère le nom du fichier complet (avec extension)
                string originalFileName = fileName; // Conserver le nom original pour le texte du bouton

                // Retirer l'extension du fichier
                fileName = Path.GetFileNameWithoutExtension(fileName);  // Supprime l'extension

                // Vérification du préfixe et suppression des 4 premiers caractères si nécessaire
                if (fileName.StartsWith("Any-"))
                {
                    fileName = fileName.Substring(4); // Retirer "Any-"
                }
                else if (fileName.StartsWith("RDS-"))
                {
                    fileName = fileName.Substring(4); // Retirer "RDS-"
                }
                else if (fileName.StartsWith("VPN-"))
                {
                    fileName = fileName.Substring(4); // Retirer "VPN-"
                }

                // Créer le bouton avec le nom modifié (sans extension ni préfixe)
                Button button = new Button
                {
                    Text = fileName,  // Afficher le nom modifié (sans l'extension et préfixe)
                    Tag = file,  // Le chemin complet du fichier
                    AutoSize = true
                };

                button.Click += FileButton_Click;

                // Catégoriser le fichier en fonction du préfixe d'origine
                if (originalFileName.StartsWith("Any-"))
                {
                    flowLayoutPanelAny.Controls.Add(button);
                }
                else if (originalFileName.StartsWith("RDS-"))
                {
                    flowLayoutPanelRDS.Controls.Add(button);
                }
                else if (originalFileName.StartsWith("VPN-"))
                {
                    flowLayoutPanelVPN.Controls.Add(button);
                }
            }
        }

        private void FileButton_Click(object sender, EventArgs e)
        {
            if (sender is Button button)
            {
                string filePath = button.Tag as string;
                string fileNamenext = Path.GetFileNameWithoutExtension(filePath);

                // Vérification du type de fichier
                if (filePath.EndsWith(".rdp"))
                {
                    string fileName = Path.GetFileNameWithoutExtension(filePath);  // Récupérer le nom du fichier
                    string rdsFileName = fileName.Substring(4); // Retirer "RDS-"

                    // Vérifier si les informations sont stockées dans le fichier JSON
                    string jsonFilePath = @"C:\Application\Clients\rds_accounts.json"; // Chemin du fichier JSON
                    RdsData.LoadRdsAccounts(jsonFilePath);  // Charger les comptes RDS depuis le fichier JSON

                    // Rechercher les informations de connexion en fonction de la description
                    var credentials = RdsData.RdsAccounts.FirstOrDefault(a => a.Description.Equals(rdsFileName, StringComparison.OrdinalIgnoreCase));

                    if (credentials != null)
                    {
                        // Récupérer les informations
                        string ip = credentials.IpDns;
                        string user = credentials.NomUtilisateur;
                        string password = credentials.MotDePasse;
                        string decryptpasswd = EncryptionHelper.Decrypt(password);

                        // Créer les informations d'identification
                        CreateCredentials(ip, user, decryptpasswd);

                        // Demander à l'utilisateur s'il souhaite utiliser un ou plusieurs moniteurs
                        string mon = PromptForMultiMonitor();

                        if (mon == "Non")
                        {
                            // Connecter MSTSC pour un seul écran
                            StartRds(ip);
                        }
                        else if (mon == "Oui")
                        {
                            // Connecter MSTSC pour plusieurs écrans
                            StartRds(ip, true);
                        }

                        // Supprimer les informations d'identification après la connexion
                        DeleteCredentials(ip);
                    }
                   
                    else
                    {
                        MessageBox.Show("Aucune information de connexion enregistrée pour ce fichier ");
                    }
                }
                else if (fileNamenext.StartsWith("VPN-Forti") || fileNamenext.StartsWith("VPN-FORTI") || fileNamenext.StartsWith("VPN-forti"))
                {
                    // Vérifier si les informations sont stockées dans le fichier JSON
                    string jsonFilePath = @"C:\Application\Clients\rds_accounts.json"; // Chemin du fichier JSON
                    RdsData.LoadRdsAccounts(jsonFilePath);  // Charger les comptes RDS depuis le fichier JSON

                    string rdsFileName = fileNamenext.Substring(4); // Retirer "VPN-"
                    string[] filedec = fileNamenext.Split('-');
                    // Rechercher les informations de connexion en fonction de la description
                    var credentials = RdsData.RdsAccounts.FirstOrDefault(a => a.Description.Equals("tonnellier", StringComparison.OrdinalIgnoreCase)&& a.NomUtilisateur.StartsWith(filedec[3], StringComparison.OrdinalIgnoreCase));

                    if (credentials != null)
                    {
                        // Récupérer les informations
                        string ip = credentials.IpDns;
                        string user = credentials.NomUtilisateur;
                        string password = credentials.MotDePasse;
                        string vpn = credentials.Description;
                        string decryptpasswd = EncryptionHelper.Decrypt(password);

                        // Connexion VPN

                        ConnectToFortiClientVPN(vpn, ip, user, password, filePath);

                    }
                    else
                    {
                        MessageBox.Show("Aucune information de connexion enregistrée pour ce fichier VPN");
                    }
                }
                else if (filePath != null)
                {
                    Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
                }
            }
        }


        async Task ConnectToFortiClientVPN(string vpn, string ip, string user, string decryptpasswd, string filePath)
        {
            var vpnConnection = new FortiClientVpnConnection();
            bool connected = await vpnConnection.ConnectToVPN(vpn, ip, user, decryptpasswd);

            if (connected)
            {
                MessageBox.Show("Connexion VPN établie avec succès");
            }
            else
            {
                MessageBox.Show("Échec de la connexion VPN / Ouverture de Forticlient Manuelle");
                Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
            }
        }



        private void CreateCredentials(string ip, string user, string decryptpasswd)
        {
            // Utiliser cmdkey pour créer les informations d'identification
            Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/C cmdkey /generic:{ip} /user:\"{user}\" /pass:\"{decryptpasswd}\"",
                CreateNoWindow = true,
                UseShellExecute = false
            });
        }

        private void DeleteCredentials(string ip)
        {
            // Supprimer les informations d'identification
            Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/C cmdkey /delete:TERMSRV/{ip}",
                CreateNoWindow = true,
                UseShellExecute = false
            });
        }

        private string PromptForMultiMonitor()
        {
            // Afficher une boîte de dialogue pour demander si l'utilisateur souhaite un ou plusieurs moniteurs
            string mon = "";
            DialogResult result = MessageBox.Show("Multi-moniteur Oui ou Non ?", "Choix d'écran", MessageBoxButtons.YesNo);

            if (result == DialogResult.Yes)
            {
                mon = "Oui";
            }
            else
            {
                mon = "Non";
            }

            return mon;
        }

        private void StartRds(string ip, bool multiMonitor = false)
        {
            // Lancer MSTSC pour se connecter au bureau à distance avec ou sans plusieurs moniteurs
            string arguments = $"/v:{ip}";
            if (multiMonitor)
            {
                arguments += " /multimon"; // Option pour plusieurs moniteurs
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = "mstsc.exe",
                Arguments = arguments,
                UseShellExecute = true
            });
        }

        // Méthode pour charger les informations d'identification depuis le fichier
        private void LoadRdsCredentials()
        {
            string filePath = Path.Combine(Application.StartupPath, "RdsCredentials.txt");

            if (File.Exists(filePath))
            {
                var lines = File.ReadAllLines(filePath);
                foreach (var line in lines)
                {
                    var parts = line.Split(',');

                    if (parts.Length == 4)
                    {
                        string fileName = parts[0];
                        string ip = parts[1];
                        string user = parts[2];
                        string password = parts[3];

                        rdsCredentials[fileName] = (ip, user, password);
                    }
                }
            }
        }

        private void SaisirRdsButton_Click(object sender, EventArgs e)
        {
            SaisieRdsForm saisieForm = new SaisieRdsForm();  // Crée une nouvelle instance de SaisieRdsForm
            saisieForm.Show();  // Affiche le formulaire
        }

        private void VisualiserRdsButton_Click(object sender, EventArgs e)
        {
            VisualiserRdsForm visualiserForm = new VisualiserRdsForm();  // Crée une nouvelle instance de VisualiserRdsForm
            visualiserForm.Show();  // Affiche le formulaire
        }
    }
}
