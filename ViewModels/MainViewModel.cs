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
            Clients = database.Clients;
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

        public void HandleFileDoubleClick(FileModel file)
        {
            if (file.Type == "RDS")
                ConnectToRemoteDesktop(file);
            if (file.Type == "AnyDesk")
                ConnectToAnyDesk(file);
            else
                MessageBox.Show($"Ouverture de {file.Type}: {file.Name}");
        }

        private void ConnectToRemoteDesktop(FileModel file)
        {
            var accounts = JsonConvert.DeserializeObject<ObservableCollection<RdsAccount>>(File.ReadAllText(_accountsFilePath));
            var credentials = accounts.FirstOrDefault(a => a.Description.Equals(file.Name, StringComparison.OrdinalIgnoreCase));

            if (credentials == null)
            {
                MessageBox.Show("Aucune information de connexion enregistrée pour ce fichier RDP.");
                return;
            }

            string decryptedPassword = EncryptionHelper.Decrypt(credentials.MotDePasse);
            string args = $"/v:{credentials.IpDns} {(IsMultiMonitor ? "/multimon" : "/f")}";

            Process.Start("cmd.exe", $"/C cmdkey /generic:{credentials.IpDns} /user:\"{credentials.NomUtilisateur}\" /pass:\"{decryptedPassword}\"");
            Process.Start("mstsc", args);
        }

        private void ConnectToAnyDesk(FileModel file)
        {
            var accounts = JsonConvert.DeserializeObject<ObservableCollection<RdsAccount>>(File.ReadAllText(_accountsFilePath));
            var credentials = accounts.FirstOrDefault(a => a.Description.Equals(file.Name, StringComparison.OrdinalIgnoreCase));

            if (credentials == null)
            {
                MessageBox.Show("Aucune information de connexion enregistrée pour AnyDesk.");
                return;
            }

            string decryptedPassword = EncryptionHelper.Decrypt(credentials.MotDePasse);
            string anyDeskPath = @"C:\Program Files (x86)\AnyDesk\AnyDesk.exe";

            if (!File.Exists(anyDeskPath))
            {
                MessageBox.Show("AnyDesk n'est pas installé à l'emplacement attendu.");
                return;
            }

            // Argument avec le nom d'utilisateur complet (même s'il y a des espaces)
            string args = $"echo \"{decryptedPassword}\" | \"{credentials.NomUtilisateur}\" --with-password";

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = anyDeskPath,
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,  // Ne pas afficher de fenêtre de commande
            };

            try
            {
                Process.Start(psi);
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
            var clientWindow = new ClientManagementWindow(Clients);
            if (clientWindow.ShowDialog() == true)
            {
                LoadClients();
                LoadFilesForSelectedClient();
            }
        }

        private void AddFile()
        {
            var database = LoadDatabase();
            var addEntryWindow = new AddEntryWindow(Clients, SelectedClient);
            if (addEntryWindow.ShowDialog() == true && addEntryWindow.FileEntry != null)
            {
                database.Files.Add(addEntryWindow.FileEntry);
                SaveDatabase(database);
                LoadFilesForSelectedClient();
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
            db.Files = new ObservableCollection<FileModel>(FilteredFiles);
            SaveDatabase(db);
        }

        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}