using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

using AccesClientWPF.Commands;
using AccesClientWPF.Helpers;
using AccesClientWPF.Models;
using AccesClientWPF.Views;

namespace AccesClientWPF.ViewModels
{
    public class SharedDatabaseViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<ClientModel> Clients { get; set; } = new();
        public ObservableCollection<FileModel> FilteredFiles { get; set; } = new();
        public ObservableCollection<FileModel> AllFiles { get; set; } = new();

        // =========================
        // Password (panneau droite)
        // =========================
        private ObservableCollection<FileModel> _passwordEntries = new();
        public ObservableCollection<FileModel> PasswordEntries
        {
            get => _passwordEntries;
            private set { _passwordEntries = value; OnPropertyChanged(nameof(PasswordEntries)); }
        }

        private FileModel _selectedPassword;
        public FileModel SelectedPassword
        {
            get => _selectedPassword;
            set { _selectedPassword = value; OnPropertyChanged(nameof(SelectedPassword)); }
        }

        // =========================
        // Sélections
        // =========================
        private ClientModel _selectedClient;
        public ClientModel SelectedClient
        {
            get => _selectedClient;
            set
            {
                _selectedClient = value;
                OnPropertyChanged(nameof(SelectedClient));
                LoadFilesForSelectedClient();
            }
        }

        private FileModel _selectedFile;
        public FileModel SelectedFile
        {
            get => _selectedFile;
            set { _selectedFile = value; OnPropertyChanged(nameof(SelectedFile)); }
        }

        // =========================
        // UI flags
        // =========================
        public string AppVersionText => Helpers.AppVersion.CurrentString;
        public string WindowTitle => $"Accès Client {AppVersionText} / Partagé";

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

        // =========================
        // Multi-niveaux (split)
        // =========================
        public ObservableCollection<FileModel> RootFiles { get; } = new();
        public ObservableCollection<FileModel> Level2Files { get; } = new();

        private FileModel _selectedRootItem;
        public FileModel SelectedRootItem
        {
            get => _selectedRootItem;
            set
            {
                _selectedRootItem = value;
                OnPropertyChanged(nameof(SelectedRootItem));

                // utile pour le context menu & actions existantes
                SelectedFile = value;

                if (_selectedRootItem?.Type == "Rangement")
                    SelectedRangementName = _selectedRootItem.Name;
            }
        }

        private string _selectedRangementName;
        public string SelectedRangementName
        {
            get => _selectedRangementName;
            set
            {
                _selectedRangementName = value;
                OnPropertyChanged(nameof(SelectedRangementName));
                RefreshLevel2Files();
            }
        }

        private bool _hasLevel2;
        public bool HasLevel2
        {
            get => _hasLevel2;
            set { _hasLevel2 = value; OnPropertyChanged(nameof(HasLevel2)); }
        }

