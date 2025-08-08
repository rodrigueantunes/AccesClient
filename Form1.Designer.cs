namespace Accès_client
{
    partial class Form1
    {
        /// <summary>
        /// Nettoyage des ressources utilisées.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private System.ComponentModel.IContainer components = null;

        #region Code généré par le Concepteur Windows Form
        private void InitializeComponent()
        {
            flowLayoutPanelClients = new FlowLayoutPanel();
            pictureBox4 = new PictureBox();
            flowLayoutPanelAny = new FlowLayoutPanel();
            flowLayoutPanelRDS = new FlowLayoutPanel();
            flowLayoutPanelVPN = new FlowLayoutPanel();
            labelAny = new Label();
            labelRDS = new Label();
            labelVPN = new Label();
            pictureBox1 = new PictureBox();
            pictureBox2 = new PictureBox();
            pictureBox3 = new PictureBox();
            visualiserRdsButton = new Button();
            ((System.ComponentModel.ISupportInitialize)pictureBox4).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox2).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox3).BeginInit();
            SuspendLayout();
            // 
            // flowLayoutPanelClients
            // 
            flowLayoutPanelClients.Location = new Point(0, 131);
            flowLayoutPanelClients.Name = "flowLayoutPanelClients";
            flowLayoutPanelClients.Size = new Size(214, 321);
            flowLayoutPanelClients.TabIndex = 0;
            // 
            // pictureBox4
            // 
            pictureBox4.BackgroundImage = Properties.Resources.volume_software_sans_detour_h80;
            pictureBox4.BackgroundImageLayout = ImageLayout.Zoom;
            pictureBox4.Location = new Point(0, 0);
            pictureBox4.Name = "pictureBox4";
            pictureBox4.Size = new Size(214, 123);
            pictureBox4.TabIndex = 10;
            pictureBox4.TabStop = false;
            // 
            // flowLayoutPanelAny
            // 
            flowLayoutPanelAny.AutoScroll = true;
            flowLayoutPanelAny.BackColor = SystemColors.ActiveCaption;
            flowLayoutPanelAny.BackgroundImageLayout = ImageLayout.Zoom;
            flowLayoutPanelAny.BorderStyle = BorderStyle.FixedSingle;
            flowLayoutPanelAny.Location = new Point(220, 147);
            flowLayoutPanelAny.Name = "flowLayoutPanelAny";
            flowLayoutPanelAny.Size = new Size(200, 363);
            flowLayoutPanelAny.TabIndex = 2;
            // 
            // flowLayoutPanelRDS
            // 
            flowLayoutPanelRDS.AutoScroll = true;
            flowLayoutPanelRDS.BackColor = SystemColors.ActiveCaption;
            flowLayoutPanelRDS.BackgroundImageLayout = ImageLayout.Zoom;
            flowLayoutPanelRDS.BorderStyle = BorderStyle.FixedSingle;
            flowLayoutPanelRDS.Location = new Point(430, 147);
            flowLayoutPanelRDS.Name = "flowLayoutPanelRDS";
            flowLayoutPanelRDS.Size = new Size(200, 363);
            flowLayoutPanelRDS.TabIndex = 4;
            // 
            // flowLayoutPanelVPN
            // 
            flowLayoutPanelVPN.AutoScroll = true;
            flowLayoutPanelVPN.BackColor = SystemColors.ActiveCaption;
            flowLayoutPanelVPN.BackgroundImageLayout = ImageLayout.Zoom;
            flowLayoutPanelVPN.BorderStyle = BorderStyle.FixedSingle;
            flowLayoutPanelVPN.Location = new Point(640, 147);
            flowLayoutPanelVPN.Name = "flowLayoutPanelVPN";
            flowLayoutPanelVPN.Size = new Size(200, 363);
            flowLayoutPanelVPN.TabIndex = 6;
            // 
            // labelAny
            // 
            labelAny.AutoSize = true;
            labelAny.Font = new Font("Arial", 10F, FontStyle.Bold);
            labelAny.Location = new Point(220, 30);
            labelAny.Name = "labelAny";
            labelAny.Size = new Size(66, 16);
            labelAny.TabIndex = 1;
            labelAny.Text = "Anydesk";
            // 
            // labelRDS
            // 
            labelRDS.AutoSize = true;
            labelRDS.Font = new Font("Arial", 10F, FontStyle.Bold);
            labelRDS.Location = new Point(430, 30);
            labelRDS.Name = "labelRDS";
            labelRDS.Size = new Size(134, 16);
            labelRDS.TabIndex = 3;
            labelRDS.Text = "Bureau à distance";
            // 
            // labelVPN
            // 
            labelVPN.AutoSize = true;
            labelVPN.Font = new Font("Arial", 10F, FontStyle.Bold);
            labelVPN.Location = new Point(640, 30);
            labelVPN.Name = "labelVPN";
            labelVPN.Size = new Size(35, 16);
            labelVPN.TabIndex = 5;
            labelVPN.Text = "VPN";
            // 
            // pictureBox1
            // 
            pictureBox1.BackgroundImage = Properties.Resources.any_bg;
            pictureBox1.BackgroundImageLayout = ImageLayout.Stretch;
            pictureBox1.Location = new Point(220, 49);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(200, 92);
            pictureBox1.TabIndex = 7;
            pictureBox1.TabStop = false;
            // 
            // pictureBox2
            // 
            pictureBox2.BackgroundImage = Properties.Resources.rds_bg;
            pictureBox2.BackgroundImageLayout = ImageLayout.Stretch;
            pictureBox2.Location = new Point(430, 49);
            pictureBox2.Name = "pictureBox2";
            pictureBox2.Size = new Size(200, 92);
            pictureBox2.TabIndex = 8;
            pictureBox2.TabStop = false;
            // 
            // pictureBox3
            // 
            pictureBox3.BackgroundImage = Properties.Resources.vpn_bg;
            pictureBox3.BackgroundImageLayout = ImageLayout.Stretch;
            pictureBox3.Location = new Point(640, 49);
            pictureBox3.Name = "pictureBox3";
            pictureBox3.Size = new Size(200, 92);
            pictureBox3.TabIndex = 9;
            pictureBox3.TabStop = false;
            // 
            // visualiserRdsButton
            // 
            visualiserRdsButton.BackColor = SystemColors.Info;
            visualiserRdsButton.Location = new Point(0, 458);
            visualiserRdsButton.Name = "visualiserRdsButton";
            visualiserRdsButton.Size = new Size(214, 40);
            visualiserRdsButton.TabIndex = 13;
            visualiserRdsButton.Text = "Gestion Connexion";
            visualiserRdsButton.UseVisualStyleBackColor = false;
            visualiserRdsButton.Click += VisualiserRdsButton_Click;
            // 
            // Form1
            // 
            ClientSize = new Size(860, 550);
            Controls.Add(pictureBox4);
            Controls.Add(pictureBox3);
            Controls.Add(pictureBox2);
            Controls.Add(pictureBox1);
            Controls.Add(flowLayoutPanelClients);
            Controls.Add(labelAny);
            Controls.Add(flowLayoutPanelAny);
            Controls.Add(labelRDS);
            Controls.Add(flowLayoutPanelRDS);
            Controls.Add(labelVPN);
            Controls.Add(flowLayoutPanelVPN);
            Controls.Add(visualiserRdsButton);
            Name = "Form1";
            Text = "Accès Client - Antunes Rodrigue / VSW";
            Load += Form1_Load;
            ((System.ComponentModel.ISupportInitialize)pictureBox4).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox2).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox3).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }
        #endregion

        private FlowLayoutPanel flowLayoutPanelClients;
        private FlowLayoutPanel flowLayoutPanelAny;
        private FlowLayoutPanel flowLayoutPanelRDS;
        private FlowLayoutPanel flowLayoutPanelVPN;
        private Label labelAny;
        private Label labelRDS;
        private Label labelVPN;
        private PictureBox pictureBox1;
        private PictureBox pictureBox2;
        private PictureBox pictureBox3;
        private PictureBox pictureBox4;
        private Button visualiserRdsButton;  // Nouveau bouton

    }
}
