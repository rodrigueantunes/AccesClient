using System.Windows;
using AccesClientWPF.Models;
using AccesClientWPF.ViewModels;
using System.Windows.Controls;
using System.Windows.Input;

namespace AccesClientWPF.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new ViewModels.MainViewModel();
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
    }
}