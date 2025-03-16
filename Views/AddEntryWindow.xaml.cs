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

namespace AccesClientWPF.Views
{
    public partial class AddEntryWindow : Window
    {
        private readonly string _jsonFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "database.json");
        private ObservableCollection<FileModel> _files = new();
        private ObservableCollection<ClientModel> _clients;
        private FileModel _editingFile;

        public FileModel FileEntry { get; private set; }

        public AddEntryWindow(ObservableCollection<ClientModel> clients, ClientModel selectedClient = null, FileModel editingFile = null)
        {
            InitializeComponent();
            _clients = clients;
            CmbClient.ItemsSource = _clients;
            CmbClient.SelectedItem = selectedClient;
            CmbClient.IsEnabled = false;
            _editingFile = editingFile;
            LoadFiles();

            if (_editingFile != null)
            {
                TxtName.Text = _editingFile.Name;
                CmbType.SelectedItem = CmbType.Items.Cast<ComboBoxItem>().FirstOrDefault(item => item.Tag.ToString() == _editingFile.Type);

                var credentials = _editingFile.FullPath.Split(':');

                if (_editingFile.Type == "RDS")
                {
                    TxtIpDns.Text = credentials.ElementAtOrDefault(0);
                    TxtUsername.Text = credentials.ElementAtOrDefault(1);
                    TxtPassword.Password = string.Empty;
                }
                else if (_editingFile.Type == "AnyDesk")
                {
                    TxtAnydeskId.Text = credentials.ElementAtOrDefault(0);
                    TxtAnydeskPassword.Password = string.Empty;
                }
                else if (_editingFile.Type == "VPN")
                {
                    TxtVpnPath.Text = _editingFile.FullPath;
                }
            }
        }

        private void CmbType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedType = (CmbType.SelectedItem as ComboBoxItem)?.Tag.ToString();
            PanelRDS.Visibility = selectedType == "RDS" ? Visibility.Visible : Visibility.Collapsed;
            PanelAnyDesk.Visibility = selectedType == "AnyDesk" ? Visibility.Visible : Visibility.Collapsed;
            PanelVPN.Visibility = selectedType == "VPN" ? Visibility.Visible : Visibility.Collapsed;
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

            var selectedType = (CmbType.SelectedItem as ComboBoxItem)?.Tag.ToString();
            string fullPath = string.Empty;

            if (selectedType == "RDS")
            {
                string encryptedPassword = EncryptionHelper.Encrypt(TxtPassword.Password);
                fullPath = $"{TxtIpDns.Text}:{TxtUsername.Text}:{encryptedPassword}";
            }
            else if (selectedType == "AnyDesk")
            {
                string encryptedPassword = EncryptionHelper.Encrypt(TxtAnydeskPassword.Password);
                fullPath = $"{TxtAnydeskId.Text}:{encryptedPassword}";
            }
            else if (selectedType == "VPN")
            {
                fullPath = TxtVpnPath.Text;
            }

            var newEntry = new FileModel
            {
                Name = TxtName.Text,
                Type = selectedType,
                FullPath = fullPath,
                Client = ((ClientModel)CmbClient.SelectedItem).Name
            };

            if (_editingFile != null && _files.Contains(_editingFile))
            {
                var index = _files.IndexOf(_editingFile);
                if (index != -1)
                {
                    _files[index] = newEntry;
                }
            }
            else
            {
                _files.Add(newEntry);
            }

            SaveFiles();
            FileEntry = newEntry;
            DialogResult = true;
            Close();
        }

        private Models.DatabaseModel LoadDatabase()
        {
            if (File.Exists(_jsonFilePath))
            {
                var jsonData = File.ReadAllText(_jsonFilePath);
                return JsonConvert.DeserializeObject<Models.DatabaseModel>(jsonData) ?? new Models.DatabaseModel();
            }
            return new Models.DatabaseModel();
        }

        private void BrowseVpnExecutable_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                TxtVpnPath.Text = openFileDialog.FileName;
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