using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using AccesClientWPF.Models;

namespace AccesClientWPF.Views
{
    public partial class EditClientWindow : Window
    {
        public ClientModel OriginalClient { get; set; }
        public ObservableCollection<ClientModel> AllClients { get; set; }

        public EditClientWindow(ClientModel client, ObservableCollection<ClientModel> allClients)
        {
            InitializeComponent();
            OriginalClient = client;
            AllClients = allClients;
            
            TxtClientName.Text = client.Name;
            TxtAcronym.Text = client.Acronym ?? string.Empty;
            TxtClientName.Focus();
            TxtClientName.SelectAll();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
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

            // Check if another client already has this name (excluding the current one)
            if (AllClients.Any(c => c.Name.Equals(clientName, StringComparison.OrdinalIgnoreCase) && !c.Name.Equals(OriginalClient.Name, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("Un client avec ce nom existe déjà.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Update the original client
            OriginalClient.Name = clientName;
            OriginalClient.Acronym = acronym;

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
