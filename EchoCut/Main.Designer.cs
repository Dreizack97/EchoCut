namespace EchoCut
{
    partial class Main
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
            components = new System.ComponentModel.Container();
            DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Main));
            label1 = new Label();
            txtPath = new TextBox();
            dataGrid = new DataGridView();
            contextMenuStrip = new ContextMenuStrip(components);
            mnuWaveform = new ToolStripMenuItem();
            mnuOpenFolder = new ToolStripMenuItem();
            mnuOpenAudacity = new ToolStripMenuItem();
            mnuEditSong = new ToolStripMenuItem();
            mnuDeleteSong = new ToolStripMenuItem();
            label2 = new Label();
            numericTolerance = new NumericUpDown();
            folderBrowserDialog = new FolderBrowserDialog();
            label3 = new Label();
            label4 = new Label();
            numericThreads = new NumericUpDown();
            saveFileDialog = new SaveFileDialog();
            openFileDialog = new OpenFileDialog();
            toolStrip = new ToolStrip();
            btnPath = new ToolStripButton();
            btnFile = new ToolStripButton();
            sepSource = new ToolStripSeparator();
            btnAnalyze = new ToolStripButton();
            btnCropAll = new ToolStripButton();
            btnStop = new ToolStripButton();
            sepProcess = new ToolStripSeparator();
            ddbUtilities = new ToolStripDropDownButton();
            mnuCleanMetadata = new ToolStripMenuItem();
            mnuNormalize = new ToolStripMenuItem();
            btnExport = new ToolStripButton();
            btnAdvanced = new ToolStripButton();
            statusStrip = new StatusStrip();
            lblStatus = new ToolStripStatusLabel();
            progressBar = new ToolStripProgressBar();
            lblProgress = new ToolStripStatusLabel();
            lblTotal = new ToolStripStatusLabel();
            lblTrimmable = new ToolStripStatusLabel();
            lblTrimmed = new ToolStripStatusLabel();
            lblErrors = new ToolStripStatusLabel();
            ((System.ComponentModel.ISupportInitialize)dataGrid).BeginInit();
            contextMenuStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numericTolerance).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numericThreads).BeginInit();
            toolStrip.SuspendLayout();
            statusStrip.SuspendLayout();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(12, 31);
            label1.Name = "label1";
            label1.Size = new Size(54, 19);
            label1.TabIndex = 1;
            label1.Text = "Origen:";
            // 
            // txtPath
            // 
            txtPath.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtPath.BackColor = SystemColors.Window;
            txtPath.Location = new Point(92, 28);
            txtPath.Name = "txtPath";
            txtPath.ReadOnly = true;
            txtPath.Size = new Size(996, 25);
            txtPath.TabIndex = 2;
            // 
            // dataGrid
            // 
            dataGrid.AllowUserToAddRows = false;
            dataGrid.AllowUserToDeleteRows = false;
            dataGrid.AllowUserToResizeRows = false;
            dataGridViewCellStyle1.BackColor = SystemColors.Control;
            dataGrid.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle1;
            dataGrid.AllowDrop = true;
            dataGrid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dataGrid.BackgroundColor = SystemColors.Window;
            dataGrid.BorderStyle = BorderStyle.None;
            dataGrid.CellBorderStyle = DataGridViewCellBorderStyle.None;
            dataGrid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            dataGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dataGrid.ContextMenuStrip = contextMenuStrip;
            dataGrid.Location = new Point(12, 100);
            dataGrid.Name = "dataGrid";
            dataGrid.ReadOnly = true;
            dataGrid.RowHeadersVisible = false;
            dataGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGrid.Size = new Size(1076, 477);
            dataGrid.TabIndex = 8;
            dataGrid.CellContentClick += dataGrid_CellContentClick;
            dataGrid.CellDoubleClick += dataGrid_CellDoubleClick;
            dataGrid.CellFormatting += dataGrid_CellFormatting;
            dataGrid.CellMouseDown += dataGrid_CellMouseDown;
            dataGrid.DataBindingComplete += dataGrid_DataBindingComplete;
            dataGrid.DragDrop += Main_DragDrop;
            dataGrid.DragEnter += Main_DragEnter;
            dataGrid.DragLeave += Main_DragLeave;
            dataGrid.Paint += dataGrid_Paint;
            dataGrid.KeyDown += dataGrid_KeyDown;
            // 
            // contextMenuStrip
            // 
            contextMenuStrip.Items.AddRange(new ToolStripItem[] { mnuWaveform, mnuOpenFolder, mnuOpenAudacity, mnuEditSong, mnuDeleteSong });
            contextMenuStrip.Name = "contextMenuStrip";
            contextMenuStrip.Size = new Size(283, 114);
            // 
            // mnuWaveform
            // 
            mnuWaveform.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            mnuWaveform.Name = "mnuWaveform";
            mnuWaveform.Size = new Size(282, 22);
            mnuWaveform.Text = "Ver forma de onda y ajustar recorte…";
            mnuWaveform.Click += mnuWaveform_Click;
            // 
            // mnuOpenFolder
            // 
            mnuOpenFolder.Name = "mnuOpenFolder";
            mnuOpenFolder.Size = new Size(282, 22);
            mnuOpenFolder.Text = "Abrir carpeta contenedora";
            mnuOpenFolder.Click += mnuOpenFolder_Click;
            // 
            // mnuOpenAudacity
            // 
            mnuOpenAudacity.Name = "mnuOpenAudacity";
            mnuOpenAudacity.Size = new Size(282, 22);
            mnuOpenAudacity.Text = "Abrir en Audacity";
            mnuOpenAudacity.Click += mnuOpenAudacity_Click;
            // 
            // mnuEditSong
            // 
            mnuEditSong.Name = "mnuEditSong";
            mnuEditSong.Size = new Size(282, 22);
            mnuEditSong.Text = "Editar propiedades";
            mnuEditSong.Click += mnuEditSong_Click;
            // 
            // mnuDeleteSong
            // 
            mnuDeleteSong.Name = "mnuDeleteSong";
            mnuDeleteSong.Size = new Size(282, 22);
            mnuDeleteSong.Text = "Eliminar archivo(s)";
            mnuDeleteSong.Click += mnuDeleteSong_Click;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(12, 68);
            label2.Name = "label2";
            label2.Size = new Size(131, 19);
            label2.TabIndex = 3;
            label2.Text = "Silencio a conservar:";
            // 
            // numericTolerance
            // 
            numericTolerance.DecimalPlaces = 1;
            numericTolerance.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
            numericTolerance.Location = new Point(149, 65);
            numericTolerance.Maximum = new decimal(new int[] { 5, 0, 0, 0 });
            numericTolerance.Name = "numericTolerance";
            numericTolerance.Size = new Size(50, 25);
            numericTolerance.TabIndex = 4;
            numericTolerance.Value = new decimal(new int[] { 3, 0, 0, 65536 });
            numericTolerance.ValueChanged += numericTolerance_ValueChanged;
            // 
            // folderBrowserDialog
            // 
            folderBrowserDialog.Multiselect = true;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(205, 68);
            label3.Name = "label3";
            label3.Size = new Size(15, 19);
            label3.TabIndex = 5;
            label3.Text = "s";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(252, 68);
            label4.Name = "label4";
            label4.Size = new Size(135, 19);
            label4.TabIndex = 6;
            label4.Text = "Archivos en paralelo:";
            // 
            // numericThreads
            // 
            numericThreads.Location = new Point(400, 65);
            numericThreads.Maximum = new decimal(new int[] { 64, 0, 0, 0 });
            numericThreads.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numericThreads.Name = "numericThreads";
            numericThreads.Size = new Size(50, 25);
            numericThreads.TabIndex = 7;
            numericThreads.Value = new decimal(new int[] { 8, 0, 0, 0 });
            // 
            // saveFileDialog
            // 
            saveFileDialog.DefaultExt = "csv";
            saveFileDialog.Filter = "Archivo CSV|*.csv";
            saveFileDialog.Title = "Exportar resultados";
            // 
            // openFileDialog
            // 
            openFileDialog.Filter = resources.GetString("openFileDialog.Filter");
            openFileDialog.Multiselect = true;
            // 
            // toolStrip
            // 
            toolStrip.Font = new Font("Segoe UI", 10F);
            toolStrip.GripStyle = ToolStripGripStyle.Hidden;
            toolStrip.Items.AddRange(new ToolStripItem[] { btnPath, btnFile, sepSource, btnAnalyze, btnCropAll, btnStop, sepProcess, ddbUtilities, btnExport, btnAdvanced });
            toolStrip.Location = new Point(0, 0);
            toolStrip.Name = "toolStrip";
            toolStrip.Size = new Size(1100, 26);
            toolStrip.TabIndex = 0;
            toolStrip.Text = "toolStrip1";
            // 
            // btnPath
            // 
            btnPath.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnPath.Name = "btnPath";
            btnPath.Size = new Size(106, 23);
            btnPath.Text = "Abrir carpeta(s)";
            btnPath.Click += btnPath_Click;
            // 
            // btnFile
            // 
            btnFile.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnFile.Name = "btnFile";
            btnFile.Size = new Size(105, 23);
            btnFile.Text = "Abrir archivo(s)";
            btnFile.Click += btnFile_Click;
            // 
            // sepSource
            // 
            sepSource.Name = "sepSource";
            sepSource.Size = new Size(6, 26);
            // 
            // btnAnalyze
            // 
            btnAnalyze.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnAnalyze.Font = new Font("Segoe UI", 10F);
            btnAnalyze.Name = "btnAnalyze";
            btnAnalyze.Size = new Size(77, 23);
            btnAnalyze.Text = "▶ Analizar";
            btnAnalyze.Click += btnAnalyze_Click;
            // 
            // btnCropAll
            // 
            btnCropAll.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnCropAll.ImageTransparentColor = Color.Magenta;
            btnCropAll.Name = "btnCropAll";
            btnCropAll.Size = new Size(120, 23);
            btnCropAll.Text = "✂ Recortar todo";
            btnCropAll.Click += btnCropAll_Click;
            // 
            // btnStop
            // 
            btnStop.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnStop.Name = "btnStop";
            btnStop.Size = new Size(85, 23);
            btnStop.Text = "⏹ Detener";
            btnStop.Click += btnStop_Click;
            // 
            // sepProcess
            // 
            sepProcess.Name = "sepProcess";
            sepProcess.Size = new Size(6, 26);
            // 
            // ddbUtilities
            // 
            ddbUtilities.DisplayStyle = ToolStripItemDisplayStyle.Text;
            ddbUtilities.DropDownItems.AddRange(new ToolStripItem[] { mnuCleanMetadata, mnuNormalize });
            ddbUtilities.Name = "ddbUtilities";
            ddbUtilities.Size = new Size(82, 23);
            ddbUtilities.Text = "Utilidades";
            // 
            // mnuCleanMetadata
            // 
            mnuCleanMetadata.Name = "mnuCleanMetadata";
            mnuCleanMetadata.Size = new Size(192, 24);
            mnuCleanMetadata.Text = "Limpiar metadatos";
            mnuCleanMetadata.Click += mnuCleanMetadata_Click;
            // 
            // mnuNormalize
            // 
            mnuNormalize.Name = "mnuNormalize";
            mnuNormalize.Size = new Size(192, 24);
            mnuNormalize.Text = "Normalizar";
            mnuNormalize.Click += mnuNormalize_Click;
            // 
            // btnExport
            // 
            btnExport.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnExport.Name = "btnExport";
            btnExport.Size = new Size(93, 23);
            btnExport.Text = "Exportar CSV";
            btnExport.Click += btnExport_Click;
            // 
            // btnAdvanced
            // 
            btnAdvanced.Alignment = ToolStripItemAlignment.Right;
            btnAdvanced.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnAdvanced.Name = "btnAdvanced";
            btnAdvanced.RightToLeft = RightToLeft.No;
            btnAdvanced.Size = new Size(96, 23);
            btnAdvanced.Text = "⚙ Avanzado";
            btnAdvanced.Click += btnAdvanced_Click;
            // 
            // statusStrip
            // 
            statusStrip.Font = new Font("Segoe UI", 9F);
            statusStrip.Items.AddRange(new ToolStripItem[] { lblStatus, progressBar, lblProgress, lblTotal, lblTrimmable, lblTrimmed, lblErrors });
            statusStrip.Location = new Point(0, 587);
            statusStrip.Name = "statusStrip";
            statusStrip.ShowItemToolTips = true;
            statusStrip.Size = new Size(1100, 24);
            statusStrip.TabIndex = 9;
            // 
            // lblStatus
            // 
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(756, 19);
            lblStatus.Spring = true;
            lblStatus.Text = "Listo.";
            lblStatus.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // progressBar
            // 
            progressBar.Name = "progressBar";
            progressBar.Size = new Size(180, 18);
            progressBar.Visible = false;
            // 
            // lblProgress
            // 
            lblProgress.Name = "lblProgress";
            lblProgress.Size = new Size(36, 19);
            lblProgress.Text = "0 / 0";
            lblProgress.Visible = false;
            // 
            // lblTotal
            // 
            lblTotal.BorderSides = ToolStripStatusLabelBorderSides.Left;
            lblTotal.BorderStyle = Border3DStyle.Etched;
            lblTotal.Name = "lblTotal";
            lblTotal.Size = new Size(54, 19);
            lblTotal.Text = "0 pistas";
            lblTotal.ToolTipText = "Pistas cargadas en la lista";
            // 
            // lblTrimmable
            // 
            lblTrimmable.BorderSides = ToolStripStatusLabelBorderSides.Left;
            lblTrimmable.BorderStyle = Border3DStyle.Etched;
            lblTrimmable.Name = "lblTrimmable";
            lblTrimmable.Size = new Size(96, 19);
            lblTrimmable.Text = "✂ 0 recortables";
            lblTrimmable.ToolTipText = "Pistas con silencio recortable que aún no se han recortado";
            // 
            // lblTrimmed
            // 
            lblTrimmed.BorderSides = ToolStripStatusLabelBorderSides.Left;
            lblTrimmed.BorderStyle = Border3DStyle.Etched;
            lblTrimmed.Name = "lblTrimmed";
            lblTrimmed.Size = new Size(95, 19);
            lblTrimmed.Text = "✔ 0 recortadas";
            lblTrimmed.ToolTipText = "Pistas ya recortadas en esta sesión";
            // 
            // lblErrors
            // 
            lblErrors.BorderSides = ToolStripStatusLabelBorderSides.Left;
            lblErrors.BorderStyle = Border3DStyle.Etched;
            lblErrors.Name = "lblErrors";
            lblErrors.Size = new Size(74, 19);
            lblErrors.Text = "⚠ 0 errores";
            lblErrors.ToolTipText = "Pistas cuyo análisis o recorte falló; el detalle va en el CSV exportado";
            lblErrors.Visible = false;
            // 
            // Main
            // 
            AllowDrop = true;
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1100, 611);
            Controls.Add(statusStrip);
            Controls.Add(toolStrip);
            Controls.Add(numericThreads);
            Controls.Add(label4);
            Controls.Add(label3);
            Controls.Add(numericTolerance);
            Controls.Add(label2);
            Controls.Add(dataGrid);
            Controls.Add(txtPath);
            Controls.Add(label1);
            Font = new Font("Segoe UI", 10F);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MinimumSize = new Size(940, 550);
            Name = "Main";
            Text = "Analizador de Audios";
            FormClosing += Main_FormClosing;
            DragDrop += Main_DragDrop;
            DragEnter += Main_DragEnter;
            DragLeave += Main_DragLeave;
            ((System.ComponentModel.ISupportInitialize)dataGrid).EndInit();
            contextMenuStrip.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)numericTolerance).EndInit();
            ((System.ComponentModel.ISupportInitialize)numericThreads).EndInit();
            toolStrip.ResumeLayout(false);
            toolStrip.PerformLayout();
            statusStrip.ResumeLayout(false);
            statusStrip.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private TextBox txtPath;
        private DataGridView dataGrid;
        private Label label2;
        private NumericUpDown numericTolerance;
        private FolderBrowserDialog folderBrowserDialog;
        private Label label3;
        private Label label4;
        private NumericUpDown numericThreads;
        private SaveFileDialog saveFileDialog;
        private ContextMenuStrip contextMenuStrip;
        private ToolStripMenuItem mnuOpenFolder;
        private ToolStripMenuItem mnuOpenAudacity;
        private ToolStripMenuItem mnuWaveform;
        private OpenFileDialog openFileDialog;
        private ToolStripMenuItem mnuEditSong;
        private ToolStripMenuItem mnuDeleteSong;
        private ToolStrip toolStrip;
        private ToolStripButton btnPath;
        private ToolStripButton btnFile;
        private ToolStripSeparator sepSource;
        private ToolStripButton btnAnalyze;
        private ToolStripButton btnCropAll;
        private ToolStripButton btnStop;
        private ToolStripSeparator sepProcess;
        private ToolStripDropDownButton ddbUtilities;
        private ToolStripButton btnExport;
        private ToolStripButton btnAdvanced;
        private ToolStripMenuItem mnuCleanMetadata;
        private ToolStripMenuItem mnuNormalize;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel lblStatus;
        private ToolStripProgressBar progressBar;
        private ToolStripStatusLabel lblProgress;
        private ToolStripStatusLabel lblTotal;
        private ToolStripStatusLabel lblTrimmable;
        private ToolStripStatusLabel lblTrimmed;
        private ToolStripStatusLabel lblErrors;
    }
}
