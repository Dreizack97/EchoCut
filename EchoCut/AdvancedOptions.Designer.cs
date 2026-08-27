namespace EchoCut
{
    partial class AdvancedOptions
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AdvancedOptions));
            propertyGrid = new PropertyGrid();
            lblHelp = new Label();
            btnDefaults = new Button();
            btnAccept = new Button();
            btnCancel = new Button();
            SuspendLayout();
            // 
            // propertyGrid
            // 
            propertyGrid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            propertyGrid.BackColor = SystemColors.Control;
            propertyGrid.Location = new Point(12, 12);
            propertyGrid.Name = "propertyGrid";
            propertyGrid.PropertySort = PropertySort.Categorized;
            propertyGrid.Size = new Size(460, 470);
            propertyGrid.TabIndex = 0;
            propertyGrid.ToolbarVisible = false;
            // 
            // lblHelp
            // 
            lblHelp.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            lblHelp.Location = new Point(12, 490);
            lblHelp.Name = "lblHelp";
            lblHelp.Size = new Size(460, 38);
            lblHelp.TabIndex = 1;
            lblHelp.Text = "Selecciona un parámetro para ver qué hace. Cambiarlos altera el resultado del análisis: si algo deja de detectarse, vuelve a los valores por defecto.";
            // 
            // btnDefaults
            // 
            btnDefaults.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnDefaults.AutoSize = true;
            btnDefaults.Location = new Point(12, 537);
            btnDefaults.Name = "btnDefaults";
            btnDefaults.Size = new Size(150, 29);
            btnDefaults.TabIndex = 2;
            btnDefaults.Text = "Valores por defecto";
            btnDefaults.UseVisualStyleBackColor = true;
            btnDefaults.Click += btnDefaults_Click;
            // 
            // btnAccept
            // 
            btnAccept.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnAccept.AutoSize = true;
            btnAccept.DialogResult = DialogResult.OK;
            btnAccept.Location = new Point(286, 537);
            btnAccept.Name = "btnAccept";
            btnAccept.Size = new Size(90, 29);
            btnAccept.TabIndex = 3;
            btnAccept.Text = "Aceptar";
            btnAccept.UseVisualStyleBackColor = true;
            btnAccept.Click += btnAccept_Click;
            // 
            // btnCancel
            // 
            btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnCancel.AutoSize = true;
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new Point(382, 537);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(90, 29);
            btnCancel.TabIndex = 4;
            btnCancel.Text = "Cancelar";
            btnCancel.UseVisualStyleBackColor = true;
            // 
            // AdvancedOptions
            // 
            AcceptButton = btnAccept;
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnCancel;
            ClientSize = new Size(484, 578);
            Controls.Add(btnCancel);
            Controls.Add(btnAccept);
            Controls.Add(btnDefaults);
            Controls.Add(lblHelp);
            Controls.Add(propertyGrid);
            Font = new Font("Segoe UI", 10F);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MinimizeBox = false;
            MinimumSize = new Size(440, 480);
            Name = "AdvancedOptions";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Parámetros avanzados";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private PropertyGrid propertyGrid;
        private Label lblHelp;
        private Button btnDefaults;
        private Button btnAccept;
        private Button btnCancel;
    }
}
