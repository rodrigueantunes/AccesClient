using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using AccesClientWPF.Commands;
using AccesClientWPF.Helpers;
using AccesClientWPF.Models;
using AccesClientWPF.Views;
using Newtonsoft.Json;

namespace AccesClientWPF.ViewModels
{
    public class SharedDatabaseViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<ClientModel> Clients { get; set; } = new();
        public ObservableCollection<FileModel> FilteredFiles { get; set; } = new();

        // Collection de tous les fichiers (pas seulement ceux du client sélectionné)
        public ObservableCollection<FileModel> AllFiles { get; set; } = new();

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

        public ICommand MoveUpFileCommand { get; }
        public ICommand MoveDownFileCommand { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        public SharedDatabaseViewModel()
        {
            MoveUpFileCommand = new RelayCommand(_ => MoveUp(), _ => SelectedFile != null && FilteredFiles.IndexOf(SelectedFile) > 0);
            MoveDownFileCommand = new RelayCommand(_ => MoveDown(), _ => SelectedFile != null && FilteredFiles.IndexOf(SelectedFile) < FilteredFiles.Count - 1);
        }

        public void LoadDatabase(AccesClientWPF.Models.DatabaseModel database)
        {
            // Charger les clients
            LoadClients(database.Clients);

            // Charger tous les fichiers
            AllFiles.Clear();
            foreach (var file in database.Files)
            {
                AllFiles.Add(file);
            }

            // Notifier les changements
            OnPropertyChanged(nameof(AllFiles));
        }

        public void LoadClients(ObservableCollection<ClientModel> clients)
        {
            // Vider la collection existante
            Clients.Clear();

            // Ajouter les clients
            foreach (var client in clients)
            {
                Clients.Add(client);
            }

            OnPropertyChanged(nameof(Clients));
        }

        private void LoadFilesForSelectedClient()
        {
            FilteredFiles.Clear();

            if (SelectedClient != null)
            {
                foreach (var file in AllFiles.Where(f => f.Client == SelectedClient.Name))
                {
                    FilteredFiles.Add(file);
                }
            }

            OnPropertyChanged(nameof(FilteredFiles));
        }

        public void AddClient(string clientName)
        {
            if (string.IsNullOrWhiteSpace(clientName))
                return;

            if (!Clients.Any(c => c.Name.Equals(clientName, StringComparison.OrdinalIgnoreCase)))
            {
                Clients.Add(new ClientModel { Name = clientName });
                OnPropertyChanged(nameof(Clients));
            }
        }

        public void DeleteClient(ClientModel client)
        {
            if (client == null)
                return;

            // Supprimer tous les fichiers associés à ce client
            var filesToRemove = AllFiles.Where(f => f.Client == client.Name).ToList();
            foreach (var file in filesToRemove)
            {
                AllFiles.Remove(file);
            }

            // Supprimer le client
            Clients.Remove(client);

            // Réinitialiser la sélection
            if (SelectedClient == client)
            {
                SelectedClient = null;
            }

            OnPropertyChanged(nameof(Clients));
            OnPropertyChanged(nameof(AllFiles));
        }

        public void MoveClientUp(ClientModel client)
        {
            if (client == null)
                return;

            int index = Clients.IndexOf(client);
            if (index > 0)
            {
                Clients.Move(index, index - 1);
                OnPropertyChanged(nameof(Clients));
            }
        }

        public void MoveClientDown(ClientModel client)
        {
            if (client == null)
                return;

            int index = Clients.IndexOf(client);
            if (index < Clients.Count - 1)
            {
                Clients.Move(index, index + 1);
                OnPropertyChanged(nameof(Clients));
            }
        }

        public void AddFile()
        {
            if (SelectedClient == null)
            {
                MessageBox.Show("Veuillez sélectionner un client avant d'ajouter un élément.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Stocker le client actuellement sélectionné
            var currentlySelectedClient = SelectedClient;

            var addEntryWindow = new AddEntryWindow(Clients, SelectedClient);
            if (addEntryWindow.ShowDialog() == true && addEntryWindow.FileEntry != null)
            {
                AllFiles.Add(addEntryWindow.FileEntry);

                // Réappliquer la sélection du client pour recharger ses fichiers
                var clientName = currentlySelectedClient.Name;
                SelectedClient = null; // Forcer la réinitialisation
                SelectedClient = Clients.FirstOrDefault(c => c.Name == clientName);
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
                var existingFile = AllFiles.FirstOrDefault(f => f.Name == selectedFile.Name && f.Client == selectedFile.Client);
                if (existingFile != null)
                {
                    int index = AllFiles.IndexOf(existingFile);
                    AllFiles[index] = editWindow.FileEntry;

                    // Réappliquer la sélection du client pour recharger ses fichiers
                    var clientName = currentlySelectedClient.Name;
                    SelectedClient = null; // Forcer la réinitialisation  
                    SelectedClient = Clients.FirstOrDefault(c => c.Name == clientName);
                }
            }
        }

        public void CreateNewDatabase()
        {
            // Réinitialiser le modèle
            Clients.Clear();
            AllFiles.Clear();
            FilteredFiles.Clear();

            // Notifier les changements
            OnPropertyChanged(nameof(Clients));
            OnPropertyChanged(nameof(AllFiles));
            OnPropertyChanged(nameof(FilteredFiles));
        }

        public void DeleteSelectedFile()
        {
            if (SelectedFile != null)
            {
                // Supprimer de tous les fichiers
                AllFiles.Remove(SelectedFile);

                // Supprimer de la vue filtrée
                FilteredFiles.Remove(SelectedFile);
            }
        }

        public void MoveUp()
        {
            var index = FilteredFiles.IndexOf(SelectedFile);
            if (index > 0)
            {
                FilteredFiles.Move(index, index - 1);
                UpdateGlobalOrder();
            }
        }

        public void MoveDown()
        {
            var index = FilteredFiles.IndexOf(SelectedFile);
            if (index < FilteredFiles.Count - 1)
            {
                FilteredFiles.Move(index, index + 1);
                UpdateGlobalOrder();
            }
        }

        private void UpdateGlobalOrder()
        {
            // Mettre à jour l'ordre des fichiers dans la collection globale
            // Pour l'instant, l'ordre est préservé uniquement dans la vue filtrée
            // Si l'ordre est important pour la base partagée, cette méthode devrait 
            // réorganiser AllFiles pour correspondre à l'ordre dans FilteredFiles
        }

        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}