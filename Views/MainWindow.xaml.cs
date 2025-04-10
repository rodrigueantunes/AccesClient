// Ajout des gestionnaires d'événements dans MainWindow.xaml.cs
using System.Windows;
using AccesClientWPF.Models;
using AccesClientWPF.ViewModels;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using AccesClientWPF.Helpers;

namespace AccesClientWPF.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
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

        // Nouveaux gestionnaires pour le menu contextuel
        private void AddContextMenu_Click(object sender, RoutedEventArgs e)
        {
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

                    // Trouver et supprimer le fichier sélectionné
                    var fileToRemove = database.Files.FirstOrDefault(f =>
                        f.Name == viewModel.SelectedFile.Name && f.Client == viewModel.SelectedFile.Client);

                    if (fileToRemove != null)
                    {
                        database.Files.Remove(fileToRemove);
                        viewModel.SaveDatabase(database);

                        // Recharger les fichiers pour le client sélectionné
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