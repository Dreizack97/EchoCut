namespace EchoCut
{
    partial class WaveformEditor
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
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(WaveformEditor));
            tableMain = new TableLayoutPanel();
            lblTrack = new Label();
            lblOverview = new Label();
            viewOverview = new EchoCut.Controls.WaveformView();
            tableEdges = new TableLayoutPanel();
            grpStart = new GroupBox();
            tableStart = new TableLayoutPanel();
            viewStart = new EchoCut.Controls.WaveformView();
            flowStart = new FlowLayoutPanel();
            lblStartCaption = new Label();
            numStart = new NumericUpDown();
            btnPlayStart = new Button();
            grpEnd = new GroupBox();
            tableEnd = new TableLayoutPanel();
            viewEnd = new EchoCut.Controls.WaveformView();
            flowEnd = new FlowLayoutPanel();
            lblEndCaption = new Label();
            numEnd = new NumericUpDown();
            btnPlayEnd = new Button();
            tableBottom = new TableLayoutPanel();
            lblSummary = new Label();
            chkDecibels = new CheckBox();
            btnReset = new Button();
            btnAccept = new Button();
            btnCancel = new Button();
            playheadTimer = new System.Windows.Forms.Timer(components);
            tableMain.SuspendLayout();
            tableEdges.SuspendLayout();
            grpStart.SuspendLayout();
            tableStart.SuspendLayout();
            flowStart.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numStart).BeginInit();
            grpEnd.SuspendLayout();
            tableEnd.SuspendLayout();
            flowEnd.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numEnd).BeginInit();
            tableBottom.SuspendLayout();
            SuspendLayout();
            // 
            // tableMain
            // 
            tableMain.ColumnCount = 1;
            tableMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableMain.Controls.Add(lblTrack, 0, 0);
            tableMain.Controls.Add(lblOverview, 0, 1);
            tableMain.Controls.Add(viewOverview, 0, 2);
            tableMain.Controls.Add(tableEdges, 0, 3);
            tableMain.Controls.Add(tableBottom, 0, 4);
            tableMain.Dock = DockStyle.Fill;
            tableMain.Location = new Point(0, 0);
            tableMain.Name = "tableMain";
            tableMain.Padding = new Padding(9);
            tableMain.RowCount = 5;
            tableMain.RowStyles.Add(new RowStyle());
            tableMain.RowStyles.Add(new RowStyle());
            tableMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 170F));
            tableMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableMain.RowStyles.Add(new RowStyle());
            tableMain.Size = new Size(1184, 721);
            tableMain.TabIndex = 0;
            // 
            // lblTrack
            // 
            lblTrack.AutoEllipsis = true;
            lblTrack.Dock = DockStyle.Fill;
            lblTrack.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold);
            lblTrack.Location = new Point(12, 9);
            lblTrack.Name = "lblTrack";
            lblTrack.Size = new Size(1160, 30);
            lblTrack.TabIndex = 0;
            lblTrack.Text = "Pista";
            lblTrack.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblOverview
            // 
            lblOverview.AutoSize = true;
            lblOverview.Location = new Point(12, 42);
            lblOverview.Margin = new Padding(3);
            lblOverview.Name = "lblOverview";
            lblOverview.Size = new Size(98, 19);
            lblOverview.TabIndex = 1;
            lblOverview.Text = "Pista completa";
            // 
            // viewOverview
            // 
            viewOverview.AccessibleDescription = "Forma de onda de la pista completa. Flechas izquierda y derecha mueven la marca activa; Inicio y Fin eligen la marca.";
            viewOverview.AccessibleName = "Forma de onda de la pista completa";
            viewOverview.Dock = DockStyle.Fill;
            viewOverview.Location = new Point(12, 67);
            viewOverview.Name = "viewOverview";
            viewOverview.Size = new Size(1160, 164);
            viewOverview.TabIndex = 2;
            viewOverview.MarkersChanged += View_MarkersChanged;
            // 
            // tableEdges
            // 
            tableEdges.ColumnCount = 2;
            tableEdges.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableEdges.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableEdges.Controls.Add(grpStart, 0, 0);
            tableEdges.Controls.Add(grpEnd, 1, 0);
            tableEdges.Dock = DockStyle.Fill;
            tableEdges.Location = new Point(9, 237);
            tableEdges.Margin = new Padding(0, 3, 0, 3);
            tableEdges.Name = "tableEdges";
            tableEdges.RowCount = 1;
            tableEdges.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableEdges.Size = new Size(1166, 418);
            tableEdges.TabIndex = 3;
            // 
            // grpStart
            // 
            grpStart.Controls.Add(tableStart);
            grpStart.Dock = DockStyle.Fill;
            grpStart.Location = new Point(3, 3);
            grpStart.Name = "grpStart";
            grpStart.Size = new Size(577, 412);
            grpStart.TabIndex = 0;
            grpStart.TabStop = false;
            grpStart.Text = "Inicio de la pista";
            // 
            // tableStart
            // 
            tableStart.ColumnCount = 1;
            tableStart.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableStart.Controls.Add(viewStart, 0, 0);
            tableStart.Controls.Add(flowStart, 0, 1);
            tableStart.Dock = DockStyle.Fill;
            tableStart.Location = new Point(3, 21);
            tableStart.Name = "tableStart";
            tableStart.RowCount = 2;
            tableStart.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableStart.RowStyles.Add(new RowStyle());
            tableStart.Size = new Size(571, 388);
            tableStart.TabIndex = 0;
            // 
            // viewStart
            // 
            viewStart.AccessibleDescription = "Detalle del inicio de la pista. Flechas izquierda y derecha mueven el comienzo de la copia: 10 ms, 100 ms con Mayús y 1 s con Ctrl.";
            viewStart.AccessibleName = "Forma de onda del inicio";
            viewStart.Dock = DockStyle.Fill;
            viewStart.Location = new Point(3, 3);
            viewStart.Name = "viewStart";
            viewStart.ShowEndMarker = false;
            viewStart.Size = new Size(565, 341);
            viewStart.TabIndex = 0;
            viewStart.MarkersChanged += View_MarkersChanged;
            // 
            // flowStart
            // 
            flowStart.AutoSize = true;
            flowStart.Controls.Add(lblStartCaption);
            flowStart.Controls.Add(numStart);
            flowStart.Controls.Add(btnPlayStart);
            flowStart.Dock = DockStyle.Fill;
            flowStart.Location = new Point(3, 350);
            flowStart.Name = "flowStart";
            flowStart.Size = new Size(565, 35);
            flowStart.TabIndex = 1;
            flowStart.WrapContents = false;
            // 
            // lblStartCaption
            // 
            lblStartCaption.Anchor = AnchorStyles.Left;
            lblStartCaption.AutoSize = true;
            lblStartCaption.Location = new Point(3, 8);
            lblStartCaption.Name = "lblStartCaption";
            lblStartCaption.Size = new Size(160, 19);
            lblStartCaption.TabIndex = 0;
            lblStartCaption.Text = "Comienzo de la copia (s):";
            // 
            // numStart
            // 
            numStart.AccessibleName = "Comienzo de la copia en segundos";
            numStart.Anchor = AnchorStyles.Left;
            numStart.DecimalPlaces = 3;
            numStart.Increment = new decimal(new int[] { 1, 0, 0, 131072 });
            numStart.Location = new Point(169, 5);
            numStart.Name = "numStart";
            numStart.Size = new Size(110, 25);
            numStart.TabIndex = 1;
            numStart.TextAlign = HorizontalAlignment.Right;
            numStart.ValueChanged += numStart_ValueChanged;
            // 
            // btnPlayStart
            // 
            btnPlayStart.AutoSize = true;
            btnPlayStart.Location = new Point(285, 3);
            btnPlayStart.Name = "btnPlayStart";
            btnPlayStart.Size = new Size(140, 29);
            btnPlayStart.TabIndex = 2;
            btnPlayStart.Text = "▶ Escuchar inicio";
            btnPlayStart.UseVisualStyleBackColor = true;
            btnPlayStart.Click += btnPlayStart_Click;
            // 
            // grpEnd
            // 
            grpEnd.Controls.Add(tableEnd);
            grpEnd.Dock = DockStyle.Fill;
            grpEnd.Location = new Point(586, 3);
            grpEnd.Name = "grpEnd";
            grpEnd.Size = new Size(577, 412);
            grpEnd.TabIndex = 1;
            grpEnd.TabStop = false;
            grpEnd.Text = "Final de la pista";
            // 
            // tableEnd
            // 
            tableEnd.ColumnCount = 1;
            tableEnd.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableEnd.Controls.Add(viewEnd, 0, 0);
            tableEnd.Controls.Add(flowEnd, 0, 1);
            tableEnd.Dock = DockStyle.Fill;
            tableEnd.Location = new Point(3, 21);
            tableEnd.Name = "tableEnd";
            tableEnd.RowCount = 2;
            tableEnd.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableEnd.RowStyles.Add(new RowStyle());
            tableEnd.Size = new Size(571, 388);
            tableEnd.TabIndex = 0;
            // 
            // viewEnd
            // 
            viewEnd.AccessibleDescription = "Detalle del final de la pista. Flechas izquierda y derecha mueven el final de la copia: 10 ms, 100 ms con Mayús y 1 s con Ctrl.";
            viewEnd.AccessibleName = "Forma de onda del final";
            viewEnd.Dock = DockStyle.Fill;
            viewEnd.Location = new Point(3, 3);
            viewEnd.Name = "viewEnd";
            viewEnd.ShowStartMarker = false;
            viewEnd.Size = new Size(565, 341);
            viewEnd.TabIndex = 0;
            viewEnd.MarkersChanged += View_MarkersChanged;
            // 
            // flowEnd
            // 
            flowEnd.AutoSize = true;
            flowEnd.Controls.Add(lblEndCaption);
            flowEnd.Controls.Add(numEnd);
            flowEnd.Controls.Add(btnPlayEnd);
            flowEnd.Dock = DockStyle.Fill;
            flowEnd.Location = new Point(3, 350);
            flowEnd.Name = "flowEnd";
            flowEnd.Size = new Size(565, 35);
            flowEnd.TabIndex = 1;
            flowEnd.WrapContents = false;
            // 
            // lblEndCaption
            // 
            lblEndCaption.Anchor = AnchorStyles.Left;
            lblEndCaption.AutoSize = true;
            lblEndCaption.Location = new Point(3, 8);
            lblEndCaption.Name = "lblEndCaption";
            lblEndCaption.Size = new Size(127, 19);
            lblEndCaption.TabIndex = 0;
            lblEndCaption.Text = "Final de la copia (s):";
            // 
            // numEnd
            // 
            numEnd.AccessibleName = "Final de la copia en segundos";
            numEnd.Anchor = AnchorStyles.Left;
            numEnd.DecimalPlaces = 3;
            numEnd.Increment = new decimal(new int[] { 1, 0, 0, 131072 });
            numEnd.Location = new Point(136, 5);
            numEnd.Name = "numEnd";
            numEnd.Size = new Size(110, 25);
            numEnd.TabIndex = 1;
            numEnd.TextAlign = HorizontalAlignment.Right;
            numEnd.ValueChanged += numEnd_ValueChanged;
            // 
            // btnPlayEnd
            // 
            btnPlayEnd.AutoSize = true;
            btnPlayEnd.Location = new Point(252, 3);
            btnPlayEnd.Name = "btnPlayEnd";
            btnPlayEnd.Size = new Size(135, 29);
            btnPlayEnd.TabIndex = 2;
            btnPlayEnd.Text = "▶ Escuchar final";
            btnPlayEnd.UseVisualStyleBackColor = true;
            btnPlayEnd.Click += btnPlayEnd_Click;
            // 
            // tableBottom
            // 
            tableBottom.AutoSize = true;
            tableBottom.ColumnCount = 5;
            tableBottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableBottom.ColumnStyles.Add(new ColumnStyle());
            tableBottom.ColumnStyles.Add(new ColumnStyle());
            tableBottom.ColumnStyles.Add(new ColumnStyle());
            tableBottom.ColumnStyles.Add(new ColumnStyle());
            tableBottom.Controls.Add(lblSummary, 0, 0);
            tableBottom.Controls.Add(chkDecibels, 1, 0);
            tableBottom.Controls.Add(btnReset, 2, 0);
            tableBottom.Controls.Add(btnAccept, 3, 0);
            tableBottom.Controls.Add(btnCancel, 4, 0);
            tableBottom.Dock = DockStyle.Fill;
            tableBottom.Location = new Point(9, 661);
            tableBottom.Margin = new Padding(0, 3, 0, 0);
            tableBottom.Name = "tableBottom";
            tableBottom.RowCount = 1;
            tableBottom.RowStyles.Add(new RowStyle());
            tableBottom.Size = new Size(1166, 51);
            tableBottom.TabIndex = 4;
            // 
            // lblSummary
            // 
            lblSummary.AutoEllipsis = true;
            lblSummary.Dock = DockStyle.Fill;
            lblSummary.Location = new Point(3, 0);
            lblSummary.Name = "lblSummary";
            lblSummary.Size = new Size(673, 51);
            lblSummary.TabIndex = 0;
            lblSummary.Text = "Calculando forma de onda…";
            lblSummary.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // chkDecibels
            // 
            chkDecibels.Anchor = AnchorStyles.Right;
            chkDecibels.AutoSize = true;
            chkDecibels.Location = new Point(682, 14);
            chkDecibels.Margin = new Padding(3, 3, 12, 3);
            chkDecibels.Name = "chkDecibels";
            chkDecibels.Size = new Size(103, 23);
            chkDecibels.TabIndex = 1;
            chkDecibels.Text = "Escala en dB";
            chkDecibels.UseVisualStyleBackColor = true;
            chkDecibels.CheckedChanged += chkDecibels_CheckedChanged;
            // 
            // btnReset
            // 
            btnReset.Anchor = AnchorStyles.Right;
            btnReset.AutoSize = true;
            btnReset.Location = new Point(800, 11);
            btnReset.MinimumSize = new Size(110, 29);
            btnReset.Name = "btnReset";
            btnReset.Size = new Size(140, 29);
            btnReset.TabIndex = 2;
            btnReset.Text = "Restablecer análisis";
            btnReset.UseVisualStyleBackColor = true;
            btnReset.Click += btnReset_Click;
            // 
            // btnAccept
            // 
            btnAccept.Anchor = AnchorStyles.Right;
            btnAccept.AutoSize = true;
            btnAccept.DialogResult = DialogResult.OK;
            btnAccept.Location = new Point(946, 11);
            btnAccept.MinimumSize = new Size(105, 29);
            btnAccept.Name = "btnAccept";
            btnAccept.Size = new Size(105, 29);
            btnAccept.TabIndex = 3;
            btnAccept.Text = "Aceptar";
            btnAccept.UseVisualStyleBackColor = true;
            btnAccept.Click += btnAccept_Click;
            // 
            // btnCancel
            // 
            btnCancel.Anchor = AnchorStyles.Right;
            btnCancel.AutoSize = true;
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new Point(1057, 11);
            btnCancel.MinimumSize = new Size(105, 29);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(106, 29);
            btnCancel.TabIndex = 4;
            btnCancel.Text = "Cancelar";
            btnCancel.UseVisualStyleBackColor = true;
            // 
            // playheadTimer
            // 
            playheadTimer.Interval = 30;
            playheadTimer.Tick += playheadTimer_Tick;
            // 
            // WaveformEditor
            // 
            AcceptButton = btnAccept;
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnCancel;
            ClientSize = new Size(1184, 721);
            Controls.Add(tableMain);
            Font = new Font("Segoe UI", 10F);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MinimizeBox = false;
            MinimumSize = new Size(900, 600);
            Name = "WaveformEditor";
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Forma de onda y ajuste del recorte";
            FormClosing += WaveformEditor_FormClosing;
            FormClosed += WaveformEditor_FormClosed;
            Shown += WaveformEditor_Shown;
            tableMain.ResumeLayout(false);
            tableMain.PerformLayout();
            tableEdges.ResumeLayout(false);
            grpStart.ResumeLayout(false);
            tableStart.ResumeLayout(false);
            tableStart.PerformLayout();
            flowStart.ResumeLayout(false);
            flowStart.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numStart).EndInit();
            grpEnd.ResumeLayout(false);
            tableEnd.ResumeLayout(false);
            tableEnd.PerformLayout();
            flowEnd.ResumeLayout(false);
            flowEnd.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numEnd).EndInit();
            tableBottom.ResumeLayout(false);
            tableBottom.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private TableLayoutPanel tableMain;
        private Label lblTrack;
        private Label lblOverview;
        private EchoCut.Controls.WaveformView viewOverview;
        private TableLayoutPanel tableEdges;
        private GroupBox grpStart;
        private TableLayoutPanel tableStart;
        private EchoCut.Controls.WaveformView viewStart;
        private FlowLayoutPanel flowStart;
        private Label lblStartCaption;
        private NumericUpDown numStart;
        private Button btnPlayStart;
        private GroupBox grpEnd;
        private TableLayoutPanel tableEnd;
        private EchoCut.Controls.WaveformView viewEnd;
        private FlowLayoutPanel flowEnd;
        private Label lblEndCaption;
        private NumericUpDown numEnd;
        private Button btnPlayEnd;
        private TableLayoutPanel tableBottom;
        private Label lblSummary;
        private CheckBox chkDecibels;
        private Button btnReset;
        private Button btnAccept;
        private Button btnCancel;
        private System.Windows.Forms.Timer playheadTimer;
    }
}
