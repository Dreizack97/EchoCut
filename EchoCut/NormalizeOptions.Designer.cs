namespace EchoCut
{
    partial class NormalizeOptions
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NormalizeOptions));
            lblIntro = new Label();
            chkAll = new CheckBox();
            lstFields = new CheckedListBox();
            lblWarning = new Label();
            btnNormalize = new Button();
            btnCancel = new Button();
            SuspendLayout();
            // 
            // lblIntro
            // 
            lblIntro.Location = new Point(12, 12);
            lblIntro.Name = "lblIntro";
            lblIntro.Size = new Size(396, 57);
            lblIntro.TabIndex = 0;
            lblIntro.Text = "Elige qué propiedades se normalizarán: se quitan los acentos (la «ñ» se conserva) y cada palabra empieza con mayúscula.";
            // 
            // chkAll
            // 
            chkAll.AutoCheck = false;
            chkAll.AutoSize = true;
            chkAll.Location = new Point(15, 76);
            chkAll.Name = "chkAll";
            chkAll.Size = new Size(130, 23);
            chkAll.TabIndex = 1;
            chkAll.Text = "Seleccionar todo";
            chkAll.UseVisualStyleBackColor = true;
            chkAll.Click += chkAll_Click;
            // 
            // lstFields
            // 
            lstFields.CheckOnClick = true;
            lstFields.FormattingEnabled = true;
            lstFields.IntegralHeight = false;
            lstFields.Location = new Point(12, 104);
            lstFields.Name = "lstFields";
            lstFields.Size = new Size(396, 208);
            lstFields.TabIndex = 2;
            lstFields.ItemCheck += lstFields_ItemCheck;
            // 
            // lblWarning
            // 
            lblWarning.ForeColor = Color.FromArgb(163, 18, 18);
            lblWarning.Location = new Point(12, 322);
            lblWarning.Name = "lblWarning";
            lblWarning.Size = new Size(396, 40);
            lblWarning.TabIndex = 3;
            lblWarning.Text = "⚠ Los archivos originales se modifican directamente en disco y el cambio no se puede deshacer.";
            // 
            // btnNormalize
            // 
            btnNormalize.AutoSize = true;
            btnNormalize.DialogResult = DialogResult.OK;
            btnNormalize.Location = new Point(222, 370);
            btnNormalize.Name = "btnNormalize";
            btnNormalize.Size = new Size(90, 29);
            btnNormalize.TabIndex = 4;
            btnNormalize.Text = "Normalizar";
            btnNormalize.UseVisualStyleBackColor = true;
            // 
            // btnCancel
            // 
            btnCancel.AutoSize = true;
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new Point(318, 370);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(90, 29);
            btnCancel.TabIndex = 5;
            btnCancel.Text = "Cancelar";
            btnCancel.UseVisualStyleBackColor = true;
            // 
            // NormalizeOptions
            // 
            AcceptButton = btnNormalize;
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnCancel;
            ClientSize = new Size(420, 411);
            Controls.Add(btnCancel);
            Controls.Add(btnNormalize);
            Controls.Add(lblWarning);
            Controls.Add(lstFields);
            Controls.Add(chkAll);
            Controls.Add(lblIntro);
            Font = new Font("Segoe UI", 10F);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "NormalizeOptions";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Normalizar canciones";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lblIntro;
        private CheckBox chkAll;
        private CheckedListBox lstFields;
        private Label lblWarning;
        private Button btnNormalize;
        private Button btnCancel;
    }
}