        // =========================
        // Commands (si tu les utilises ailleurs)
        // =========================
        public ICommand AddFileCommand { get; }

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
                MessageBox.Show($"Mot de passe copié.", "Copie", MessageBoxButton.OK, MessageBoxImage.Information);
        });

        public event PropertyChangedEventHandler PropertyChanged;

        public SharedDatabaseViewModel()
        {
            // Bouton "Ajouter un élément" (thème) + context menu "Ajouter"
            AddFileCommand = new RelayCommand(_ => AddFile());

            MoveUpFileCommand = new RelayCommand(_ => MoveCenter(-1), _ => CanMoveCenter(-1));
            MoveDownFileCommand = new RelayCommand(_ => MoveCenter(+1), _ => CanMoveCenter(+1));

            // Password panel actions
            AddPasswordFromRightCommand = new RelayCommand(_ => AddPassword());
            EditPasswordCommand = new RelayCommand(p => EditPassword((p as FileModel) ?? SelectedPassword));
            DeletePasswordCommand = new RelayCommand(p => DeletePassword((p as FileModel) ?? SelectedPassword));
            MovePasswordUpCommand = new RelayCommand(p => MovePasswordUp((p as FileModel) ?? SelectedPassword));
            MovePasswordDownCommand = new RelayCommand(p => MovePasswordDown((p as FileModel) ?? SelectedPassword));
        }

        private FileModel GetSelectedCenterItem()
        {
            return HasLevel2 ? (SelectedFile ?? SelectedRootItem) : SelectedFile;
        }

        private bool CanMoveCenter(int delta)
        {
            var item = GetSelectedCenterItem();
            if (item == null) return false;

            if (!HasLevel2)
            {
                int idx = FilteredFiles.IndexOf(item);
                if (idx < 0) return false;
                int nidx = idx + delta;
                return nidx >= 0 && nidx < FilteredFiles.Count;
            }

            bool inLevel2 = !string.IsNullOrWhiteSpace(item.RangementParent);
            var list = inLevel2 ? Level2Files : RootFiles;

            int i = list.IndexOf(item);
            if (i < 0) return false;
            int ni = i + delta;
            return ni >= 0 && ni < list.Count;
        }

        private void MoveCenter(int delta)
        {
            var item = GetSelectedCenterItem();
            if (item == null) return;

            if (!HasLevel2)
            {
                int idx = FilteredFiles.IndexOf(item);
                int nidx = idx + delta;
                if (idx < 0 || nidx < 0 || nidx >= FilteredFiles.Count) return;

                FilteredFiles.Move(idx, nidx);

                var allScope = AllFiles
                    .Where(f => f.Client == SelectedClient?.Name && f.Type != "MotDePasse")
                    .ToList();

                int aidx = allScope.IndexOf(item);
                int naidx = aidx + delta;
                if (aidx >= 0 && naidx >= 0 && naidx < allScope.Count)
                {
                    allScope.RemoveAt(aidx);
                    allScope.Insert(naidx, item);

                    var rebuilt = AllFiles.ToList();
                    rebuilt.RemoveAll(f => f.Client == SelectedClient?.Name && f.Type != "MotDePasse");
                    rebuilt.AddRange(allScope);

                    AllFiles = new ObservableCollection<FileModel>(rebuilt);
                    OnPropertyChanged(nameof(AllFiles));
                }

                OnPropertyChanged(nameof(FilteredFiles));
                return;
            }

            bool inLevel2 = !string.IsNullOrWhiteSpace(item.RangementParent);
            var list = inLevel2 ? Level2Files : RootFiles;

            int i = list.IndexOf(item);
            int ni = i + delta;
            if (i < 0 || ni < 0 || ni >= list.Count) return;

            list.Move(i, ni);

            string parent = item.RangementParent ?? "";
            var allScope2 = AllFiles
                .Where(f =>
                    f.Client == SelectedClient?.Name &&
                    f.Type != "MotDePasse" &&
                    string.Equals((f.RangementParent ?? ""), parent, StringComparison.OrdinalIgnoreCase))
                .ToList();

            int a = allScope2.IndexOf(item);
            int na = a + delta;
            if (a >= 0 && na >= 0 && na < allScope2.Count)
            {
                allScope2.RemoveAt(a);
                allScope2.Insert(na, item);

                var rebuilt = AllFiles.ToList();
                rebuilt.RemoveAll(f =>
                    f.Client == SelectedClient?.Name &&
                    f.Type != "MotDePasse" &&
                    string.Equals((f.RangementParent ?? ""), parent, StringComparison.OrdinalIgnoreCase));

                rebuilt.AddRange(allScope2);

                AllFiles = new ObservableCollection<FileModel>(rebuilt);
                OnPropertyChanged(nameof(AllFiles));
            }

            OnPropertyChanged(nameof(RootFiles));
            OnPropertyChanged(nameof(Level2Files));
        }

        // =========================
        // Import / Export DB
        // =========================
        public void LoadDatabase(AccesClientWPF.Models.DatabaseModel database)
        {
            LoadClients(database.Clients);

            AllFiles.Clear();
            foreach (var file in database.Files)
                AllFiles.Add(file);

            OnPropertyChanged(nameof(AllFiles));
            LoadFilesForSelectedClient();
        }

        public AccesClientWPF.Models.DatabaseModel ExportDatabase()
        {
            return new AccesClientWPF.Models.DatabaseModel
            {
                Clients = new ObservableCollection<ClientModel>(Clients),
                Files = new ObservableCollection<FileModel>(AllFiles)
            };
        }

        public void LoadClients(ObservableCollection<ClientModel> clients)
        {
            Clients.Clear();
            foreach (var client in clients)
                Clients.Add(client);

            OnPropertyChanged(nameof(Clients));
        }

        // =========================
        // Filtrage + Split
        // =========================
        private void LoadFilesForSelectedClient()
        {
            FilteredFiles.Clear();
            RootFiles.Clear();
            Level2Files.Clear();

            if (SelectedClient == null)
            {
                PasswordEntries = new ObservableCollection<FileModel>();
                HasLevel2 = false;
                SelectedRangementName = null;
                OnPropertyChanged(nameof(FilteredFiles));
                return;
            }

            var all = AllFiles
                .Where(f => f.Client == SelectedClient.Name)
                .ToList();

            PasswordEntries = new ObservableCollection<FileModel>(
                all.Where(f => f.Type == "MotDePasse")
            );

            var center = all.Where(f => f.Type != "MotDePasse").ToList();

            HasLevel2 =
                center.Any(f => f.Type == "Rangement" && string.IsNullOrWhiteSpace(f.RangementParent)) ||
                center.Any(f => !string.IsNullOrWhiteSpace(f.RangementParent));

            if (!HasLevel2)
            {
                foreach (var file in center)
                    FilteredFiles.Add(file);

                OnPropertyChanged(nameof(FilteredFiles));
                return;
            }

            // Racine : rangements (Type=Rangement) + éléments sans parent
            foreach (var file in center.Where(f => string.IsNullOrWhiteSpace(f.RangementParent)))
                RootFiles.Add(file);

            // sélection par défaut : premier rangement
            var firstRangement = RootFiles.FirstOrDefault(x => x.Type == "Rangement")?.Name;

            if (string.IsNullOrWhiteSpace(SelectedRangementName) ||
                !RootFiles.Any(x => x.Type == "Rangement" && x.Name == SelectedRangementName))
            {
                SelectedRangementName = firstRangement;
            }
            else
            {
                RefreshLevel2Files();
            }

            OnPropertyChanged(nameof(RootFiles));
            OnPropertyChanged(nameof(Level2Files));
        }

        private void RefreshLevel2Files()
        {
            Level2Files.Clear();

            if (SelectedClient == null || string.IsNullOrWhiteSpace(SelectedRangementName))
            {
                OnPropertyChanged(nameof(Level2Files));
                return;
            }

            foreach (var file in AllFiles.Where(f =>
                         f.Client == SelectedClient.Name &&
                         f.Type != "MotDePasse" &&
                         string.Equals(f.RangementParent, SelectedRangementName, StringComparison.OrdinalIgnoreCase)))
            {
                Level2Files.Add(file);
            }

            OnPropertyChanged(nameof(Level2Files));
        }

        // =========================
        // Déplacements multi-niveaux
        // =========================
        public void MoveToRacine(FileModel item)
        {
            if (SelectedClient == null || item == null) return;
            if (string.IsNullOrWhiteSpace(item.RangementParent)) return;

            item.RangementParent = null;
            LoadFilesForSelectedClient();
        }

        public void MoveToRangement(FileModel item, string rangement)
        {
            if (SelectedClient == null || item == null) return;
            if (string.IsNullOrWhiteSpace(rangement)) return;
            if (string.Equals(item.Type, "Rangement", StringComparison.OrdinalIgnoreCase)) return;

            item.RangementParent = rangement;
            LoadFilesForSelectedClient();
        }

        // =========================
        // Clients (utilisé par SharedClientManagementWindow)
        // =========================
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

        // =========================
        // DB operations
        // =========================
        public void CreateNewDatabase()
        {
            Clients.Clear();
            AllFiles.Clear();
            FilteredFiles.Clear();
            RootFiles.Clear();
            Level2Files.Clear();

            PasswordEntries = new ObservableCollection<FileModel>();
            SelectedClient = null;
            SelectedFile = null;
            SelectedRangementName = null;

            OnPropertyChanged(nameof(Clients));
            OnPropertyChanged(nameof(AllFiles));
            OnPropertyChanged(nameof(FilteredFiles));
            OnPropertyChanged(nameof(RootFiles));
            OnPropertyChanged(nameof(Level2Files));
        }

        // =========================
        // Files CRUD (AddEntryWindow)
        // =========================
        public void AddFile()
        {
            if (SelectedClient == null)
            {
                MessageBox.Show("Veuillez sélectionner un client avant d'ajouter un élément.",
                    "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var current = SelectedClient;

            // AddEntryWindow existe déjà dans ton projet (utilisé dans ta base partagée)
            var add = new AddEntryWindow(Clients, SelectedClient);
            if (add.ShowDialog() == true && add.FileEntry != null)
            {
                AllFiles.Add(add.FileEntry);

                // refresh UI (retrigger SelectedClient setter)
                SelectedClient = null;
                SelectedClient = Clients.FirstOrDefault(c => c.Name == current.Name);
            }
        }

        public void EditSelectedFile() => EditSelectedFile(null);

        public void EditSelectedFile(FileModel file)
        {
            var original = file ?? SelectedFile;
            if (original == null)
            {
                MessageBox.Show("Veuillez sélectionner un élément à modifier.", "Information",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // ✅ RÈGLE : si le rangement contient des éléments => pas de modification (renommage interdit)
            if (string.Equals((original.Type ?? "").Trim(), "Rangement", StringComparison.OrdinalIgnoreCase))
            {
                bool hasChildren = AllFiles.Any(f =>
                    string.Equals(f.Client, original.Client, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(f.Type, "MotDePasse", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(f.RangementParent ?? "", original.Name ?? "", StringComparison.OrdinalIgnoreCase));

                if (hasChildren)
                {
                    MessageBox.Show(
                        "Modification interdite : ce rangement contient des éléments.\n" +
                        "Déplace d'abord les éléments vers la racine ou un autre rangement, puis renomme.",
                        "Modification interdite",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }
            }

            int originalIndex = AllFiles.IndexOf(original);
            if (originalIndex < 0)
            {
                MessageBox.Show("Élément introuvable dans la liste.", "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var workingCopy = new FileModel
            {
                Name = original.Name,
                Client = original.Client,
                Type = original.Type,
                FullPath = original.FullPath,
                CustomIconPath = original.CustomIconPath,
                WindowsUsername = original.WindowsUsername,
                WindowsPassword = original.WindowsPassword,
                Username = original.Username,
                Password = original.Password,
                RangementParent = original.RangementParent
            };

            var currentClient = SelectedClient;

            var edit = new AddEntryWindow(Clients, SelectedClient, workingCopy);
            if (edit.ShowDialog() == true && edit.FileEntry != null)
            {
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

                AllFiles[originalIndex] = edit.FileEntry;

                SelectedClient = null;
                SelectedClient = Clients.FirstOrDefault(c => c.Name == currentClient?.Name);

                SelectedFile = null;
            }
        }

        public void DeleteSelectedFile()
        {
            if (SelectedFile == null) return;

            AllFiles.Remove(SelectedFile);
            SelectedFile = null;

            LoadFilesForSelectedClient();
        }

        // =========================
        // Up/Down for Files (mode "single")
        // =========================
        private void MoveUp()
        {
            if (SelectedFile == null) return;
            int index = FilteredFiles.IndexOf(SelectedFile);
            if (index <= 0) return;

            // Move in FilteredFiles (UI)
            FilteredFiles.Move(index, index - 1);

            // Also try to keep ordering in AllFiles for persistence
            int allIndex = AllFiles.IndexOf(SelectedFile);
            if (allIndex > 0) AllFiles.Move(allIndex, allIndex - 1);

            OnPropertyChanged(nameof(FilteredFiles));
            OnPropertyChanged(nameof(AllFiles));
        }

        private void MoveDown()
        {
            if (SelectedFile == null) return;
            int index = FilteredFiles.IndexOf(SelectedFile);
            if (index < 0 || index >= FilteredFiles.Count - 1) return;

            FilteredFiles.Move(index, index + 1);

            int allIndex = AllFiles.IndexOf(SelectedFile);
            if (allIndex >= 0 && allIndex < AllFiles.Count - 1) AllFiles.Move(allIndex, allIndex + 1);

            OnPropertyChanged(nameof(FilteredFiles));
            OnPropertyChanged(nameof(AllFiles));
        }

        // =========================
        // Password actions (panneau droite)
        // =========================
        private void AddPassword()
        {
            if (SelectedClient == null)
            {
                MessageBox.Show("Sélectionne un client d’abord.", "Information",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var current = SelectedClient;
            var add = new AddEntryWindow(Clients, SelectedClient); // l’utilisateur choisit Type=MotDePasse
            if (add.ShowDialog() == true && add.FileEntry != null)
            {
                AllFiles.Add(add.FileEntry);
                SelectedClient = null;
                SelectedClient = Clients.FirstOrDefault(c => c.Name == current.Name);
            }
        }

        private void EditPassword(FileModel pwd)
        {
            if (pwd == null)
            {
                MessageBox.Show("Sélectionne un mot de passe.", "Information",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            EditSelectedFile(pwd);
        }

        private void DeletePassword(FileModel pwd)
        {
            if (pwd == null)
            {
                MessageBox.Show("Sélectionne un mot de passe.", "Information",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (AllFiles.Contains(pwd))
                AllFiles.Remove(pwd);

            LoadFilesForSelectedClient();
        }

        private void MovePasswordUp(FileModel pwd)
        {
            if (pwd == null) return;
            int index = PasswordEntries.IndexOf(pwd);
            if (index <= 0) return;

            PasswordEntries.Move(index, index - 1);

            int allIndex = AllFiles.IndexOf(pwd);
            if (allIndex > 0) AllFiles.Move(allIndex, allIndex - 1);

            OnPropertyChanged(nameof(PasswordEntries));
            OnPropertyChanged(nameof(AllFiles));
        }

        private void MovePasswordDown(FileModel pwd)
        {
            if (pwd == null) return;
            int index = PasswordEntries.IndexOf(pwd);
            if (index < 0 || index >= PasswordEntries.Count - 1) return;

            PasswordEntries.Move(index, index + 1);

            int allIndex = AllFiles.IndexOf(pwd);
            if (allIndex >= 0 && allIndex < AllFiles.Count - 1) AllFiles.Move(allIndex, allIndex + 1);

            OnPropertyChanged(nameof(PasswordEntries));
            OnPropertyChanged(nameof(AllFiles));
        }

        // =========================
        // INotifyPropertyChanged
        // =========================
        protected virtual void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}