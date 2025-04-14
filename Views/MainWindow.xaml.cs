using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using AccesClientWPF.Helpers;
using AccesClientWPF.Models;
using AccesClientWPF.ViewModels;

namespace AccesClientWPF.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            // Le DataContext est bien assigné à MainViewModel qui contient désormais FilteredClients pour afficher la liste filtrée.
            DataContext = new MainViewModel();
        }

        private void FileList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListView listView && listView.SelectedItem is FileModel selectedFile)
            {
                if (DataContext is MainViewModel viewModel)
                {
                    viewModel.HandleFileDoubleClick(selectedFile);
                }
            }
        }

        private void MoveUpButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.MoveUp();
            }
        }

        private void MoveDownButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.MoveDown();
            }
        }

        private void FileList_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is ListView listView)
            {
                ScrollViewer scrollViewer = FindVisualChild<ScrollViewer>(listView);
                if (scrollViewer != null)
                {
                    scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta / 3);
                    e.Handled = true;
                }
            }
        }

        private void MainScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is ScrollViewer scv)
            {
                scv.ScrollToVerticalOffset(scv.VerticalOffset - e.Delta / 3);
                e.Handled = true;
            }
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

        // Gestionnaire pour l'ajout via le menu contextuel
        private void AddContextMenu_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                // Utilisez la propriété SelectedClient qui reste la même (la liste affichée est désormais FilteredClients).
                if (viewModel.SelectedClient == null)
                {
                    MessageBox.Show("Veuillez sélectionner un client avant d'ajouter un élément.",
                        "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                viewModel.AddFile();
            }
        }

        // Gestionnaire pour la modification via le menu contextuel
        private void EditContextMenu_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                if (viewModel.SelectedFile == null)
                {
                    MessageBox.Show("Veuillez sélectionner un élément à modifier.",
                        "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                viewModel.EditSelectedFile();
            }
        }

        // Gestionnaire pour la suppression via le menu contextuel
        private void DeleteContextMenu_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel viewModel && viewModel.SelectedFile != null)
            {
                MessageBoxResult result = MessageBox.Show(
                    $"Êtes-vous sûr de vouloir supprimer l'élément '{viewModel.SelectedFile.Name}' ?",
                    "Confirmation de suppression",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // Obtenir la base de données actuelle
                    var database = viewModel.LoadDatabase();

                    // Recherche du fichier à supprimer dans la collection.
                    var fileToRemove = database.Files.FirstOrDefault(f =>
                        f.Name == viewModel.SelectedFile.Name &&
                        // Vous pouvez conserver f.Client puisque SelectedClient est identique (la liste visible est désormais filtrée via FilteredClients).
                        f.Client == viewModel.SelectedFile.Client);

                    if (fileToRemove != null)
                    {
                        database.Files.Remove(fileToRemove);
                        viewModel.SaveDatabase(database);

                        // Forcer le rechargement de la sélection pour rafraîchir la vue.
                        var currentClient = viewModel.SelectedClient;
                        if (currentClient != null)
                        {
                            viewModel.SelectedClient = null;
                            viewModel.SelectedClient = currentClient;
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("Veuillez sélectionner un élément à supprimer.",
                    "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void AddButtonDirect_Click(object sender, RoutedEventArgs e)
        {
            // Appel direct à la méthode d'ajout (similaire au menu contextuel)
            if (DataContext is MainViewModel viewModel)
            {
                if (viewModel.SelectedClient == null)
                {
                    MessageBox.Show("Veuillez sélectionner un client avant d'ajouter un élément.",
                        "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                viewModel.AddFile();
            }
        }

        // Méthode helper pour trouver un élément visuel enfant dans l'arborescence
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
