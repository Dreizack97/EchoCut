namespace EchoCut
{
    partial class DuplicatesDialog
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DuplicatesDialog));
            tableMain = new TableLayoutPanel();
            lblSummary = new Label();
            lvwDuplicates = new ListView();
            colName = new ColumnHeader();
            colSimilarity = new ColumnHeader();
            colDuration = new ColumnHeader();
            colFormat = new ColumnHeader();
            colBitrate = new ColumnHeader();
            colSize = new ColumnHeader();
            colFolder = new ColumnHeader();
            flowActions = new FlowLayoutPanel();
            btnPlay = new Button();
            btnReveal = new Button();
            btnSuggest = new Button();
            btnClear = new Button();
            tableBottom = new TableLayoutPanel();
            lblSelection = new Label();
            btnRecycle = new Button();
            btnClose = new Button();
            tableMain.SuspendLayout();
            flowActions.SuspendLayout();
            tableBottom.SuspendLayout();
            SuspendLayout();
            // 
            // tableMain
            // 
            tableMain.ColumnCount = 1;
            tableMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableMain.Controls.Add(lblSummary, 0, 0);
            tableMain.Controls.Add(lvwDuplicates, 0, 1);
            tableMain.Controls.Add(flowActions, 0, 2);
            tableMain.Controls.Add(tableBottom, 0, 3);
            tableMain.Dock = DockStyle.Fill;
            tableMain.Location = new Point(0, 0);
            tableMain.Name = "tableMain";
            tableMain.Padding = new Padding(9);
            tableMain.RowCount = 4;
            tableMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            tableMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableMain.RowStyles.Add(new RowStyle());
            tableMain.RowStyles.Add(new RowStyle());
            tableMain.Size = new Size(1084, 621);
            tableMain.TabIndex = 0;
            // 
            // lblSummary
            // 
            lblSummary.Dock = DockStyle.Fill;
            lblSummary.Location = new Point(12, 9);
            lblSummary.Name = "lblSummary";
            lblSummary.Size = new Size(1060, 50);
            lblSummary.TabIndex = 0;
            lblSummary.Text = "Grupos de canciones con el mismo audio.";
            lblSummary.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lvwDuplicates
            // 
            lvwDuplicates.AccessibleName = "Canciones duplicadas por su audio";
            lvwDuplicates.CheckBoxes = true;
            lvwDuplicates.Columns.AddRange(new ColumnHeader[] { colName, colSimilarity, colDuration, colFormat, colBitrate, colSize, colFolder });
            lvwDuplicates.Dock = DockStyle.Fill;
            lvwDuplicates.FullRowSelect = true;
            lvwDuplicates.HideSelection = false;
            lvwDuplicates.Location = new Point(12, 62);
            lvwDuplicates.MultiSelect = false;
            lvwDuplicates.Name = "lvwDuplicates";
            lvwDuplicates.ShowItemToolTips = true;
            lvwDuplicates.Size = new Size(1060, 450);
            lvwDuplicates.TabIndex = 1;
            lvwDuplicates.UseCompatibleStateImageBehavior = false;
            lvwDuplicates.View = View.Details;
            lvwDuplicates.ItemActivate += lvwDuplicates_ItemActivate;
            lvwDuplicates.ItemChecked += lvwDuplicates_ItemChecked;
            lvwDuplicates.SelectedIndexChanged += lvwDuplicates_SelectedIndexChanged;
            // 
            // colName
            // 
            colName.Text = "Nombre";
            colName.Width = 300;
            // 
            // colSimilarity
            // 
            colSimilarity.Text = "Similitud";
            colSimilarity.TextAlign = HorizontalAlignment.Right;
            colSimilarity.Width = 80;
            // 
            // colDuration
            // 
            colDuration.Text = "Duración";
            colDuration.TextAlign = HorizontalAlignment.Right;
            colDuration.Width = 80;
            // 
            // colFormat
            // 
            colFormat.Text = "Formato";
            colFormat.Width = 70;
            // 
            // colBitrate
            // 
            colBitrate.Text = "Bitrate";
            colBitrate.TextAlign = HorizontalAlignment.Right;
            colBitrate.Width = 90;
            // 
            // colSize
            // 
            colSize.Text = "Tamaño";
            colSize.TextAlign = HorizontalAlignment.Right;
            colSize.Width = 90;
            // 
            // colFolder
            // 
            colFolder.Text = "Carpeta";
            colFolder.Width = 320;
            // 
            // flowActions
            // 
            flowActions.AutoSize = true;
            flowActions.Controls.Add(btnPlay);
            flowActions.Controls.Add(btnReveal);
            flowActions.Controls.Add(btnSuggest);
            flowActions.Controls.Add(btnClear);
            flowActions.Dock = DockStyle.Fill;
            flowActions.Location = new Point(9, 515);
            flowActions.Margin = new Padding(0, 3, 0, 3);
            flowActions.Name = "flowActions";
            flowActions.Size = new Size(1066, 35);
            flowActions.TabIndex = 2;
            // 
            // btnPlay
            // 
            btnPlay.AutoSize = true;
            btnPlay.Enabled = false;
            btnPlay.Location = new Point(3, 3);
            btnPlay.MinimumSize = new Size(105, 29);
            btnPlay.Name = "btnPlay";
            btnPlay.Size = new Size(105, 29);
            btnPlay.TabIndex = 0;
            btnPlay.Text = "▶ Escuchar";
            btnPlay.UseVisualStyleBackColor = true;
            btnPlay.Click += btnPlay_Click;
            // 
            // btnReveal
            // 
            btnReveal.AutoSize = true;
            btnReveal.Enabled = false;
            btnReveal.Location = new Point(114, 3);
            btnReveal.Name = "btnReveal";
            btnReveal.Size = new Size(125, 29);
            btnReveal.TabIndex = 1;
            btnReveal.Text = "Abrir ubicación";
            btnReveal.UseVisualStyleBackColor = true;
            btnReveal.Click += btnReveal_Click;
            // 
            // btnSuggest
            // 
            btnSuggest.AutoSize = true;
            btnSuggest.Location = new Point(245, 3);
            btnSuggest.Name = "btnSuggest";
            btnSuggest.Size = new Size(215, 29);
            btnSuggest.TabIndex = 2;
            btnSuggest.Text = "Marcar las de menor calidad";
            btnSuggest.UseVisualStyleBackColor = true;
            btnSuggest.Click += btnSuggest_Click;
            // 
            // btnClear
            // 
            btnClear.AutoSize = true;
            btnClear.Location = new Point(466, 3);
            btnClear.Name = "btnClear";
            btnClear.Size = new Size(125, 29);
            btnClear.TabIndex = 3;
            btnClear.Text = "Desmarcar todo";
            btnClear.UseVisualStyleBackColor = true;
            btnClear.Click += btnClear_Click;
            // 
            // tableBottom
            // 
            tableBottom.AutoSize = true;
            tableBottom.ColumnCount = 3;
            tableBottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableBottom.ColumnStyles.Add(new ColumnStyle());
            tableBottom.ColumnStyles.Add(new ColumnStyle());
            tableBottom.Controls.Add(lblSelection, 0, 0);
            tableBottom.Controls.Add(btnRecycle, 1, 0);
            tableBottom.Controls.Add(btnClose, 2, 0);
            tableBottom.Dock = DockStyle.Fill;
            tableBottom.Location = new Point(9, 553);
            tableBottom.Margin = new Padding(0);
            tableBottom.Name = "tableBottom";
            tableBottom.RowCount = 1;
            tableBottom.RowStyles.Add(new RowStyle());
            tableBottom.Size = new Size(1066, 59);
            tableBottom.TabIndex = 3;
            // 
            // lblSelection
            // 
            lblSelection.Dock = DockStyle.Fill;
            lblSelection.Location = new Point(3, 0);
            lblSelection.Name = "lblSelection";
            lblSelection.Size = new Size(732, 59);
            lblSelection.TabIndex = 0;
            lblSelection.Text = "Ninguna copia marcada.";
            lblSelection.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // btnRecycle
            // 
            btnRecycle.Anchor = AnchorStyles.Right;
            btnRecycle.AutoSize = true;
            btnRecycle.Location = new Point(741, 15);
            btnRecycle.Name = "btnRecycle";
            btnRecycle.Size = new Size(210, 29);
            btnRecycle.TabIndex = 1;
            btnRecycle.Text = "🗑 Enviar marcadas a la Papelera";
            btnRecycle.UseVisualStyleBackColor = true;
            btnRecycle.Click += btnRecycle_Click;
            // 
            // btnClose
            // 
            btnClose.Anchor = AnchorStyles.Right;
            btnClose.AutoSize = true;
            btnClose.DialogResult = DialogResult.Cancel;
            btnClose.Location = new Point(957, 15);
            btnClose.MinimumSize = new Size(105, 29);
            btnClose.Name = "btnClose";
            btnClose.Size = new Size(106, 29);
            btnClose.TabIndex = 2;
            btnClose.Text = "Cerrar";
            btnClose.UseVisualStyleBackColor = true;
            // 
            // DuplicatesDialog
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnClose;
            ClientSize = new Size(1084, 621);
            Controls.Add(tableMain);
            Font = new Font("Segoe UI", 10F);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MinimizeBox = false;
            MinimumSize = new Size(800, 450);
            Name = "DuplicatesDialog";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Canciones duplicadas por su audio";
            FormClosing += DuplicatesDialog_FormClosing;
            tableMain.ResumeLayout(false);
            tableMain.PerformLayout();
            flowActions.ResumeLayout(false);
            flowActions.PerformLayout();
            tableBottom.ResumeLayout(false);
            tableBottom.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private TableLayoutPanel tableMain;
        private Label lblSummary;
        private ListView lvwDuplicates;
        private ColumnHeader colName;
        private ColumnHeader colSimilarity;
        private ColumnHeader colDuration;
        private ColumnHeader colFormat;
        private ColumnHeader colBitrate;
        private ColumnHeader colSize;
        private ColumnHeader colFolder;
        private FlowLayoutPanel flowActions;
        private Button btnPlay;
        private Button btnReveal;
        private Button btnSuggest;
        private Button btnClear;
        private TableLayoutPanel tableBottom;
        private Label lblSelection;
        private Button btnRecycle;
        private Button btnClose;
    }
}
