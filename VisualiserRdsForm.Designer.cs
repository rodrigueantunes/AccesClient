namespace Accès_client
{
    partial class VisualiserRdsForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.ListView listViewRds;
        private System.Windows.Forms.Button buttonModifier;
        private System.Windows.Forms.Button buttonSupprimer;
        private System.Windows.Forms.Button buttonAjouter;

        /// <summary>
        ///  Nettoyage des ressources utilisées.
        /// </summary>
        /// <param name="disposing">True si les ressources managées doivent être supprimées; sinon, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.listViewRds = new System.Windows.Forms.ListView();
            this.buttonModifier = new System.Windows.Forms.Button();
            this.buttonSupprimer = new System.Windows.Forms.Button();
            this.buttonAjouter = new System.Windows.Forms.Button();
            this.listViewRds.Columns.Add("Description", 100);
            this.listViewRds.Columns.Add("IP/DNS", 150);
            this.listViewRds.Columns.Add("Nom d'utilisateur", 150);
            this.listViewRds.Columns.Add("Date de création", 150);

            // 
            // listViewRds
            // 
            this.listViewRds.Location = new System.Drawing.Point(12, 12);
            this.listViewRds.Name = "listViewRds";
            this.listViewRds.Size = new System.Drawing.Size(540, 200);
            this.listViewRds.TabIndex = 0;
            this.listViewRds.UseCompatibleStateImageBehavior = false;
            this.listViewRds.View = System.Windows.Forms.View.Details;

            // 
            // buttonModifier
            // 
            this.buttonModifier.Location = new System.Drawing.Point(116, 218);
            this.buttonModifier.Name = "buttonModifier";
            this.buttonModifier.Size = new System.Drawing.Size(75, 23);
            this.buttonModifier.TabIndex = 1;
            this.buttonModifier.Text = "Modifier";
            this.buttonModifier.UseVisualStyleBackColor = true;
            this.buttonModifier.Click += new System.EventHandler(this.ButtonModifier_Click);

            // 
            // buttonSupprimer
            // 
            this.buttonSupprimer.Location = new System.Drawing.Point(197, 218);
            this.buttonSupprimer.Name = "buttonSupprimer";
            this.buttonSupprimer.Size = new System.Drawing.Size(75, 23);
            this.buttonSupprimer.TabIndex = 2;
            this.buttonSupprimer.Text = "Supprimer";
            this.buttonSupprimer.UseVisualStyleBackColor = true;
            this.buttonSupprimer.Click += new System.EventHandler(this.ButtonSupprimer_Click);

            // 
            // buttonAjouter
            // 
            this.buttonAjouter.Location = new System.Drawing.Point(35, 218);
            this.buttonAjouter.Name = "buttonAjouter";
            this.buttonAjouter.Size = new System.Drawing.Size(75, 23);
            this.buttonAjouter.TabIndex = 3;
            this.buttonAjouter.Text = "Ajouter";
            this.buttonAjouter.UseVisualStyleBackColor = true;
            this.buttonAjouter.Click += new System.EventHandler(this.ButtonAjouter_Click);

            // 
            // VisualiserRdsForm
            // 
            this.ClientSize = new System.Drawing.Size(567, 261);
            this.Controls.Add(this.buttonAjouter);
            this.Controls.Add(this.buttonSupprimer);
            this.Controls.Add(this.buttonModifier);
            this.Controls.Add(this.listViewRds);
            this.Name = "VisualiserRdsForm";
            this.Text = "Visualiser Compte de connexion";
            this.ResumeLayout(false);
        }
    }
}
