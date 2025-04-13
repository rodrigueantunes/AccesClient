using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using AccesClientWPF.Models;
using AccesClientWPF.ViewModels;
using AccesClientWPF.Helpers;
using System.Windows.Media;
using Newtonsoft.Json;
using HelpersDatabaseModel = AccesClientWPF.Helpers.DatabaseModel;
using ModelsDatabaseModel = AccesClientWPF.Models.DatabaseModel;

namespace AccesClientWPF.Views
{
    // Ajout de la déclaration partielle qui est probablement manquante
    public partial class SharedDatabaseWindow : Window
    {
        private SharedDatabaseViewModel _viewModel;
        private string _currentFilePath;
        private string _originalFileName;
        private Button _saveButton;
        private Button _cancelButton;

        public SharedDatabaseWindow()
        {
            InitializeComponent();

            _viewModel = new SharedDatabaseViewModel();
            DataContext = _viewModel;

            this.Loaded += SharedDatabaseWindow_Loaded;
        }

        private void SharedDatabaseWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Rechercher les boutons par leur nom après le chargement de la fenêtre
            _saveButton = FindButtonByName("BtnSave");
            _cancelButton = FindButtonByName("BtnCancel");

            // Désactiver les boutons s'ils sont trouvés
            if (_saveButton != null) _saveButton.IsEnabled = false;
            if (_cancelButton != null) _cancelButton.IsEnabled = false;
        }

        // Méthode pour trouver un bouton par son nom
        private Button FindButtonByName(string name)
        {
            return FindVisualChild<Button>(this, button => button.Name == name);
        }

        // Version modifiée de FindVisualChild qui accepte un prédicat
        private T FindVisualChild<T>(DependencyObject parent, Func<T, bool> predicate) where T : DependencyObject
        {
            if (parent == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T typedChild && predicate(typedChild))
                    return typedChild;

                var result = FindVisualChild<T>(child, predicate);
                if (result != null)
                    return result;
            }

