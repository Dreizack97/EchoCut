namespace EchoCut
{
    partial class ShortcutsDialog
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ShortcutsDialog));
            lblIntro = new Label();
            lstShortcuts = new ListView();
            colAction = new ColumnHeader();
            colKeys = new ColumnHeader();
            btnClose = new Button();
            SuspendLayout();
            // 
            // lblIntro
            // 
            lblIntro.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            lblIntro.Location = new Point(12, 12);
            lblIntro.Name = "lblIntro";
            lblIntro.Size = new Size(536, 40);
            lblIntro.TabIndex = 0;
            lblIntro.Text = "Los atajos de la rejilla funcionan con la lista de canciones seleccionada. Al pasar el ratón por un botón o una opción de menú, la barra de estado muestra qué hace y su atajo.";
            // 
            // lstShortcuts
            // 
            lstShortcuts.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            lstShortcuts.Columns.AddRange(new ColumnHeader[] { colAction, colKeys });
            lstShortcuts.FullRowSelect = true;
            lstShortcuts.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            lstShortcuts.Location = new Point(12, 58);
            lstShortcuts.MultiSelect = false;
            lstShortcuts.Name = "lstShortcuts";
            lstShortcuts.Size = new Size(536, 442);
            lstShortcuts.TabIndex = 1;
            lstShortcuts.UseCompatibleStateImageBehavior = false;
            lstShortcuts.View = View.Details;
            // 
            // colAction
            // 
            colAction.Text = "Acción";
            colAction.Width = 360;
            // 
            // colKeys
            // 
            colKeys.Text = "Atajo";
            colKeys.Width = 150;
            // 
            // btnClose
            // 
            btnClose.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnClose.AutoSize = true;
            btnClose.DialogResult = DialogResult.Cancel;
            btnClose.Location = new Point(458, 510);
            btnClose.Name = "btnClose";
            btnClose.Size = new Size(90, 29);
            btnClose.TabIndex = 2;
            btnClose.Text = "Cerrar";
            btnClose.UseVisualStyleBackColor = true;
            // 
            // ShortcutsDialog
            // 
            AcceptButton = btnClose;
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnClose;
            ClientSize = new Size(560, 551);
            Controls.Add(btnClose);
            Controls.Add(lstShortcuts);
            Controls.Add(lblIntro);
            Font = new Font("Segoe UI", 10F);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MinimizeBox = false;
            MinimumSize = new Size(460, 400);
            Name = "ShortcutsDialog";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Atajos de teclado";
            ResumeLayout(false);
        }

        #endregion

        private Label lblIntro;
        private ListView lstShortcuts;
        private ColumnHeader colAction;
        private ColumnHeader colKeys;
        private Button btnClose;
    }
}
