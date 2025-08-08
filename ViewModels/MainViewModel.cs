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
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Controls;

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

        private ObservableCollection<FileModel> _passwordEntries = new();
        public ObservableCollection<FileModel> PasswordEntries
        {
            get => _passwordEntries;
            private set { _passwordEntries = value; OnPropertyChanged(nameof(PasswordEntries)); }
        }


        // Copie presse-papiers
        public ICommand CopyUsernameCommand => new RelayCommand(u =>
        {
            var txt = u as string ?? string.Empty;
            Helpers.ClipboardHelper.CopyPlainText(txt);
            ShowCopyToast("Nom d'utilisateur copié");
        });

        public ICommand CopyPasswordCommand => new RelayCommand(p =>
        {
            var s = p as string ?? string.Empty;
            var dec = Helpers.EncryptionHelper.Decrypt(s);
            var toCopy = string.IsNullOrEmpty(dec) ? s : dec;

            Helpers.ClipboardHelper.CopyPlainText(toCopy);
            ShowCopyToast("Mot de passe copié");
        });

        private void CopyWindowsUsername_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string username && !string.IsNullOrEmpty(username))
            {
                Helpers.ClipboardHelper.CopyPlainText(username);
                ShowCopyToast("Nom d'utilisateur copié");
            }
        }

        private void CopyWindowsPassword_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is FileModel file)
            {
                if (!string.IsNullOrEmpty(file.WindowsPassword))
                {
                    string decryptedPassword = EncryptionHelper.Decrypt(file.WindowsPassword);
                    Helpers.ClipboardHelper.CopyPlainText(decryptedPassword);
                    ShowCopyToast("Mot de passe copié");
                }
            }
        }


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

        public IEnumerable<FileModel> AllFiles
        {
            get
            {
                var database = LoadDatabase();
                return database.Files;
            }
        }

        public ICommand ManageJsonCommand { get; }
        public ICommand ManageClientsCommand { get; }
        public ICommand AddFileCommand { get; }
        public ICommand MoveUpFileCommand { get; }
        public ICommand MoveDownFileCommand { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        private bool _showCredentials;
        public bool ShowCredentials
        {
            get => _showCredentials;
            set
            {
                if (_showCredentials != value)
                {
                    _showCredentials = value;
                    OnPropertyChanged(nameof(ShowCredentials));
                }
            }
        }

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

            Clients.Clear();
            foreach (var client in database.Clients)
            {
                Clients.Add(client);
            }

            OnPropertyChanged(nameof(Clients));
        }

        public Models.DatabaseModel LoadDatabase()
        {
            if (!File.Exists(_jsonFilePath))
                return new Models.DatabaseModel();

            var json = File.ReadAllText(_jsonFilePath);
            return JsonConvert.DeserializeObject<Models.DatabaseModel>(json) ?? new Models.DatabaseModel();
        }

        public void SaveDatabase(Models.DatabaseModel database)
        {
            var json = JsonConvert.SerializeObject(database, Formatting.Indented);
            File.WriteAllText(_jsonFilePath, json);
        }

        // Commandes du panneau de droite (mot de passe)
        public ICommand EditPasswordCommand => new RelayCommand(p =>
        {
            if (SelectedClient == null || p is not FileModel item) return;

            var win = new AddEntryWindow(Clients, SelectedClient, item);
            try { win.SetTypeMotDePasse(); } catch { }

            if (win.ShowDialog() == true && win.FileEntry != null)
            {
                var updated = win.FileEntry;

                // Type et client figés
                updated.Type = "MotDePasse";
                updated.Client = item.Client;

                // Conserver le nom si laissé vide
                if (string.IsNullOrWhiteSpace(updated.Name))
                    updated.Name = item.Name;

                // Conserver l'utilisateur s'il n'a pas été saisi
                if (string.IsNullOrWhiteSpace(updated.WindowsUsername))
                    updated.WindowsUsername = item.WindowsUsername;

                // Conserver le mot de passe si non saisi, chiffrer si saisi en clair
                if (string.IsNullOrWhiteSpace(updated.WindowsPassword))
                {
                    updated.WindowsPassword = item.WindowsPassword; // garde l'existant chifré
                }
                else
                {
                    var testDecrypt = EncryptionHelper.Decrypt(updated.WindowsPassword);
                    if (string.IsNullOrEmpty(testDecrypt))
                        updated.WindowsPassword = EncryptionHelper.Encrypt(updated.WindowsPassword);
                }

                // Remplacement par VALEUR dans la DB
                var db = LoadDatabase();
                var existing = db.Files.FirstOrDefault(f =>
                    f.Type == "MotDePasse" &&
                    string.Equals(f.Client, item.Client, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(f.Name, item.Name, StringComparison.OrdinalIgnoreCase));

                if (existing != null)
                {
                    var index = db.Files.IndexOf(existing);
                    db.Files[index] = updated;
                }
                else
                {
                    db.Files.Add(updated);
                }

                SaveDatabase(db);
                LoadFilesForSelectedClient();
            }
        });



        public ICommand DeletePasswordCommand => new RelayCommand(p =>
        {
            if (SelectedClient == null || p is not FileModel item) return;

            var db = LoadDatabase();

            // Cherche l’élément correspondant par valeurs (évite le problème de référence)
            var toRemove = db.Files.FirstOrDefault(f =>
                f.Type == "MotDePasse" &&
                string.Equals(f.Client, item.Client, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(f.Name, item.Name, StringComparison.OrdinalIgnoreCase));

            if (toRemove != null)
            {
                db.Files.Remove(toRemove);
                SaveDatabase(db);
                LoadFilesForSelectedClient();
            }
            else
            {
                // Cas limite: si pas trouvé par nom (doublons, etc.), on tente une correspondance plus large
                toRemove = db.Files.FirstOrDefault(f =>
                    f.Type == "MotDePasse" &&
                    string.Equals(f.Client, item.Client, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(f.WindowsUsername ?? "", item.WindowsUsername ?? "", StringComparison.Ordinal) &&
                    string.Equals(f.WindowsPassword ?? "", item.WindowsPassword ?? "", StringComparison.Ordinal));

                if (toRemove != null)
                {
                    db.Files.Remove(toRemove);
                    SaveDatabase(db);
                    LoadFilesForSelectedClient();
                }
            }
        });


        public ICommand MovePasswordUpCommand => new RelayCommand(p =>
        {
            if (p is not FileModel item) return;
            int index = PasswordEntries.IndexOf(item);
            if (index > 0)
            {
                PasswordEntries.Move(index, index - 1);
                SavePasswordsOrder();
            }
        });

        public ICommand MovePasswordDownCommand => new RelayCommand(p =>
        {
            if (p is not FileModel item) return;
            int index = PasswordEntries.IndexOf(item);
            if (index < PasswordEntries.Count - 1)
            {
                PasswordEntries.Move(index, index + 1);
                SavePasswordsOrder();
            }
        });

        private void SavePasswordsOrder()
        {
            if (SelectedClient == null) return;

            var db = LoadDatabase();

            // Garde tout SAUF les "MotDePasse" du client courant
            var others = db.Files.Where(f => !(f.Client == SelectedClient.Name && f.Type == "MotDePasse")).ToList();

            db.Files.Clear();
            foreach (var f in others) db.Files.Add(f);

            // Réinjecte SEULEMENT les mots de passe du client courant dans le nouvel ordre
            foreach (var p in PasswordEntries)
            {
                if (p.Client == SelectedClient.Name && p.Type == "MotDePasse")
                    db.Files.Add(p);
            }

            SaveDatabase(db);
            LoadFilesForSelectedClient();
        }

        public ICommand AddPasswordFromRightCommand => new RelayCommand(_ =>
        {
            if (SelectedClient == null)
            {
                MessageBox.Show("Veuillez sélectionner un client avant d'ajouter un mot de passe.", "Information",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var win = new AddEntryWindow(Clients, SelectedClient);
            // Force le type "Mot de passe" et masque le choix dans la fenêtre
            try { win.SetTypeMotDePasse(); } catch { /* ignore si pas dispo */ }

            if (win.ShowDialog() == true && win.FileEntry != null)
            {
                win.FileEntry.Type = "MotDePasse";      // sécurité
                win.FileEntry.Client = SelectedClient.Name;

                // S'assurer que le mot de passe est chiffré
                if (!string.IsNullOrWhiteSpace(win.FileEntry.WindowsPassword))
                {
                    var test = EncryptionHelper.Decrypt(win.FileEntry.WindowsPassword);
                    if (string.IsNullOrEmpty(test))
                        win.FileEntry.WindowsPassword = EncryptionHelper.Encrypt(win.FileEntry.WindowsPassword);
                }

                var db = LoadDatabase();
                db.Files.Add(win.FileEntry);
                SaveDatabase(db);

                LoadFilesForSelectedClient();
            }
        });



        public void LoadFilesForSelectedClient()
        {
            FilteredFiles.Clear();

            if (SelectedClient != null)
            {
                var database = LoadDatabase();

                // Centre : tout sauf MotDePasse
                foreach (var file in database.Files.Where(f => f.Client == SelectedClient.Name && f.Type != "MotDePasse"))
                    FilteredFiles.Add(file);

                // Droite : uniquement MotDePasse
                PasswordEntries = new ObservableCollection<FileModel>(
                    database.Files.Where(f => f.Client == SelectedClient.Name && f.Type == "MotDePasse")
                );
            }
            else
            {
                PasswordEntries = new ObservableCollection<FileModel>();
            }

            OnPropertyChanged(nameof(FilteredFiles));
        }

        // --- Toast de copie ---
        private bool _isCopyToastVisible;
        public bool IsCopyToastVisible
        {
            get => _isCopyToastVisible;
            set { _isCopyToastVisible = value; OnPropertyChanged(nameof(IsCopyToastVisible)); }
        }

        private string _copyToastText;
        public string CopyToastText
        {
            get => _copyToastText;
            set { _copyToastText = value; OnPropertyChanged(nameof(CopyToastText)); }
        }

        private System.Timers.Timer _copyToastTimer;

        public void ShowCopyToast(string text)
        {
            CopyToastText = text;
            IsCopyToastVisible = true;

            // (Re)lance un timer 1.5s pour masquer
            _copyToastTimer?.Stop();
            _copyToastTimer = new System.Timers.Timer(1500) { AutoReset = false };
            _copyToastTimer.Elapsed += (_, __) =>
            {
                // Revenir au thread UI
                Application.Current.Dispatcher.Invoke(() => IsCopyToastVisible = false);
            };
            _copyToastTimer.Start();
        }



        public void EditSelectedFile(FileModel file = null)
        {
            FileModel selectedFile = file ?? SelectedFile;
            if (selectedFile == null)
            {
                MessageBox.Show("Veuillez sélectionner un élément à modifier.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var originalFile = new FileModel
            {
                Name = selectedFile.Name,
                Client = selectedFile.Client,
                Type = selectedFile.Type,
                FullPath = selectedFile.FullPath,
                CustomIconPath = selectedFile.CustomIconPath,
                WindowsUsername = selectedFile.WindowsUsername,
                WindowsPassword = selectedFile.WindowsPassword
            };

            var current = SelectedClient;

            var editWindow = new AddEntryWindow(Clients, SelectedClient, selectedFile);
            if (editWindow.ShowDialog() == true && editWindow.FileEntry != null)
            {
                var database = LoadDatabase();
                var itemToReplace = database.Files.FirstOrDefault(f => f.Name == originalFile.Name && f.Client == originalFile.Client);

                if (itemToReplace != null)
                {
                    // éviter doublon nom
                    bool duplicateExists = database.Files.Any(f =>
                        f != itemToReplace &&
                        f.Name == editWindow.FileEntry.Name &&
                        f.Client == editWindow.FileEntry.Client);

                    if (duplicateExists)
                    {
                        MessageBox.Show($"Un élément avec le nom '{editWindow.FileEntry.Name}' existe déjà pour ce client.",
                                        "Nom en double", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    int index = database.Files.IndexOf(itemToReplace);
                    database.Files[index] = editWindow.FileEntry;

                    SaveDatabase(database);

                    // recharge centre + droite
                    SelectedClient = null;
                    SelectedClient = Clients.FirstOrDefault(c => c.Name == current.Name);
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
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = $"\"{path}\"",
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
            try
            {
                var credentials = file.FullPath.Split(':');
                if (credentials.Length < 3)
                {
                    MessageBox.Show("Les informations de connexion RDS sont incomplètes.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string ipDns = credentials[0];
                string username = credentials[1];
                string encryptedPassword = credentials[2];
                string password = EncryptionHelper.Decrypt(encryptedPassword);

                if (string.IsNullOrEmpty(password))
                {
                    MessageBox.Show($"Aucun mot de passe n'a été renseigné pour la connexion RDS '{file.Name}'.\nVeuillez éditer cette connexion et spécifier un mot de passe.",
                                   "Mot de passe manquant", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string rdpFilePath = Path.Combine(Path.GetTempPath(), $"{file.Name.Replace(' ', '_')}_{Guid.NewGuid().ToString().Substring(0, 8)}.rdp");

                using (StreamWriter sw = new StreamWriter(rdpFilePath))
                {
                    sw.WriteLine("screen mode id:i:2");
                    sw.WriteLine($"full address:s:{ipDns}");
                    sw.WriteLine($"username:s:{username}");
                    sw.WriteLine("prompt for credentials:i:0");
                    sw.WriteLine("desktopwidth:i:0");
                    sw.WriteLine("desktopheight:i:0");
                    sw.WriteLine("session bpp:i:32");
                    sw.WriteLine($"use multimon:i:{(IsMultiMonitor ? "1" : "0")}");
                    sw.WriteLine("connection type:i:7");
                    sw.WriteLine("networkautodetect:i:1");
                    sw.WriteLine("bandwidthautodetect:i:1");
                    sw.WriteLine("authentication level:i:2");
                    sw.WriteLine("redirectsmartcards:i:1");
                    sw.WriteLine("redirectclipboard:i:1");
                    sw.WriteLine("audiomode:i:0");
                    sw.WriteLine("autoreconnection enabled:i:1");
                    sw.WriteLine("alternate shell:s:");
                    sw.WriteLine("shell working directory:s:");
                    sw.WriteLine("disable wallpaper:i:0");
                    sw.WriteLine("allow font smoothing:i:1");
                    sw.WriteLine("allow desktop composition:i:1");
                    sw.WriteLine($"title:s:{file.Name}");
                    sw.WriteLine("promptcredentialonce:i:1");
                    sw.WriteLine("winposstr:s:0,3,0,0,800,600");
                }

                try
                {
                    var cmdKeyInfo = new ProcessStartInfo
                    {
                        FileName = "cmdkey.exe",
                        Arguments = $"/generic:{ipDns} /user:{username} /pass:{password}",
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
                    // ignore
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = "mstsc.exe",
                    Arguments = $"\"{rdpFilePath}\" /f",
                    UseShellExecute = true
                };

                using (Process mstscProcess = Process.Start(startInfo))
                {
                }

                Task.Delay(5000).ContinueWith(_ =>
                {
                    try
                    {
                        if (File.Exists(rdpFilePath))
                        {
                            File.Delete(rdpFilePath);
                        }

                        Task.Delay(30000).ContinueWith(__ =>
                        {
                            try
                            {
                                var cleanupInfo = new ProcessStartInfo
                                {
                                    FileName = "cmdkey.exe",
                                    Arguments = $"/delete:{ipDns}",
                                    CreateNoWindow = true,
                                    UseShellExecute = false,
                                    WindowStyle = ProcessWindowStyle.Hidden
                                };

                                using (Process cleanupProcess = Process.Start(cleanupInfo))
                                {
                                }
                            }
                            catch { }
                        });
                    }
                    catch { }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la connexion RDS : {ex.Message}",
                              "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ConnectToAnyDesk(FileModel file)
        {
            try
            {
                var credentials = file.FullPath.Split(':');
                string id = credentials[0];
                string password = string.Empty;

                if (credentials.Length > 1 && !string.IsNullOrEmpty(credentials[1]))
                {
                    password = EncryptionHelper.Decrypt(credentials[1]);
                }

                if (!VerifyAndSetAnyDeskPath())
                {
                    return;
                }

                string anyDeskPath = AppSettings.Instance.AnyDeskPath;

                try
                {
                    if (!string.IsNullOrEmpty(password))
                    {
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
                    MessageBox.Show($"Erreur lors de l'ouverture de AnyDesk : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la préparation de la connexion AnyDesk : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Vérifier et demander le chemin d'AnyDesk si nécessaire
        private bool VerifyAndSetAnyDeskPath()
        {
            if (AppSettings.Instance.IsAnyDeskPathValid())
            {
                return true;
            }

            MessageBox.Show(
                "Le chemin d'AnyDesk n'est pas valide ou n'a pas été configuré.\nVeuillez sélectionner l'exécutable AnyDesk sur votre système.",
                "Configuration AnyDesk",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            var openFileDialog = new OpenFileDialog
            {
                Title = "Sélectionner l'exécutable AnyDesk",
                Filter = "Exécutable (*.exe)|*.exe",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string selectedPath = openFileDialog.FileName;

                if (Path.GetFileName(selectedPath).ToLower() == "anydesk.exe")
                {
                    AppSettings.Instance.AnyDeskPath = selectedPath;
                    AppSettings.Instance.Save();
                    return true;
                }
                else
                {
                    MessageBox.Show(
                        "Le fichier sélectionné ne semble pas être AnyDesk.\nVeuillez sélectionner l'exécutable AnyDesk.exe.",
                        "Erreur de sélection",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return VerifyAndSetAnyDeskPath();
                }
            }

            return false;
        }

        private void OpenRdsAccountWindow(object parameter)
        {
            new RdsAccountWindow().ShowDialog();
        }

        private void OpenClientManagementWindow(object parameter)
        {
            var currentlySelectedClient = SelectedClient;

            var clientWindow = new ClientManagementWindow(Clients);
            if (clientWindow.ShowDialog() == true)
            {
                LoadClients();

                if (currentlySelectedClient != null)
                {
                    SelectedClient = Clients.FirstOrDefault(c => c.Name == currentlySelectedClient.Name);
                }
            }
        }

        public void AddFile()
        {
            if (SelectedClient == null)
            {
                MessageBox.Show("Veuillez sélectionner un client avant d'ajouter un élément.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var current = SelectedClient;
            var database = LoadDatabase();

            var addEntryWindow = new AddEntryWindow(Clients, SelectedClient);
            if (addEntryWindow.ShowDialog() == true && addEntryWindow.FileEntry != null)
            {
                database.Files.Add(addEntryWindow.FileEntry);
                SaveDatabase(database);

                SelectedClient = null;
                SelectedClient = Clients.FirstOrDefault(c => c.Name == current.Name);
            }
        }

        private void OpenFile(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    var startInfo = new ProcessStartInfo
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
            var db = LoadDatabase();

            // retire les fichiers du client courant
            db.Files = new ObservableCollection<FileModel>(
                db.Files.Where(f => f.Client != SelectedClient?.Name)
            );

            // réinjecte ceux du centre
            foreach (var file in FilteredFiles)
                db.Files.Add(file);

            // réinjecte aussi les mots de passe existants du client (on ne les perd pas)
            if (SelectedClient != null)
            {
                foreach (var p in PasswordEntries)
                    db.Files.Add(p);
            }

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
                Func<string>[] dropboxPathFinders = new Func<string>[]
                {
                    () => {
                        try {
                            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Dropbox\InstallPath");
                            return key?.GetValue("") as string;
                        } catch { return null; }
                    },
                    () => {
                        string[] defaultPaths = new[]
                        {
                            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Dropbox"),
                            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Dropbox"),
                            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Dropbox")
                        };
                        return defaultPaths.FirstOrDefault(Directory.Exists);
                    },
                    () => {
                        try {
                            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                            string parentFolder = Directory.GetParent(userProfile).FullName;

                            return Directory.GetDirectories(parentFolder)
                                .Select(dir => Path.Combine(dir, "Dropbox"))
                                .FirstOrDefault(Directory.Exists);
                        } catch { return null; }
                    }
                };

                foreach (var finder in dropboxPathFinders)
                {
                    string path = finder();
                    if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
                    {
                        return path;
                    }
                }

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
                MessageBox.Show($"Erreur lors de la recherche du dossier Dropbox : {ex.Message}",
                                "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        private void OpenOnlineHelp()
        {
            try
            {
                string userChoice = Registry.GetValue(
                    @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\.htm\UserChoice",
                    "ProgId", null) as string ?? "";

                if (string.IsNullOrEmpty(userChoice))
                    userChoice = Registry.GetValue(@"HKEY_CLASSES_ROOT\.htm", "", null) as string ?? "";

                string browserPath = Registry.GetValue($@"HKEY_CLASSES_ROOT\{userChoice}\shell\open\command", "", null) as string ?? "";

                string[] knownBrowsers = { "chrome.exe", "msedge.exe", "firefox.exe", "iexplore.exe", "opera.exe", "seamonkey.exe", "brave.exe", "vivaldi.exe", "safari.exe", "maxthon.exe" };
                bool associationIncorrecte = string.IsNullOrEmpty(browserPath) || !knownBrowsers.Any(b => browserPath.ToLower().Contains(b));

                string dropboxPath = FindDropboxPath();
                if (string.IsNullOrEmpty(dropboxPath))
                {
                    MessageBox.Show("Impossible de localiser le dossier Dropbox. Veuillez sélectionner manuellement le dossier.",
                        "Dossier Dropbox introuvable", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

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

                string helpFileParams = "?rhhlterm=qualite%20client&rhsyns=%20#t=Content%2FAccueil%2FAccueil.htm&rhhlterm=Volupack%20principe%20de%20base&rhsyns=%20&ux=search";
                string fullUrl = new Uri(helpFilePath).AbsoluteUri + helpFileParams;

                if (associationIncorrecte)
                {
                    var installedBrowsers = GetInstalledBrowsers();
                    if (installedBrowsers.Count == 0)
                    {
                        MessageBox.Show("Aucun navigateur reconnu automatiquement.\nVeuillez en sélectionner un manuellement.",
                                        "Navigateur introuvable", MessageBoxButton.OK, MessageBoxImage.Warning);

                        var openDlg = new OpenFileDialog
                        {
                            Title = "Sélectionnez le navigateur (.exe)",
                            Filter = "Fichiers EXE|*.exe",
                            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)
                        };
                        if (openDlg.ShowDialog() == true)
                        {
                            Process.Start(openDlg.FileName, fullUrl);
                        }
                        return;
                    }

                    var chooseWin = new Views.ChooseBrowserWindow(installedBrowsers);
                    bool? dialogResult = chooseWin.ShowDialog();
                    if (dialogResult == true)
                    {
                        string chosenExe = chooseWin.SelectedBrowserExe;

                        if (chooseWin.JustOpenOnce)
                        {
                            Process.Start(chosenExe, fullUrl);
                        }
                        else if (chooseWin.SetAsDefault)
                        {
                            bool success = ForceAssociateHtmWithExe(chosenExe);
                            if (!success)
                            {
                                MessageBox.Show(
                                    "Impossible de changer l'association .htm.\nVeuillez exécuter l'application en tant qu'administrateur ou modifier manuellement via Paramètres Windows.",
                                    "Échec", MessageBoxButton.OK, MessageBoxImage.Error
                                );
                            }
                            else
                            {
                                MessageBox.Show(
                                    "Association mise à jour avec succès !\nRelancez l'application ou essayez à nouveau pour ouvrir l'aide.",
                                    "Succès", MessageBoxButton.OK, MessageBoxImage.Information
                                );
                            }
                        }
                    }
                    return;
                }

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

        private List<string> GetInstalledBrowsers()
        {
            var foundPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foundPaths.UnionWith(GetBrowsersFromRegistry(@"HKEY_LOCAL_MACHINE\Software\Clients\StartMenuInternet"));
            foundPaths.UnionWith(GetBrowsersFromRegistry(@"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Clients\StartMenuInternet"));
            foundPaths.UnionWith(GetBrowsersFromRegistry(@"HKEY_CURRENT_USER\SOFTWARE\Clients\StartMenuInternet"));

            foundPaths.UnionWith(FindBrowsersInCommonPaths());
            foundPaths.UnionWith(FindBrowsersInLocalAppData());

            foundPaths.UnionWith(ScanProgramFilesForPotentialBrowsers(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)));
            foundPaths.UnionWith(ScanProgramFilesForPotentialBrowsers(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)));

            return foundPaths.Where(File.Exists).ToList();
        }

        private IEnumerable<string> GetBrowsersFromRegistry(string baseKeyPath)
        {
            var result = new List<string>();

            ParseHiveAndSubPath(baseKeyPath, out string hiveName, out string subPath);
            if (!Enum.TryParse(hiveName, out RegistryHive hive))
                yield break;

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
            return command;
        }

        private IEnumerable<string> FindBrowsersInCommonPaths()
        {
            var found = new List<string>();

            var knownPaths = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Google\\Chrome\\Application\\chrome.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Google\\Chrome\\Application\\chrome.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Mozilla Firefox\\firefox.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Mozilla Firefox\\firefox.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Opera\\launcher.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Opera\\launcher.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Opera GX\\launcher.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Opera GX\\launcher.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft\\Edge\\Application\\msedge.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft\\Edge\\Application\\msedge.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "BraveSoftware\\Brave-Browser\\Application\\brave.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "BraveSoftware\\Brave-Browser\\Application\\brave.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Vivaldi\\Application\\vivaldi.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Vivaldi\\Application\\vivaldi.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "SeaMonkey\\seamonkey.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "SeaMonkey\\seamonkey.exe"),
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
            var userLocal = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            var localPaths = new[]
            {
                Path.Combine(userLocal, "Programs", "Opera GX", "launcher.exe"),
                Path.Combine(userLocal, "Programs", "Opera", "launcher.exe"),
            };

            foreach (var path in localPaths)
            {
                if (File.Exists(path))
                    results.Add(path);
            }

            return results;
        }

        private IEnumerable<string> ScanProgramFilesForPotentialBrowsers(string folder)
        {
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
                yield break;

            var keywords = new[] { "chrome", "opera", "browser", "firefox", "brave", "vivaldi", "seamonkey", "yandex", "palemoon", "waterfox" };

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

                using (var htmKey = Registry.ClassesRoot.CreateSubKey(".htm"))
                {
                    htmKey?.SetValue("", progId, RegistryValueKind.String);
                }

                string commandKeyPath = $"{progId}\\shell\\open\\command";
                using (var cmdKey = Registry.ClassesRoot.CreateSubKey(commandKeyPath))
                {
                    string commandValue = $"\"{browserExe}\" \"%1\"";
                    cmdKey?.SetValue("", commandValue, RegistryValueKind.String);
                }

                using (var progIdKey = Registry.ClassesRoot.CreateSubKey(progId))
                {
                    progIdKey?.SetValue("", "HTML Document (Custom)", RegistryValueKind.String);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private ICommand _manageSharedDatabaseCommand;
        public ICommand ManageSharedDatabaseCommand => new RelayCommand(_ => ManageSharedDatabase());

        private void ManageSharedDatabase()
        {
            string previousClientName = SelectedClient?.Name;

            var sharedDbWindow = new SharedDatabaseWindow();
            sharedDbWindow.Owner = Application.Current.MainWindow;

            sharedDbWindow.ShowDialog();

            ReloadDatabase();

            if (!string.IsNullOrEmpty(previousClientName))
            {
                SelectedClient = Clients.FirstOrDefault(c => c.Name == previousClientName);
            }
        }

        private void ReloadDatabase()
        {
            var loadedDatabase = LoadDatabase();

            Clients.Clear();
            foreach (var client in loadedDatabase.Clients)
                Clients.Add(client);

            var tmp = SelectedClient;
            SelectedClient = null;
            SelectedClient = tmp;

            if (SelectedClient != null)
                LoadFilesForSelectedClient();
            else
            {
                FilteredFiles.Clear();
                PasswordEntries = new ObservableCollection<FileModel>();
            }
        }

        private void OpenSharedDatabaseWindow()
        {
            try
            {
                var sharedDatabaseWindow = new SharedDatabaseWindow();
                sharedDatabaseWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ouverture de la fenêtre de gestion de base partagée : {ex.Message}",
                              "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
