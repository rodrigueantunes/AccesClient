using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using AccesClientWPF.Models;
using Newtonsoft.Json;
using AccesClientWPF.Helpers;
using System.Windows.Media.Imaging;

namespace AccesClientWPF.Views
{
    public partial class AddEntryWindow : Window
    {
        private readonly string _jsonFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "database.json");

        private ObservableCollection<FileModel> _files = new();
        private ObservableCollection<ClientModel> _clients;
        private FileModel _editingFile;

        // ✅ NEW : mode "source injectée" (Share / .antclient)
        private readonly ObservableCollection<FileModel> _injectedFiles;
        private readonly bool _useInjectedFiles;

        public FileModel FileEntry { get; private set; }

        // "MotDePasse" si on vient du bouton orange (optionnel)
        public string PresetType { get; set; }

        public AddEntryWindow(
    ObservableCollection<ClientModel> clients,
    ClientModel selectedClient = null,
    FileModel editingFile = null,
    ObservableCollection<FileModel> injectedFiles = null) // ✅ NEW
        {
            InitializeComponent();

            _clients = clients ?? new ObservableCollection<ClientModel>();

            _injectedFiles = injectedFiles;
            _useInjectedFiles = injectedFiles != null;

            if (selectedClient == null && editingFile != null && !string.IsNullOrEmpty(editingFile.Client))
            {
                selectedClient = _clients.FirstOrDefault(c => c.Name == editingFile.Client);
            }

            CmbClient.ItemsSource = _clients;
            if (selectedClient != null)
                CmbClient.SelectedItem = _clients.FirstOrDefault(c => c.Name == selectedClient.Name);
            else if (_clients.Count > 0 && CmbClient.SelectedItem == null)
                CmbClient.SelectedIndex = 0;

            CmbClient.IsEnabled = selectedClient == null;

            _editingFile = editingFile;

            LoadFiles();

            // Charger liste rangements + refresh quand client change
            CmbClient.SelectionChanged += (_, __) => RefreshRangements();
            RefreshRangements();

            // Pré-remplissage en mode édition
            if (_editingFile != null)
            {
                TxtName.Text = _editingFile.Name ?? string.Empty;

                var typeItem = CmbType.Items.OfType<ComboBoxItem>()
                    .FirstOrDefault(i => string.Equals(i.Tag as string, _editingFile.Type, StringComparison.OrdinalIgnoreCase));
                if (typeItem != null)
                    CmbType.SelectedItem = typeItem;

                // Préselection rangement (si présent)
                if (!string.IsNullOrWhiteSpace(_editingFile.RangementParent))
                {
                    var match = CmbRangementParent.Items.Cast<object>()
                        .Select(x => x?.ToString())
                        .FirstOrDefault(x => string.Equals(x, _editingFile.RangementParent, StringComparison.OrdinalIgnoreCase));

                    if (match != null)
                        CmbRangementParent.SelectedItem = match;
                }
                else
                {
                    CmbRangementParent.SelectedIndex = 0; // racine
                }

                string[] credentials = Array.Empty<string>();
                if (!string.IsNullOrEmpty(_editingFile.FullPath))
                    credentials = _editingFile.FullPath.Split(':');

                if (string.Equals(_editingFile.Type, "RDS", StringComparison.OrdinalIgnoreCase))
                {
                    TxtIpDns.Text = credentials.ElementAtOrDefault(0) ?? string.Empty;
                    TxtUsername.Text = credentials.ElementAtOrDefault(1) ?? string.Empty;

                    // ✅ NEW : pré-remplir le mot de passe "normal" (3e partie)
                    var rawRdsPass = credentials.ElementAtOrDefault(2) ?? string.Empty;
                    TxtPassword.Password = GetDisplayedPassword(rawRdsPass);
                }
                else if (string.Equals(_editingFile.Type, "AnyDesk", StringComparison.OrdinalIgnoreCase))
                {
                    TxtAnydeskId.Text = credentials.ElementAtOrDefault(0) ?? string.Empty;

                    // ✅ NEW : pré-remplir password AnyDesk (2e partie du FullPath)
                    var anydeskRaw = credentials.ElementAtOrDefault(1) ?? string.Empty;
                    TxtAnydeskPassword.Password = GetDisplayedPassword(anydeskRaw);

                    // ✅ Windows
                    TxtWindowsUsername.Text = _editingFile.WindowsUsername ?? string.Empty;

                    // ✅ NEW : afficher comme les autres (decrypt / clair si ancien)
                    TxtWindowsPassword.Password = GetDisplayedPassword(_editingFile.WindowsPassword ?? "");
                }
                else if (string.Equals(_editingFile.Type, "VPN", StringComparison.OrdinalIgnoreCase))
                {
                    TxtVpnPath.Text = _editingFile.FullPath ?? string.Empty;
                }
                else if (string.Equals(_editingFile.Type, "Dossier", StringComparison.OrdinalIgnoreCase))
                {
                    TxtFolderPath.Text = _editingFile.FullPath ?? string.Empty;
                }
                else if (string.Equals(_editingFile.Type, "Fichier", StringComparison.OrdinalIgnoreCase))
                {
                    TxtFilePath.Text = _editingFile.FullPath ?? string.Empty;
                    TxtIconPath.Text = _editingFile.CustomIconPath ?? string.Empty;

                    if (!string.IsNullOrEmpty(_editingFile.CustomIconPath) && File.Exists(_editingFile.CustomIconPath))
                    {
                        try
                        {
                            BitmapImage bitmap = new BitmapImage(new Uri(_editingFile.CustomIconPath));
                            IconPreview.Source = bitmap;
                            IconPreview.Visibility = Visibility.Visible;
                        }
                        catch { }
                    }
                }
                else if (string.Equals(_editingFile.Type, "MotDePasse", StringComparison.OrdinalIgnoreCase))
                {
                    TxtPasswordUser.Text = _editingFile.WindowsUsername ?? string.Empty;

                    // ✅ NEW : afficher comme les autres (decrypt / clair si ancien)
                    TxtPasswordPass.Password = GetDisplayedPassword(_editingFile.WindowsPassword ?? "");
                }
                else if (string.Equals(_editingFile.Type, "Rangement", StringComparison.OrdinalIgnoreCase))
                {
                    // rien
                }

                CmbType_SelectionChanged(this, null);
            }

            if (!string.IsNullOrWhiteSpace(PresetType))
                SetTypeMotDePasse();
        }

