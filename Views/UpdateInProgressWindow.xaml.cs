using System.Windows;

namespace AccesClientWPF.Views
{
    public partial class UpdateInProgressWindow : Window
    {
        public bool WaitChosen { get; private set; }

        public UpdateInProgressWindow()
        {
            InitializeComponent();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            WaitChosen = false;
            DialogResult = true;
            Close();
        }

        private void Wait_Click(object sender, RoutedEventArgs e)
        {
            WaitChosen = true;
            DialogResult = true;
            Close();
        }
    }
}