            return null;
        }

        private void OpenSharedDatabase_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Ouvrir une base partagée",
                Filter = "Fichiers base partagée (*.antclient)|*.antclient|Fichiers JSON (*.json)|*.json|Tous les fichiers (*.*)|*.*",
                DefaultExt = ".antclient"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    _currentFilePath = openFileDialog.FileName;
                    _originalFileName = Path.GetFileNameWithoutExtension(_currentFilePath);

                    // Mettre à jour le titre de la fenêtre
                    this.Title = $"Accès Client 1.2.2 / Partagé ({_originalFileName})";

                    // Charger les données du fichier
                    var jsonData = File.ReadAllText(_currentFilePath);
                    var importedDatabase = JsonConvert.DeserializeObject<AccesClientWPF.Models.DatabaseModel>(jsonData);

                    if (importedDatabase != null)
                    {
                        _viewModel.LoadDatabase(importedDatabase);

                        // Activer les boutons Sauvegarder et Annuler s'ils existent
                        if (_saveButton != null) _saveButton.IsEnabled = true;
                        if (_cancelButton != null) _cancelButton.IsEnabled = true;
                    }
                    else
                    {
                        MessageBox.Show("Le fichier n'a pas pu être chargé ou est vide.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de l'ouverture du fichier : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ManageClientsCommand_Click(object sender, RoutedEventArgs e)
        {

            var sharedClientWindow = new SharedClientManagementWindow(_viewModel);

            if (sharedClientWindow.ShowDialog() == true)
            {
               
                if (_viewModel.SelectedClient != null &&
                    !_viewModel.Clients.Contains(_viewModel.SelectedClient))
                {
                    _viewModel.SelectedClient = null;
                }
            }
        }

        private void CreateNewDatabase_Click(object sender, RoutedEventArgs e)
        {
            // Demander confirmation avant de réinitialiser
            if (_saveButton != null && _saveButton.IsEnabled)
            {
                var result = MessageBox.Show("Voulez-vous enregistrer la base actuelle avant d'en créer une nouvelle ?",
                    "Confirmation", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                if (result == MessageBoxResult.Cancel)
                    return;

                if (result == MessageBoxResult.Yes)
                    SaveSharedDatabase_Click(null, null);
            }

            // Réinitialiser
            _viewModel.CreateNewDatabase();
            _currentFilePath = null;
            _originalFileName = null;
            this.Title = "Accès Client 1.2.2 / Partagé (Nouveau)";

            // Activer les boutons
            if (_saveButton != null) _saveButton.IsEnabled = true;
            if (_cancelButton != null) _cancelButton.IsEnabled = true;
        }

        // Corrigeons la fonction d'importation vers database.json
        private void ImportToMainDatabase_Click(object sender, RoutedEventArgs e)
        {
            // Vérifier d'abord si une base est chargée
            if (string.IsNullOrEmpty(_currentFilePath) && _viewModel.Clients.Count == 0)
            {
                MessageBox.Show("Veuillez d'abord ouvrir ou créer une base de données.",
                    "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                // Créer l'objet de base de données à partir du viewModel actuel
                var importedDatabase = new Models.DatabaseModel
                {
                    Clients = _viewModel.Clients,
                    Files = new ObservableCollection<FileModel>(_viewModel.AllFiles)
                };

                // Charger la base principale
                string mainDbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "database.json");
                Models.DatabaseModel currentDatabase;

                if (File.Exists(mainDbPath))
                {
                    string jsonData = File.ReadAllText(mainDbPath);
                    currentDatabase = JsonConvert.DeserializeObject<Models.DatabaseModel>(jsonData)
                        ?? new Models.DatabaseModel();
                }
                else
                {
                    currentDatabase = new Models.DatabaseModel
                    {
                        Clients = new ObservableCollection<ClientModel>(),
                        Files = new ObservableCollection<FileModel>()
                    };
                }

                // Fusionner les clients
                foreach (var importedClient in importedDatabase.Clients)
                {
                    var existingClient = currentDatabase.Clients
                        .FirstOrDefault(c => c.Name == importedClient.Name);

                    if (existingClient == null)
                    {
                        currentDatabase.Clients.Add(importedClient);
                    }
                }

                // Fusionner les fichiers
                foreach (var importedFile in importedDatabase.Files)
                {
                    var existingFile = currentDatabase.Files
                        .FirstOrDefault(f => f.Name == importedFile.Name && f.Client == importedFile.Client);

                    if (existingFile == null)
                    {
                        currentDatabase.Files.Add(importedFile);
                    }
                    else
                    {
                        // Mise à jour de toutes les propriétés
                        existingFile.Type = importedFile.Type;
                        existingFile.FullPath = importedFile.FullPath;
                        existingFile.CustomIconPath = importedFile.CustomIconPath;
                        existingFile.WindowsUsername = importedFile.WindowsUsername;
                        existingFile.WindowsPassword = importedFile.WindowsPassword;
                    }
                }

                // Sauvegarder
                File.WriteAllText(mainDbPath, JsonConvert.SerializeObject(currentDatabase, Formatting.Indented));
                MessageBox.Show("Import réussi dans la base principale !", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'importation : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        private void SaveSharedDatabase_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentFilePath))
            {
                SaveAsSharedDatabase();
                return;
            }

            try
            {
                // Créer l'objet de base de données à partir du viewModel
                var database = new Models.DatabaseModel
                {
                    Clients = _viewModel.Clients,
                    Files = new ObservableCollection<FileModel>(_viewModel.AllFiles)
                };

                // Sauvegarder au même emplacement
                File.WriteAllText(_currentFilePath, JsonConvert.SerializeObject(database, Formatting.Indented));
                MessageBox.Show("Base partagée sauvegardée avec succès !", "Sauvegarde réussie", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la sauvegarde : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveAsSharedDatabase()
        {
            var saveFileDialog = new SaveFileDialog
            {
                Title = "Sauvegarder la base partagée",
                Filter = "Fichiers base partagée (*.antclient)|*.antclient|Fichiers JSON (*.json)|*.json",
                DefaultExt = ".antclient"
            };

            if (_originalFileName != null)
            {
                saveFileDialog.FileName = _originalFileName;
            }

            if (saveFileDialog.ShowDialog() == true)
            {
                _currentFilePath = saveFileDialog.FileName;
                _originalFileName = Path.GetFileNameWithoutExtension(_currentFilePath);
                this.Title = $"Accès Client 1.1.0 ({_originalFileName})";
                SaveSharedDatabase_Click(null, null);
            }
        }

        private void CancelAndClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void FileList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListView listView && listView.SelectedItem is FileModel selectedFile)
            {
                // Afficher les détails du fichier plutôt que de l'exécuter
                _viewModel.EditSelectedFile(selectedFile);
            }
        }

        private void AddContextMenu_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedClient == null)
            {
                MessageBox.Show("Veuillez sélectionner un client avant d'ajouter un élément.",
                    "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            _viewModel.AddFile();
        }

        private void EditContextMenu_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedFile == null)
            {
                MessageBox.Show("Veuillez sélectionner un élément à modifier.",
                    "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            _viewModel.EditSelectedFile();
        }

        private void DeleteContextMenu_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedFile != null)
            {
                MessageBoxResult result = MessageBox.Show(
                    $"Êtes-vous sûr de vouloir supprimer l'élément '{_viewModel.SelectedFile.Name}' ?",
                    "Confirmation de suppression",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _viewModel.DeleteSelectedFile();
                }
            }
            else
            {
                MessageBox.Show("Veuillez sélectionner un élément à supprimer.",
                    "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void MainScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer scv = (ScrollViewer)sender;
            scv.ScrollToVerticalOffset(scv.VerticalOffset - e.Delta / 3);
            e.Handled = true;
        }

        private void CopyWindowsUsername_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string username && !string.IsNullOrEmpty(username))
            {
                Clipboard.SetText(username);
                MessageBox.Show("Nom d'utilisateur Windows copié dans le presse-papiers.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void CopyWindowsPassword_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is FileModel file)
            {
                if (!string.IsNullOrEmpty(file.WindowsPassword))
                {
                    string decryptedPassword = EncryptionHelper.Decrypt(file.WindowsPassword);
                    Clipboard.SetText(decryptedPassword);
                    MessageBox.Show("Mot de passe Windows copié dans le presse-papiers.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        // Méthode helper pour trouver un élément visuel enfant
        private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                    return typedChild;
                var childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                    return childOfChild;
            }
            return null;
        }


    }
}