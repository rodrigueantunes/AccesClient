using System.Windows;

namespace AccesClientWPF.Views
{
    public partial class UpdatePromptWindow : Window
    {
        public UpdatePromptWindow()
        {
            InitializeComponent();
        }

        private void Later_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Update_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}