using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Newtonsoft.Json;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Accès_client
{
    // Classe pour représenter un compte RDS
    public class RdsAccount
    {
        public string NomUtilisateur { get; set; }
        public string Description { get; set; }
        public string IpDns { get; set; }  // Ajout de l'IP
        public string MotDePasse { get; set; }  // Ajout du mot de passe
        public DateTime DateCreation { get; set; }
    }

    // Classe statique pour stocker tous les comptes RDS
    public static class RdsData
    {
        // Liste pour stocker les comptes RDS
        public static List<RdsAccount> RdsAccounts = new List<RdsAccount>();

        // Sauvegarder les comptes RDS dans un fichier JSON
        public static void SaveRdsAccounts(string filePath)
        {
            string json = JsonConvert.SerializeObject(RdsAccounts, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }

        // Charger les comptes RDS depuis un fichier JSON
        public static void LoadRdsAccounts(string filePath)
        {
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                RdsAccounts = JsonConvert.DeserializeObject<List<RdsAccount>>(json);
            }
        }
    }
    public class EncryptionHelper
    {
        private static readonly string key = "1234567890123456"; // Clé de 16 caractères pour AES-128
        private static readonly string iv = "1234567890123456"; // IV de 16 caractères pour AES

        // Méthode pour chiffrer un texte
        public static string Encrypt(string plainText)
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Encoding.UTF8.GetBytes(key);
                aesAlg.IV = Encoding.UTF8.GetBytes(iv);

                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(plainText);
                        }
                    }
                    return Convert.ToBase64String(msEncrypt.ToArray());
                }
            }
        }

        // Méthode pour déchiffrer un texte
        public static string Decrypt(string cipherText)
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Encoding.UTF8.GetBytes(key);
                aesAlg.IV = Encoding.UTF8.GetBytes(iv);

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msDecrypt = new MemoryStream(Convert.FromBase64String(cipherText)))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            return srDecrypt.ReadToEnd();
                        }
                    }
                }
            }
        }
    }


    // Formulaire pour saisir les informations des comptes RDS
    public partial class SaisieRdsForm : Form
    {
        private RdsAccount rdsAccount;  // Champ pour stocker le compte RDS à modifier

        // Constructeur pour la création d'un nouveau compte
        public SaisieRdsForm()
        {
            InitializeComponent();
        }

        // Constructeur pour la modification d'un compte existant
        public SaisieRdsForm(RdsAccount account)
        {
            InitializeComponent();
            rdsAccount = account;

            // Remplir les champs avec les informations du compte
            textBoxNomRds.Text = rdsAccount.Description;
            textBoxIpDns.Text = rdsAccount.IpDns;
            textBoxUtilisateur.Text = rdsAccount.NomUtilisateur;
            textBoxMotDePasse.Text = rdsAccount.MotDePasse;
        }

        // Gestion du clic sur le bouton "Enregistrer"
        private void ButtonEnregistrer_Click(object sender, EventArgs e)
        {
            string nomRds = textBoxNomRds.Text;
            string ipDns = textBoxIpDns.Text;
            string utilisateur = textBoxUtilisateur.Text;
            string motDePasse = textBoxMotDePasse.Text;

            // Vérifier si tous les champs sont remplis
            if (string.IsNullOrEmpty(nomRds) || string.IsNullOrEmpty(ipDns) || string.IsNullOrEmpty(utilisateur) || string.IsNullOrEmpty(motDePasse))
            {
                MessageBox.Show("Tous les champs doivent être remplis.");
                return;
            }

            // Si c'est un compte existant, on le met à jour, sinon on en crée un nouveau
            if (rdsAccount != null)
            {
                rdsAccount.Description = nomRds;
                rdsAccount.IpDns = ipDns;
                rdsAccount.NomUtilisateur = utilisateur;
                rdsAccount.MotDePasse = EncryptionHelper.Encrypt(motDePasse);
                rdsAccount.DateCreation = DateTime.Now;  // Mettre à jour la date de modification
            }
            else
            {
                // Créer un nouvel objet RdsAccount et l'ajouter à la liste
                SaveCredentials(nomRds, ipDns, utilisateur, motDePasse);
            }

            // Sauvegarder les comptes RDS dans le fichier
            RdsData.SaveRdsAccounts("C:\\Application\\Clients\\rds_accounts.json");

            MessageBox.Show("Les informations ont été enregistrées avec succès.");
            this.Close();
        }

        private void SaveCredentials(string nomRds, string ipDns, string utilisateur, string motDePasse)
        {
            // Créer un nouvel objet RdsAccount et l'ajouter à la liste
            RdsData.RdsAccounts.Add(new RdsAccount
            {
                NomUtilisateur = utilisateur,
                Description = nomRds,
                IpDns = ipDns,
                MotDePasse = EncryptionHelper.Encrypt(motDePasse),
                DateCreation = DateTime.Now
            });
        }
    }
}