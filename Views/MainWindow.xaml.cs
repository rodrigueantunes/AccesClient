using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using AccesClientWPF.Models;
using AccesClientWPF.ViewModels;

namespace AccesClientWPF.Views
{
    public partial class MainWindow : Window
    {
        private sealed class MovePayload
        {
            public FileModel Item { get; init; }
            public string Rangement { get; init; }
        }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }

        private void FileList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not MainViewModel vm) return;
            if (sender is not ListView lv) return;
            if (lv.SelectedItem is not FileModel file) return;

            vm.HandleFileDoubleClick(file);
        }

        private void MainScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is ScrollViewer scv)
            {
                scv.ScrollToVerticalOffset(scv.VerticalOffset - e.Delta / 3);
                e.Handled = true;
            }
        }

        private void FileList_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is not ListView listView) return;

            var scrollViewer = FindVisualChild<ScrollViewer>(listView);
            if (scrollViewer == null) return;

            scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta / 3);
            e.Handled = true;
        }

        private void ListViewItem_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListViewItem item)
            {
                item.IsSelected = true;
                item.Focus();
            }
        }

        private void AddContextMenu_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not MainViewModel vm) return;

            if (vm.SelectedClient == null)
            {
                MessageBox.Show("Veuillez sélectionner un client avant d'ajouter un élément.",
                    "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            vm.AddFile();
            RefreshUI();
        }

        private void AddButtonDirect_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not MainViewModel vm) return;

            if (vm.SelectedClient == null)
            {
                MessageBox.Show("Veuillez sélectionner un client avant d'ajouter un élément.",
                    "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            vm.AddFile();
            RefreshUI();
        }

        private void EditContextMenu_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not MainViewModel vm) return;
            var file = GetSelectedFileFromContextMenu(sender);
            if (file == null)
            {
                MessageBox.Show("Veuillez sélectionner un élément à modifier.",
                    "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            vm.EditSelectedFile(file);
            RefreshUI();
        }

        private void DeleteContextMenu_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not MainViewModel vm) return;
            var file = GetSelectedFileFromContextMenu(sender);
            if (file == null)
            {
                MessageBox.Show("Veuillez sélectionner un élément à supprimer.",
                    "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var db = vm.LoadDatabase();

            // ✅ Sécurité : interdiction de supprimer un rangement s’il est utilisé
            if (string.Equals(file.Type, "Rangement", StringComparison.OrdinalIgnoreCase))
            {
                bool hasChildren = db.Files.Any(f =>
                    string.Equals(f.Client, file.Client, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(f.Type, "MotDePasse", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals((f.RangementParent ?? "").Trim(), (file.Name ?? "").Trim(), StringComparison.OrdinalIgnoreCase));

                if (hasChildren)
                {
                    MessageBox.Show(
                        "Suppression interdite : ce rangement est utilisé (il contient des éléments).\n" +
                        "Déplace d'abord les éléments vers la racine ou un autre rangement.",
                        "Suppression interdite",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }
            }

            var result = MessageBox.Show(
                $"Êtes-vous sûr de vouloir supprimer l'élément '{file.Name}' ?",
                "Confirmation de suppression",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            var toRemove = db.Files.FirstOrDefault(f =>
                string.Equals(f.Client, file.Client, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(f.Type, file.Type, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(f.Name, file.Name, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(f.RangementParent ?? "", file.RangementParent ?? "", StringComparison.OrdinalIgnoreCase));

            if (toRemove != null)
            {
                db.Files.Remove(toRemove);
                vm.SaveDatabase(db);
            }

            RefreshUI();
        }

        private void MoveToRoot_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not MainViewModel vm) return;
            var file = GetSelectedFileFromContextMenu(sender);
            if (file == null) return;

            vm.MoveToRacine(file);
            RefreshUI();
        }

        private void MoveToRangement_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not MainViewModel vm) return;
            if (sender is not MenuItem mi) return;
            if (mi.Tag is not MovePayload payload) return;
            if (payload.Item == null || string.IsNullOrWhiteSpace(payload.Rangement)) return;

            vm.MoveToRangement(payload.Item, payload.Rangement);
            RefreshUI();
        }

        private void ElementsContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            if (sender is not ContextMenu cm) return;
            if (cm.PlacementTarget is not ListView lv) return;
            if (DataContext is not MainViewModel vm) return;

            var item = lv.SelectedItem as FileModel;

            var miGoRoot = cm.Items.OfType<MenuItem>().FirstOrDefault(m => (m.Tag as string) == "GoRoot");
            var miGoTo = cm.Items.OfType<MenuItem>().FirstOrDefault(m => (m.Tag as string) == "GoTo");

            if (item == null)
            {
                if (miGoRoot != null) miGoRoot.Visibility = Visibility.Collapsed;
                if (miGoTo != null) { miGoTo.IsEnabled = false; miGoTo.Items.Clear(); }
                return;
            }

            if (miGoRoot != null)
                miGoRoot.Visibility = string.IsNullOrWhiteSpace(item.RangementParent) ? Visibility.Collapsed : Visibility.Visible;

            if (miGoTo != null)
            {
                miGoTo.IsEnabled = !string.Equals(item.Type, "Rangement", StringComparison.OrdinalIgnoreCase);
                PopulateGoToMenu(vm, miGoTo, item);
            }
        }

        private void PopulateGoToMenu(MainViewModel vm, MenuItem miGoTo, FileModel selectedItem)
        {
            miGoTo.Items.Clear();

            if (vm.SelectedClient == null)
            {
                miGoTo.Items.Add(new MenuItem { Header = "(Sélectionne un client)", IsEnabled = false });
                return;
            }

            var db = vm.LoadDatabase();
            var clientName = (vm.SelectedClient.Name ?? "").Trim();

            var official = db.Files
                .Where(f => string.Equals((f.Client ?? "").Trim(), clientName, StringComparison.OrdinalIgnoreCase))
                .Where(f => string.Equals((f.Type ?? "").Trim(), "Rangement", StringComparison.OrdinalIgnoreCase))
                .Where(f => string.IsNullOrWhiteSpace((f.RangementParent ?? "").Trim()))
                .Select(f => (f.Name ?? "").Trim())
                .Where(n => !string.IsNullOrWhiteSpace(n));

            var inferred = db.Files
                .Where(f => string.Equals((f.Client ?? "").Trim(), clientName, StringComparison.OrdinalIgnoreCase))
                .Where(f => !string.Equals((f.Type ?? "").Trim(), "MotDePasse", StringComparison.OrdinalIgnoreCase))
                .Select(f => (f.RangementParent ?? "").Trim())
                .Where(p => !string.IsNullOrWhiteSpace(p));

            var rangements = official
                .Concat(inferred)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(n => n)
                .ToList();

            if (rangements.Count == 0)
            {
                miGoTo.Items.Add(new MenuItem { Header = "(Aucun rangement)", IsEnabled = false });
                return;
            }

            foreach (var r in rangements)
            {
                var it = new MenuItem
                {
                    Header = r,
                    Tag = new MovePayload { Item = selectedItem, Rangement = r }
                };
                it.Click += MoveToRangement_Click;
                miGoTo.Items.Add(it);
            }
        }

        private FileModel GetSelectedFileFromContextMenu(object sender)
        {
            if (sender is not MenuItem mi) return null;

            var cm = FindVisualParent<ContextMenu>(mi);
            if (cm?.PlacementTarget is not ListView lv) return null;

            return lv.SelectedItem as FileModel;
        }

        private void RefreshUI()
        {
            if (DataContext is not MainViewModel vm) return;
            vm.LoadFilesForSelectedClient();
            CommandManager.InvalidateRequerySuggested();
        }

        private static T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            while (child != null)
            {
                if (child is T parent) return parent;
                child = VisualTreeHelper.GetParent(child);
            }
            return null;
        }

        private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild) return typedChild;

                var childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null) return childOfChild;
            }
            return null;
        }
    }
}