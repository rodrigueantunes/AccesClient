using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using AccesClientWPF.Commands;
using AccesClientWPF.Helpers;
using AccesClientWPF.Models;
using AccesClientWPF.Views;
using System.Collections.Generic;

namespace AccesClientWPF.ViewModels
{
    public class SharedDatabaseViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<ClientModel> Clients { get; set; } = new();
        public ObservableCollection<FileModel> FilteredFiles { get; set; } = new();
        public ObservableCollection<FileModel> AllFiles { get; set; } = new();

        private ObservableCollection<FileModel> _passwordEntries = new();
        public ObservableCollection<FileModel> PasswordEntries
        {
            get => _passwordEntries;
            private set { _passwordEntries = value; OnPropertyChanged(nameof(PasswordEntries)); }
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
            set { selectedFile = value; OnPropertyChanged(nameof(SelectedFile)); }
        }

        private FileModel selectedPassword;
        public FileModel SelectedPassword
        {
            get => selectedPassword;
            set { selectedPassword = value; OnPropertyChanged(nameof(SelectedPassword)); }
        }

        private bool _isMultiMonitor;
        public bool IsMultiMonitor
        {
            get => _isMultiMonitor;
            set { if (_isMultiMonitor != value) { _isMultiMonitor = value; OnPropertyChanged(nameof(IsMultiMonitor)); } }
        }

        private bool _showCredentials;
        public bool ShowCredentials
        {
            get => _showCredentials;
            set { if (_showCredentials != value) { _showCredentials = value; OnPropertyChanged(nameof(ShowCredentials)); } }
        }

        public ICommand MoveUpFileCommand { get; }
        public ICommand MoveDownFileCommand { get; }

        public ICommand AddPasswordFromRightCommand { get; }
        public ICommand EditPasswordCommand { get; }
        public ICommand DeletePasswordCommand { get; }
        public ICommand MovePasswordUpCommand { get; }
        public ICommand MovePasswordDownCommand { get; }

        public ICommand CopyUsernameCommand => new RelayCommand(u =>
        {
            var txt = u as string ?? string.Empty;
            ClipboardHelper.CopyPlainText(txt);
            if (!string.IsNullOrWhiteSpace(txt))
                MessageBox.Show($"Nom d’utilisateur « {txt} » copié.", "Copie", MessageBoxButton.OK, MessageBoxImage.Information);
        });

        public ICommand CopyPasswordCommand => new RelayCommand(p =>
        {
            var s = p as string ?? string.Empty;
            var dec = EncryptionHelper.Decrypt(s);
            var toCopy = string.IsNullOrEmpty(dec) ? s : dec;
            ClipboardHelper.CopyPlainText(toCopy);
            if (!string.IsNullOrWhiteSpace(toCopy))
                MessageBox.Show($"Mot de passe « {toCopy} » copié.", "Copie", MessageBoxButton.OK, MessageBoxImage.Information);
        });

        public event PropertyChangedEventHandler PropertyChanged;

        public SharedDatabaseViewModel()
        {
            MoveUpFileCommand = new RelayCommand(_ => MoveUp(), _ => SelectedFile != null && FilteredFiles.IndexOf(SelectedFile) > 0);
            MoveDownFileCommand = new RelayCommand(_ => MoveDown(), _ => SelectedFile != null && FilteredFiles.IndexOf(SelectedFile) < FilteredFiles.Count - 1);

            AddPasswordFromRightCommand = new RelayCommand(_ => AddPassword());
            EditPasswordCommand = new RelayCommand(p => EditPassword((p as FileModel) ?? SelectedPassword));
            DeletePasswordCommand = new RelayCommand(p => DeletePassword((p as FileModel) ?? SelectedPassword));
            MovePasswordUpCommand = new RelayCommand(p => MovePasswordUp((p as FileModel) ?? SelectedPassword));
            MovePasswordDownCommand = new RelayCommand(p => MovePasswordDown((p as FileModel) ?? SelectedPassword));
        }

        public void LoadDatabase(AccesClientWPF.Models.DatabaseModel database)

        {
            LoadClients(database.Clients);

            AllFiles.Clear();
            foreach (var file in database.Files)
                AllFiles.Add(file);

            OnPropertyChanged(nameof(AllFiles));
            LoadFilesForSelectedClient();
        }

        public void LoadClients(ObservableCollection<ClientModel> clients)
        {
            Clients.Clear();
            foreach (var client in clients)
                Clients.Add(client);

            OnPropertyChanged(nameof(Clients));
        }

