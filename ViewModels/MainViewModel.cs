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

                    if (_isMultiMonitor)
                    {
                        RefreshMonitors();
                        UseAllMonitors = true;
                    }
                    else
                    {
                        MonitorSlots = new ObservableCollection<MonitorSelectionSlot>();
                    }
                }
            }
        }

        public string AppVersionText => Helpers.AppVersion.CurrentString;
        public string WindowTitle => $"Accès Client {AppVersionText}";

        private bool _useAllMonitors = true;
        public bool UseAllMonitors
        {
            get => _useAllMonitors;
            set
            {
                if (_useAllMonitors != value)
                {
                    _useAllMonitors = value;
                    OnPropertyChanged(nameof(UseAllMonitors));

                    if (!IsMultiMonitor)
                        return;

                    if (_useAllMonitors)
                    {
                        SelectedMonitorCount = AvailableMonitorCount;
                        MonitorSlots = new ObservableCollection<MonitorSelectionSlot>();
                    }
                    else
                    {
                        if (SelectedMonitorCount < 1)
                            SelectedMonitorCount = 1;

                        BuildMonitorSlots(SelectedMonitorCount);
                    }
                }
            }
        }


        private int _availableMonitorCount;
        public int AvailableMonitorCount
        {
            get => _availableMonitorCount;
            private set
            {
                if (_availableMonitorCount != value)
                {
                    _availableMonitorCount = value;
                    OnPropertyChanged(nameof(AvailableMonitorCount));
                }
            }
        }

        private ObservableCollection<int> _monitorCountOptions = new();
        public ObservableCollection<int> MonitorCountOptions
        {
            get => _monitorCountOptions;
            private set
            {
                if (!ReferenceEquals(_monitorCountOptions, value))
                {
                    _monitorCountOptions = value;
                    OnPropertyChanged(nameof(MonitorCountOptions));
                }
            }
        }

        private int _selectedMonitorCount = 1;
        public int SelectedMonitorCount
        {
            get => _selectedMonitorCount;
            set
            {
                var clamped = Math.Max(1, Math.Min(value, Math.Max(1, AvailableMonitorCount)));
                if (_selectedMonitorCount != clamped)
                {
                    _selectedMonitorCount = clamped;
                    OnPropertyChanged(nameof(SelectedMonitorCount));

                    if (IsMultiMonitor && !UseAllMonitors)
                        BuildMonitorSlots(_selectedMonitorCount);
                }
            }
        }

        private ObservableCollection<MonitorSelectionSlot> _monitorSlots = new();
        public ObservableCollection<MonitorSelectionSlot> MonitorSlots
        {
            get => _monitorSlots;
            private set
            {
                if (!ReferenceEquals(_monitorSlots, value))
                {
                    _monitorSlots = value;
                    OnPropertyChanged(nameof(MonitorSlots));
                }
            }
        }

        private List<AccesClientWPF.Models.ScreenItem> _allScreens = new();
        private bool _isUpdatingMonitorSlots;

        public ObservableCollection<ClientModel> Clients { get; set; } = new();
        public ObservableCollection<FileModel> FilteredFiles { get; set; } = new();

        private ObservableCollection<FileModel> _passwordEntries = new();
        public ObservableCollection<FileModel> PasswordEntries
        {
            get => _passwordEntries;
            private set { _passwordEntries = value; OnPropertyChanged(nameof(PasswordEntries)); }
        }

        private async void ShowScreenNumbers()
        {
            // refresh rapide au cas où
            _allScreens = Helpers.MonitorHelper.GetAllScreens();

            // On affiche 2 secondes
            var windows = new List<Window>();

            try
            {
                foreach (var s in _allScreens)
                {
                    var boundsDips = AccesClientWPF.Views.ScreenNumberOverlayWindow.PixelsToDips(
                        s.Left, s.Top, s.Width, s.Height);

                    var w = new AccesClientWPF.Views.ScreenNumberOverlayWindow(s.Index + 1, boundsDips);
                    windows.Add(w);
                    w.Show();
                }

                await System.Threading.Tasks.Task.Delay(2000);
            }
            finally
            {
                foreach (var w in windows)
                {
                    try { w.Close(); } catch { /* ignore */ }
                }
            }
        }

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


        public ICommand ShowScreenNumbersCommand { get; }

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
            MoveUpFileCommand = new RelayCommand(_ => MoveUp(), _ => CanMoveSelected(-1));
            MoveDownFileCommand = new RelayCommand(_ => MoveDown(), _ => CanMoveSelected(+1));

            ShowScreenNumbersCommand = new RelayCommand(_ => ShowScreenNumbers());

            RefreshMonitors();
            SystemEvents.DisplaySettingsChanged += (_, __) => Application.Current.Dispatcher.Invoke(RefreshMonitors);

            LoadClients();
        }

        public ObservableCollection<string> AvailableRangements { get; } = new();

        private void ReloadAvailableRangements()
        {
            AvailableRangements.Clear();

            if (SelectedClient == null) return;

            var db = LoadDatabase();

            var rangements = db.Files
                .Where(f => f.Client == SelectedClient.Name)
                .Where(f => string.Equals(f.Type, "Rangement", StringComparison.OrdinalIgnoreCase))
                .Where(f => string.IsNullOrWhiteSpace(f.RangementParent))
                .Select(f => f.Name)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(n => n);

            foreach (var r in rangements)
                AvailableRangements.Add(r);
        }

        private bool CanMoveSelected(int delta)
        {
            if (SelectedClient == null) return false;

            var item = GetSelectedCenterItem();
            if (item == null) return false;

            var db = LoadDatabase();

            var center = db.Files
                .Where(f => f.Client == SelectedClient.Name && f.Type != "MotDePasse")
                .ToList();

            bool inLevel2 = !string.IsNullOrWhiteSpace(item.RangementParent);

            var scope = center
                .Where(f => inLevel2
                    ? string.Equals(f.RangementParent ?? "", item.RangementParent ?? "", StringComparison.OrdinalIgnoreCase)
                    : string.IsNullOrWhiteSpace(f.RangementParent))
                .ToList();

            int idx = scope.FindIndex(f =>
                string.Equals(f.Name, item.Name, StringComparison.OrdinalIgnoreCase)
                && string.Equals(f.Type, item.Type, StringComparison.OrdinalIgnoreCase)
                && string.Equals(f.RangementParent ?? "", item.RangementParent ?? "", StringComparison.OrdinalIgnoreCase));

            if (idx < 0) return false;

            int newIdx = idx + delta;
            return newIdx >= 0 && newIdx < scope.Count;
        }

        private void RefreshMonitors()
        {
            _allScreens = MonitorHelper.OrderByWindowsLayout(MonitorHelper.GetAllScreens());

            AvailableMonitorCount = _allScreens.Count;
            MonitorCountOptions = new ObservableCollection<int>(Enumerable.Range(1, Math.Max(1, AvailableMonitorCount)));

            if (!IsMultiMonitor)
                return;

            if (UseAllMonitors)
            {
                SelectedMonitorCount = AvailableMonitorCount;
                MonitorSlots = new ObservableCollection<MonitorSelectionSlot>();
                return;
            }

            if (SelectedMonitorCount > AvailableMonitorCount)
                SelectedMonitorCount = AvailableMonitorCount;

            BuildMonitorSlots(SelectedMonitorCount);
        }


        private void BuildMonitorSlots(int count)
        {
            if (AvailableMonitorCount <= 0)
            {
                MonitorSlots = new ObservableCollection<MonitorSelectionSlot>();
                return;
            }

            count = Math.Max(1, Math.Min(count, AvailableMonitorCount));

            _isUpdatingMonitorSlots = true;
            try
            {
                var prev = MonitorSlots.Select(s => s.SelectedScreen?.Index).Where(i => i.HasValue).Select(i => i.Value).ToList();
                var used = new HashSet<int>();
                var slots = new ObservableCollection<MonitorSelectionSlot>();

                for (int i = 0; i < count; i++)
                {
                    var slot = new MonitorSelectionSlot(i + 1, OnMonitorSlotChanged);

                    AccesClientWPF.Models.ScreenItem chosen = null;

                    if (i < prev.Count)
                    {
                        var wanted = prev[i];
                        chosen = _allScreens.FirstOrDefault(s => s.Index == wanted && !used.Contains(wanted));
                    }

                    if (chosen == null)
                        chosen = _allScreens.FirstOrDefault(s => !used.Contains(s.Index));

                    if (chosen != null)
                        used.Add(chosen.Index);

                    slot.SelectedScreen = chosen;
                    slots.Add(slot);
                }

                MonitorSlots = slots;
            }
            finally
            {
                _isUpdatingMonitorSlots = false;
            }

            UpdateAvailableScreensForAllSlots();
        }

        private void OnMonitorSlotChanged(MonitorSelectionSlot changedSlot)
        {
            if (_isUpdatingMonitorSlots)
                return;

            _isUpdatingMonitorSlots = true;
            try
            {
                if (changedSlot.SelectedScreen != null)
                {
                    var idx = changedSlot.SelectedScreen.Index;
                    foreach (var s in MonitorSlots.Where(s => s != changedSlot && s.SelectedScreen?.Index == idx))
                        s.SelectedScreen = null;
                }

                UpdateAvailableScreensForAllSlots();
            }
            finally
            {
                _isUpdatingMonitorSlots = false;
            }
        }

        private void UpdateAvailableScreensForAllSlots()
        {
            _isUpdatingMonitorSlots = true;
            try
            {
                var selected = MonitorSlots.Select(s => s.SelectedScreen?.Index).Where(i => i.HasValue).Select(i => i.Value).ToHashSet();

                foreach (var slot in MonitorSlots)
                {
                    var keep = slot.SelectedScreen?.Index;
                    var allowed = _allScreens.Where(s => !selected.Contains(s.Index) || (keep.HasValue && s.Index == keep.Value)).ToList();
                    slot.AvailableScreens = new ObservableCollection<AccesClientWPF.Models.ScreenItem>(allowed);

                    if (slot.SelectedScreen != null && !allowed.Any(a => a.Index == slot.SelectedScreen.Index))
                        slot.SelectedScreen = null;
                }
            }
            finally
            {
                _isUpdatingMonitorSlots = false;
            }
        }

        private bool TryGetSelectedMonitorsForRdp(out string selectedMonitors)
        {
            selectedMonitors = null;

            if (!IsMultiMonitor)
                return true;

            if (UseAllMonitors)
                return true;

            if (MonitorSlots.Count == 0)
                return false;

            if (MonitorSlots.Any(s => s.SelectedScreen == null))
                return false;

            var ids = MonitorSlots.Select(s => s.SelectedScreen.Index).ToList();
            if (ids.Distinct().Count() != ids.Count)
                return false;

            selectedMonitors = OrderSelectedMonitorsByWindowsLayout(ids, _allScreens);
            return true;
        }

        private static string OrderSelectedMonitorsByWindowsLayout(List<int> selectedIndexes, List<ScreenItem> allScreens)
        {
            // allScreens est déjà trié Windows (RefreshMonitors)
            var set = selectedIndexes.ToHashSet();
            var ordered = allScreens.Where(s => set.Contains(s.Index)).Select(s => s.Index.ToString());
            return string.Join(",", ordered);
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

        public ICommand EditPasswordCommand => new RelayCommand(p =>
        {
            if (SelectedClient == null || p is not FileModel item) return;

            var win = new AddEntryWindow(Clients, SelectedClient, item);
            try { win.SetTypeMotDePasse(); } catch { }

            if (win.ShowDialog() == true && win.FileEntry != null)
            {
                var updated = win.FileEntry;

                updated.Type = "MotDePasse";
                updated.Client = item.Client;

                if (string.IsNullOrWhiteSpace(updated.Name))
                    updated.Name = item.Name;

                if (string.IsNullOrWhiteSpace(updated.WindowsUsername))
                    updated.WindowsUsername = item.WindowsUsername;

                if (string.IsNullOrWhiteSpace(updated.WindowsPassword))
                {
                    updated.WindowsPassword = item.WindowsPassword;
                }
                else
                {
                    var testDecrypt = EncryptionHelper.Decrypt(updated.WindowsPassword);
                    if (string.IsNullOrEmpty(testDecrypt))
                        updated.WindowsPassword = EncryptionHelper.Encrypt(updated.WindowsPassword);
                }

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
            var others = db.Files.Where(f => !(f.Client == SelectedClient.Name && f.Type == "MotDePasse")).ToList();

            db.Files.Clear();
            foreach (var f in others) db.Files.Add(f);

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
            try { win.SetTypeMotDePasse(); } catch { }

            if (win.ShowDialog() == true && win.FileEntry != null)
            {
                win.FileEntry.Type = "MotDePasse";
                win.FileEntry.Client = SelectedClient.Name;

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

            var database = LoadDatabase();

            var all = database.Files
                .Where(f => f.Client == SelectedClient.Name)
                .ToList();

            PasswordEntries = new ObservableCollection<FileModel>(
                all.Where(f => f.Type == "MotDePasse")
            );

            var center = all.Where(f => f.Type != "MotDePasse").ToList();

            ReloadAvailableRangements();

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

            foreach (var file in center.Where(f => string.IsNullOrWhiteSpace(f.RangementParent)))
                RootFiles.Add(file);

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

        public void MoveToRacine(FileModel item)
        {
            if (SelectedClient == null || item == null) return;
            if (string.IsNullOrWhiteSpace(item.RangementParent)) return; // déjà racine

            var db = LoadDatabase();

            var existing = db.Files.FirstOrDefault(f =>
                string.Equals(f.Client, item.Client, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(f.Type, item.Type, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(f.Name, item.Name, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(f.RangementParent ?? "", item.RangementParent ?? "", StringComparison.OrdinalIgnoreCase));

            if (existing == null) return;

            existing.RangementParent = null;

            SaveDatabase(db);
            LoadFilesForSelectedClient();
        }

        public void MoveToRangement(FileModel item, string rangementName)
        {
            if (SelectedClient == null || item == null) return;
            if (string.IsNullOrWhiteSpace(rangementName)) return;

            // Interdit : un rangement ne va pas dans un rangement
            if (string.Equals(item.Type, "Rangement", StringComparison.OrdinalIgnoreCase))
                return;

            // Si déjà dans ce rangement : rien à faire
            if (string.Equals(item.RangementParent ?? "", rangementName, StringComparison.OrdinalIgnoreCase))
                return;

            var db = LoadDatabase();

            // ✅ crée le rangement "officiel" si absent
            EnsureRangementExists(db, rangementName);

            var existing = db.Files.FirstOrDefault(f =>
                string.Equals(f.Client, item.Client, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(f.Type, item.Type, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(f.Name, item.Name, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(f.RangementParent ?? "", item.RangementParent ?? "", StringComparison.OrdinalIgnoreCase));

            if (existing == null) return;

            existing.RangementParent = rangementName;

            SaveDatabase(db);
            LoadFilesForSelectedClient();
        }

        private FileModel GetSelectedCenterItem()
        {
            // En split :
            // - colonne gauche : SelectedRootItem
            // - colonne droite : SelectedFile
            // En mode classique : SelectedFile
            return SelectedFile ?? SelectedRootItem;
        }

        private void MoveSelected(int delta)
        {
            if (SelectedClient == null) return;

            var item = GetSelectedCenterItem();
            if (item == null) return;

            var db = LoadDatabase();

            var center = db.Files
                .Where(f => f.Client == SelectedClient.Name && f.Type != "MotDePasse")
                .ToList();

            bool inLevel2 = !string.IsNullOrWhiteSpace(item.RangementParent);

            var scope = center
                .Where(f => inLevel2
                    ? string.Equals(f.RangementParent ?? "", item.RangementParent ?? "", StringComparison.OrdinalIgnoreCase)
                    : string.IsNullOrWhiteSpace(f.RangementParent))
                .ToList();

            int idx = scope.FindIndex(f =>
                string.Equals(f.Name, item.Name, StringComparison.OrdinalIgnoreCase)
                && string.Equals(f.Type, item.Type, StringComparison.OrdinalIgnoreCase)
                && string.Equals(f.RangementParent ?? "", item.RangementParent ?? "", StringComparison.OrdinalIgnoreCase));

            if (idx < 0) return;

            int newIdx = idx + delta;
            if (newIdx < 0 || newIdx >= scope.Count) return;

            var moving = scope[idx];
            scope.RemoveAt(idx);
            scope.Insert(newIdx, moving);

            // Rebuild db.Files en conservant tout le reste
            var rebuilt = db.Files.ToList();

            rebuilt.RemoveAll(f =>
                string.Equals(f.Client, SelectedClient.Name, StringComparison.OrdinalIgnoreCase)
                && f.Type != "MotDePasse"
                && (inLevel2
                    ? string.Equals(f.RangementParent ?? "", item.RangementParent ?? "", StringComparison.OrdinalIgnoreCase)
                    : string.IsNullOrWhiteSpace(f.RangementParent)));

            rebuilt.AddRange(scope);

            db.Files = new ObservableCollection<FileModel>(rebuilt);
            SaveDatabase(db);

            LoadFilesForSelectedClient();

            // reselection
            if (HasLevel2)
            {
                if (inLevel2)
                    SelectedFile = Level2Files.FirstOrDefault(f =>
                        string.Equals(f.Name, item.Name, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(f.Type, item.Type, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(f.RangementParent ?? "", item.RangementParent ?? "", StringComparison.OrdinalIgnoreCase));
                else
                    SelectedRootItem = RootFiles.FirstOrDefault(f =>
                        string.Equals(f.Name, item.Name, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(f.Type, item.Type, StringComparison.OrdinalIgnoreCase)
                        && string.IsNullOrWhiteSpace(f.RangementParent));
            }
            else
            {
                SelectedFile = FilteredFiles.FirstOrDefault(f =>
                    string.Equals(f.Name, item.Name, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(f.Type, item.Type, StringComparison.OrdinalIgnoreCase));
            }
        }



        private void RefreshLevel2Files()
        {
            Level2Files.Clear();

            if (SelectedClient == null || string.IsNullOrWhiteSpace(SelectedRangementName))
            {
                OnPropertyChanged(nameof(Level2Files));
                return;
            }

            var database = LoadDatabase();

            foreach (var file in database.Files.Where(f =>
                         f.Client == SelectedClient.Name &&
                         f.Type != "MotDePasse" &&
                         f.RangementParent == SelectedRangementName))
            {
                Level2Files.Add(file);
            }

            OnPropertyChanged(nameof(Level2Files));
        }

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

        public void ShowCopyToast(string text) => ShowToast(text, isError: false);

        private void ShowToast(string text, bool isError)
        {
            CopyToastText = text;
            CopyToastBackground = isError ? "#E74C3C" : "#323232"; // rouge danger / gris
            IsCopyToastVisible = true;

            _copyToastTimer?.Stop();
            _copyToastTimer = new System.Timers.Timer(1500) { AutoReset = false };
            _copyToastTimer.Elapsed += (_, __) =>
            {
                Application.Current.Dispatcher.Invoke(() => IsCopyToastVisible = false);
            };
            _copyToastTimer.Start();
        }

        public void EditSelectedFile(FileModel file = null)
        {
            var target = file ?? SelectedFile ?? SelectedRootItem;
            if (target == null)
            {
                MessageBox.Show("Veuillez sélectionner un élément à modifier.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // ✅ RÈGLE : si le rangement contient des éléments => pas de modification (renommage interdit)
            if (string.Equals((target.Type ?? "").Trim(), "Rangement", StringComparison.OrdinalIgnoreCase))
            {
                var dbCheck = LoadDatabase();
                if (RangementHasChildren(target, dbCheck))
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

            // copie “clé” robuste
            var original = new FileModel
            {
                Name = target.Name,
                Client = target.Client,
                Type = target.Type,
                FullPath = target.FullPath,
                CustomIconPath = target.CustomIconPath,
                WindowsUsername = target.WindowsUsername,
                WindowsPassword = target.WindowsPassword,
                RangementParent = target.RangementParent
            };

            var editWindow = new AddEntryWindow(Clients, SelectedClient, target);
            if (editWindow.ShowDialog() == true && editWindow.FileEntry != null)
            {
                var db = LoadDatabase();

                // retrouver l’élément exact (inclut RangementParent)
                var itemToReplace = db.Files.FirstOrDefault(f =>
                    string.Equals(f.Client, original.Client, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(f.Type, original.Type, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(f.Name, original.Name, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(f.RangementParent ?? "", original.RangementParent ?? "", StringComparison.OrdinalIgnoreCase));

                if (itemToReplace == null) return;

                // check doublon NOM + TYPE + parent (sinon tu bloques trop)
                bool duplicateExists = db.Files.Any(f =>
                    f != itemToReplace &&
                    string.Equals(f.Client, editWindow.FileEntry.Client, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(f.Type, editWindow.FileEntry.Type, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(f.Name, editWindow.FileEntry.Name, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(f.RangementParent ?? "", editWindow.FileEntry.RangementParent ?? "", StringComparison.OrdinalIgnoreCase));

                if (duplicateExists)
                {
                    MessageBox.Show($"Un élément identique existe déjà (nom/type/rangement).",
                                    "Doublon", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int index = db.Files.IndexOf(itemToReplace);
                db.Files[index] = editWindow.FileEntry;

                SaveDatabase(db);
                LoadFilesForSelectedClient();
            }
        }

        public void HandleFileDoubleClick(FileModel file)
        {
            if (file == null) return;

            switch (file.Type)
            {
                case "Rangement":
                    SelectedRangementName = file.Name;
                    break;

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
                if (!TryGetSelectedMonitorsForRdp(out var selectedMonitors))
                {
                    MessageBox.Show("Sélection multi-moniteur incomplète. Choisissez un écran pour chaque liste.", "Multi-moniteur",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

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

                bool useMulti = IsMultiMonitor;
                string selectedMonitorsForRdp = selectedMonitors;

                if (useMulti)
                {
                    // ✅ Si "Tous les écrans" est coché => PAS DE TEST
                    if (!UseAllMonitors)
                    {
                        var screensToUse = _allScreens;

                        if (!string.IsNullOrWhiteSpace(selectedMonitorsForRdp))
                        {
                            var ids = selectedMonitorsForRdp
                                .Split(',')
                                .Select(x => int.TryParse(x, out var v) ? v : -1)
                                .Where(v => v >= 0)
                                .ToHashSet();

                            screensToUse = _allScreens.Where(s => ids.Contains(s.Index)).ToList();
                        }

                        if (!Helpers.MonitorHelper.AreSelectedScreensContiguousInWindowsOrder(_allScreens, screensToUse, out var reason))
                        {
                            useMulti = false;
                            selectedMonitorsForRdp = null;
                            ShowToast(reason, isError: true);
                        }
                    }
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
                    sw.WriteLine($"use multimon:i:{(useMulti ? "1" : "0")}");
                    if (useMulti && !UseAllMonitors && !string.IsNullOrWhiteSpace(selectedMonitorsForRdp))
                        sw.WriteLine($"selectedmonitors:s:{selectedMonitorsForRdp}");
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

        private string _copyToastBackground = "#323232";
        public string CopyToastBackground
        {
            get => _copyToastBackground;
            set { _copyToastBackground = value; OnPropertyChanged(nameof(CopyToastBackground)); }
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
            MoveSelected(-1);
        }

        public void MoveDown()
        {
            MoveSelected(+1);
        }
        

        private void SaveFiles()
        {
            var db = LoadDatabase();

            db.Files = new ObservableCollection<FileModel>(
                db.Files.Where(f => f.Client != SelectedClient?.Name)
            );

            foreach (var file in FilteredFiles)
                db.Files.Add(file);

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

        private void EnsureRangementExists(Models.DatabaseModel db, string rangementName)
        {
            if (SelectedClient == null) return;
            if (string.IsNullOrWhiteSpace(rangementName)) return;

            bool exists = db.Files.Any(f =>
                string.Equals(f.Client, SelectedClient.Name, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(f.Type, "Rangement", StringComparison.OrdinalIgnoreCase) &&
                string.IsNullOrWhiteSpace(f.RangementParent) &&
                string.Equals(f.Name, rangementName, StringComparison.OrdinalIgnoreCase));

            if (exists) return;

            // Création "officielle" à la racine
            db.Files.Add(new FileModel
            {
                Client = SelectedClient.Name,
                Type = "Rangement",
                Name = rangementName,
                FullPath = "",
                RangementParent = null
            });
        }

        public bool RangementHasChildren(FileModel rangement, Models.DatabaseModel db = null)
        {
            if (rangement == null) return false;
            if (!string.Equals((rangement.Type ?? "").Trim(), "Rangement", StringComparison.OrdinalIgnoreCase))
                return false;

            db ??= LoadDatabase();

            var client = (rangement.Client ?? SelectedClient?.Name ?? "").Trim();
            var name = (rangement.Name ?? "").Trim();

            if (string.IsNullOrWhiteSpace(client) || string.IsNullOrWhiteSpace(name))
                return false;

            return db.Files.Any(f =>
                string.Equals((f.Client ?? "").Trim(), client, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals((f.Type ?? "").Trim(), "MotDePasse", StringComparison.OrdinalIgnoreCase) &&
                string.Equals((f.RangementParent ?? "").Trim(), name, StringComparison.OrdinalIgnoreCase));
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
