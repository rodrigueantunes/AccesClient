using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AccesClientWPF.Models;
using AccesClientWPF.Helpers;

namespace AccesClientWPF.Views
{
    public partial class EditRdsAccountWindow : Window
    {
        public RdsAccount RdsAccount { get; private set; }

        public EditRdsAccountWindow(RdsAccount account)
        {
            InitializeComponent();
            RdsAccount = new RdsAccount
            {
                Description = account.Description,
                IpDns = account.IpDns,
                NomUtilisateur = account.NomUtilisateur,
                MotDePasse = "" // Toujours vide au lancement
            };
            DataContext = RdsAccount;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (FindName("PasswordBox") is PasswordBox passwordBox)
            {
                RdsAccount.MotDePasse = EncryptionHelper.Encrypt(passwordBox.Password); // Encryptage du mot de passe
            }

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}