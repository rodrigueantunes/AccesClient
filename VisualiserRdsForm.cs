using System;
using System.Linq;
using System.Windows.Forms;

namespace Accès_client
{
    // Formulaire pour visualiser les comptes RDS enregistrés
    public partial class VisualiserRdsForm : Form
    {
        private string rdsFilePath = "C:\\Application\\Clients\\rds_accounts.json"; // Chemin du fichier JSON

        public VisualiserRdsForm()
        {
            InitializeComponent();
            
            // Charger les comptes RDS depuis le fichier JSON
            RdsData.LoadRdsAccounts(rdsFilePath);
            // Remplir la liste avec les comptes RDS
            PopulateRdsList();
        }

        // Méthode pour peupler la liste des comptes RDS
        
        private void PopulateRdsList()
        {
            // Effacer les éléments existants
            listViewRds.Items.Clear();

            // Vérifier si des comptes sont présents
            if (RdsData.RdsAccounts.Any())
            {
                // Parcourir les comptes RDS et les ajouter à la ListView
                foreach (var account in RdsData.RdsAccounts)
                {
                    ListViewItem item = new ListViewItem(account.Description);
                    item.SubItems.Add(account.IpDns);
                    item.SubItems.Add(account.NomUtilisateur);
                    item.SubItems.Add(account.DateCreation.ToString("yyyy-MM-dd HH:mm:ss"));
                    listViewRds.Items.Add(item);
                }
            }
            else
            {
                MessageBox.Show("Aucun compte trouvé.");
            }
        }



        private void ButtonAjouter_Click(object sender, EventArgs e)
        {
            SaisieRdsForm saisieForm = new SaisieRdsForm();  // Crée une nouvelle instance de SaisieRdsForm pour l'ajout
            if (saisieForm.ShowDialog() == DialogResult.OK)  // Si l'utilisateur valide l'ajout
            {
                // Sauvegarder les comptes RDS après l'ajout
                RdsData.SaveRdsAccounts(rdsFilePath);
                // Recharger la liste des comptes
                // Avant de mettre à jour la ListView
                listViewRds.BeginUpdate();

                // Recharger la liste avec les données mises à jour
                PopulateRdsList();

                // Après la mise à jour
                listViewRds.EndUpdate();
            }
        }


        // Bouton pour modifier un compte RDS
        private void ButtonModifier_Click(object sender, EventArgs e)
        {
            // Vérifier si un élément est sélectionné
            if (listViewRds.SelectedItems.Count == 0)
            {
                MessageBox.Show("Veuillez sélectionner un compte à modifier.");
                return;
            }

            // Récupérer l'élément sélectionné
            ListViewItem selectedItem = listViewRds.SelectedItems[0];

            // Récupérer l'index du compte sélectionné dans la liste
            var account = RdsData.RdsAccounts.FirstOrDefault(a => a.Description == selectedItem.Text);

            if (account != null)
            {
                // Ouvrir un formulaire de modification
                SaisieRdsForm modificationForm = new SaisieRdsForm(account);
                if (modificationForm.ShowDialog() == DialogResult.OK)
                {
                    // Recharger la liste avec les données mises à jour
                    RdsData.SaveRdsAccounts(rdsFilePath);
                    // Avant de mettre à jour la ListView
                    listViewRds.BeginUpdate();

                    // Recharger la liste avec les données mises à jour
                    PopulateRdsList();

                    // Après la mise à jour
                    listViewRds.EndUpdate();
                }
            }
        }

        // Bouton pour supprimer un compte RDS
        private void ButtonSupprimer_Click(object sender, EventArgs e)
        {
            // Vérifier si un élément est sélectionné
            if (listViewRds.SelectedItems.Count == 0)
            {
                MessageBox.Show("Veuillez sélectionner un compte à supprimer.");
                return;
            }

            // Récupérer l'élément sélectionné
            ListViewItem selectedItem = listViewRds.SelectedItems[0];

            // Récupérer l'index du compte sélectionné dans la liste
            var account = RdsData.RdsAccounts.FirstOrDefault(a => a.Description == selectedItem.Text);

            if (account != null)
            {
                // Confirmer la suppression
                var result = MessageBox.Show("Êtes-vous sûr de vouloir supprimer ce compte ?", "Confirmation", MessageBoxButtons.YesNo);
                if (result == DialogResult.Yes)
                {
                    // Supprimer le compte de la liste
                    RdsData.RdsAccounts.Remove(account);
                    // Sauvegarder les comptes modifiés dans le fichier
                    RdsData.SaveRdsAccounts(rdsFilePath);
                    // Recharger la liste
                    // Avant de mettre à jour la ListView
                    listViewRds.BeginUpdate();

                    // Recharger la liste avec les données mises à jour
                    PopulateRdsList();

                    // Après la mise à jour
                    listViewRds.EndUpdate();
                }
            }
        }
    }
}
