namespace EchoCut
{
    partial class VolumeDialog
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(VolumeDialog));
            toolStrip = new ToolStrip();
            btnAnalyze = new ToolStripButton();
            btnApply = new ToolStripButton();
            btnRestore = new ToolStripButton();
            btnStop = new ToolStripButton();
            sepCheck = new ToolStripSeparator();
            btnCheckAll = new ToolStripButton();
            btnCheckNone = new ToolStripButton();
            pnlTarget = new Panel();
            lblTarget = new Label();
            numTarget = new NumericUpDown();
            lblUnit = new Label();
            btnDefault = new Button();
            lblHint = new Label();
            lvwTracks = new ListView();
            colName = new ColumnHeader();
            colStatus = new ColumnHeader();
            colVolume = new ColumnHeader();
            colAdjustment = new ColumnHeader();
            colResult = new ColumnHeader();
            colPeak = new ColumnHeader();
            colClipping = new ColumnHeader();
            colAccumulated = new ColumnHeader();
            pnlButtons = new Panel();
            btnClose = new Button();
            statusStrip = new StatusStrip();
            lblStatus = new ToolStripStatusLabel();
            progressBar = new ToolStripProgressBar();
            toolStrip.SuspendLayout();
            pnlTarget.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numTarget).BeginInit();
            pnlButtons.SuspendLayout();
            statusStrip.SuspendLayout();
            SuspendLayout();
            //
            // toolStrip
            //
            toolStrip.Font = new Font("Segoe UI", 10F);
            toolStrip.GripStyle = ToolStripGripStyle.Hidden;
            toolStrip.Items.AddRange(new ToolStripItem[] { btnAnalyze, btnApply, btnRestore, btnStop, sepCheck, btnCheckAll, btnCheckNone });
            toolStrip.Location = new Point(0, 0);
            toolStrip.Name = "toolStrip";
            toolStrip.Padding = new Padding(8, 2, 8, 2);
            toolStrip.Size = new Size(1084, 30);
            toolStrip.TabIndex = 0;
            //
            // btnAnalyze
            //
            btnAnalyze.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnAnalyze.Name = "btnAnalyze";
            btnAnalyze.Size = new Size(87, 23);
            btnAnalyze.Text = "▶ Analizar";
            btnAnalyze.ToolTipText = "Medir el volumen de las canciones marcadas, sin modificarlas (F5)";
            btnAnalyze.Click += btnAnalyze_Click;
            //
            // btnApply
            //
            btnApply.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnApply.Name = "btnApply";
            btnApply.Size = new Size(118, 23);
            btnApply.Text = "✔ Aplicar ajuste";
            btnApply.ToolTipText = "Llevar las canciones marcadas al volumen objetivo, sin recodificar ni perder calidad (Ctrl+Entrar)";
            btnApply.Click += btnApply_Click;
            //
            // btnRestore
            //
            btnRestore.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnRestore.Name = "btnRestore";
            btnRestore.Size = new Size(140, 23);
            btnRestore.Text = "↺ Restaurar original";
            btnRestore.ToolTipText = "Devolver las canciones marcadas al volumen que tenían antes de cualquier ajuste";
            btnRestore.Click += btnRestore_Click;
            //
            // btnStop
            //
            btnStop.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnStop.Enabled = false;
            btnStop.Name = "btnStop";
            btnStop.Size = new Size(85, 23);
            btnStop.Text = "⏹ Detener";
            btnStop.ToolTipText = "Detener la operación en curso; ningún archivo queda a medias (Esc)";
            btnStop.Click += btnStop_Click;
            //
            // sepCheck
            //
            sepCheck.Name = "sepCheck";
            sepCheck.Size = new Size(6, 26);
            //
            // btnCheckAll
            //
            btnCheckAll.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnCheckAll.Name = "btnCheckAll";
            btnCheckAll.Size = new Size(116, 23);
            btnCheckAll.Text = "☑ Marcar todas";
            btnCheckAll.ToolTipText = "Marcar todas las canciones";
            btnCheckAll.Click += btnCheckAll_Click;
            //
            // btnCheckNone
            //
            btnCheckNone.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnCheckNone.Name = "btnCheckNone";
            btnCheckNone.Size = new Size(134, 23);
            btnCheckNone.Text = "☐ Desmarcar todas";
            btnCheckNone.ToolTipText = "Desmarcar todas las canciones";
            btnCheckNone.Click += btnCheckNone_Click;
            //
            // pnlTarget
            //
            pnlTarget.Controls.Add(lblTarget);
            pnlTarget.Controls.Add(numTarget);
            pnlTarget.Controls.Add(lblUnit);
            pnlTarget.Controls.Add(btnDefault);
            pnlTarget.Controls.Add(lblHint);
            pnlTarget.Dock = DockStyle.Top;
            pnlTarget.Location = new Point(0, 30);
            pnlTarget.Name = "pnlTarget";
            pnlTarget.Size = new Size(1084, 46);
            pnlTarget.TabIndex = 1;
            //
            // lblTarget
            //
            lblTarget.AutoSize = true;
            lblTarget.Location = new Point(12, 14);
            lblTarget.Name = "lblTarget";
            lblTarget.Size = new Size(118, 19);
            lblTarget.TabIndex = 0;
            lblTarget.Text = "&Volumen objetivo:";
            //
            // numTarget
            //
            numTarget.DecimalPlaces = 1;
            numTarget.Increment = new decimal(new int[] { 5, 0, 0, 65536 });
            numTarget.Location = new Point(138, 11);
            numTarget.Maximum = new decimal(new int[] { 105, 0, 0, 0 });
            numTarget.Minimum = new decimal(new int[] { 75, 0, 0, 0 });
            numTarget.Name = "numTarget";
            numTarget.Size = new Size(72, 25);
            numTarget.TabIndex = 1;
            numTarget.TextAlign = HorizontalAlignment.Right;
            numTarget.Value = new decimal(new int[] { 89, 0, 0, 0 });
            numTarget.ValueChanged += numTarget_ValueChanged;
            //
            // lblUnit
            //
            lblUnit.AutoSize = true;
            lblUnit.Location = new Point(214, 14);
            lblUnit.Name = "lblUnit";
            lblUnit.Size = new Size(23, 19);
            lblUnit.TabIndex = 2;
            lblUnit.Text = "dB";
            //
            // btnDefault
            //
            btnDefault.AutoSize = true;
            btnDefault.Location = new Point(250, 9);
            btnDefault.Name = "btnDefault";
            btnDefault.Size = new Size(150, 29);
            btnDefault.TabIndex = 3;
            btnDefault.Text = "&Restablecer 89 dB";
            btnDefault.UseVisualStyleBackColor = true;
            btnDefault.Click += btnDefault_Click;
            //
            // lblHint
            //
            lblHint.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            lblHint.ForeColor = SystemColors.GrayText;
            lblHint.Location = new Point(414, 6);
            lblHint.Name = "lblHint";
            lblHint.Size = new Size(658, 36);
            lblHint.TabIndex = 4;
            lblHint.Text = "89 dB es el estándar ReplayGain. El volumen cambia en pasos de 1.5 dB, sin recodificar ni perder calidad.";
            lblHint.TextAlign = ContentAlignment.MiddleLeft;
            //
            // lvwTracks
            //
            lvwTracks.AccessibleName = "Canciones y su volumen";
            lvwTracks.CheckBoxes = true;
            lvwTracks.Columns.AddRange(new ColumnHeader[] { colName, colStatus, colVolume, colAdjustment, colResult, colPeak, colClipping, colAccumulated });
            lvwTracks.Dock = DockStyle.Fill;
            lvwTracks.FullRowSelect = true;
            lvwTracks.GridLines = true;
            lvwTracks.HideSelection = false;
            lvwTracks.Location = new Point(0, 76);
            lvwTracks.Name = "lvwTracks";
            lvwTracks.ShowItemToolTips = true;
            lvwTracks.Size = new Size(1084, 401);
            lvwTracks.TabIndex = 2;
            lvwTracks.UseCompatibleStateImageBehavior = false;
            lvwTracks.View = View.Details;
            lvwTracks.ColumnClick += lvwTracks_ColumnClick;
            lvwTracks.ItemChecked += lvwTracks_ItemChecked;
            //
            // colName
            //
            colName.Text = "Canción";
            colName.Width = 300;
            //
            // colStatus
            //
            colStatus.Text = "Estado";
            colStatus.Width = 130;
            //
            // colVolume
            //
            colVolume.Text = "Volumen";
            colVolume.TextAlign = HorizontalAlignment.Right;
            colVolume.Width = 90;
            //
            // colAdjustment
            //
            colAdjustment.Text = "Ajuste";
            colAdjustment.TextAlign = HorizontalAlignment.Right;
            colAdjustment.Width = 170;
            //
            // colResult
            //
            colResult.Text = "Resultado";
            colResult.TextAlign = HorizontalAlignment.Right;
            colResult.Width = 95;
            //
            // colPeak
            //
            colPeak.Text = "Pico";
            colPeak.TextAlign = HorizontalAlignment.Right;
            colPeak.Width = 95;
            //
            // colClipping
            //
            colClipping.Text = "Saturación";
            colClipping.Width = 135;
            //
            // colAccumulated
            //
            colAccumulated.Text = "Acumulado";
            colAccumulated.TextAlign = HorizontalAlignment.Right;
            colAccumulated.Width = 95;
            //
            // pnlButtons
            //
            pnlButtons.Controls.Add(btnClose);
            pnlButtons.Dock = DockStyle.Bottom;
            pnlButtons.Location = new Point(0, 477);
            pnlButtons.Name = "pnlButtons";
            pnlButtons.Size = new Size(1084, 48);
            pnlButtons.TabIndex = 3;
            //
            // btnClose
            //
            btnClose.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnClose.AutoSize = true;
            btnClose.DialogResult = DialogResult.Cancel;
            btnClose.Location = new Point(982, 10);
            btnClose.Name = "btnClose";
            btnClose.Size = new Size(90, 29);
            btnClose.TabIndex = 0;
            btnClose.Text = "Cerrar";
            btnClose.UseVisualStyleBackColor = true;
            //
            // statusStrip
            //
            statusStrip.Font = new Font("Segoe UI", 9F);
            statusStrip.Items.AddRange(new ToolStripItem[] { lblStatus, progressBar });
            statusStrip.Location = new Point(0, 525);
            statusStrip.Name = "statusStrip";
            statusStrip.Size = new Size(1084, 24);
            statusStrip.SizingGrip = false;
            statusStrip.TabIndex = 4;
            //
            // lblStatus
            //
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(1069, 19);
            lblStatus.Spring = true;
            lblStatus.Text = "Listo.";
            lblStatus.TextAlign = ContentAlignment.MiddleLeft;
            //
            // progressBar
            //
            progressBar.Name = "progressBar";
            progressBar.Size = new Size(200, 18);
            progressBar.Visible = false;
            //
            // VolumeDialog
            //
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnClose;
            ClientSize = new Size(1084, 549);
            Controls.Add(lvwTracks);
            Controls.Add(pnlTarget);
            Controls.Add(toolStrip);
            Controls.Add(pnlButtons);
            Controls.Add(statusStrip);
            Font = new Font("Segoe UI", 10F);
            Icon = (Icon)resources.GetObject("$this.Icon");
            KeyPreview = true;
            MinimizeBox = false;
            MinimumSize = new Size(760, 400);
            Name = "VolumeDialog";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Regularizar volumen";
            FormClosing += VolumeDialog_FormClosing;
            KeyDown += VolumeDialog_KeyDown;
            toolStrip.ResumeLayout(false);
            toolStrip.PerformLayout();
            pnlTarget.ResumeLayout(false);
            pnlTarget.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numTarget).EndInit();
            pnlButtons.ResumeLayout(false);
            pnlButtons.PerformLayout();
            statusStrip.ResumeLayout(false);
            statusStrip.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ToolStrip toolStrip;
        private ToolStripButton btnAnalyze;
        private ToolStripButton btnApply;
        private ToolStripButton btnRestore;
        private ToolStripButton btnStop;
        private ToolStripSeparator sepCheck;
        private ToolStripButton btnCheckAll;
        private ToolStripButton btnCheckNone;
        private Panel pnlTarget;
        private Label lblTarget;
        private NumericUpDown numTarget;
        private Label lblUnit;
        private Button btnDefault;
        private Label lblHint;
        private ListView lvwTracks;
        private ColumnHeader colName;
        private ColumnHeader colStatus;
        private ColumnHeader colVolume;
        private ColumnHeader colAdjustment;
        private ColumnHeader colResult;
        private ColumnHeader colPeak;
        private ColumnHeader colClipping;
        private ColumnHeader colAccumulated;
        private Panel pnlButtons;
        private Button btnClose;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel lblStatus;
        private ToolStripProgressBar progressBar;
    }
}
