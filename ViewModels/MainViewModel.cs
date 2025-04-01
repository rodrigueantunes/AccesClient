using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ComponentModel;
using System.Windows.Input;
using Newtonsoft.Json;
using AccesClientWPF.Models;
using AccesClientWPF.Commands;
using System.Windows;
using AccesClientWPF.Views;
using AccesClientWPF.Helpers;
using Microsoft.Win32;

namespace AccesClientWPF.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly string _jsonFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "database.json");
        private readonly string _accountsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "rds_accounts.json");

        private bool _isMultiMonitor;
        public bool IsMultiMonitor
        {
            get => _isMultiMonitor;
            set
            {
                if (_isMultiMonitor != value)
                {
                    _isMultiMonitor = value;
                    OnPropertyChanged(nameof(IsMultiMonitor));
                }
            }
        }

        public ObservableCollection<ClientModel> Clients { get; set; } = new();
        public ObservableCollection<FileModel> FilteredFiles { get; set; } = new();

        private ClientModel selectedClient;
        public ClientModel SelectedClient
        {
            get => selectedClient;
            set
            {
                selectedClient = value;
                OnPropertyChanged(nameof(SelectedClient));
                LoadFilesForSelectedClient();
            }
        }

        private FileModel selectedFile;
        public FileModel SelectedFile
        {
            get => selectedFile;
            set
            {
                selectedFile = value;
                OnPropertyChanged(nameof(SelectedFile));
            }
        }

        public ICommand ManageJsonCommand { get; }
        public ICommand ManageClientsCommand { get; }
        public ICommand AddFileCommand { get; }
        public ICommand MoveUpFileCommand { get; }
        public ICommand MoveDownFileCommand { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        public MainViewModel()
        {
            ManageJsonCommand = new RelayCommand(OpenRdsAccountWindow);
            ManageClientsCommand = new RelayCommand(OpenClientManagementWindow);
            AddFileCommand = new RelayCommand(_ => AddFile());
            MoveUpFileCommand = new RelayCommand(_ => MoveUp(), _ => SelectedFile != null && FilteredFiles.IndexOf(SelectedFile) > 0);
            MoveDownFileCommand = new RelayCommand(_ => MoveDown(), _ => SelectedFile != null && FilteredFiles.IndexOf(SelectedFile) < FilteredFiles.Count - 1);

            LoadClients();
        }

        private void LoadClients()
        {
            var database = LoadDatabase();

            // Vider la collection existante au lieu de créer une nouvelle instance
            Clients.Clear();

            // Ajouter les clients de la base de données
            foreach (var client in database.Clients)
            {
                Clients.Add(client);
            }

            OnPropertyChanged(nameof(Clients));
        }

        private Models.DatabaseModel LoadDatabase()
        {
            if (!File.Exists(_jsonFilePath))
                return new Models.DatabaseModel();

            var json = File.ReadAllText(_jsonFilePath);
            return JsonConvert.DeserializeObject<Models.DatabaseModel>(json) ?? new Models.DatabaseModel();
        }

        private void SaveDatabase(Models.DatabaseModel database)
        {
            var json = JsonConvert.SerializeObject(database, Formatting.Indented);
            File.WriteAllText(_jsonFilePath, json);
        }

        private void LoadFilesForSelectedClient()
        {
            FilteredFiles.Clear();

            if (SelectedClient != null)
            {
                var database = LoadDatabase();
                foreach (var file in database.Files.Where(f => f.Client == SelectedClient.Name))
                    FilteredFiles.Add(file);
            }
        }

        public void EditSelectedFile(FileModel file = null)
        {
            FileModel selectedFile = file ?? SelectedFile;
            if (selectedFile == null)
            {
                MessageBox.Show("Veuillez sélectionner un élément à modifier.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Stocker le client actuellement sélectionné
            var currentlySelectedClient = SelectedClient;

            var editWindow = new AddEntryWindow(Clients, SelectedClient, selectedFile);
            if (editWindow.ShowDialog() == true && editWindow.FileEntry != null)
            {
                var database = LoadDatabase();
                var existingFile = database.Files.FirstOrDefault(f => f.Name == selectedFile.Name && f.Client == selectedFile.Client);
                if (existingFile != null)
                {
                    int index = database.Files.IndexOf(existingFile);
                    database.Files[index] = editWindow.FileEntry;
                    SaveDatabase(database);

                    // Réappliquer la sélection du client pour recharger ses fichiers
                    var clientName = currentlySelectedClient.Name;
                    SelectedClient = null; // Forcer la réinitialisation  
                    SelectedClient = Clients.FirstOrDefault(c => c.Name == clientName);
                }
            }
        }

        public void HandleFileDoubleClick(FileModel file)
        {
            switch (file.Type)
            {
                case "RDS":
                    ConnectToRemoteDesktop(file);
                    break;
                case "AnyDesk":
                    ConnectToAnyDesk(file);
                    break;
                case "VPN":
                    LaunchProgram(file.FullPath);
                    break;
                case "Dossier":
                    OpenFolder(file.FullPath);
                    break;
                case "Fichier":
                    OpenFile(file.FullPath);
                    break;
                default:
                    MessageBox.Show($"Ouverture de {file.Type}: {file.Name}", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    break;
            }
        }
        private void LaunchProgram(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    Process.Start(path);
                }
                else
                {
                    MessageBox.Show($"Le fichier '{path}' n'existe pas.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ouverture du programme : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenFolder(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    // Utiliser ProcessStartInfo pour une meilleure gestion
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = string.Format("\"{0}\"", path),
                        UseShellExecute = true
                    };
                    Process.Start(startInfo);
                }
                else
                {
                    MessageBox.Show($"Le dossier '{path}' n'existe pas.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ouverture du dossier : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ConnectToRemoteDesktop(FileModel file)
        {
            var credentials = file.FullPath.Split(':');
            if (credentials.Length < 3)
            {
                MessageBox.Show("Les informations de connexion RDS sont incomplètes.");
                return;
            }

            string ipDns = credentials[0];
            string username = credentials[1];
            string encryptedPassword = credentials[2];
            string password = EncryptionHelper.Decrypt(encryptedPassword);
            string args = $"/v:{ipDns} {(IsMultiMonitor ? "/multimon" : "/f")}";

            // Ajout des informations d'identification
            try
            {
                Process.Start("cmd.exe", $"/C cmdkey /generic:{ipDns} /user:\"{username}\" /pass:\"{password}\"");

                // Démarrage de la connexion RDS
                Process.Start("mstsc", args);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la connexion RDS : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ConnectToAnyDesk(FileModel file)
        {
            var credentials = file.FullPath.Split(':');
            string id = credentials[0];
            string password = string.Empty;

            if (credentials.Length > 1 && !string.IsNullOrEmpty(credentials[1]))
            {
                password = EncryptionHelper.Decrypt(credentials[1]);
            }

            string anyDeskPath = @"C:\Program Files (x86)\AnyDesk\AnyDesk.exe";
            if (!File.Exists(anyDeskPath))
            {
                MessageBox.Show("AnyDesk n'est pas installé à l'emplacement attendu.");
                return;
            }

            try
            {
                if (!string.IsNullOrEmpty(password))
                {
                    // Exécution via CMD pour utiliser 'echo' avec le mot de passe
                    string command = $"echo {password} | \"{anyDeskPath}\" \"{id}\" --with-password";
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/C {command}",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    });
                }
                else
                {
                    // Si pas de mot de passe, exécution simple
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = anyDeskPath,
                        Arguments = $"\"{id}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ouverture de AnyDesk : {ex.Message}");
            }
        }

        private void OpenRdsAccountWindow(object parameter)
        {
            new RdsAccountWindow().ShowDialog();
        }

        private void OpenClientManagementWindow(object parameter)
        {
            // Stocker le client actuellement sélectionné
            var currentlySelectedClient = SelectedClient;

            var clientWindow = new ClientManagementWindow(Clients);
            if (clientWindow.ShowDialog() == true)
            {
                LoadClients();

                // Restaurer la sélection du client si possible
                if (currentlySelectedClient != null)
                {
                    SelectedClient = Clients.FirstOrDefault(c => c.Name == currentlySelectedClient.Name);
                }
            }
        }

        private void AddFile()
        {
            if (SelectedClient == null)
            {
                MessageBox.Show("Veuillez sélectionner un client avant d'ajouter un élément.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Stocker le client actuellement sélectionné
            var currentlySelectedClient = SelectedClient;

            var database = LoadDatabase();
            var addEntryWindow = new AddEntryWindow(Clients, SelectedClient);
            if (addEntryWindow.ShowDialog() == true && addEntryWindow.FileEntry != null)
            {
                database.Files.Add(addEntryWindow.FileEntry);
                SaveDatabase(database);

                // Réappliquer la sélection du client pour recharger ses fichiers
                var clientName = currentlySelectedClient.Name;
                SelectedClient = null; // Forcer la réinitialisation
                SelectedClient = Clients.FirstOrDefault(c => c.Name == clientName);
            }
        }

        private void OpenFile(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        FileName = path,
                        UseShellExecute = true
                    };
                    Process.Start(startInfo);
                }
                else
                {
                    MessageBox.Show($"Le fichier '{path}' n'existe pas.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ouverture du fichier : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void MoveUp()
        {
            var index = FilteredFiles.IndexOf(SelectedFile);
            if (index > 0)
            {
                FilteredFiles.Move(index, index - 1);
                SaveFiles();
            }
        }

        public void MoveDown()
        {
            var index = FilteredFiles.IndexOf(SelectedFile);
            if (index < FilteredFiles.Count - 1)
            {
                FilteredFiles.Move(index, index + 1);
                SaveFiles();
            }
        }

        private void SaveFiles()
        {
            // Charger la base de données existante
            var db = LoadDatabase();

            // Supprimer uniquement les fichiers du client actuel
            db.Files = new ObservableCollection<FileModel>(
                db.Files.Where(f => f.Client != SelectedClient.Name)
            );

            // Ajouter les fichiers triés du client actuel
            foreach (var file in FilteredFiles)
            {
                db.Files.Add(file);
            }

            // Sauvegarder la base de données complète
            SaveDatabase(db);
        }

        private ICommand _openExtranetCommand;
        public ICommand OpenExtranetCommand => _openExtranetCommand ??= new RelayCommand(_ => OpenExtranet());

        private ICommand _openOnlineHelpCommand;
        public ICommand OpenOnlineHelpCommand => _openOnlineHelpCommand ??= new RelayCommand(_ => OpenOnlineHelp());

        private void OpenExtranet()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://extranet.volume-software.com/",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Impossible d'ouvrir l'extranet : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string FindDropboxPath()
        {
            try
            {
                // Liste des méthodes pour trouver le chemin Dropbox
                Func<string>[] dropboxPathFinders = new Func<string>[]
                {
            // Méthode 1 : Recherche via la base de registre
            () => {
                try
                {
                    using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Dropbox\InstallPath"))
                    {
                        return key?.GetValue("") as string;
                    }
                }
                catch { return null; }
            },

            // Méthode 2 : Chemins par défaut connus
            () => {
                string[] defaultPaths = new[]
                {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Dropbox"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Dropbox"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Dropbox")
                };

                return defaultPaths.FirstOrDefault(Directory.Exists);
            },

            // Méthode 3 : Recherche dans les dossiers utilisateur
            () => {
                try
                {
                    string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    string parentFolder = Directory.GetParent(userProfile).FullName;

                    return Directory.GetDirectories(parentFolder)
                        .Select(dir => Path.Combine(dir, "Dropbox"))
                        .FirstOrDefault(Directory.Exists);
                }
                catch { return null; }
            }
                };

                // Essayer chaque méthode
                foreach (var finder in dropboxPathFinders)
                {
                    string path = finder();
                    if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
                    {
                        return path;
                    }
                }

                // Dernière tentative : demander à l'utilisateur
                var dialog = new Microsoft.Win32.OpenFolderDialog
                {
                    Title = "Localiser le dossier Dropbox",
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
                };

                if (dialog.ShowDialog() == true && Directory.Exists(dialog.FolderName))
                {
                    return dialog.FolderName;
                }

                return null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la recherche du dossier Dropbox : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        private void OpenOnlineHelp()
        {
            try
            {
                // 1) Vérifier association .htm
                string userChoice = Microsoft.Win32.Registry.GetValue(
                    @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\.htm\UserChoice",
                    "ProgId", null) as string ?? "";

                if (string.IsNullOrEmpty(userChoice))
                    userChoice = Microsoft.Win32.Registry.GetValue(@"HKEY_CLASSES_ROOT\.htm", "", null) as string ?? "";

                string browserPath = Microsoft.Win32.Registry.GetValue($@"HKEY_CLASSES_ROOT\{userChoice}\shell\open\command", "", null) as string ?? "";

                // Vérifie si c'est un navigateur connu
                string[] knownBrowsers = { "chrome.exe", "msedge.exe", "firefox.exe", "iexplore.exe", "opera.exe", "seamonkey.exe", "brave.exe", "vivaldi.exe", "safari.exe", "maxthon.exe" };
                bool associationIncorrecte = string.IsNullOrEmpty(browserPath) || !knownBrowsers.Any(b => browserPath.ToLower().Contains(b));

                // 2) Récupérer le dossier Dropbox
                string dropboxPath = FindDropboxPath();
                if (string.IsNullOrEmpty(dropboxPath))
                {
                    MessageBox.Show("Impossible de localiser le dossier Dropbox. Veuillez sélectionner manuellement le dossier.",
                        "Dossier Dropbox introuvable", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 3) Chercher le fichier d'aide
                string[] possibleFilenames = { "default.htm", "Default.htm", "defaut.htm", "Defaut.htm" };
                string helpFilePath = possibleFilenames
                    .Select(filename => Path.Combine(dropboxPath, "VoluHelp", filename))
                    .FirstOrDefault(File.Exists);

                if (helpFilePath == null)
                {
                    string helpFolder = Path.Combine(dropboxPath, "VoluHelp");
                    if (!Directory.Exists(helpFolder))
                    {
                        MessageBox.Show($"Le dossier VoluHelp n'existe pas : {helpFolder}\n\n" +
                                        $"Contenu du dossier Dropbox : {string.Join(", ", Directory.GetDirectories(dropboxPath))}",
                                        "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var filesInFolder = Directory.GetFiles(helpFolder);
                    MessageBox.Show($"Aucun fichier d'aide trouvé. Fichiers présents :\n{string.Join("\n", filesInFolder)}",
                        "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 4) Construire l'URL (file:// + paramètres)
                string helpFileParams = "?rhhlterm=qualite%20client&rhsyns=%20#t=Content%2FAccueil%2FAccueil.htm&rhhlterm=Volupack%20principe%20de%20base&rhsyns=%20&ux=search";
                string fullUrl = new Uri(helpFilePath).AbsoluteUri + helpFileParams;

                // 5) Si association incorrecte → proposer la fenêtre pour choisir un navigateur
                if (associationIncorrecte)
                {
                    var installedBrowsers = GetInstalledBrowsers();
                    if (installedBrowsers.Count == 0)
                    {
                        MessageBox.Show("Aucun navigateur reconnu automatiquement.\n" +
                                        "Veuillez en sélectionner un manuellement.",
                                        "Navigateur introuvable", MessageBoxButton.OK, MessageBoxImage.Warning);

                        // Ouvre la fenêtre standard pour .exe (fallback)
                        var openDlg = new Microsoft.Win32.OpenFileDialog
                        {
                            Title = "Sélectionnez le navigateur (.exe)",
                            Filter = "Fichiers EXE|*.exe",
                            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)
                        };
                        if (openDlg.ShowDialog() == true)
                        {
                            // Ouvrir l'aide pour cette session uniquement (pas de modif Registre)
                            Process.Start(openDlg.FileName, fullUrl);
                        }
                        return;
                    }

                    // Sinon, ouvrir notre fenêtre de choix
                    var chooseWin = new Views.ChooseBrowserWindow(installedBrowsers);
                    bool? dialogResult = chooseWin.ShowDialog();
                    if (dialogResult == true)
                    {
                        // L'utilisateur a choisi un navigateur
                        string chosenExe = chooseWin.SelectedBrowserExe;

                        if (chooseWin.JustOpenOnce)
                        {
                            // Ouvre juste 1 fois avec ce navigateur, sans changer .htm
                            Process.Start(chosenExe, fullUrl);
                        }
                        else if (chooseWin.SetAsDefault)
                        {
                            // Tente de changer l'association en Registre
                            bool success = ForceAssociateHtmWithExe(chosenExe);
                            if (!success)
                            {
                                MessageBox.Show(
                                    "Impossible de changer l'association .htm.\n" +
                                    "Veuillez exécuter l'application en tant qu'administrateur ou modifier manuellement via Paramètres Windows.",
                                    "Échec",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Error
                                );
                            }
                            else
                            {
                                // Succès
                                MessageBox.Show(
                                    "Association mise à jour avec succès !\n" +
                                    "Relancez l'application ou essayez à nouveau pour ouvrir l'aide.",
                                    "Succès",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Information
                                );
                            }
                        }
                    }
                    return;
                }

                // 6) Sinon association correcte → on ouvre
                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd",
                    Arguments = $"/c start \"\" \"{fullUrl}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Impossible d'ouvrir l'aide en ligne : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Détecte quelques chemins courants pour les navigateurs (Chrome, Firefox, Opera, Edge, etc.)
        /// </summary>
        /// <summary>
        /// Récupère la liste la plus exhaustive possible de navigateurs installés
        /// en combinant :
        /// 1) StartMenuInternet du Registre (HKLM + HKCU)
        /// 2) Dossiers Program Files
        /// 3) Dossiers AppData/Local/Programs (par ex. Opera GX)
        /// </summary>
        private List<string> GetInstalledBrowsers()
        {
            var foundPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 1) Parcours du registre (StartMenuInternet)
            foundPaths.UnionWith(GetBrowsersFromRegistry(
                @"HKEY_LOCAL_MACHINE\Software\Clients\StartMenuInternet"));
            foundPaths.UnionWith(GetBrowsersFromRegistry(
                @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Clients\StartMenuInternet"));
            foundPaths.UnionWith(GetBrowsersFromRegistry(
                @"HKEY_CURRENT_USER\SOFTWARE\Clients\StartMenuInternet"));

            // 2) Recherche dans Program Files / Program Files (x86)
            foundPaths.UnionWith(FindBrowsersInCommonPaths());

            // 3) Recherche dans AppData/Local/Programs (utile pour Opera GX, etc.)
            foundPaths.UnionWith(FindBrowsersInLocalAppData());

            // 4) (Optionnel) Scanne entièrement le dossier Program Files
            //    pour trouver tout .exe contenant \"chrome\", \"opera\", etc. 
            
            foundPaths.UnionWith(ScanProgramFilesForPotentialBrowsers(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)));
            foundPaths.UnionWith(ScanProgramFilesForPotentialBrowsers(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)));
            

            // Ne garder que ceux qui existent vraiment
            return foundPaths.Where(File.Exists).ToList();
        }


        private IEnumerable<string> GetBrowsersFromRegistry(string baseKeyPath)
        {
            var result = new List<string>();

            ParseHiveAndSubPath(baseKeyPath, out string hiveName, out string subPath);
            if (!Enum.TryParse(hiveName, out RegistryHive hive))
                yield break; // clé non reconnue

            // On utilise RegistryView.Default ou RegistryView.Registry64, au choix
            using var root = RegistryKey.OpenBaseKey(hive, RegistryView.Default);
            using var mainKey = root.OpenSubKey(subPath);
            if (mainKey == null)
                yield break;

            foreach (var browserName in mainKey.GetSubKeyNames())
            {
                using var browserKey = mainKey.OpenSubKey(browserName);
                if (browserKey == null) continue;

                using var shellKey = browserKey.OpenSubKey("shell");
                using var openKey = shellKey?.OpenSubKey("open");
                using var commandKey = openKey?.OpenSubKey("command");
                if (commandKey == null) continue;

                var command = commandKey.GetValue(null) as string;
                if (string.IsNullOrEmpty(command)) continue;

                var exePath = ExtractExePath(command);
                if (!string.IsNullOrEmpty(exePath))
                    result.Add(exePath);
            }

            foreach (var path in result)
                yield return path;
        }

        private string ExtractExePath(string command)
        {
            command = command.Trim();

            if (command.StartsWith("\""))
            {
                // Cherche la 2e occurrence de \"
                int secondQuote = command.IndexOf('\"', 1);
                if (secondQuote > 1)
                {
                    return command.Substring(1, secondQuote - 1);
                }
            }
            else
            {
                var parts = command.Split(' ');
                if (parts.Length > 0)
                    return parts[0];
            }
            return command; // fallback
        }


        /// <summary>
        /// Cherche les navigateurs dans quelques chemins classiques sur le disque
        /// (Program Files, Program Files (x86), etc.)
        /// </summary>
        private IEnumerable<string> FindBrowsersInCommonPaths()
        {
            var found = new List<string>();

            var knownPaths = new[]
            {
        // Chrome
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Google\\Chrome\\Application\\chrome.exe"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Google\\Chrome\\Application\\chrome.exe"),

        // Firefox
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Mozilla Firefox\\firefox.exe"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Mozilla Firefox\\firefox.exe"),

        // Opera
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Opera\\launcher.exe"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Opera\\launcher.exe"),

        // Opera GX (souvent dans Program Files, parfois ailleurs)
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Opera GX\\launcher.exe"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Opera GX\\launcher.exe"),

        // Edge
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft\\Edge\\Application\\msedge.exe"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft\\Edge\\Application\\msedge.exe"),

        // Brave
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "BraveSoftware\\Brave-Browser\\Application\\brave.exe"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "BraveSoftware\\Brave-Browser\\Application\\brave.exe"),

        // Vivaldi
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Vivaldi\\Application\\vivaldi.exe"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Vivaldi\\Application\\vivaldi.exe"),

        // SeaMonkey
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "SeaMonkey\\seamonkey.exe"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "SeaMonkey\\seamonkey.exe"),

        // Yandex
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Yandex\\YandexBrowser\\browser.exe"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Yandex\\YandexBrowser\\browser.exe"),
    };

            foreach (var path in knownPaths)
            {
                if (File.Exists(path))
                    found.Add(path);
            }

            return found;
        }

        private IEnumerable<string> FindBrowsersInLocalAppData()
        {
            var results = new List<string>();

            // 1) Chemins typiques (Opera GX, Opera, etc.)
            var userLocal = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            // Quelques chemins spécifiques
            var localPaths = new[]
            {
        Path.Combine(userLocal, "Programs", "Opera GX", "launcher.exe"),
        Path.Combine(userLocal, "Programs", "Opera", "launcher.exe"),
        // etc. Ajoute d'autres si tu sais qu'ils se mettent dans Programs\\Nom
    };

            foreach (var path in localPaths)
            {
                if (File.Exists(path))
                    results.Add(path);
            }

            // 2) (Optionnel) On pourrait aussi scanner tout \"AppData\\Local\\Programs\" 
            //    pour trouver d'éventuels .exe contenant \"opera\", \"browser\", etc.
            //    Mais c'est potentiellement lourd et lent.

            return results;
        }

        private IEnumerable<string> ScanProgramFilesForPotentialBrowsers(string folder)
        {
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
                yield break;

            var keywords = new[] { "chrome", "opera", "browser", "firefox", "brave",
                           "vivaldi", "seamonkey", "yandex", "palemoon", "waterfox" };

            var foundList = new List<string>();
            try
            {
                var exeFiles = Directory.EnumerateFiles(folder, "*.exe", SearchOption.AllDirectories);
                foreach (var file in exeFiles)
                {
                    var name = Path.GetFileName(file).ToLowerInvariant();
                    if (keywords.Any(k => name.Contains(k)))
                        foundList.Add(file);
                }
            }
            catch
            {
                // Accès refusé ? On ignore
            }

            foreach (var item in foundList)
                yield return item;
        }

        private void ParseHiveAndSubPath(string fullPath, out string hive, out string subPath)
        {
            int idx = fullPath.IndexOf('\\');
            if (idx < 0)
            {
                hive = fullPath;
                subPath = "";
                return;
            }
            hive = fullPath.Substring(0, idx);
            subPath = fullPath.Substring(idx + 1);
        }

        private bool ForceAssociateHtmWithExe(string browserExe)
        {
            try
            {
                const string progId = "MonNavigateurPersoHTML";

                // Associer .htm -> ce progId
                using (var htmKey = Microsoft.Win32.Registry.ClassesRoot.CreateSubKey(".htm"))
                {
                    htmKey?.SetValue("", progId, Microsoft.Win32.RegistryValueKind.String);
                }

                // Créer ou MAJ la clé HKEY_CLASSES_ROOT\\MonNavigateurPersoHTML\\shell\\open\\command
                string commandKeyPath = $"{progId}\\shell\\open\\command";
                using (var cmdKey = Microsoft.Win32.Registry.ClassesRoot.CreateSubKey(commandKeyPath))
                {
                    string commandValue = $"\"{browserExe}\" \"%1\"";
                    cmdKey?.SetValue("", commandValue, Microsoft.Win32.RegistryValueKind.String);
                }

                // Nom descriptif
                using (var progIdKey = Microsoft.Win32.Registry.ClassesRoot.CreateSubKey(progId))
                {
                    progIdKey?.SetValue("", "HTML Document (Custom)", Microsoft.Win32.RegistryValueKind.String);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }



        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}