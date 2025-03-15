using System.Windows;
using AccesClientWPF.Models;
using AccesClientWPF.ViewModels;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

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

    }
}