        private void LoadFilesForSelectedClient()
        {
            FilteredFiles.Clear();

            if (SelectedClient != null)
            {
                foreach (var f in AllFiles.Where(f => f.Client == SelectedClient.Name && f.Type != "MotDePasse"))
                    FilteredFiles.Add(f);

                PasswordEntries = new ObservableCollection<FileModel>(
                    AllFiles.Where(f => f.Client == SelectedClient.Name && f.Type == "MotDePasse"));
            }
            else
            {
                PasswordEntries = new ObservableCollection<FileModel>();
            }

            OnPropertyChanged(nameof(FilteredFiles));
        }

        public void AddClient(string clientName)
        {
            if (string.IsNullOrWhiteSpace(clientName)) return;

            if (!Clients.Any(c => string.Equals(c.Name, clientName, StringComparison.OrdinalIgnoreCase)))
            {
                Clients.Add(new ClientModel { Name = clientName });
                OnPropertyChanged(nameof(Clients));
            }
        }

        public void DeleteClient(ClientModel client)
        {
            if (client == null) return;

            var filesToRemove = AllFiles.Where(f => f.Client == client.Name).ToList();
            foreach (var f in filesToRemove)
                AllFiles.Remove(f);

            Clients.Remove(client);

            if (SelectedClient == client)
                SelectedClient = null;

            OnPropertyChanged(nameof(Clients));
            OnPropertyChanged(nameof(AllFiles));
            LoadFilesForSelectedClient();
        }

        public void MoveClientUp(ClientModel client)
        {
            if (client == null) return;

            int index = Clients.IndexOf(client);
            if (index > 0)
            {
                Clients.Move(index, index - 1);
                OnPropertyChanged(nameof(Clients));
            }
        }

        public void MoveClientDown(ClientModel client)
        {
            if (client == null) return;

            int index = Clients.IndexOf(client);
            if (index < Clients.Count - 1)
            {
                Clients.Move(index, index + 1);
                OnPropertyChanged(nameof(Clients));
            }
        }

        public void CreateNewDatabase()
        {
            Clients.Clear();
            AllFiles.Clear();
            FilteredFiles.Clear();
            PasswordEntries = new ObservableCollection<FileModel>();

            OnPropertyChanged(nameof(Clients));
            OnPropertyChanged(nameof(AllFiles));
            OnPropertyChanged(nameof(FilteredFiles));
        }

