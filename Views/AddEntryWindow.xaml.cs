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
using System.Windows.Controls.Primitives;

namespace AccesClientWPF.Views
{
    public partial class AddEntryWindow : Window
    {
        private readonly string _jsonFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "database.json");
        private ObservableCollection<FileModel> _files = new();
        private ObservableCollection<ClientModel> _clients;
        private FileModel _editingFile;

        public FileModel FileEntry { get; private set; }

        // "MotDePasse" si on vient du bouton orange (optionnel)
        public string PresetType { get; set; }

        public AddEntryWindow(ObservableCollection<ClientModel> clients, ClientModel selectedClient = null, FileModel editingFile = null)
        {
            InitializeComponent();

            // Défenses anti-null
            _clients = clients ?? new ObservableCollection<ClientModel>();

            // Si pas de selectedClient mais on édite un élément → le déduire par nom
            if (selectedClient == null && editingFile != null && !string.IsNullOrEmpty(editingFile.Client))
            {
                selectedClient = _clients.FirstOrDefault(c => c.Name == editingFile.Client);
            }

            CmbClient.ItemsSource = _clients;
            if (selectedClient != null)
                CmbClient.SelectedItem = _clients.FirstOrDefault(c => c.Name == selectedClient.Name);
            else if (_clients.Count > 0 && CmbClient.SelectedItem == null)
                CmbClient.SelectedIndex = 0;

            // Si on a reçu un selectedClient, on verrouille le choix (comportement d'origine)
            CmbClient.IsEnabled = selectedClient == null;

            _editingFile = editingFile;
            LoadFiles();

            // Pré-remplissage en mode édition
            if (_editingFile != null)
            {
                TxtName.Text = _editingFile.Name ?? string.Empty;

                // Sélection du type en sécurité (Tag peut être null)
                var typeItem = CmbType.Items.OfType<ComboBoxItem>()
                                    .FirstOrDefault(i => string.Equals(i.Tag as string, _editingFile.Type, StringComparison.OrdinalIgnoreCase));
                if (typeItem != null)
                {
                    CmbType.SelectedItem = typeItem;
                }
                else if (!string.IsNullOrEmpty(_editingFile.Type))
                {
                    // fallback : sélectionne rien si type inconnu, et les panneaux resteront cachés
                }

                // Pour éviter le crash : on ne split que si FullPath est non vide
                string[] credentials = Array.Empty<string>();
                if (!string.IsNullOrEmpty(_editingFile.FullPath))
                    credentials = _editingFile.FullPath.Split(':');

                // Remplissage selon le type
                if (string.Equals(_editingFile.Type, "RDS", StringComparison.OrdinalIgnoreCase))
                {
                    TxtIpDns.Text = credentials.ElementAtOrDefault(0) ?? string.Empty;
                    TxtUsername.Text = credentials.ElementAtOrDefault(1) ?? string.Empty;
                    // volontairement vide en édition pour que l'utilisateur resaisisse si besoin
                    TxtPassword.Password = string.Empty;
                }
                else if (string.Equals(_editingFile.Type, "AnyDesk", StringComparison.OrdinalIgnoreCase))
                {
                    TxtAnydeskId.Text = credentials.ElementAtOrDefault(0) ?? string.Empty;
                    // On laisse vide pour ne pas écraser si l'utilisateur ne retape rien
                    TxtAnydeskPassword.Password = string.Empty;

                    // Champs Windows
                    TxtWindowsUsername.Text = _editingFile.WindowsUsername ?? string.Empty;
                    if (!string.IsNullOrEmpty(_editingFile.WindowsPassword))
                    {
                        var dec = EncryptionHelper.Decrypt(_editingFile.WindowsPassword);
                        if (!string.IsNullOrEmpty(dec))
                            TxtWindowsPassword.Password = dec;
                        else
                            TxtWindowsPassword.Password = string.Empty;
                    }
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
                        catch { /* pas grave */ }
                    }
                }
                else if (string.Equals(_editingFile.Type, "MotDePasse", StringComparison.OrdinalIgnoreCase))
                {
                    // Forcer la sélection / verrouiller le type
                    SetTypeMotDePasse();

                    // Remplir les champs
                    TxtPasswordUser.Text = _editingFile.WindowsUsername ?? string.Empty;

                    if (!string.IsNullOrEmpty(_editingFile.WindowsPassword))
                    {
                        var dec = EncryptionHelper.Decrypt(_editingFile.WindowsPassword);
                        TxtPasswordPass.Password = dec ?? string.Empty;
                    }
                    else
                    {
                        TxtPasswordPass.Password = string.Empty;
                    }
                }
            }
            else
            {
                // Nouveau : si on a un type imposé ("MotDePasse" côté droit), l'appliquer
                if (string.Equals(PresetType, "MotDePasse", StringComparison.OrdinalIgnoreCase))
                    SetTypeMotDePasse();
                else if (CmbType.Items.Count > 0 && CmbType.SelectedIndex < 0)
                    CmbType.SelectedIndex = 0;
            }
        }

        private void CmbType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedType = (CmbType.SelectedItem as ComboBoxItem)?.Tag as string;

            PanelRDS.Visibility = string.Equals(selectedType, "RDS", StringComparison.OrdinalIgnoreCase) ? Visibility.Visible : Visibility.Collapsed;
            PanelAnyDesk.Visibility = string.Equals(selectedType, "AnyDesk", StringComparison.OrdinalIgnoreCase) ? Visibility.Visible : Visibility.Collapsed;
            PanelVPN.Visibility = string.Equals(selectedType, "VPN", StringComparison.OrdinalIgnoreCase) ? Visibility.Visible : Visibility.Collapsed;
            PanelFolder.Visibility = string.Equals(selectedType, "Dossier", StringComparison.OrdinalIgnoreCase) ? Visibility.Visible : Visibility.Collapsed;
            PanelFile.Visibility = string.Equals(selectedType, "Fichier", StringComparison.OrdinalIgnoreCase) ? Visibility.Visible : Visibility.Collapsed;
            PanelPassword.Visibility = string.Equals(selectedType, "MotDePasse", StringComparison.OrdinalIgnoreCase) ? Visibility.Visible : Visibility.Collapsed;
        }

        public void SetTypeMotDePasse()
        {
            var item = CmbType.Items.OfType<ComboBoxItem>().FirstOrDefault(i => string.Equals(i.Tag as string, "MotDePasse", StringComparison.OrdinalIgnoreCase));
            if (item != null)
            {
                CmbType.SelectedItem = item;
                CmbType.IsEnabled = false;
                PanelPassword.Visibility = Visibility.Visible; // sécurité
            }
        }

        private void LoadFiles()
        {
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
                MessageBox.Show("Veuillez remplir tous les champs.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var selectedType = (CmbType.SelectedItem as ComboBoxItem)?.Tag as string ?? string.Empty;
            string fullPath = string.Empty;
            string customIconPath = string.Empty;

            // Ces deux-là servent pour AnyDesk / MotDePasse (et potentiellement autres)
            string windowsUsername = string.Empty;
            string windowsPassword = string.Empty;

            if (string.Equals(selectedType, "RDS", StringComparison.OrdinalIgnoreCase))
            {
                string encryptedPassword = EncryptionHelper.Encrypt(TxtPassword.Password ?? string.Empty);
                fullPath = $"{TxtIpDns.Text}:{TxtUsername.Text}:{encryptedPassword}";
            }
            else if (string.Equals(selectedType, "AnyDesk", StringComparison.OrdinalIgnoreCase))
            {
                string encryptedPassword = EncryptionHelper.Encrypt(TxtAnydeskPassword.Password ?? string.Empty);
                fullPath = $"{TxtAnydeskId.Text}:{encryptedPassword}";

                windowsUsername = TxtWindowsUsername.Text ?? string.Empty;

                // Si le champ est vide et qu'on édite, conserver l'ancien chiffré
                if (!string.IsNullOrEmpty(TxtWindowsPassword.Password))
                {
                    windowsPassword = EncryptionHelper.Encrypt(TxtWindowsPassword.Password);
                }
                else if (_editingFile != null && !string.IsNullOrEmpty(_editingFile.WindowsPassword))
                {
                    windowsPassword = _editingFile.WindowsPassword;
                }
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
                // Pas de FullPath pour ce type
                windowsUsername = (TxtPasswordUser.Text ?? string.Empty).Trim();

                // Si on saisit un mot de passe → chiffrer
                if (!string.IsNullOrWhiteSpace(TxtPasswordPass.Password))
                {
                    windowsPassword = EncryptionHelper.Encrypt(TxtPasswordPass.Password.Trim());
                }
                // Sinon, si on édite et il y en a déjà un, on le conserve
                else if (_editingFile != null && !string.IsNullOrEmpty(_editingFile.WindowsPassword))
                {
                    windowsPassword = _editingFile.WindowsPassword;
                }
                else
                {
                    windowsPassword = string.Empty;
                }
            }

            var newEntry = new FileModel
            {
                Name = TxtName.Text,
                Type = selectedType,
                FullPath = fullPath,
                Client = ((ClientModel)CmbClient.SelectedItem).Name,
                CustomIconPath = customIconPath,
                WindowsUsername = windowsUsername,
                WindowsPassword = windowsPassword
            };

            FileEntry = newEntry;

            // ⚠️ NE PAS réécraser ici pour MotDePasse : on a déjà géré la conservation de l'ancien si champ vide.
            // (Ton ancien code ré-encryptait systématiquement le PasswordBox, effaçant l'ancien si vide.)

            DialogResult = true;
            Close();
        }

        private AccesClientWPF.Models.DatabaseModel LoadDatabase()
        {
            if (File.Exists(_jsonFilePath))
            {
                var jsonData = File.ReadAllText(_jsonFilePath);
                return JsonConvert.DeserializeObject<AccesClientWPF.Models.DatabaseModel>(jsonData) ?? new AccesClientWPF.Models.DatabaseModel();
            }
            return new AccesClientWPF.Models.DatabaseModel();
        }

        private void BrowseVpnExecutable_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                TxtVpnPath.Text = openFileDialog.FileName;
            }
        }

        private void BrowseFile_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Sélectionnez un fichier",
                Filter = "Tous les fichiers (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                TxtFilePath.Text = openFileDialog.FileName;
            }
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

                // Aperçu
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

        private void SaveFiles()
        {
            AccesClientWPF.Models.DatabaseModel existingDatabase;

            if (File.Exists(_jsonFilePath))
            {
                var jsonData = File.ReadAllText(_jsonFilePath);
                existingDatabase = JsonConvert.DeserializeObject<AccesClientWPF.Models.DatabaseModel>(jsonData) ?? new AccesClientWPF.Models.DatabaseModel();
            }
            else
            {
                existingDatabase = new AccesClientWPF.Models.DatabaseModel();
            }

            foreach (var client in _clients)
            {
                if (!existingDatabase.Clients.Any(c => c.Name == client.Name))
                {
                    existingDatabase.Clients.Add(client);
                }
            }

            foreach (var file in _files)
            {
                var existingFile = existingDatabase.Files.FirstOrDefault(f => f.Name == file.Name && f.Client == file.Client);
                if (existingFile != null)
                {
                    existingDatabase.Files[existingDatabase.Files.IndexOf(existingFile)] = file;
                }
                else
                {
                    existingDatabase.Files.Add(file);
                }
            }

            File.WriteAllText(_jsonFilePath, JsonConvert.SerializeObject(existingDatabase, Formatting.Indented));
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
