using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using AccesClientWPF.Models;
using Newtonsoft.Json;
using AccesClientWPF.Helpers;

namespace AccesClientWPF.Views
{
    public partial class ExistingElementsWindow : Window
    {
        private readonly string _jsonFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "database.json");
        private ObservableCollection<FileModel> _files = new();
        private readonly ClientModel _client;

        public ExistingElementsWindow(ClientModel client)
        {
            InitializeComponent();
            _client = client;
            TxtClientName.Text = client.Name;
            LoadElements();
        }

        private void LoadElements()
        {
            if (File.Exists(_jsonFilePath))
            {
                var jsonData = File.ReadAllText(_jsonFilePath);
                var database = JsonConvert.DeserializeObject<AccesClientWPF.Models.DatabaseModel>(jsonData)
                               ?? new AccesClientWPF.Models.DatabaseModel();

                var clientFiles = database.Files
                    .Where(f => f.Client == _client.Name)
                    .ToList();

                _files.Clear();
                foreach (var file in clientFiles)
                {
                    _files.Add(file);
                }

                LstElements.ItemsSource = _files;
            }
        }

        private void SaveElements()
        {
            var database = File.Exists(_jsonFilePath)
                ? JsonConvert.DeserializeObject<AccesClientWPF.Models.DatabaseModel>(File.ReadAllText(_jsonFilePath))
                : new AccesClientWPF.Models.DatabaseModel();

            database.Files = new ObservableCollection<FileModel>(database.Files.Where(f => f.Client != _client.Name));

            foreach (var file in _files)
                database.Files.Add(file);

            File.WriteAllText(_jsonFilePath, JsonConvert.SerializeObject(database, Formatting.Indented));
        }

        private void AddElement_Click(object sender, RoutedEventArgs e)
        {
            var addEntryWindow = new AddEntryWindow(new ObservableCollection<ClientModel> { _client }, _client);
            if (addEntryWindow.ShowDialog() == true && addEntryWindow.FileEntry != null)
            {
                _files.Add(addEntryWindow.FileEntry);
                SaveElements();
                LoadElements();
            }
        }

        private void EditElement_Click(object sender, RoutedEventArgs e)
        {
            if (LstElements.SelectedItem is FileModel selectedFile)
            {
                var editFile = PrepareEditingFile(selectedFile);
                var editWindow = new AddEntryWindow(new ObservableCollection<ClientModel> { _client }, _client, editFile);
                if (editWindow.ShowDialog() == true && editWindow.FileEntry != null)
                {
                    var index = _files.IndexOf(selectedFile);
                    if (index != -1)
                    {
                        _files[index] = editWindow.FileEntry;
                    }

                    SaveElements();
                    LoadElements();
                }
            }
            else
            {
                MessageBox.Show("Veuillez sélectionner un élément à modifier.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private FileModel PrepareEditingFile(FileModel file)
        {
            var credentials = file.FullPath.Split(':');
            string fullPath = string.Empty;

            if (file.Type == "RDS" && credentials.Length >= 2)
            {
                fullPath = $"{credentials[0]}:{credentials[1]}:"; // Mot de passe vide
            }
            else if (file.Type == "AnyDesk" && credentials.Length >= 1)
            {
                fullPath = $"{credentials[0]}:"; // Mot de passe vide
            }
            else
            {
                fullPath = file.FullPath;
            }

            return new FileModel
            {
                Name = file.Name,
                Type = file.Type,
                FullPath = fullPath,
                Client = file.Client,
                CustomIconPath = file.CustomIconPath,
                WindowsUsername = file.WindowsUsername,
                WindowsPassword = file.WindowsPassword
            };
        }

        private void DeleteElement_Click(object sender, RoutedEventArgs e)
        {
            if (LstElements.SelectedItem is FileModel selectedFile)
            {
                _files.Remove(selectedFile);
                SaveElements();
                LoadElements();
            }
            else
            {
                MessageBox.Show("Veuillez sélectionner un élément à supprimer.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
