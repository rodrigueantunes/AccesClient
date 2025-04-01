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

        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}