        private void CmbType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedType = (CmbType.SelectedItem as ComboBoxItem)?.Tag as string;

            PanelRDS.Visibility = Visibility.Collapsed;
            PanelAnyDesk.Visibility = Visibility.Collapsed;
            PanelVPN.Visibility = Visibility.Collapsed;
            PanelFolder.Visibility = Visibility.Collapsed;
            PanelFile.Visibility = Visibility.Collapsed;
            PanelPassword.Visibility = Visibility.Collapsed;

            // Par défaut : on peut choisir Racine / Rangement
            CmbRangementParent.IsEnabled = true;

            if (string.Equals(selectedType, "RDS", StringComparison.OrdinalIgnoreCase))
                PanelRDS.Visibility = Visibility.Visible;
            else if (string.Equals(selectedType, "AnyDesk", StringComparison.OrdinalIgnoreCase))
                PanelAnyDesk.Visibility = Visibility.Visible;
            else if (string.Equals(selectedType, "VPN", StringComparison.OrdinalIgnoreCase))
                PanelVPN.Visibility = Visibility.Visible;
            else if (string.Equals(selectedType, "Dossier", StringComparison.OrdinalIgnoreCase))
                PanelFolder.Visibility = Visibility.Visible;
            else if (string.Equals(selectedType, "Fichier", StringComparison.OrdinalIgnoreCase))
                PanelFile.Visibility = Visibility.Visible;
            else if (string.Equals(selectedType, "MotDePasse", StringComparison.OrdinalIgnoreCase))
                PanelPassword.Visibility = Visibility.Visible;
            else if (string.Equals(selectedType, "Rangement", StringComparison.OrdinalIgnoreCase))
            {
                // Un rangement est toujours à la racine
                CmbRangementParent.SelectedIndex = 0;
                CmbRangementParent.IsEnabled = false;
            }
        }

        public void SetTypeMotDePasse()
        {
            var item = CmbType.Items.OfType<ComboBoxItem>()
                .FirstOrDefault(i => string.Equals(i.Tag as string, "MotDePasse", StringComparison.OrdinalIgnoreCase));
            if (item != null)
            {
                CmbType.SelectedItem = item;
                CmbType.IsEnabled = false;
                PanelPassword.Visibility = Visibility.Visible;
            }
        }

        private void LoadFiles()
        {
            // ✅ MODE SHARE : on ne lit JAMAIS database.json
            if (_useInjectedFiles)
            {
                _files = _injectedFiles ?? new ObservableCollection<FileModel>();
                return;
            }

            // MODE MAIN (comme avant)
            if (File.Exists(_jsonFilePath))
            {
                var jsonData = File.ReadAllText(_jsonFilePath);
                var database = JsonConvert.DeserializeObject<AccesClientWPF.Models.DatabaseModel>(jsonData);
                _files = database?.Files ?? new ObservableCollection<FileModel>();
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtName.Text) || CmbType.SelectedItem == null || CmbClient.SelectedItem == null)
            {
                MessageBox.Show("Veuillez remplir tous les champs obligatoires.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedType = (CmbType.SelectedItem as ComboBoxItem)?.Tag as string;
            if (string.IsNullOrWhiteSpace(selectedType))
            {
                MessageBox.Show("Type invalide.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var clientName = ((ClientModel)CmbClient.SelectedItem).Name;

            // --- RangementParent ---
            string rangementParent = null;
            var selectedParentText = CmbRangementParent.SelectedItem?.ToString();
            if (!string.IsNullOrWhiteSpace(selectedParentText) && selectedParentText != "(Racine)")
                rangementParent = selectedParentText;

            // Un rangement ne peut pas être dans un rangement
            if (string.Equals(selectedType, "Rangement", StringComparison.OrdinalIgnoreCase))
                rangementParent = null;

            // --- Unicité rangement (même client + même nom) ---
            if (string.Equals(selectedType, "Rangement", StringComparison.OrdinalIgnoreCase))
            {
                // ✅ IMPORTANT : en mode Share, on valide contre _files (= .antclient)
                var sourceFiles = _files ?? new ObservableCollection<FileModel>();

                bool isDuplicate = sourceFiles.Any(f =>
                    string.Equals(f.Client, clientName, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(f.Type, "Rangement", StringComparison.OrdinalIgnoreCase)
                    && string.IsNullOrWhiteSpace(f.RangementParent)
                    && string.Equals(f.Name?.Trim(), TxtName.Text.Trim(), StringComparison.OrdinalIgnoreCase)
                    && !ReferenceEquals(f, _editingFile));

                if (!isDuplicate && _editingFile != null &&
                    !string.Equals(_editingFile.Name?.Trim(), TxtName.Text.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    isDuplicate = sourceFiles.Any(f =>
                        string.Equals(f.Client, clientName, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(f.Type, "Rangement", StringComparison.OrdinalIgnoreCase)
                        && string.IsNullOrWhiteSpace(f.RangementParent)
                        && string.Equals(f.Name?.Trim(), TxtName.Text.Trim(), StringComparison.OrdinalIgnoreCase));
                }

                if (isDuplicate)
                {
                    MessageBox.Show("Ce rangement existe déjà pour ce client. Merci de choisir un nom unique.", "Doublon", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            // --- Construction entry ---
            string fullPath = string.Empty;
            string customIconPath = string.Empty;
            string windowsUsername = string.Empty;
            string windowsPassword = string.Empty;

            if (string.Equals(selectedType, "RDS", StringComparison.OrdinalIgnoreCase))
            {
                var ip = (TxtIpDns.Text ?? string.Empty).Trim();
                var user = (TxtUsername.Text ?? string.Empty).Trim();

                string encryptedPass;
                if (!string.IsNullOrEmpty(TxtPassword.Password))
                    encryptedPass = EncryptionHelper.Encrypt(TxtPassword.Password);
                else if (_editingFile != null && !string.IsNullOrEmpty(_editingFile.FullPath))
                {
                    var parts = _editingFile.FullPath.Split(':');
                    encryptedPass = parts.ElementAtOrDefault(2) ?? string.Empty;
                }
                else
                    encryptedPass = string.Empty;

                fullPath = $"{ip}:{user}:{encryptedPass}";
            }
            else if (string.Equals(selectedType, "AnyDesk", StringComparison.OrdinalIgnoreCase))
            {
                var id = (TxtAnydeskId.Text ?? string.Empty).Trim();

                string encryptedPass = string.Empty;
                if (!string.IsNullOrEmpty(TxtAnydeskPassword.Password))
                    encryptedPass = EncryptionHelper.Encrypt(TxtAnydeskPassword.Password);
                else if (_editingFile != null && !string.IsNullOrEmpty(_editingFile.FullPath))
                {
                    var parts = _editingFile.FullPath.Split(':');
                    encryptedPass = parts.ElementAtOrDefault(1) ?? string.Empty;
                }

                fullPath = $"{id}:{encryptedPass}";

                windowsUsername = (TxtWindowsUsername.Text ?? string.Empty).Trim();

                if (!string.IsNullOrEmpty(TxtWindowsPassword.Password))
                    windowsPassword = EncryptionHelper.Encrypt(TxtWindowsPassword.Password);
                else if (_editingFile != null && !string.IsNullOrEmpty(_editingFile.WindowsPassword))
                    windowsPassword = _editingFile.WindowsPassword;
            }
            else if (string.Equals(selectedType, "VPN", StringComparison.OrdinalIgnoreCase))
            {
                fullPath = TxtVpnPath.Text ?? string.Empty;
            }
            else if (string.Equals(selectedType, "Dossier", StringComparison.OrdinalIgnoreCase))
            {
                fullPath = TxtFolderPath.Text ?? string.Empty;
            }
            else if (string.Equals(selectedType, "Fichier", StringComparison.OrdinalIgnoreCase))
            {
                fullPath = TxtFilePath.Text ?? string.Empty;
                customIconPath = TxtIconPath.Text ?? string.Empty;
            }
            else if (string.Equals(selectedType, "MotDePasse", StringComparison.OrdinalIgnoreCase))
            {
                windowsUsername = (TxtPasswordUser.Text ?? string.Empty).Trim();

                if (!string.IsNullOrWhiteSpace(TxtPasswordPass.Password))
                    windowsPassword = EncryptionHelper.Encrypt(TxtPasswordPass.Password.Trim());
                else if (_editingFile != null && !string.IsNullOrEmpty(_editingFile.WindowsPassword))
                    windowsPassword = _editingFile.WindowsPassword;
                else
                    windowsPassword = string.Empty;
            }
            else if (string.Equals(selectedType, "Rangement", StringComparison.OrdinalIgnoreCase))
            {
                fullPath = string.Empty;
            }

            var newEntry = new FileModel
            {
                Name = TxtName.Text.Trim(),
                Type = selectedType,
                FullPath = fullPath,
                Client = clientName,
                CustomIconPath = customIconPath,
                WindowsUsername = windowsUsername,
                WindowsPassword = windowsPassword,
                RangementParent = rangementParent
            };

            FileEntry = newEntry;
            DialogResult = true;
            Close();
        }

        private void BrowseVpnExecutable_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
                TxtVpnPath.Text = openFileDialog.FileName;
        }

        private void BrowseFile_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Sélectionnez un fichier",
                Filter = "Tous les fichiers (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
                TxtFilePath.Text = openFileDialog.FileName;
        }

        private void BrowseIcon_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Sélectionnez une icône",
                Filter = "Fichiers image (*.png;*.jpg;*.jpeg;*.bmp;*.gif)|*.png;*.jpg;*.jpeg;*.bmp;*.gif"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                TxtIconPath.Text = openFileDialog.FileName;

                try
                {
                    BitmapImage bitmap = new BitmapImage(new Uri(openFileDialog.FileName));
                    IconPreview.Source = bitmap;
                    IconPreview.Visibility = Visibility.Visible;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors du chargement de l'image : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BrowseFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Sélectionnez un dossier",
                CheckFileExists = false,
                CheckPathExists = true,
                FileName = "Sélectionner ce dossier",
                Filter = "Dossiers|*.none",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };

            if (dialog.ShowDialog() == true)
            {
                string folderPath = System.IO.Path.GetDirectoryName(dialog.FileName);
                TxtFolderPath.Text = folderPath;
            }
        }

        private static string GetDisplayedPassword(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return string.Empty;

            var dec = EncryptionHelper.Decrypt(raw);
            if (!string.IsNullOrEmpty(dec))
                return dec;

            // Anciennes données en clair => on affiche tel quel
            // Si ça ressemble à du Base64 mais pas déchiffrable => on affiche vide
            // (et au Save, ton code conserve l’ancien chiffré si champ vide)
            return IsBase64(raw) ? string.Empty : raw;
        }

        private static bool IsBase64(string s)
        {
            if (string.IsNullOrWhiteSpace(s) || s.Length % 4 != 0)
                return false;

            byte[] buffer = new byte[s.Length];
            return Convert.TryFromBase64String(s, buffer, out _);
        }

        private void RefreshRangements()
        {
            CmbRangementParent.Items.Clear();
            CmbRangementParent.Items.Add("(Racine)");

            if (CmbClient.SelectedItem is not ClientModel client)
            {
                CmbRangementParent.SelectedIndex = 0;
                return;
            }

            // ✅ IMPORTANT : la source des rangements dépend du mode
            // - Share : _files = .antclient injecté
            // - Main  : _files = database.json chargé
            var sourceFiles = _files ?? new ObservableCollection<FileModel>();

            var rangements = sourceFiles
                .Where(f => string.Equals(f.Client, client.Name, StringComparison.OrdinalIgnoreCase)
                            && string.Equals(f.Type, "Rangement", StringComparison.OrdinalIgnoreCase)
                            && string.IsNullOrWhiteSpace(f.RangementParent))
                .Select(f => f.Name)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase);

            foreach (var r in rangements)
                CmbRangementParent.Items.Add(r);

            CmbRangementParent.SelectedIndex = 0;
        }

        private void ShowExistingElements_Click(object sender, RoutedEventArgs e)
        {
            if (CmbClient.SelectedItem is not ClientModel client)
            {
                MessageBox.Show("Choisis d’abord un client.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // ✅ IMPORTANT :
            // _files = soit database.json (main), soit .antclient injecté (share)
            var w = new ExistingElementsWindow(client, _files)
            {
                Owner = this
            };
            w.ShowDialog();

            RefreshRangements();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}