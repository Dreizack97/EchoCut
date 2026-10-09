namespace EchoCut
{
    partial class AddMetadataDialog
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AddMetadataDialog));
            lblIntro = new Label();
            lblArtist = new Label();
            txtArtist = new TextBox();
            lblTitle = new Label();
            txtTitle = new TextBox();
            chkTitleFromFile = new CheckBox();
            lblAlbum = new Label();
            txtAlbum = new TextBox();
            lblGenre = new Label();
            txtGenre = new TextBox();
            lblComment = new Label();
            txtComment = new TextBox();
            lblHint = new Label();
            lblWarning = new Label();
            btnApply = new Button();
            btnCancel = new Button();
            SuspendLayout();
            // 
            // lblIntro
            // 
            lblIntro.Location = new Point(12, 12);
            lblIntro.Name = "lblIntro";
            lblIntro.Size = new Size(408, 60);
            lblIntro.TabIndex = 0;
            lblIntro.Text = "Los valores se aplicarán a las canciones cargadas y reemplazarán los que ya tengan. Deja en blanco lo que no quieras cambiar.";
            // 
            // lblArtist
            // 
            lblArtist.AutoSize = true;
            lblArtist.Location = new Point(12, 87);
            lblArtist.Name = "lblArtist";
            lblArtist.Size = new Size(51, 19);
            lblArtist.TabIndex = 1;
            lblArtist.Text = "&Artista:";
            // 
            // txtArtist
            // 
            txtArtist.Location = new Point(110, 84);
            txtArtist.Name = "txtArtist";
            txtArtist.Size = new Size(310, 25);
            txtArtist.TabIndex = 2;
            txtArtist.TextChanged += Field_Changed;
            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.Location = new Point(12, 121);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(43, 19);
            lblTitle.TabIndex = 3;
            lblTitle.Text = "&Título:";
            // 
            // txtTitle
            // 
            txtTitle.Location = new Point(110, 118);
            txtTitle.Name = "txtTitle";
            txtTitle.Size = new Size(310, 25);
            txtTitle.TabIndex = 4;
            txtTitle.TextChanged += Field_Changed;
            // 
            // chkTitleFromFile
            // 
            chkTitleFromFile.AutoSize = true;
            chkTitleFromFile.Location = new Point(110, 148);
            chkTitleFromFile.Name = "chkTitleFromFile";
            chkTitleFromFile.Size = new Size(296, 23);
            chkTitleFromFile.TabIndex = 5;
            chkTitleFromFile.Text = "&Usar el nombre del archivo de cada canción";
            chkTitleFromFile.UseVisualStyleBackColor = true;
            chkTitleFromFile.CheckedChanged += Field_Changed;
            // 
            // lblAlbum
            // 
            lblAlbum.AutoSize = true;
            lblAlbum.Location = new Point(12, 183);
            lblAlbum.Name = "lblAlbum";
            lblAlbum.Size = new Size(49, 19);
            lblAlbum.TabIndex = 6;
            lblAlbum.Text = "Á&lbum:";
            // 
            // txtAlbum
            // 
            txtAlbum.Location = new Point(110, 180);
            txtAlbum.Name = "txtAlbum";
            txtAlbum.Size = new Size(310, 25);
            txtAlbum.TabIndex = 7;
            txtAlbum.TextChanged += Field_Changed;
            // 
            // lblGenre
            // 
            lblGenre.AutoSize = true;
            lblGenre.Location = new Point(12, 217);
            lblGenre.Name = "lblGenre";
            lblGenre.Size = new Size(54, 19);
            lblGenre.TabIndex = 8;
            lblGenre.Text = "&Género:";
            // 
            // txtGenre
            // 
            txtGenre.Location = new Point(110, 214);
            txtGenre.Name = "txtGenre";
            txtGenre.Size = new Size(310, 25);
            txtGenre.TabIndex = 9;
            txtGenre.TextChanged += Field_Changed;
            // 
            // lblComment
            // 
            lblComment.AutoSize = true;
            lblComment.Location = new Point(12, 251);
            lblComment.Name = "lblComment";
            lblComment.Size = new Size(86, 19);
            lblComment.TabIndex = 10;
            lblComment.Text = "&Comentarios:";
            // 
            // txtComment
            // 
            txtComment.AcceptsReturn = true;
            txtComment.Location = new Point(110, 248);
            txtComment.Multiline = true;
            txtComment.Name = "txtComment";
            txtComment.ScrollBars = ScrollBars.Vertical;
            txtComment.Size = new Size(310, 52);
            txtComment.TabIndex = 11;
            txtComment.TextChanged += Field_Changed;
            // 
            // lblHint
            // 
            lblHint.ForeColor = Color.FromArgb(77, 77, 77);
            lblHint.Location = new Point(12, 308);
            lblHint.Name = "lblHint";
            lblHint.Size = new Size(408, 21);
            lblHint.TabIndex = 12;
            lblHint.Text = "Para indicar varios artistas o géneros, sepáralos con «;».";
            // 
            // lblWarning
            // 
            lblWarning.ForeColor = Color.FromArgb(163, 18, 18);
            lblWarning.Location = new Point(12, 334);
            lblWarning.Name = "lblWarning";
            lblWarning.Size = new Size(408, 40);
            lblWarning.TabIndex = 13;
            lblWarning.Text = "⚠ Los archivos originales se modifican directamente en disco y el cambio no se puede deshacer.";
            // 
            // btnApply
            // 
            btnApply.AutoSize = true;
            btnApply.DialogResult = DialogResult.OK;
            btnApply.Location = new Point(234, 382);
            btnApply.Name = "btnApply";
            btnApply.Size = new Size(90, 29);
            btnApply.TabIndex = 14;
            btnApply.Text = "Aplicar";
            btnApply.UseVisualStyleBackColor = true;
            // 
            // btnCancel
            // 
            btnCancel.AutoSize = true;
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new Point(330, 382);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(90, 29);
            btnCancel.TabIndex = 15;
            btnCancel.Text = "Cancelar";
            btnCancel.UseVisualStyleBackColor = true;
            // 
            // AddMetadataDialog
            // 
            AcceptButton = btnApply;
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnCancel;
            ClientSize = new Size(432, 423);
            Controls.Add(btnCancel);
            Controls.Add(btnApply);
            Controls.Add(lblWarning);
            Controls.Add(lblHint);
            Controls.Add(txtComment);
            Controls.Add(lblComment);
            Controls.Add(txtGenre);
            Controls.Add(lblGenre);
            Controls.Add(txtAlbum);
            Controls.Add(lblAlbum);
            Controls.Add(chkTitleFromFile);
            Controls.Add(txtTitle);
            Controls.Add(lblTitle);
            Controls.Add(txtArtist);
            Controls.Add(lblArtist);
            Controls.Add(lblIntro);
            Font = new Font("Segoe UI", 10F);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "AddMetadataDialog";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Agregar metadatos";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lblIntro;
        private Label lblArtist;
        private TextBox txtArtist;
        private Label lblTitle;
        private TextBox txtTitle;
        private CheckBox chkTitleFromFile;
        private Label lblAlbum;
        private TextBox txtAlbum;
        private Label lblGenre;
        private TextBox txtGenre;
        private Label lblComment;
        private TextBox txtComment;
        private Label lblHint;
        private Label lblWarning;
        private Button btnApply;
        private Button btnCancel;
    }
}
