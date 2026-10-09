namespace EchoCut
{
    partial class RenameSongsDialog
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RenameSongsDialog));
            lblIntro = new Label();
            pnlSequence = new Panel();
            lblStart = new Label();
            numStart = new NumericUpDown();
            lblDigits = new Label();
            numDigits = new NumericUpDown();
            lblSeparator = new Label();
            txtSeparator = new TextBox();
            pnlRemove = new Panel();
            lblCount = new Label();
            numCount = new NumericUpDown();
            chkTrim = new CheckBox();
            lstPreview = new ListView();
            colOld = new ColumnHeader();
            colNew = new ColumnHeader();
            colNote = new ColumnHeader();
            lblProblem = new Label();
            btnRename = new Button();
            btnCancel = new Button();
            pnlSequence.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numStart).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numDigits).BeginInit();
            pnlRemove.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numCount).BeginInit();
            SuspendLayout();
            // 
            // lblIntro
            // 
            lblIntro.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            lblIntro.Location = new Point(12, 12);
            lblIntro.Name = "lblIntro";
            lblIntro.Size = new Size(676, 40);
            lblIntro.TabIndex = 0;
            lblIntro.Text = "Se renombrarán las canciones del listado.";
            // 
            // pnlSequence
            // 
            pnlSequence.Controls.Add(lblStart);
            pnlSequence.Controls.Add(numStart);
            pnlSequence.Controls.Add(lblDigits);
            pnlSequence.Controls.Add(numDigits);
            pnlSequence.Controls.Add(lblSeparator);
            pnlSequence.Controls.Add(txtSeparator);
            pnlSequence.Location = new Point(12, 58);
            pnlSequence.Name = "pnlSequence";
            pnlSequence.Size = new Size(676, 62);
            pnlSequence.TabIndex = 1;
            // 
            // lblStart
            // 
            lblStart.AutoSize = true;
            lblStart.Location = new Point(0, 6);
            lblStart.Name = "lblStart";
            lblStart.Size = new Size(104, 19);
            lblStart.TabIndex = 0;
            lblStart.Text = "&Número inicial:";
            // 
            // numStart
            // 
            numStart.Location = new Point(110, 3);
            numStart.Maximum = new decimal(new int[] { 999999, 0, 0, 0 });
            numStart.Name = "numStart";
            numStart.Size = new Size(80, 25);
            numStart.TabIndex = 1;
            numStart.Value = new decimal(new int[] { 1, 0, 0, 0 });
            numStart.ValueChanged += Option_Changed;
            // 
            // lblDigits
            // 
            lblDigits.AutoSize = true;
            lblDigits.Location = new Point(212, 6);
            lblDigits.Name = "lblDigits";
            lblDigits.Size = new Size(55, 19);
            lblDigits.TabIndex = 2;
            lblDigits.Text = "&Dígitos:";
            // 
            // numDigits
            // 
            numDigits.Location = new Point(273, 3);
            numDigits.Maximum = new decimal(new int[] { 9, 0, 0, 0 });
            numDigits.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numDigits.Name = "numDigits";
            numDigits.Size = new Size(50, 25);
            numDigits.TabIndex = 3;
            numDigits.Value = new decimal(new int[] { 4, 0, 0, 0 });
            numDigits.ValueChanged += Option_Changed;
            // 
            // lblSeparator
            // 
            lblSeparator.AutoSize = true;
            lblSeparator.Location = new Point(345, 6);
            lblSeparator.Name = "lblSeparator";
            lblSeparator.Size = new Size(74, 19);
            lblSeparator.TabIndex = 4;
            lblSeparator.Text = "&Separador:";
            // 
            // txtSeparator
            // 
            txtSeparator.Location = new Point(425, 3);
            txtSeparator.Name = "txtSeparator";
            txtSeparator.Size = new Size(80, 25);
            txtSeparator.TabIndex = 5;
            txtSeparator.TextChanged += Option_Changed;
            // 
            // pnlRemove
            // 
            pnlRemove.Controls.Add(lblCount);
            pnlRemove.Controls.Add(numCount);
            pnlRemove.Controls.Add(chkTrim);
            pnlRemove.Location = new Point(12, 58);
            pnlRemove.Name = "pnlRemove";
            pnlRemove.Size = new Size(676, 62);
            pnlRemove.TabIndex = 2;
            // 
            // lblCount
            // 
            lblCount.AutoSize = true;
            lblCount.Location = new Point(0, 6);
            lblCount.Name = "lblCount";
            lblCount.Size = new Size(134, 19);
            lblCount.TabIndex = 0;
            lblCount.Text = "&Caracteres a quitar:";
            // 
            // numCount
            // 
            numCount.Location = new Point(140, 3);
            numCount.Maximum = new decimal(new int[] { 255, 0, 0, 0 });
            numCount.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numCount.Name = "numCount";
            numCount.Size = new Size(60, 25);
            numCount.TabIndex = 1;
            numCount.Value = new decimal(new int[] { 1, 0, 0, 0 });
            numCount.ValueChanged += Option_Changed;
            // 
            // chkTrim
            // 
            chkTrim.AutoSize = true;
            chkTrim.Checked = true;
            chkTrim.CheckState = CheckState.Checked;
            chkTrim.Location = new Point(0, 36);
            chkTrim.Name = "chkTrim";
            chkTrim.Size = new Size(437, 23);
            chkTrim.TabIndex = 2;
            chkTrim.Text = "&Quitar también los espacios y separadores que queden al principio";
            chkTrim.UseVisualStyleBackColor = true;
            chkTrim.CheckedChanged += Option_Changed;
            // 
            // lstPreview
            // 
            lstPreview.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            lstPreview.Columns.AddRange(new ColumnHeader[] { colOld, colNew, colNote });
            lstPreview.FullRowSelect = true;
            lstPreview.GridLines = true;
            lstPreview.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            lstPreview.Location = new Point(12, 126);
            lstPreview.Name = "lstPreview";
            lstPreview.Size = new Size(676, 290);
            lstPreview.TabIndex = 3;
            lstPreview.UseCompatibleStateImageBehavior = false;
            lstPreview.View = View.Details;
            // 
            // colOld
            // 
            colOld.Text = "Nombre actual";
            colOld.Width = 230;
            // 
            // colNew
            // 
            colNew.Text = "Nombre nuevo";
            colNew.Width = 230;
            // 
            // colNote
            // 
            colNote.Text = "Aviso";
            colNote.Width = 200;
            // 
            // lblProblem
            // 
            lblProblem.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            lblProblem.ForeColor = Color.FromArgb(163, 18, 18);
            lblProblem.Location = new Point(12, 422);
            lblProblem.Name = "lblProblem";
            lblProblem.Size = new Size(676, 21);
            lblProblem.TabIndex = 4;
            // 
            // btnRename
            // 
            btnRename.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnRename.AutoSize = true;
            btnRename.DialogResult = DialogResult.OK;
            btnRename.Location = new Point(502, 450);
            btnRename.Name = "btnRename";
            btnRename.Size = new Size(90, 29);
            btnRename.TabIndex = 5;
            btnRename.Text = "Renombrar";
            btnRename.UseVisualStyleBackColor = true;
            // 
            // btnCancel
            // 
            btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnCancel.AutoSize = true;
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new Point(598, 450);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(90, 29);
            btnCancel.TabIndex = 6;
            btnCancel.Text = "Cancelar";
            btnCancel.UseVisualStyleBackColor = true;
            // 
            // RenameSongsDialog
            // 
            AcceptButton = btnRename;
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnCancel;
            ClientSize = new Size(700, 491);
            Controls.Add(btnCancel);
            Controls.Add(btnRename);
            Controls.Add(lblProblem);
            Controls.Add(lstPreview);
            Controls.Add(pnlRemove);
            Controls.Add(pnlSequence);
            Controls.Add(lblIntro);
            Font = new Font("Segoe UI", 10F);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MinimizeBox = false;
            MinimumSize = new Size(560, 400);
            Name = "RenameSongsDialog";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Renombrar canciones";
            pnlSequence.ResumeLayout(false);
            pnlSequence.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numStart).EndInit();
            ((System.ComponentModel.ISupportInitialize)numDigits).EndInit();
            pnlRemove.ResumeLayout(false);
            pnlRemove.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numCount).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Label lblIntro;
        private Panel pnlSequence;
        private Label lblStart;
        private NumericUpDown numStart;
        private Label lblDigits;
        private NumericUpDown numDigits;
        private Label lblSeparator;
        private TextBox txtSeparator;
        private Panel pnlRemove;
        private Label lblCount;
        private NumericUpDown numCount;
        private CheckBox chkTrim;
        private ListView lstPreview;
        private ColumnHeader colOld;
        private ColumnHeader colNew;
        private ColumnHeader colNote;
        private Label lblProblem;
        private Button btnRename;
        private Button btnCancel;
    }
}
