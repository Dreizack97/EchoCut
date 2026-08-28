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
            btnPath = new Button();
            dataGrid = new DataGridView();
            contextMenuStrip = new ContextMenuStrip(components);
            mnuOpenFolder = new ToolStripMenuItem();
            mnuOpenAudacity = new ToolStripMenuItem();
            mnuEditSong = new ToolStripMenuItem();
            mnuDeleteSong = new ToolStripMenuItem();
            btnAnalyze = new Button();
            btnCancel = new Button();
            btnCropAll = new Button();
            btnStop = new Button();
            btnExport = new Button();
            label2 = new Label();
            numericTolerance = new NumericUpDown();
            folderBrowserDialog = new FolderBrowserDialog();
            label3 = new Label();
            label4 = new Label();
            numericThreads = new NumericUpDown();
            btnAdvanced = new Button();
            progressBar = new ProgressBar();
            lblStatus = new Label();
            saveFileDialog = new SaveFileDialog();
            btnFile = new Button();
            openFileDialog = new OpenFileDialog();
            btnClean = new Button();
            ((System.ComponentModel.ISupportInitialize)dataGrid).BeginInit();
            contextMenuStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numericTolerance).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numericThreads).BeginInit();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(12, 15);
            label1.Name = "label1";
            label1.Size = new Size(74, 19);
            label1.TabIndex = 0;
            label1.Text = "Carpeta(s):";
            // 
            // txtPath
            // 
            txtPath.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtPath.BackColor = SystemColors.Window;
            txtPath.Location = new Point(92, 12);
            txtPath.Name = "txtPath";
            txtPath.ReadOnly = true;
            txtPath.Size = new Size(686, 25);
            txtPath.TabIndex = 1;
            // 
            // btnPath
            // 
            btnPath.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnPath.AutoSize = true;
            btnPath.Location = new Point(784, 9);
            btnPath.Name = "btnPath";
            btnPath.Size = new Size(149, 29);
            btnPath.TabIndex = 2;
            btnPath.Text = "Seleccionar carpeta(s)";
            btnPath.UseVisualStyleBackColor = true;
            btnPath.Click += btnPath_Click;
            // 
            // dataGrid
            // 
            dataGrid.AllowUserToAddRows = false;
            dataGrid.AllowUserToDeleteRows = false;
            dataGrid.AllowUserToResizeRows = false;
            dataGridViewCellStyle1.BackColor = SystemColors.Control;
            dataGrid.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle1;
            dataGrid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dataGrid.BorderStyle = BorderStyle.None;
            dataGrid.CellBorderStyle = DataGridViewCellBorderStyle.None;
            dataGrid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            dataGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dataGrid.ContextMenuStrip = contextMenuStrip;
            dataGrid.Location = new Point(12, 134);
            dataGrid.Name = "dataGrid";
            dataGrid.ReadOnly = true;
            dataGrid.RowHeadersVisible = false;
            dataGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGrid.Size = new Size(1076, 428);
            dataGrid.TabIndex = 11;
            dataGrid.CellContentClick += dataGrid_CellContentClick;
            dataGrid.CellDoubleClick += dataGrid_CellDoubleClick;
            dataGrid.CellFormatting += dataGrid_CellFormatting;
            dataGrid.CellMouseDown += dataGrid_CellMouseDown;
            dataGrid.DataBindingComplete += dataGrid_DataBindingComplete;
            dataGrid.KeyDown += dataGrid_KeyDown;
            // 
            // contextMenuStrip
            // 
            contextMenuStrip.Items.AddRange(new ToolStripItem[] { mnuOpenFolder, mnuOpenAudacity, mnuEditSong, mnuDeleteSong });
            contextMenuStrip.Name = "contextMenuStrip";
            contextMenuStrip.Size = new Size(213, 92);
            // 
            // mnuOpenFolder
            // 
            mnuOpenFolder.Name = "mnuOpenFolder";
            mnuOpenFolder.Size = new Size(212, 22);
            mnuOpenFolder.Text = "Abrir carpeta contenedora";
            mnuOpenFolder.Click += mnuOpenFolder_Click;
            // 
            // mnuOpenAudacity
            // 
            mnuOpenAudacity.Name = "mnuOpenAudacity";
            mnuOpenAudacity.Size = new Size(212, 22);
            mnuOpenAudacity.Text = "Abrir en Audacity";
            mnuOpenAudacity.Click += mnuOpenAudacity_Click;
            // 
            // mnuEditSong
            // 
            mnuEditSong.Name = "mnuEditSong";
            mnuEditSong.Size = new Size(212, 22);
            mnuEditSong.Text = "Editar propiedades";
            mnuEditSong.Click += mnuEditSong_Click;
            // 
            // mnuDeleteSong
            // 
            mnuDeleteSong.Name = "mnuDeleteSong";
            mnuDeleteSong.Size = new Size(212, 22);
            mnuDeleteSong.Text = "Eliminar archivo(s)";
            mnuDeleteSong.Click += mnuDeleteSong_Click;
            // 
            // btnAnalyze
            // 
            btnAnalyze.AutoSize = true;
            btnAnalyze.Location = new Point(12, 99);
            btnAnalyze.Name = "btnAnalyze";
            btnAnalyze.Size = new Size(140, 29);
            btnAnalyze.TabIndex = 6;
            btnAnalyze.Text = "Analizar";
            btnAnalyze.UseVisualStyleBackColor = true;
            btnAnalyze.Click += btnAnalyze_Click;
            // 
            // btnCancel
            // 
            btnCancel.AutoSize = true;
            btnCancel.Location = new Point(171, 99);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(140, 29);
            btnCancel.TabIndex = 7;
            btnCancel.Text = "Cancelar";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;
            // 
            // btnCropAll
            // 
            btnCropAll.AutoSize = true;
            btnCropAll.Location = new Point(330, 99);
            btnCropAll.Name = "btnCropAll";
            btnCropAll.Size = new Size(140, 29);
            btnCropAll.TabIndex = 8;
            btnCropAll.Text = "Recortar todos";
            btnCropAll.UseVisualStyleBackColor = true;
            btnCropAll.Click += btnCropAll_Click;
            // 
            // btnStop
            // 
            btnStop.AutoSize = true;
            btnStop.Location = new Point(489, 99);
            btnStop.Name = "btnStop";
            btnStop.Size = new Size(140, 29);
            btnStop.TabIndex = 9;
            btnStop.Text = "⏹ Detener";
            btnStop.UseVisualStyleBackColor = true;
            btnStop.Click += btnStop_Click;
            // 
            // btnExport
            // 
            btnExport.AutoSize = true;
            btnExport.Location = new Point(648, 99);
            btnExport.Name = "btnExport";
            btnExport.Size = new Size(140, 29);
            btnExport.TabIndex = 10;
            btnExport.Text = "Exportar resultados";
            btnExport.UseVisualStyleBackColor = true;
            btnExport.Click += btnExport_Click;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(12, 59);
            label2.Name = "label2";
            label2.Size = new Size(222, 19);
            label2.TabIndex = 3;
            label2.Text = "Silencio que se conserva al recortar";
            // 
            // numericTolerance
            // 
            numericTolerance.DecimalPlaces = 1;
            numericTolerance.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
            numericTolerance.Location = new Point(240, 56);
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
            label3.Location = new Point(296, 59);
            label3.Name = "label3";
            label3.Size = new Size(71, 19);
            label3.TabIndex = 5;
            label3.Text = "segundos.";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(400, 59);
            label4.Name = "label4";
            label4.Size = new Size(135, 19);
            label4.TabIndex = 12;
            label4.Text = "Archivos en paralelo:";
            // 
            // numericThreads
            // 
            numericThreads.Location = new Point(548, 56);
            numericThreads.Maximum = new decimal(new int[] { 64, 0, 0, 0 });
            numericThreads.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numericThreads.Name = "numericThreads";
            numericThreads.Size = new Size(50, 25);
            numericThreads.TabIndex = 13;
            numericThreads.Value = new decimal(new int[] { 8, 0, 0, 0 });
            // 
            // btnAdvanced
            // 
            btnAdvanced.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnAdvanced.AutoSize = true;
            btnAdvanced.Location = new Point(953, 54);
            btnAdvanced.Name = "btnAdvanced";
            btnAdvanced.Size = new Size(135, 29);
            btnAdvanced.TabIndex = 14;
            btnAdvanced.Text = "Avanzado…";
            btnAdvanced.UseVisualStyleBackColor = true;
            btnAdvanced.Click += btnAdvanced_Click;
            // 
            // progressBar
            // 
            progressBar.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            progressBar.Location = new Point(12, 574);
            progressBar.Name = "progressBar";
            progressBar.Size = new Size(1076, 22);
            progressBar.TabIndex = 15;
            // 
            // lblStatus
            // 
            lblStatus.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            lblStatus.AutoEllipsis = true;
            lblStatus.Location = new Point(953, 104);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(135, 19);
            lblStatus.TabIndex = 16;
            lblStatus.Text = "Listo.";
            lblStatus.TextAlign = ContentAlignment.MiddleRight;
            // 
            // saveFileDialog
            // 
            saveFileDialog.DefaultExt = "csv";
            saveFileDialog.Filter = "Archivo CSV|*.csv";
            saveFileDialog.Title = "Exportar resultados";
            // 
            // btnFile
            // 
            btnFile.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnFile.AutoSize = true;
            btnFile.Location = new Point(939, 9);
            btnFile.Name = "btnFile";
            btnFile.Size = new Size(149, 29);
            btnFile.TabIndex = 17;
            btnFile.Text = "Seleccionar archivo(s)";
            btnFile.UseVisualStyleBackColor = true;
            btnFile.Click += btnFile_Click;
            // 
            // openFileDialog
            // 
            openFileDialog.Filter = resources.GetString("openFileDialog.Filter");
            openFileDialog.Multiselect = true;
            // 
            // btnClean
            // 
            btnClean.AutoSize = true;
            btnClean.Location = new Point(807, 99);
            btnClean.Name = "btnClean";
            btnClean.Size = new Size(140, 29);
            btnClean.TabIndex = 18;
            btnClean.Text = "Limpiar metadatos";
            btnClean.UseVisualStyleBackColor = true;
            btnClean.Click += btnClean_Click;
            // 
            // Main
            // 
            AllowDrop = true;
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1100, 611);
            Controls.Add(btnClean);
            Controls.Add(btnFile);
            Controls.Add(lblStatus);
            Controls.Add(progressBar);
            Controls.Add(btnAdvanced);
            Controls.Add(numericThreads);
            Controls.Add(label4);
            Controls.Add(label3);
            Controls.Add(numericTolerance);
            Controls.Add(label2);
            Controls.Add(btnExport);
            Controls.Add(btnStop);
            Controls.Add(btnCropAll);
            Controls.Add(btnCancel);
            Controls.Add(btnAnalyze);
            Controls.Add(dataGrid);
            Controls.Add(btnPath);
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
            ((System.ComponentModel.ISupportInitialize)dataGrid).EndInit();
            contextMenuStrip.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)numericTolerance).EndInit();
            ((System.ComponentModel.ISupportInitialize)numericThreads).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private TextBox txtPath;
        private Button btnPath;
        private DataGridView dataGrid;
        private Button btnAnalyze;
        private Button btnCancel;
        private Button btnCropAll;
        private Button btnStop;
        private Button btnExport;
        private Label label2;
        private NumericUpDown numericTolerance;
        private FolderBrowserDialog folderBrowserDialog;
        private Label label3;
        private Label label4;
        private NumericUpDown numericThreads;
        private Button btnAdvanced;
        private ProgressBar progressBar;
        private Label lblStatus;
        private SaveFileDialog saveFileDialog;
        private ContextMenuStrip contextMenuStrip;
        private ToolStripMenuItem mnuOpenFolder;
        private ToolStripMenuItem mnuOpenAudacity;
        private Button btnFile;
        private OpenFileDialog openFileDialog;
        private ToolStripMenuItem mnuEditSong;
        private ToolStripMenuItem mnuDeleteSong;
        private Button btnClean;
    }
}
