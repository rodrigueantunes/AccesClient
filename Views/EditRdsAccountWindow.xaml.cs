using System;
using System.Windows;
using System.Windows.Controls;
using AccesClientWPF.Models;
using AccesClientWPF.Helpers;

namespace AccesClientWPF.Views
{
    public partial class EditRdsAccountWindow : Window
    {
        public RdsAccount RdsAccount { get; private set; }
        private readonly string _originalEncryptedPassword;

        public EditRdsAccountWindow(RdsAccount account)
        {
            InitializeComponent();

            _originalEncryptedPassword = account?.MotDePasse ?? string.Empty;

            RdsAccount = new RdsAccount
            {
                Description = account?.Description,
                IpDns = account?.IpDns,
                NomUtilisateur = account?.NomUtilisateur,
                // On conserve en interne l'ancien (pour éviter l'effacement si champ vide)
                MotDePasse = _originalEncryptedPassword,
                DateCreation = account?.DateCreation ?? DateTime.Now
            };

            DataContext = RdsAccount;

            // Pré-remplissage visible (comme demandé)
            if (FindName("PasswordBox") is PasswordBox pb)
                pb.Password = GetDisplayedPassword(_originalEncryptedPassword);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (FindName("PasswordBox") is PasswordBox passwordBox)
            {
                // Si vide => on ne touche pas au mot de passe existant
                if (string.IsNullOrEmpty(passwordBox.Password))
                    RdsAccount.MotDePasse = _originalEncryptedPassword;
                else
                    RdsAccount.MotDePasse = EncryptionHelper.Encrypt(passwordBox.Password);
            }

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private static string GetDisplayedPassword(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return string.Empty;

            var dec = EncryptionHelper.Decrypt(raw);
            if (!string.IsNullOrEmpty(dec))
                return dec;

            return IsBase64(raw) ? string.Empty : raw;
        }

        private static bool IsBase64(string s)
        {
            if (string.IsNullOrWhiteSpace(s) || s.Length % 4 != 0)
                return false;

            byte[] buffer = new byte[s.Length];
            return Convert.TryFromBase64String(s, buffer, out _);
        }
    }
}