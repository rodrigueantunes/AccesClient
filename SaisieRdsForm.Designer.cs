namespace Accès_client
{
    partial class SaisieRdsForm
    {
        /// <summary>
        /// Variable nécessaire au concepteur.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Nettoyage des ressources utilisées.
        /// </summary>
        /// <param name="disposing">Vrai si les ressources managées doivent être supprimées ; sinon, faux.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Code généré par le Concepteur Windows Forms

        /// <summary>
        /// Méthode requise pour la prise en charge du Concepteur - ne modifiez pas
        /// le contenu de cette méthode avec l'éditeur de code.
        /// </summary>
        private void InitializeComponent()
        {
            this.labelNomRds = new System.Windows.Forms.Label();
            this.labelIpDns = new System.Windows.Forms.Label();
            this.labelUtilisateur = new System.Windows.Forms.Label();
            this.labelMotDePasse = new System.Windows.Forms.Label();
            this.textBoxNomRds = new System.Windows.Forms.TextBox();
            this.textBoxIpDns = new System.Windows.Forms.TextBox();
            this.textBoxUtilisateur = new System.Windows.Forms.TextBox();
            this.textBoxMotDePasse = new System.Windows.Forms.TextBox();
            this.buttonEnregistrer = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // labelNomRds
            // 
            this.labelNomRds.AutoSize = true;
            this.labelNomRds.Location = new System.Drawing.Point(12, 9);
            this.labelNomRds.Name = "labelNomRds";
            this.labelNomRds.Size = new System.Drawing.Size(58, 13);
            this.labelNomRds.TabIndex = 0;
            this.labelNomRds.Text = "Nom connexion :";
            // 
            // labelIpDns
            // 
            this.labelIpDns.AutoSize = true;
            this.labelIpDns.Location = new System.Drawing.Point(12, 35);
            this.labelIpDns.Name = "labelIpDns";
            this.labelIpDns.Size = new System.Drawing.Size(56, 13);
            this.labelIpDns.TabIndex = 1;
            this.labelIpDns.Text = "IP/DNS :";
            // 
            // labelUtilisateur
            // 
            this.labelUtilisateur.AutoSize = true;
            this.labelUtilisateur.Location = new System.Drawing.Point(12, 61);
            this.labelUtilisateur.Name = "labelUtilisateur";
            this.labelUtilisateur.Size = new System.Drawing.Size(63, 13);
            this.labelUtilisateur.TabIndex = 2;
            this.labelUtilisateur.Text = "Utilisateur :";
            // 
            // labelMotDePasse
            // 
            this.labelMotDePasse.AutoSize = true;
            this.labelMotDePasse.Location = new System.Drawing.Point(12, 87);
            this.labelMotDePasse.Name = "labelMotDePasse";
            this.labelMotDePasse.Size = new System.Drawing.Size(80, 13);
            this.labelMotDePasse.TabIndex = 3;
            this.labelMotDePasse.Text = "Mot de passe :";
            // 
            // textBoxNomRds
            // 
            this.textBoxNomRds.Location = new System.Drawing.Point(100, 6);
            this.textBoxNomRds.Name = "textBoxNomRds";
            this.textBoxNomRds.Size = new System.Drawing.Size(200, 20);
            this.textBoxNomRds.TabIndex = 4;
            // 
            // textBoxIpDns
            // 
            this.textBoxIpDns.Location = new System.Drawing.Point(100, 32);
            this.textBoxIpDns.Name = "textBoxIpDns";
            this.textBoxIpDns.Size = new System.Drawing.Size(200, 20);
            this.textBoxIpDns.TabIndex = 5;
            // 
            // textBoxUtilisateur
            // 
            this.textBoxUtilisateur.Location = new System.Drawing.Point(100, 58);
            this.textBoxUtilisateur.Name = "textBoxUtilisateur";
            this.textBoxUtilisateur.Size = new System.Drawing.Size(200, 20);
            this.textBoxUtilisateur.TabIndex = 6;
            // 
            // textBoxMotDePasse
            // 
            this.textBoxMotDePasse.Location = new System.Drawing.Point(100, 84);
            this.textBoxMotDePasse.Name = "textBoxMotDePasse";
            this.textBoxMotDePasse.PasswordChar = '*';
            this.textBoxMotDePasse.Size = new System.Drawing.Size(200, 20);
            this.textBoxMotDePasse.TabIndex = 7;
            // 
            // buttonEnregistrer
            // 
            this.buttonEnregistrer.Location = new System.Drawing.Point(100, 110);
            this.buttonEnregistrer.Name = "buttonEnregistrer";
            this.buttonEnregistrer.Size = new System.Drawing.Size(75, 23);
            this.buttonEnregistrer.TabIndex = 8;
            this.buttonEnregistrer.Text = "Enregistrer";
            this.buttonEnregistrer.UseVisualStyleBackColor = true;
            this.buttonEnregistrer.Click += new System.EventHandler(this.ButtonEnregistrer_Click); // Evénement lié
            // 
            // SaisieRdsForm
            // 
            this.ClientSize = new System.Drawing.Size(320, 150);
            this.Controls.Add(this.buttonEnregistrer);
            this.Controls.Add(this.textBoxMotDePasse);
            this.Controls.Add(this.textBoxUtilisateur);
            this.Controls.Add(this.textBoxIpDns);
            this.Controls.Add(this.textBoxNomRds);
            this.Controls.Add(this.labelMotDePasse);
            this.Controls.Add(this.labelUtilisateur);
            this.Controls.Add(this.labelIpDns);
            this.Controls.Add(this.labelNomRds);
            this.Name = "SaisieRdsForm";
            this.Text = "Saisie - Compte de connexion";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label labelNomRds;
        private System.Windows.Forms.Label labelIpDns;
        private System.Windows.Forms.Label labelUtilisateur;
        private System.Windows.Forms.Label labelMotDePasse;
        private System.Windows.Forms.TextBox textBoxNomRds;
        private System.Windows.Forms.TextBox textBoxIpDns;
        private System.Windows.Forms.TextBox textBoxUtilisateur;
        private System.Windows.Forms.TextBox textBoxMotDePasse;
        private System.Windows.Forms.Button buttonEnregistrer;
    }
}
