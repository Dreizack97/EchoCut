namespace EchoCut
{
    partial class ConvertMp3Dialog
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ConvertMp3Dialog));
            lblIntro = new Label();
            rdoVbrV0 = new RadioButton();
            rdoVbrV2 = new RadioButton();
            rdoCbr320 = new RadioButton();
            rdoCbr192 = new RadioButton();
            lblNote = new Label();
            btnConvert = new Button();
            btnCancel = new Button();
            SuspendLayout();
            // 
            // lblIntro
            // 
            lblIntro.Location = new Point(12, 12);
            lblIntro.Name = "lblIntro";
            lblIntro.Size = new Size(476, 57);
            lblIntro.TabIndex = 0;
            lblIntro.Text = "Se convertirán las canciones a MP3 con la calidad que elijas.";
            // 
            // rdoVbrV0
            // 
            rdoVbrV0.CheckAlign = ContentAlignment.TopLeft;
            rdoVbrV0.Location = new Point(15, 76);
            rdoVbrV0.Name = "rdoVbrV0";
            rdoVbrV0.Size = new Size(473, 46);
            rdoVbrV0.TabIndex = 1;
            rdoVbrV0.TabStop = true;
            rdoVbrV0.Text = "VBR V0";
            rdoVbrV0.TextAlign = ContentAlignment.TopLeft;
            rdoVbrV0.UseVisualStyleBackColor = true;
            // 
            // rdoVbrV2
            // 
            rdoVbrV2.CheckAlign = ContentAlignment.TopLeft;
            rdoVbrV2.Location = new Point(15, 128);
            rdoVbrV2.Name = "rdoVbrV2";
            rdoVbrV2.Size = new Size(473, 46);
            rdoVbrV2.TabIndex = 2;
            rdoVbrV2.Text = "VBR V2";
            rdoVbrV2.TextAlign = ContentAlignment.TopLeft;
            rdoVbrV2.UseVisualStyleBackColor = true;
            // 
            // rdoCbr320
            // 
            rdoCbr320.CheckAlign = ContentAlignment.TopLeft;
            rdoCbr320.Location = new Point(15, 180);
            rdoCbr320.Name = "rdoCbr320";
            rdoCbr320.Size = new Size(473, 46);
            rdoCbr320.TabIndex = 3;
            rdoCbr320.Text = "CBR 320";
            rdoCbr320.TextAlign = ContentAlignment.TopLeft;
            rdoCbr320.UseVisualStyleBackColor = true;
            // 
            // rdoCbr192
            // 
            rdoCbr192.CheckAlign = ContentAlignment.TopLeft;
            rdoCbr192.Location = new Point(15, 232);
            rdoCbr192.Name = "rdoCbr192";
            rdoCbr192.Size = new Size(473, 46);
            rdoCbr192.TabIndex = 4;
            rdoCbr192.Text = "CBR 192";
            rdoCbr192.TextAlign = ContentAlignment.TopLeft;
            rdoCbr192.UseVisualStyleBackColor = true;
            // 
            // lblNote
            // 
            lblNote.ForeColor = SystemColors.GrayText;
            lblNote.Location = new Point(12, 290);
            lblNote.Name = "lblNote";
            lblNote.Size = new Size(476, 44);
            lblNote.TabIndex = 5;
            lblNote.Text = "Las copias se escriben en la subcarpeta «MP3» junto a cada original. Los originales no se modifican.";
            // 
            // btnConvert
            // 
            btnConvert.AutoSize = true;
            btnConvert.DialogResult = DialogResult.OK;
            btnConvert.Location = new Point(302, 346);
            btnConvert.Name = "btnConvert";
            btnConvert.Size = new Size(90, 29);
            btnConvert.TabIndex = 6;
            btnConvert.Text = "Convertir";
            btnConvert.UseVisualStyleBackColor = true;
            // 
            // btnCancel
            // 
            btnCancel.AutoSize = true;
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new Point(398, 346);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(90, 29);
            btnCancel.TabIndex = 7;
            btnCancel.Text = "Cancelar";
            btnCancel.UseVisualStyleBackColor = true;
            // 
            // ConvertMp3Dialog
            // 
            AcceptButton = btnConvert;
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnCancel;
            ClientSize = new Size(500, 387);
            Controls.Add(btnCancel);
            Controls.Add(btnConvert);
            Controls.Add(lblNote);
            Controls.Add(rdoCbr192);
            Controls.Add(rdoCbr320);
            Controls.Add(rdoVbrV2);
            Controls.Add(rdoVbrV0);
            Controls.Add(lblIntro);
            Font = new Font("Segoe UI", 10F);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "ConvertMp3Dialog";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Convertir a MP3";
            ResumeLayout(false);
        }

        #endregion

        private Label lblIntro;
        private RadioButton rdoVbrV0;
        private RadioButton rdoVbrV2;
        private RadioButton rdoCbr320;
        private RadioButton rdoCbr192;
        private Label lblNote;
        private Button btnConvert;
        private Button btnCancel;
    }
}
