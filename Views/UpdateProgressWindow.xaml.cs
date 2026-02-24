using System.Windows;
using AccesClientWPF.ViewModels;

namespace AccesClientWPF.Views
{
    public partial class UpdateProgressWindow : Window
    {
        public UpdateProgressWindow()
        {
            InitializeComponent();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not UpdateProgressViewModel vm)
                return;

            var r = MessageBox.Show(
                "Annuler la mise à jour ?\n\nL'application pourra reproposer la mise à jour au prochain démarrage.",
                "Annuler",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (r == MessageBoxResult.Yes)
                vm.RaiseCancelRequested();
        }
    }
}