using System;
using System.Windows;
using AccesClientUpdaterHost.Services;

namespace AccesClientUpdaterHost
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Loaded += async (_, __) =>
            {
                try
                {
                    var svc = new UpdaterHostService();
                    svc.Progress += (p, s) =>
                    {
                        Bar.Value = p;
                        StatusText.Text = s;
                    };

                    await svc.RunAsync();
                    Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString(), "Erreur mise à jour", MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                }
            };
        }
    }
}