        public void AddFile()
        {
            if (SelectedClient == null)
            {
                MessageBox.Show("Veuillez sélectionner un client avant d'ajouter un élément.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var current = SelectedClient;
            var add = new AddEntryWindow(Clients, SelectedClient);
            if (add.ShowDialog() == true && add.FileEntry != null)
            {
                AllFiles.Add(add.FileEntry);
                SelectedClient = null;
                SelectedClient = Clients.FirstOrDefault(c => c.Name == current.Name);
            }
        }

        public void EditSelectedFile(FileModel file = null)
        {
            var original = file ?? SelectedFile;
            if (original == null)
            {
                MessageBox.Show("Veuillez sélectionner un élément à modifier.", "Information",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // On mémorise l’index de l’élément dans AllFiles pour garantir le remplacement
            int originalIndex = AllFiles.IndexOf(original);
            if (originalIndex < 0)
            {
                MessageBox.Show("Élément introuvable dans la liste.", "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Copie de travail (pour éviter de bidouiller l'original tant que l'utilisateur n'a pas validé)
            var workingCopy = new FileModel
            {
                Name = original.Name,
                Client = original.Client,
                Type = original.Type,
                FullPath = original.FullPath,
                CustomIconPath = original.CustomIconPath,
                WindowsUsername = original.WindowsUsername,
                WindowsPassword = original.WindowsPassword
            };

            var currentClient = SelectedClient;

            // Ouverture en mode édition sur la copie
            var edit = new AddEntryWindow(Clients, SelectedClient, workingCopy);
            if (edit.ShowDialog() == true && edit.FileEntry != null)
            {
                // Optionnel : empêcher les doublons (même Client + même Nom sur un autre item)
                bool duplicateExists = AllFiles
                    .Where((f, idx) => idx != originalIndex)
                    .Any(f => f.Client == edit.FileEntry.Client && f.Name == edit.FileEntry.Name);

                if (duplicateExists)
                {
                    MessageBox.Show(
                        $"Un élément nommé '{edit.FileEntry.Name}' existe déjà pour le client '{edit.FileEntry.Client}'.",
                        "Nom en double", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Remplacer à la même position
                AllFiles[originalIndex] = edit.FileEntry;

                // Rafraîchir la vue filtrée
                SelectedClient = null;
                SelectedClient = Clients.FirstOrDefault(c => c.Name == currentClient?.Name);
                // Reselect l’élément modifié si encore visible
                SelectedFile = FilteredFiles.FirstOrDefault(f =>
                    f.Name == edit.FileEntry.Name && f.Client == edit.FileEntry.Client && f.Type == edit.FileEntry.Type);
            }
        }


        public void DeleteSelectedFile()
        {
            if (SelectedFile == null) return;

            var toRemove = AllFiles.FirstOrDefault(f => f == SelectedFile) ??
                           AllFiles.FirstOrDefault(f => f.Client == SelectedFile.Client && f.Name == SelectedFile.Name && f.Type == SelectedFile.Type);
            if (toRemove != null)
            {
                AllFiles.Remove(toRemove);
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
            if (SelectedClient == null) return;

            var others = AllFiles.Where(f => !(f.Client == SelectedClient.Name && f.Type != "MotDePasse")).ToList();
            AllFiles.Clear();
            foreach (var o in others) AllFiles.Add(o);
            foreach (var f in FilteredFiles) AllFiles.Add(f);
            foreach (var p in PasswordEntries) AllFiles.Add(p);
        }

        private void AddPassword()
        {
            if (SelectedClient == null)
            {
                MessageBox.Show("Veuillez sélectionner un client avant d'ajouter un mot de passe.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var current = SelectedClient;
            var win = new AddEntryWindow(Clients, SelectedClient);
            win.SetTypeMotDePasse();
            if (win.ShowDialog() == true && win.FileEntry != null)
            {
                win.FileEntry.Type = "MotDePasse";
                AllFiles.Add(win.FileEntry);

                SelectedClient = null;
                SelectedClient = Clients.FirstOrDefault(c => c.Name == current.Name);
            }
        }

        private void EditPassword(FileModel item)
        {
            if (item == null) return;

            // Toujours retrouver l'instance Client depuis la liste
            var currentClient = Clients.FirstOrDefault(c => c.Name == item.Client) ?? SelectedClient;
            if (currentClient == null)
            {
                MessageBox.Show("Aucun client sélectionné pour cette entrée.", "Information",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var tempFile = new FileModel
            {
                Name = item.Name,
                Client = item.Client,
                Type = "MotDePasse",
                WindowsUsername = item.WindowsUsername,
                WindowsPassword = item.WindowsPassword
            };

            var win = new AddEntryWindow(Clients, currentClient, tempFile);
            win.SetTypeMotDePasse();

            if (win.ShowDialog() == true && win.FileEntry != null)
            {
                win.FileEntry.Type = "MotDePasse";

                var existing = AllFiles.FirstOrDefault(f => f == item) ??
                               AllFiles.FirstOrDefault(f => f.Client == item.Client &&
                                                            f.Name == item.Name &&
                                                            f.Type == "MotDePasse");

                if (existing != null)
                {
                    int index = AllFiles.IndexOf(existing);
                    AllFiles[index] = win.FileEntry;
                }
                else
                {
                    AllFiles.Add(win.FileEntry);
                }

                // Refresh pour reconstituer PasswordEntries / FilteredFiles proprement
                var restore = Clients.FirstOrDefault(c => c.Name == currentClient.Name);
                SelectedClient = null;
                SelectedClient = restore;
            }
        }


        private void DeletePassword(FileModel item)
        {
            if (item == null) return;

            var ask = MessageBox.Show($"Supprimer le mot de passe '{item.Name}' ?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (ask != MessageBoxResult.Yes) return;

            var toRemove = AllFiles.FirstOrDefault(f => f == item) ??
                           AllFiles.FirstOrDefault(f => f.Client == item.Client && f.Name == item.Name && f.Type == "MotDePasse");
            if (toRemove != null)
            {
                AllFiles.Remove(toRemove);
                PasswordEntries.Remove(item);
            }
        }

        private void MovePasswordUp(FileModel item)
        {
            if (item == null) return;
            int index = PasswordEntries.IndexOf(item);
            if (index > 0)
            {
                PasswordEntries.Move(index, index - 1);
                SelectedPassword = item;
                SavePasswordsOrder();
            }
        }

        private void MovePasswordDown(FileModel item)
        {
            if (item == null) return;
            int index = PasswordEntries.IndexOf(item);
            if (index < PasswordEntries.Count - 1)
            {
                PasswordEntries.Move(index, index + 1);
                SelectedPassword = item;
                SavePasswordsOrder();
            }
        }

        private void SavePasswordsOrder()
        {
            if (SelectedClient == null) return;

            var others = AllFiles.Where(f => !(f.Client == SelectedClient.Name && f.Type == "MotDePasse")).ToList();
            AllFiles.Clear();
            foreach (var o in others) AllFiles.Add(o);
            foreach (var p in PasswordEntries) AllFiles.Add(p);
        }

        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
