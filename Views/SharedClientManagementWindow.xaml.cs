using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AccesClientWPF.Models;
using AccesClientWPF.ViewModels;

namespace AccesClientWPF.Views
{
    public partial class SharedClientManagementWindow : Window
    {
        private SharedDatabaseViewModel _viewModel;
        private bool _isAscendingSort = true;

        public SharedClientManagementWindow(SharedDatabaseViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            LstClients.ItemsSource = _viewModel.Clients;
        }


        private void AddClient_Click(object sender, RoutedEventArgs e)
        {
            string clientName = TxtClientName.Text.Trim();

            if (!string.IsNullOrWhiteSpace(clientName))
            {
                if (!_viewModel.Clients.Any(c => c.Name.Equals(clientName, StringComparison.OrdinalIgnoreCase)))
                {
                    _viewModel.AddClient(clientName);
                    TxtClientName.Clear();
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

        private void MoveUpClient_Click(object sender, RoutedEventArgs e)
        {
            if (LstClients.SelectedItem is ClientModel selectedClient)
            {
                _viewModel.MoveClientUp(selectedClient);
                LstClients.SelectedItem = selectedClient; // Garder la sélection active
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
                _viewModel.MoveClientDown(selectedClient);
                LstClients.SelectedItem = selectedClient; // Garder la sélection active
            }
            else
            {
                MessageBox.Show("Veuillez sélectionner un client à déplacer.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
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
            var sortedList = new List<ClientModel>(_viewModel.Clients);

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
            _viewModel.Clients.Clear();
            foreach (var client in sortedList)
            {
                _viewModel.Clients.Add(client);
            }

            // Restaurer la sélection si possible
            if (selectedClient != null)
            {
                LstClients.SelectedItem = _viewModel.Clients.FirstOrDefault(c => c.Name == selectedClient.Name);
            }
        }

        private void AddEntry_Click(object sender, RoutedEventArgs e)
        {
            if (LstClients.SelectedItem is ClientModel selectedClient)
            {
                var addEntryWindow = new AddEntryWindow(_viewModel.Clients, selectedClient);
                if (addEntryWindow.ShowDialog() == true && addEntryWindow.FileEntry != null)
                {
                    _viewModel.AllFiles.Add(addEntryWindow.FileEntry);

                    // Si le client sélectionné dans cette fenêtre est le même que celui du ViewModel parent,
                    // on recharge également les fichiers filtrés
                    if (_viewModel.SelectedClient != null && _viewModel.SelectedClient.Name == selectedClient.Name)
                    {
                        _viewModel.SelectedClient = null; // Forcer la réinitialisation
                        _viewModel.SelectedClient = selectedClient;
                    }
                }
            }
            else
            {
                MessageBox.Show("Veuillez sélectionner un client avant d'ajouter un élément.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void DeleteClient_Click(object sender, RoutedEventArgs e)
        {
            if (LstClients.SelectedItem is ClientModel client)
            {
                MessageBoxResult result = MessageBox.Show(
                    $"Êtes-vous sûr de vouloir supprimer le client '{client.Name}' et tous ses éléments associés ?",
                    "Confirmation",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    _viewModel.DeleteClient(client);
                }
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
    }
}