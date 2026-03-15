using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AccesClientWPF.Models;
using Newtonsoft.Json;

namespace AccesClientWPF.Views
{
    public partial class ClientManagementWindow : Window
    {
        private ObservableCollection<ClientModel> _clients;
        private ObservableCollection<FileModel> _files;
        private readonly string _jsonFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "database.json");

        private const string BaseFolderPath = @"\\172.16.0.49\Docs\Fiches de fin de développement";

        private bool _isAscendingSort = true;

        public ClientManagementWindow(ObservableCollection<ClientModel> clients)
        {
            InitializeComponent();
            _clients = clients ?? new ObservableCollection<ClientModel>();
            LstClients.ItemsSource = _clients;
            LoadFiles();
        }

        private void SortClients_Click(object sender, RoutedEventArgs e)
        {
            _isAscendingSort = !_isAscendingSort;

            BtnSort.ToolTip = _isAscendingSort ? "Trier A-Z" : "Trier Z-A";

            if (_isAscendingSort)
                SortIcon.Data = Geometry.Parse("M3,13H15V11H3M3,6V8H21V6M3,18H9V16H3V18Z");
            else
                SortIcon.Data = Geometry.Parse("M3,11H15V13H3M3,18V16H21V18M3,6H9V8H3V6Z");

            SortClientsList();
        }

        private void SortClientsList()
        {
            var selectedClient = LstClients.SelectedItem as ClientModel;
            var sortedList = new List<ClientModel>(_clients);

            if (_isAscendingSort)
                sortedList.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase));
            else
                sortedList.Sort((x, y) => string.Compare(y.Name, x.Name, StringComparison.OrdinalIgnoreCase));

            _clients.Clear();
            foreach (var client in sortedList)
                _clients.Add(client);

            if (selectedClient != null)
                LstClients.SelectedItem = _clients.FirstOrDefault(c => c.Name == selectedClient.Name);

            SaveData();
        }

        private void LoadFiles()
        {
            try
            {
                if (File.Exists(_jsonFilePath))
                {
                    var jsonData = File.ReadAllText(_jsonFilePath);
                    var database = JsonConvert.DeserializeObject<DatabaseModel>(jsonData);
                    _files = new ObservableCollection<FileModel>(
                        database?.Files?.Where(f => f != null && !string.IsNullOrWhiteSpace(f.Name))
                        ?? new ObservableCollection<FileModel>());
                }
                else
                {
                    _files = new ObservableCollection<FileModel>();
                }
            }
            catch (Exception ex)
            {
                _files = new ObservableCollection<FileModel>();
                MessageBox.Show($"Erreur lors du chargement des données : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveData()
        {
            try
            {
                var database = new DatabaseModel
                {
                    Clients = _clients,
                    Files = _files
                };
                File.WriteAllText(_jsonFilePath, JsonConvert.SerializeObject(database, Formatting.Indented));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la sauvegarde des données : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddClient_Click(object sender, RoutedEventArgs e)
        {
            string clientName = TxtClientName.Text.Trim();
            string acronym = TxtAcronym.Text.Trim().ToUpper();

            if (string.IsNullOrWhiteSpace(clientName))
            {
                MessageBox.Show("Veuillez saisir un nom de client valide.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (acronym.Length > 3)
            {
                MessageBox.Show("L'acronyme ne doit pas dépasser 3 caractères.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (_clients.Any(c => c.Name.Equals(clientName, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("Un client avec ce nom existe déjà.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _clients.Add(new ClientModel { Name = clientName, Acronym = acronym });
            TxtClientName.Clear();
            TxtAcronym.Clear();
            SaveData();
        }

        private void OpenClientFolder_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not string acronym || string.IsNullOrWhiteSpace(acronym))
            {
                MessageBox.Show("Aucun acronyme défini pour ce client.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                if (!Directory.Exists(BaseFolderPath))
                {
                    MessageBox.Show($"Le chemin réseau '{BaseFolderPath}' est inaccessible.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var matchingDir = Directory.GetDirectories(BaseFolderPath)
                    .Select(d => new DirectoryInfo(d))
                    .FirstOrDefault(d =>
                    {
                        var name = d.Name;
                        var parenStart = name.LastIndexOf('(');
                        var parenEnd = name.LastIndexOf(')');
                        if (parenStart < 0 || parenEnd <= parenStart) return false;
                        var code = name.Substring(parenStart + 1, parenEnd - parenStart - 1).Trim();
                        return code.Equals(acronym, StringComparison.OrdinalIgnoreCase);
                    });

                if (matchingDir != null)
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = $"\"{matchingDir.FullName}\"",
                        UseShellExecute = true
                    });
                }
                else
                {
                    MessageBox.Show($"Aucun dossier trouvé pour l'acronyme '{acronym}'.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ouverture du dossier : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddEntry_Click(object sender, RoutedEventArgs e)
        {
            if (LstClients.SelectedItem is ClientModel selectedClient)
            {
                var addEntryWindow = new AddEntryWindow(_clients, selectedClient);
                if (addEntryWindow.ShowDialog() == true && addEntryWindow.FileEntry != null)
                {
                    _files.Add(addEntryWindow.FileEntry);
                    SaveData();
                }
            }
            else
            {
                MessageBox.Show("Veuillez sélectionner un client avant d'ajouter un élément.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ViewElements_Click(object sender, RoutedEventArgs e)
        {
            if (LstClients.SelectedItem is ClientModel selectedClient)
            {
                var elementsWindow = new ExistingElementsWindow(selectedClient);
                elementsWindow.ShowDialog();
            }
            else
            {
                MessageBox.Show("Veuillez sélectionner un client avant de voir ses éléments.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteClient_Click(object sender, RoutedEventArgs e)
        {
            if (LstClients.SelectedItem is ClientModel client)
            {
                var clientFiles = _files?
                    .Where(f => f.Client != null && f.Client.Equals(client.Name, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (clientFiles != null)
                    foreach (var file in clientFiles)
                        _files.Remove(file);

                _clients.Remove(client);
                SaveData();
            }
            else
            {
                MessageBox.Show("Veuillez sélectionner un client à supprimer.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditClient_Click(object sender, RoutedEventArgs e)
        {
            if (LstClients.SelectedItem is ClientModel selectedClient)
            {
                var editWindow = new EditClientWindow(selectedClient, _clients);
                if (editWindow.ShowDialog() == true)
                {
                    SaveData();
                }
            }
            else
            {
                MessageBox.Show("Veuillez sélectionner un client à modifier.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void MoveUpClient_Click(object sender, RoutedEventArgs e)
        {
            if (LstClients.SelectedItem is ClientModel selectedClient)
            {
                int index = _clients.IndexOf(selectedClient);
                if (index > 0)
                {
                    _clients.Move(index, index - 1);
                    LstClients.SelectedItem = selectedClient;
                    SaveData();
                }
            }
            else
            {
                MessageBox.Show("Veuillez sélectionner un client à déplacer.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void MoveDownClient_Click(object sender, RoutedEventArgs e)
        {
            if (LstClients.SelectedItem is ClientModel selectedClient)
            {
                int index = _clients.IndexOf(selectedClient);
                if (index < _clients.Count - 1)
                {
                    _clients.Move(index, index + 1);
                    LstClients.SelectedItem = selectedClient;
                    SaveData();
                }
            }
            else
            {
                MessageBox.Show("Veuillez sélectionner un client à déplacer.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}
