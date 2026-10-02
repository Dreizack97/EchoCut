namespace EchoCut
{
    partial class SongPropertiesDialog
    {
        /// <summary>
        /// Variable del diseñador requerida.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Limpiar los recursos que se estén utilizando.
        /// </summary>
        /// <param name="disposing">true si los recursos administrados se deben desechar; false en caso contrario.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Código generado por el Diseñador de Windows Forms

        /// <summary>
        /// Método necesario para admitir el Diseñador. No se puede modificar
        /// el contenido de este método con el editor de código.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SongPropertiesDialog));
            tabControl = new TabControl();
            tabGeneral = new TabPage();
            lblAccessedValue = new Label();
            lblAccessedHeader = new Label();
            lblModifiedValue = new Label();
            lblModifiedHeader = new Label();
            lblCreatedValue = new Label();
            lblCreatedHeader = new Label();
            sep3 = new Label();
            lblSizeValue = new Label();
            lblSizeHeader = new Label();
            txtLocation = new TextBox();
            lblLocationHeader = new Label();
            sep2 = new Label();
            lblOpensWithValue = new Label();
            lblOpensWithHeader = new Label();
            lblTypeValue = new Label();
            lblTypeHeader = new Label();
            sep1 = new Label();
            lblExtension = new Label();
            txtName = new TextBox();
            picIcon = new PictureBox();
            tabDetails = new TabPage();
            propertyGrid = new PropertyGrid();
            btnAccept = new Button();
            btnCancel = new Button();
            btnApply = new Button();
            lnkRemovePersonal = new LinkLabel();
            tabControl.SuspendLayout();
            tabGeneral.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)picIcon).BeginInit();
            tabDetails.SuspendLayout();
            SuspendLayout();
            // 
            // tabControl
            // 
            tabControl.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            tabControl.Controls.Add(tabGeneral);
            tabControl.Controls.Add(tabDetails);
            tabControl.Location = new Point(12, 12);
            tabControl.Name = "tabControl";
            tabControl.SelectedIndex = 0;
            tabControl.Size = new Size(396, 405);
            tabControl.TabIndex = 0;
            // 
            // tabGeneral
            // 
            tabGeneral.BackColor = SystemColors.Window;
            tabGeneral.Controls.Add(lblAccessedValue);
            tabGeneral.Controls.Add(lblAccessedHeader);
            tabGeneral.Controls.Add(lblModifiedValue);
            tabGeneral.Controls.Add(lblModifiedHeader);
            tabGeneral.Controls.Add(lblCreatedValue);
            tabGeneral.Controls.Add(lblCreatedHeader);
            tabGeneral.Controls.Add(sep3);
            tabGeneral.Controls.Add(lblSizeValue);
            tabGeneral.Controls.Add(lblSizeHeader);
            tabGeneral.Controls.Add(txtLocation);
            tabGeneral.Controls.Add(lblLocationHeader);
            tabGeneral.Controls.Add(sep2);
            tabGeneral.Controls.Add(lblOpensWithValue);
            tabGeneral.Controls.Add(lblOpensWithHeader);
            tabGeneral.Controls.Add(lblTypeValue);
            tabGeneral.Controls.Add(lblTypeHeader);
            tabGeneral.Controls.Add(sep1);
            tabGeneral.Controls.Add(lblExtension);
            tabGeneral.Controls.Add(txtName);
            tabGeneral.Controls.Add(picIcon);
            tabGeneral.Location = new Point(4, 24);
            tabGeneral.Name = "tabGeneral";
            tabGeneral.Padding = new Padding(3);
            tabGeneral.Size = new Size(388, 377);
            tabGeneral.TabIndex = 0;
            tabGeneral.Text = "General";
            // 
            // lblAccessedValue
            // 
            lblAccessedValue.AutoSize = true;
            lblAccessedValue.Location = new Point(115, 316);
            lblAccessedValue.Name = "lblAccessedValue";
            lblAccessedValue.Size = new Size(12, 15);
            lblAccessedValue.TabIndex = 19;
            lblAccessedValue.Text = "-";
            // 
            // lblAccessedHeader
            // 
            lblAccessedHeader.AutoSize = true;
            lblAccessedHeader.Location = new Point(15, 316);
            lblAccessedHeader.Name = "lblAccessedHeader";
            lblAccessedHeader.Size = new Size(85, 15);
            lblAccessedHeader.TabIndex = 18;
            lblAccessedHeader.Text = "Último acceso:";
            // 
            // lblModifiedValue
            // 
            lblModifiedValue.AutoSize = true;
            lblModifiedValue.Location = new Point(115, 286);
            lblModifiedValue.Name = "lblModifiedValue";
            lblModifiedValue.Size = new Size(12, 15);
            lblModifiedValue.TabIndex = 17;
            lblModifiedValue.Text = "-";
            // 
            // lblModifiedHeader
            // 
            lblModifiedHeader.AutoSize = true;
            lblModifiedHeader.Location = new Point(15, 286);
            lblModifiedHeader.Name = "lblModifiedHeader";
            lblModifiedHeader.Size = new Size(71, 15);
            lblModifiedHeader.TabIndex = 16;
            lblModifiedHeader.Text = "Modificado:";
            // 
            // lblCreatedValue
            // 
            lblCreatedValue.AutoSize = true;
            lblCreatedValue.Location = new Point(115, 256);
            lblCreatedValue.Name = "lblCreatedValue";
            lblCreatedValue.Size = new Size(12, 15);
            lblCreatedValue.TabIndex = 15;
            lblCreatedValue.Text = "-";
            // 
            // lblCreatedHeader
            // 
            lblCreatedHeader.AutoSize = true;
            lblCreatedHeader.Location = new Point(15, 256);
            lblCreatedHeader.Name = "lblCreatedHeader";
            lblCreatedHeader.Size = new Size(48, 15);
            lblCreatedHeader.TabIndex = 14;
            lblCreatedHeader.Text = "Creado:";
            // 
            // sep3
            // 
            sep3.BorderStyle = BorderStyle.Fixed3D;
            sep3.Location = new Point(15, 236);
            sep3.Name = "sep3";
            sep3.Size = new Size(358, 2);
            sep3.TabIndex = 13;
            // 
            // lblSizeValue
            // 
            lblSizeValue.AutoSize = true;
            lblSizeValue.Location = new Point(115, 204);
            lblSizeValue.Name = "lblSizeValue";
            lblSizeValue.Size = new Size(12, 15);
            lblSizeValue.TabIndex = 12;
            lblSizeValue.Text = "-";
            // 
            // lblSizeHeader
            // 
            lblSizeHeader.AutoSize = true;
            lblSizeHeader.Location = new Point(15, 204);
            lblSizeHeader.Name = "lblSizeHeader";
            lblSizeHeader.Size = new Size(53, 15);
            lblSizeHeader.TabIndex = 11;
            lblSizeHeader.Text = "Tamaño:";
            // 
            // txtLocation
            // 
            txtLocation.BackColor = SystemColors.Window;
            txtLocation.BorderStyle = BorderStyle.None;
            txtLocation.Location = new Point(115, 174);
            txtLocation.Name = "txtLocation";
            txtLocation.ReadOnly = true;
            txtLocation.Size = new Size(255, 16);
            txtLocation.TabIndex = 10;
            // 
            // lblLocationHeader
            // 
            lblLocationHeader.AutoSize = true;
            lblLocationHeader.Location = new Point(15, 174);
            lblLocationHeader.Name = "lblLocationHeader";
            lblLocationHeader.Size = new Size(63, 15);
            lblLocationHeader.TabIndex = 9;
            lblLocationHeader.Text = "Ubicación:";
            // 
            // sep2
            // 
            sep2.BorderStyle = BorderStyle.Fixed3D;
            sep2.Location = new Point(15, 154);
            sep2.Name = "sep2";
            sep2.Size = new Size(358, 2);
            sep2.TabIndex = 8;
            // 
            // lblOpensWithValue
            // 
            lblOpensWithValue.AutoSize = true;
            lblOpensWithValue.Location = new Point(115, 122);
            lblOpensWithValue.Name = "lblOpensWithValue";
            lblOpensWithValue.Size = new Size(130, 15);
            lblOpensWithValue.TabIndex = 7;
            lblOpensWithValue.Text = "Reproductor de música";
            // 
            // lblOpensWithHeader
            // 
            lblOpensWithHeader.AutoSize = true;
            lblOpensWithHeader.Location = new Point(15, 122);
            lblOpensWithHeader.Name = "lblOpensWithHeader";
            lblOpensWithHeader.Size = new Size(71, 15);
            lblOpensWithHeader.TabIndex = 6;
            lblOpensWithHeader.Text = "Se abre con:";
            // 
            // lblTypeValue
            // 
            lblTypeValue.AutoSize = true;
            lblTypeValue.Location = new Point(115, 92);
            lblTypeValue.Name = "lblTypeValue";
            lblTypeValue.Size = new Size(135, 15);
            lblTypeValue.TabIndex = 5;
            lblTypeValue.Text = "Archivo de audio (.mp3)";
            // 
            // lblTypeHeader
            // 
            lblTypeHeader.AutoSize = true;
            lblTypeHeader.Location = new Point(15, 92);
            lblTypeHeader.Name = "lblTypeHeader";
            lblTypeHeader.Size = new Size(92, 15);
            lblTypeHeader.TabIndex = 4;
            lblTypeHeader.Text = "Tipo de archivo:";
            // 
            // sep1
            // 
            sep1.BorderStyle = BorderStyle.Fixed3D;
            sep1.Location = new Point(15, 72);
            sep1.Name = "sep1";
            sep1.Size = new Size(358, 2);
            sep1.TabIndex = 3;
            // 
            // lblExtension
            // 
            lblExtension.AutoSize = true;
            lblExtension.ForeColor = SystemColors.GrayText;
            lblExtension.Location = new Point(342, 27);
            lblExtension.Name = "lblExtension";
            lblExtension.Size = new Size(34, 15);
            lblExtension.TabIndex = 2;
            lblExtension.Text = ".mp3";
            // 
            // txtName
            // 
            txtName.Location = new Point(68, 24);
            txtName.Name = "txtName";
            txtName.Size = new Size(268, 23);
            txtName.TabIndex = 1;
            // 
            // picIcon
            // 
            picIcon.Location = new Point(18, 18);
            picIcon.Name = "picIcon";
            picIcon.Size = new Size(36, 36);
            picIcon.SizeMode = PictureBoxSizeMode.CenterImage;
            picIcon.TabIndex = 0;
            picIcon.TabStop = false;
            // 
            // tabDetails
            // 
            tabDetails.Controls.Add(propertyGrid);
            tabDetails.Location = new Point(4, 24);
            tabDetails.Name = "tabDetails";
            tabDetails.Padding = new Padding(3);
            tabDetails.Size = new Size(388, 377);
            tabDetails.TabIndex = 1;
            tabDetails.Text = "Detalles";
            tabDetails.UseVisualStyleBackColor = true;
            // 
            // propertyGrid
            // 
            propertyGrid.BackColor = SystemColors.Control;
            propertyGrid.Dock = DockStyle.Fill;
            propertyGrid.HelpVisible = false;
            propertyGrid.Location = new Point(3, 3);
            propertyGrid.Name = "propertyGrid";
            propertyGrid.PropertySort = PropertySort.Categorized;
            propertyGrid.Size = new Size(382, 371);
            propertyGrid.TabIndex = 0;
            propertyGrid.ToolbarVisible = false;
            // 
            // btnAccept
            // 
            btnAccept.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnAccept.Location = new Point(164, 452);
            btnAccept.Name = "btnAccept";
            btnAccept.Size = new Size(75, 26);
            btnAccept.TabIndex = 1;
            btnAccept.Text = "Aceptar";
            btnAccept.UseVisualStyleBackColor = true;
            btnAccept.Click += btnAccept_Click;
            // 
            // btnCancel
            // 
            btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new Point(245, 452);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(75, 26);
            btnCancel.TabIndex = 2;
            btnCancel.Text = "Cancelar";
            btnCancel.UseVisualStyleBackColor = true;
            // 
            // btnApply
            // 
            btnApply.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnApply.Enabled = false;
            btnApply.Location = new Point(326, 452);
            btnApply.Name = "btnApply";
            btnApply.Size = new Size(75, 26);
            btnApply.TabIndex = 3;
            btnApply.Text = "Aplicar";
            btnApply.UseVisualStyleBackColor = true;
            btnApply.Click += btnApply_Click;
            // 
            // lnkRemovePersonal
            // 
            lnkRemovePersonal.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            lnkRemovePersonal.AutoSize = true;
            lnkRemovePersonal.Location = new Point(12, 426);
            lnkRemovePersonal.Name = "lnkRemovePersonal";
            lnkRemovePersonal.Size = new Size(233, 15);
            lnkRemovePersonal.TabIndex = 4;
            lnkRemovePersonal.TabStop = true;
            lnkRemovePersonal.Text = "Quitar propiedades e información personal";
            lnkRemovePersonal.LinkClicked += lnkRemovePersonal_LinkClicked;
            // 
            // SongPropertiesDialog
            // 
            AcceptButton = btnAccept;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnCancel;
            ClientSize = new Size(420, 490);
            Controls.Add(lnkRemovePersonal);
            Controls.Add(btnApply);
            Controls.Add(btnCancel);
            Controls.Add(btnAccept);
            Controls.Add(tabControl);
            Font = new Font("Segoe UI", 9F);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "SongPropertiesDialog";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Propiedades de la canción";
            tabControl.ResumeLayout(false);
            tabGeneral.ResumeLayout(false);
            tabGeneral.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)picIcon).EndInit();
            tabDetails.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TabControl tabControl;
        private TabPage tabGeneral;
        private TabPage tabDetails;
        private PictureBox picIcon;
        private TextBox txtName;
        private Label lblExtension;
        private Label sep1;
        private Label lblTypeHeader;
        private Label lblTypeValue;
        private Label lblOpensWithHeader;
        private Label lblOpensWithValue;
        private Label sep2;
        private Label lblLocationHeader;
        private TextBox txtLocation;
        private Label lblSizeHeader;
        private Label lblSizeValue;
        private Label sep3;
        private Label lblCreatedHeader;
        private Label lblCreatedValue;
        private Label lblModifiedHeader;
        private Label lblModifiedValue;
        private Label lblAccessedHeader;
        private Label lblAccessedValue;
        private PropertyGrid propertyGrid;
        private Button btnAccept;
        private Button btnCancel;
        private Button btnApply;
        private LinkLabel lnkRemovePersonal;
    }
}
