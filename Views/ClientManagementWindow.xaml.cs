using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
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

        // Variable pour le tri
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
            // Inverser le mode de tri
            _isAscendingSort = !_isAscendingSort;

            // Mettre à jour l'apparence du bouton
            if (_isAscendingSort)
            {
                TxtSortLabel.Text = "Trier A-Z";
                // Flèche vers le bas
                SortIcon.Data = Geometry.Parse("M7,21L12,17L17,21V3H7V21Z");
            }
            else
            {
                TxtSortLabel.Text = "Trier Z-A";
                // Flèche vers le haut
                SortIcon.Data = Geometry.Parse("M7,3L12,7L17,3V21H7V3Z");
            }

            // Effectuer le tri
            SortClientsList();
        }

        private void SortClientsList()
        {
            var selectedClient = LstClients.SelectedItem as ClientModel;

            // Créer une liste temporaire pour le tri
            var sortedList = new List<ClientModel>(_clients);

            if (_isAscendingSort)
            {
                // Tri ascendant (A-Z)
                sortedList.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                // Tri descendant (Z-A)
                sortedList.Sort((x, y) => string.Compare(y.Name, x.Name, StringComparison.OrdinalIgnoreCase));
            }

            // Vider et remplir la collection avec les éléments triés
            _clients.Clear();
            foreach (var client in sortedList)
            {
                _clients.Add(client);
            }

            // Restaurer la sélection si possible
            if (selectedClient != null)
            {
                LstClients.SelectedItem = _clients.FirstOrDefault(c => c.Name == selectedClient.Name);
            }

            // Sauvegarder les changements
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

                    // Nettoyage des éléments null et chargement des fichiers
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

            if (!string.IsNullOrWhiteSpace(clientName))
            {
                if (!_clients.Any(c => c.Name.Equals(clientName, StringComparison.OrdinalIgnoreCase)))
                {
                    _clients.Add(new ClientModel { Name = clientName });
                    TxtClientName.Clear();
                    SaveData();
                }
                else
                {
                    MessageBox.Show("Un client avec ce nom existe déjà.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Veuillez saisir un nom de client valide.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
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
                {
                    foreach (var file in clientFiles)
                    {
                        _files.Remove(file);
                    }
                }

                _clients.Remove(client);
                SaveData();
            }
            else
            {
                MessageBox.Show("Veuillez sélectionner un client à supprimer.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    LstClients.SelectedItem = selectedClient; // Garder la sélection active
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
                    LstClients.SelectedItem = selectedClient; // Garder la sélection active
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