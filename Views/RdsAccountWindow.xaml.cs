using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AccesClientWPF.ViewModels;
using AccesClientWPF.Models;

namespace AccesClientWPF.Views
{
    public partial class RdsAccountWindow : Window
    {
        public RdsAccountWindow()
        {
            InitializeComponent();
            DataContext = new RdsAccountViewModel();
        }

        private void ListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListView listView && listView.SelectedItem is RdsAccount selectedAccount)
            {
                if (DataContext is RdsAccountViewModel viewModel)
                {
                    viewModel.EditCommand.Execute(selectedAccount);
                }
            }
        }
    }
}
