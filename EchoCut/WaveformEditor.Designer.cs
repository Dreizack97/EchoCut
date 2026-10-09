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
            toolStrip = new ToolStrip();
            btnPlay = new ToolStripButton();
            toolStripSeparator1 = new ToolStripSeparator();
            btnFadeIn = new ToolStripButton();
            btnFadeOut = new ToolStripButton();
            btnDelete = new ToolStripButton();
            btnRestore = new ToolStripButton();
            toolStripSeparator2 = new ToolStripSeparator();
            btnUndo = new ToolStripButton();
            btnRedo = new ToolStripButton();
            btnShortcuts = new ToolStripButton();
            btnDecibels = new ToolStripButton();
            statusStrip = new StatusStrip();
            lblStatus = new ToolStripStatusLabel();
            lblFinalLength = new ToolStripStatusLabel();
            lblFadeCount = new ToolStripStatusLabel();
            lblDeletedTotal = new ToolStripStatusLabel();
            lblEncoding = new ToolStripStatusLabel();
            tableMain = new TableLayoutPanel();
            lblTrack = new Label();
            tableViews = new TableLayoutPanel();
            lblOverview = new Label();
            viewOverview = new EchoCut.Controls.WaveformView();
            lblStartView = new Label();
            lblEndView = new Label();
            viewStart = new EchoCut.Controls.WaveformView();
            viewEnd = new EchoCut.Controls.WaveformView();
            tableInspector = new TableLayoutPanel();
            lblCopyHeader = new Label();
            lblStartCaption = new Label();
            numStart = new NumericUpDown();
            lblEndCaption = new Label();
            numEnd = new NumericUpDown();
            lblFinalCaption = new Label();
            lblFinalValue = new Label();
            flowCopyButtons = new FlowLayoutPanel();
            btnPlayStart = new Button();
            btnPlayEnd = new Button();
            btnReset = new Button();
            lblDivider = new Label();
            lblContextHeader = new Label();
            lblContextHint = new Label();
            lblFromCaption = new Label();
            numFrom = new NumericUpDown();
            lblToCaption = new Label();
            numTo = new NumericUpDown();
            lblLengthCaption = new Label();
            lblLengthValue = new Label();
            lblCurveCaption = new Label();
            cmbCurve = new ComboBox();
            pnlCurve = new Panel();
            btnContextAction = new Button();
            flowButtons = new FlowLayoutPanel();
            btnCancel = new Button();
            btnAccept = new Button();
            btnSave = new Button();
            playheadTimer = new System.Windows.Forms.Timer(components);
            toolTip = new ToolTip(components);
            toolStrip.SuspendLayout();
            statusStrip.SuspendLayout();
            tableMain.SuspendLayout();
            tableViews.SuspendLayout();
            tableInspector.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numStart).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numEnd).BeginInit();
            flowCopyButtons.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numFrom).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numTo).BeginInit();
            flowButtons.SuspendLayout();
            SuspendLayout();
            // 
            // toolStrip
            // 
            toolStrip.Font = new Font("Segoe UI", 10F);
            toolStrip.GripStyle = ToolStripGripStyle.Hidden;
            toolStrip.Items.AddRange(new ToolStripItem[] { btnPlay, toolStripSeparator1, btnFadeIn, btnFadeOut, btnDelete, btnRestore, toolStripSeparator2, btnUndo, btnRedo, btnShortcuts, btnDecibels });
            toolStrip.Location = new Point(0, 0);
            toolStrip.Name = "toolStrip";
            toolStrip.Padding = new Padding(6, 2, 6, 2);
            toolStrip.Size = new Size(1184, 29);
            toolStrip.TabIndex = 0;
            // 
            // btnPlay
            // 
            btnPlay.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnPlay.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            btnPlay.Name = "btnPlay";
            btnPlay.Size = new Size(97, 22);
            btnPlay.Text = "▶ Reproducir";
            btnPlay.ToolTipText = "Escuchar la selección, o desde el punto marcado con un clic, tal como sonará en la copia";
            btnPlay.Click += btnPlay_Click;
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(6, 25);
            // 
            // btnFadeIn
            // 
            btnFadeIn.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnFadeIn.Enabled = false;
            btnFadeIn.Name = "btnFadeIn";
            btnFadeIn.Size = new Size(91, 22);
            btnFadeIn.Text = "◢ Aparición";
            btnFadeIn.ToolTipText = "Convertir la selección en el fundido de aparición";
            btnFadeIn.Click += btnFadeIn_Click;
            // 
            // btnFadeOut
            // 
            btnFadeOut.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnFadeOut.Enabled = false;
            btnFadeOut.Name = "btnFadeOut";
            btnFadeOut.Size = new Size(112, 22);
            btnFadeOut.Text = "◣ Desaparición";
            btnFadeOut.ToolTipText = "Convertir la selección en el fundido de desaparición";
            btnFadeOut.Click += btnFadeOut_Click;
            // 
            // btnDelete
            // 
            btnDelete.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnDelete.Enabled = false;
            btnDelete.Name = "btnDelete";
            btnDelete.Size = new Size(65, 22);
            btnDelete.Text = "⌫ Borrar";
            btnDelete.ToolTipText = "Quitar de la copia el audio seleccionado y unir lo anterior con lo posterior";
            btnDelete.Click += btnDelete_Click;
            // 
            // btnRestore
            // 
            btnRestore.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnRestore.Enabled = false;
            btnRestore.Name = "btnRestore";
            btnRestore.Size = new Size(85, 22);
            btnRestore.Text = "↺ Restaurar";
            btnRestore.ToolTipText = "Devolver a la copia el audio borrado que cae dentro de la selección";
            btnRestore.Click += btnRestore_Click;
            // 
            // toolStripSeparator2
            // 
            toolStripSeparator2.Name = "toolStripSeparator2";
            toolStripSeparator2.Size = new Size(6, 25);
            // 
            // btnUndo
            // 
            btnUndo.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnUndo.Enabled = false;
            btnUndo.Name = "btnUndo";
            btnUndo.Size = new Size(85, 22);
            btnUndo.Text = "↶ Deshacer";
            btnUndo.ToolTipText = "Deshacer el último cambio";
            btnUndo.Click += btnUndo_Click;
            // 
            // btnRedo
            // 
            btnRedo.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnRedo.Enabled = false;
            btnRedo.Name = "btnRedo";
            btnRedo.Size = new Size(73, 22);
            btnRedo.Text = "↷ Rehacer";
            btnRedo.ToolTipText = "Rehacer el último cambio deshecho";
            btnRedo.Click += btnRedo_Click;
            // 
            // btnShortcuts
            // 
            btnShortcuts.Alignment = ToolStripItemAlignment.Right;
            btnShortcuts.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnShortcuts.Name = "btnShortcuts";
            btnShortcuts.Size = new Size(66, 22);
            btnShortcuts.Text = "⌨ Atajos";
            btnShortcuts.ToolTipText = "Ver todos los atajos del editor";
            btnShortcuts.Click += btnShortcuts_Click;
            // 
            // btnDecibels
            // 
            btnDecibels.Alignment = ToolStripItemAlignment.Right;
            btnDecibels.CheckOnClick = true;
            btnDecibels.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnDecibels.Name = "btnDecibels";
            btnDecibels.Size = new Size(96, 22);
            btnDecibels.Text = "Escala en dB";
            btnDecibels.ToolTipText = "Mostrar la amplitud en dB para ver colas de fundido y hiss";
            btnDecibels.CheckedChanged += btnDecibels_CheckedChanged;
            // 
            // statusStrip
            // 
            statusStrip.Font = new Font("Segoe UI", 9F);
            statusStrip.Items.AddRange(new ToolStripItem[] { lblStatus, lblFinalLength, lblFadeCount, lblDeletedTotal, lblEncoding });
            statusStrip.Location = new Point(0, 778);
            statusStrip.Name = "statusStrip";
            statusStrip.ShowItemToolTips = true;
            statusStrip.Size = new Size(1184, 22);
            statusStrip.TabIndex = 2;
            // 
            // lblStatus
            // 
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(700, 17);
            lblStatus.Spring = true;
            lblStatus.Text = "Arrastra sobre la onda para seleccionar; haz clic para marcar desde dónde escuchar.";
            lblStatus.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblFinalLength
            // 
            lblFinalLength.BorderSides = ToolStripStatusLabelBorderSides.Left;
            lblFinalLength.Name = "lblFinalLength";
            lblFinalLength.Size = new Size(110, 17);
            lblFinalLength.Text = "⏱ 00:00.000";
            lblFinalLength.ToolTipText = "Duración que tendrá la copia";
            // 
            // lblFadeCount
            // 
            lblFadeCount.BorderSides = ToolStripStatusLabelBorderSides.Left;
            lblFadeCount.Name = "lblFadeCount";
            lblFadeCount.Size = new Size(90, 17);
            lblFadeCount.Text = "◢◣ 0 fundidos";
            lblFadeCount.ToolTipText = "Fundidos que lleva la copia";
            // 
            // lblDeletedTotal
            // 
            lblDeletedTotal.BorderSides = ToolStripStatusLabelBorderSides.Left;
            lblDeletedTotal.Name = "lblDeletedTotal";
            lblDeletedTotal.Size = new Size(100, 17);
            lblDeletedTotal.Text = "⌫ 0.00 s borrados";
            lblDeletedTotal.ToolTipText = "Audio borrado dentro de lo que conserva la copia";
            // 
            // lblEncoding
            // 
            lblEncoding.BorderSides = ToolStripStatusLabelBorderSides.Left;
            lblEncoding.Name = "lblEncoding";
            lblEncoding.Size = new Size(120, 17);
            lblEncoding.Text = "✔ Sin pérdida";
            lblEncoding.ToolTipText = "Si la copia sale por copia de flujo, sin pérdida, o se vuelve a codificar";
            // 
            // tableMain
            // 
            tableMain.ColumnCount = 2;
            tableMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableMain.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 300F));
            tableMain.Controls.Add(lblTrack, 0, 0);
            tableMain.Controls.Add(tableViews, 0, 1);
            tableMain.Controls.Add(tableInspector, 1, 1);
            tableMain.Controls.Add(flowButtons, 0, 2);
            tableMain.Dock = DockStyle.Fill;
            tableMain.Location = new Point(0, 29);
            tableMain.Name = "tableMain";
            tableMain.Padding = new Padding(9, 3, 9, 3);
            tableMain.RowCount = 3;
            tableMain.RowStyles.Add(new RowStyle());
            tableMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableMain.RowStyles.Add(new RowStyle());
            tableMain.SetColumnSpan(lblTrack, 2);
            tableMain.SetColumnSpan(flowButtons, 2);
            tableMain.Size = new Size(1184, 749);
            tableMain.TabIndex = 1;
            // 
            // lblTrack
            // 
            lblTrack.AutoEllipsis = true;
            lblTrack.Dock = DockStyle.Fill;
            lblTrack.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold);
            lblTrack.Location = new Point(12, 3);
            lblTrack.Name = "lblTrack";
            lblTrack.Size = new Size(1160, 30);
            lblTrack.TabIndex = 0;
            lblTrack.Text = "Pista";
            lblTrack.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // tableViews
            // 
            tableViews.ColumnCount = 2;
            tableViews.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableViews.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableViews.Controls.Add(lblOverview, 0, 0);
            tableViews.Controls.Add(viewOverview, 0, 1);
            tableViews.Controls.Add(lblStartView, 0, 2);
            tableViews.Controls.Add(lblEndView, 1, 2);
            tableViews.Controls.Add(viewStart, 0, 3);
            tableViews.Controls.Add(viewEnd, 1, 3);
            tableViews.Dock = DockStyle.Fill;
            tableViews.Location = new Point(9, 33);
            tableViews.Margin = new Padding(0);
            tableViews.Name = "tableViews";
            tableViews.RowCount = 4;
            tableViews.RowStyles.Add(new RowStyle());
            tableViews.RowStyles.Add(new RowStyle(SizeType.Percent, 42F));
            tableViews.RowStyles.Add(new RowStyle());
            tableViews.RowStyles.Add(new RowStyle(SizeType.Percent, 58F));
            tableViews.SetColumnSpan(lblOverview, 2);
            tableViews.SetColumnSpan(viewOverview, 2);
            tableViews.Size = new Size(866, 668);
            tableViews.TabIndex = 1;
            // 
            // lblOverview
            // 
            lblOverview.AutoSize = true;
            lblOverview.Location = new Point(3, 3);
            lblOverview.Margin = new Padding(3);
            lblOverview.Name = "lblOverview";
            lblOverview.Size = new Size(98, 19);
            lblOverview.TabIndex = 0;
            lblOverview.Text = "Pista completa";
            // 
            // viewOverview
            // 
            viewOverview.AccessibleDescription = "Forma de onda de la pista completa. Arrastrar selecciona un tramo; un clic marca desde dónde escuchar. Rueda: desplazar; Ctrl+rueda: zoom. Flechas izquierda y derecha mueven la marca activa; Inicio y Fin la eligen.";
            viewOverview.AccessibleName = "Forma de onda de la pista completa";
            viewOverview.Dock = DockStyle.Fill;
            viewOverview.Location = new Point(3, 28);
            viewOverview.Name = "viewOverview";
            viewOverview.Size = new Size(860, 248);
            viewOverview.TabIndex = 1;
            viewOverview.FadeAdjusted += View_FadeAdjusted;
            viewOverview.MarkersChanged += View_MarkersChanged;
            viewOverview.SelectionChanged += View_SelectionChanged;
            viewOverview.WaveformClicked += View_WaveformClicked;
            viewOverview.MouseUp += View_MouseUp;
            // 
            // lblStartView
            // 
            lblStartView.AutoSize = true;
            lblStartView.Location = new Point(3, 285);
            lblStartView.Margin = new Padding(3, 6, 3, 3);
            lblStartView.Name = "lblStartView";
            lblStartView.Size = new Size(110, 19);
            lblStartView.TabIndex = 2;
            lblStartView.Text = "Inicio de la pista";
            // 
            // lblEndView
            // 
            lblEndView.AutoSize = true;
            lblEndView.Location = new Point(436, 285);
            lblEndView.Margin = new Padding(3, 6, 3, 3);
            lblEndView.Name = "lblEndView";
            lblEndView.Size = new Size(104, 19);
            lblEndView.TabIndex = 4;
            lblEndView.Text = "Final de la pista";
            // 
            // viewStart
            // 
            viewStart.AccessibleDescription = "Detalle del inicio de la pista. Flechas izquierda y derecha mueven el comienzo de la copia: 10 ms, 100 ms con Mayús y 1 s con Ctrl. Arrastrar selecciona un tramo; rueda y Ctrl+rueda desplazan y acercan.";
            viewStart.AccessibleName = "Forma de onda del inicio";
            viewStart.Dock = DockStyle.Fill;
            viewStart.Location = new Point(3, 310);
            viewStart.Name = "viewStart";
            viewStart.ShowEndMarker = false;
            viewStart.Size = new Size(427, 355);
            viewStart.TabIndex = 3;
            viewStart.FadeAdjusted += View_FadeAdjusted;
            viewStart.MarkersChanged += View_MarkersChanged;
            viewStart.SelectionChanged += View_SelectionChanged;
            viewStart.WaveformClicked += View_WaveformClicked;
            viewStart.MouseUp += View_MouseUp;
            // 
            // viewEnd
            // 
            viewEnd.AccessibleDescription = "Detalle del final de la pista. Flechas izquierda y derecha mueven el final de la copia: 10 ms, 100 ms con Mayús y 1 s con Ctrl. Arrastrar selecciona un tramo; rueda y Ctrl+rueda desplazan y acercan.";
            viewEnd.AccessibleName = "Forma de onda del final";
            viewEnd.Dock = DockStyle.Fill;
            viewEnd.Location = new Point(436, 310);
            viewEnd.Name = "viewEnd";
            viewEnd.ShowStartMarker = false;
            viewEnd.Size = new Size(427, 355);
            viewEnd.TabIndex = 5;
            viewEnd.FadeAdjusted += View_FadeAdjusted;
            viewEnd.MarkersChanged += View_MarkersChanged;
            viewEnd.SelectionChanged += View_SelectionChanged;
            viewEnd.WaveformClicked += View_WaveformClicked;
            viewEnd.MouseUp += View_MouseUp;
            // 
            // tableInspector
            // 
            tableInspector.ColumnCount = 2;
            tableInspector.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            tableInspector.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableInspector.Controls.Add(lblCopyHeader, 0, 0);
            tableInspector.Controls.Add(lblStartCaption, 0, 1);
            tableInspector.Controls.Add(numStart, 1, 1);
            tableInspector.Controls.Add(lblEndCaption, 0, 2);
            tableInspector.Controls.Add(numEnd, 1, 2);
            tableInspector.Controls.Add(lblFinalCaption, 0, 3);
            tableInspector.Controls.Add(lblFinalValue, 1, 3);
            tableInspector.Controls.Add(flowCopyButtons, 0, 4);
            tableInspector.Controls.Add(lblDivider, 0, 5);
            tableInspector.Controls.Add(lblContextHeader, 0, 6);
            tableInspector.Controls.Add(lblContextHint, 0, 7);
            tableInspector.Controls.Add(lblFromCaption, 0, 8);
            tableInspector.Controls.Add(numFrom, 1, 8);
            tableInspector.Controls.Add(lblToCaption, 0, 9);
            tableInspector.Controls.Add(numTo, 1, 9);
            tableInspector.Controls.Add(lblLengthCaption, 0, 10);
            tableInspector.Controls.Add(lblLengthValue, 1, 10);
            tableInspector.Controls.Add(lblCurveCaption, 0, 11);
            tableInspector.Controls.Add(cmbCurve, 1, 11);
            tableInspector.Controls.Add(pnlCurve, 0, 12);
            tableInspector.Controls.Add(btnContextAction, 0, 13);
            tableInspector.Dock = DockStyle.Fill;
            tableInspector.Location = new Point(878, 36);
            tableInspector.Margin = new Padding(6, 3, 0, 3);
            tableInspector.Name = "tableInspector";
            tableInspector.RowCount = 15;
            tableInspector.RowStyles.Add(new RowStyle());
            tableInspector.RowStyles.Add(new RowStyle());
            tableInspector.RowStyles.Add(new RowStyle());
            tableInspector.RowStyles.Add(new RowStyle());
            tableInspector.RowStyles.Add(new RowStyle());
            tableInspector.RowStyles.Add(new RowStyle());
            tableInspector.RowStyles.Add(new RowStyle());
            tableInspector.RowStyles.Add(new RowStyle());
            tableInspector.RowStyles.Add(new RowStyle());
            tableInspector.RowStyles.Add(new RowStyle());
            tableInspector.RowStyles.Add(new RowStyle());
            tableInspector.RowStyles.Add(new RowStyle());
            tableInspector.RowStyles.Add(new RowStyle(SizeType.Absolute, 96F));
            tableInspector.RowStyles.Add(new RowStyle());
            tableInspector.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableInspector.SetColumnSpan(lblCopyHeader, 2);
            tableInspector.SetColumnSpan(flowCopyButtons, 2);
            tableInspector.SetColumnSpan(lblDivider, 2);
            tableInspector.SetColumnSpan(lblContextHeader, 2);
            tableInspector.SetColumnSpan(lblContextHint, 2);
            tableInspector.SetColumnSpan(pnlCurve, 2);
            tableInspector.SetColumnSpan(btnContextAction, 2);
            tableInspector.Size = new Size(294, 662);
            tableInspector.TabIndex = 2;
            // 
            // lblCopyHeader
            // 
            lblCopyHeader.AutoSize = true;
            lblCopyHeader.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            lblCopyHeader.Location = new Point(3, 3);
            lblCopyHeader.Margin = new Padding(3, 3, 3, 6);
            lblCopyHeader.Name = "lblCopyHeader";
            lblCopyHeader.Size = new Size(44, 19);
            lblCopyHeader.TabIndex = 0;
            lblCopyHeader.Text = "Copia";
            // 
            // lblStartCaption
            // 
            lblStartCaption.Anchor = AnchorStyles.Left;
            lblStartCaption.AutoSize = true;
            lblStartCaption.Location = new Point(3, 34);
            lblStartCaption.Name = "lblStartCaption";
            lblStartCaption.Size = new Size(97, 19);
            lblStartCaption.TabIndex = 1;
            lblStartCaption.Text = "Comienzo (s):";
            // 
            // numStart
            // 
            numStart.AccessibleName = "Comienzo de la copia en segundos";
            numStart.DecimalPlaces = 3;
            numStart.Dock = DockStyle.Fill;
            numStart.Increment = new decimal(new int[] { 1, 0, 0, 131072 });
            numStart.Location = new Point(123, 31);
            numStart.Name = "numStart";
            numStart.Size = new Size(168, 25);
            numStart.TabIndex = 2;
            numStart.TextAlign = HorizontalAlignment.Right;
            numStart.ValueChanged += numStart_ValueChanged;
            // 
            // lblEndCaption
            // 
            lblEndCaption.Anchor = AnchorStyles.Left;
            lblEndCaption.AutoSize = true;
            lblEndCaption.Location = new Point(3, 65);
            lblEndCaption.Name = "lblEndCaption";
            lblEndCaption.Size = new Size(64, 19);
            lblEndCaption.TabIndex = 3;
            lblEndCaption.Text = "Final (s):";
            // 
            // numEnd
            // 
            numEnd.AccessibleName = "Final de la copia en segundos";
            numEnd.DecimalPlaces = 3;
            numEnd.Dock = DockStyle.Fill;
            numEnd.Increment = new decimal(new int[] { 1, 0, 0, 131072 });
            numEnd.Location = new Point(123, 62);
            numEnd.Name = "numEnd";
            numEnd.Size = new Size(168, 25);
            numEnd.TabIndex = 4;
            numEnd.TextAlign = HorizontalAlignment.Right;
            numEnd.ValueChanged += numEnd_ValueChanged;
            // 
            // lblFinalCaption
            // 
            lblFinalCaption.Anchor = AnchorStyles.Left;
            lblFinalCaption.AutoSize = true;
            lblFinalCaption.Location = new Point(3, 93);
            lblFinalCaption.Name = "lblFinalCaption";
            lblFinalCaption.Size = new Size(100, 19);
            lblFinalCaption.TabIndex = 5;
            lblFinalCaption.Text = "Duración final:";
            // 
            // lblFinalValue
            // 
            lblFinalValue.Anchor = AnchorStyles.Right;
            lblFinalValue.AutoSize = true;
            lblFinalValue.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            lblFinalValue.Location = new Point(220, 93);
            lblFinalValue.Name = "lblFinalValue";
            lblFinalValue.Size = new Size(71, 19);
            lblFinalValue.TabIndex = 6;
            lblFinalValue.Text = "00:00.000";
            // 
            // flowCopyButtons
            // 
            flowCopyButtons.AutoSize = true;
            flowCopyButtons.Controls.Add(btnPlayStart);
            flowCopyButtons.Controls.Add(btnPlayEnd);
            flowCopyButtons.Controls.Add(btnReset);
            flowCopyButtons.Dock = DockStyle.Fill;
            flowCopyButtons.Location = new Point(0, 118);
            flowCopyButtons.Margin = new Padding(0, 3, 0, 3);
            flowCopyButtons.Name = "flowCopyButtons";
            flowCopyButtons.Size = new Size(294, 70);
            flowCopyButtons.TabIndex = 7;
            // 
            // btnPlayStart
            // 
            btnPlayStart.AutoSize = true;
            btnPlayStart.Location = new Point(3, 3);
            btnPlayStart.Name = "btnPlayStart";
            btnPlayStart.Size = new Size(84, 29);
            btnPlayStart.TabIndex = 0;
            btnPlayStart.Text = "▶ Inicio";
            toolTip.SetToolTip(btnPlayStart, "Escuchar el comienzo de la copia tal como quedará");
            btnPlayStart.UseVisualStyleBackColor = true;
            btnPlayStart.Click += btnPlayStart_Click;
            // 
            // btnPlayEnd
            // 
            btnPlayEnd.AutoSize = true;
            btnPlayEnd.Location = new Point(93, 3);
            btnPlayEnd.Name = "btnPlayEnd";
            btnPlayEnd.Size = new Size(84, 29);
            btnPlayEnd.TabIndex = 1;
            btnPlayEnd.Text = "▶ Final";
            toolTip.SetToolTip(btnPlayEnd, "Escuchar el final de la copia tal como quedará");
            btnPlayEnd.UseVisualStyleBackColor = true;
            btnPlayEnd.Click += btnPlayEnd_Click;
            // 
            // btnReset
            // 
            btnReset.AutoSize = true;
            btnReset.Location = new Point(3, 38);
            btnReset.Name = "btnReset";
            btnReset.Size = new Size(140, 29);
            btnReset.TabIndex = 2;
            btnReset.Text = "Restablecer análisis";
            toolTip.SetToolTip(btnReset, "Volver al comienzo y al final que propuso el análisis");
            btnReset.UseVisualStyleBackColor = true;
            btnReset.Click += btnReset_Click;
            // 
            // lblDivider
            // 
            lblDivider.BorderStyle = BorderStyle.Fixed3D;
            lblDivider.Dock = DockStyle.Top;
            lblDivider.Location = new Point(3, 200);
            lblDivider.Margin = new Padding(3, 9, 3, 9);
            lblDivider.Name = "lblDivider";
            lblDivider.Size = new Size(288, 2);
            lblDivider.TabIndex = 8;
            // 
            // lblContextHeader
            // 
            lblContextHeader.AutoSize = true;
            lblContextHeader.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            lblContextHeader.Location = new Point(3, 214);
            lblContextHeader.Margin = new Padding(3, 3, 3, 3);
            lblContextHeader.Name = "lblContextHeader";
            lblContextHeader.Size = new Size(58, 19);
            lblContextHeader.TabIndex = 9;
            lblContextHeader.Text = "Detalles";
            // 
            // lblContextHint
            // 
            lblContextHint.AutoSize = true;
            lblContextHint.ForeColor = SystemColors.GrayText;
            lblContextHint.Location = new Point(3, 239);
            lblContextHint.Margin = new Padding(3, 0, 3, 6);
            lblContextHint.MaximumSize = new Size(288, 0);
            lblContextHint.Name = "lblContextHint";
            lblContextHint.Size = new Size(280, 38);
            lblContextHint.TabIndex = 10;
            lblContextHint.Text = "Selecciona un tramo, o haz clic en un fundido o en un fragmento borrado, para ver aquí sus detalles.";
            // 
            // lblFromCaption
            // 
            lblFromCaption.Anchor = AnchorStyles.Left;
            lblFromCaption.AutoSize = true;
            lblFromCaption.Location = new Point(3, 289);
            lblFromCaption.Name = "lblFromCaption";
            lblFromCaption.Size = new Size(76, 19);
            lblFromCaption.TabIndex = 11;
            lblFromCaption.Text = "Desde (s):";
            // 
            // numFrom
            // 
            numFrom.AccessibleName = "Comienzo del elemento en segundos";
            numFrom.DecimalPlaces = 3;
            numFrom.Dock = DockStyle.Fill;
            numFrom.Increment = new decimal(new int[] { 1, 0, 0, 131072 });
            numFrom.Location = new Point(123, 286);
            numFrom.Name = "numFrom";
            numFrom.Size = new Size(168, 25);
            numFrom.TabIndex = 12;
            numFrom.TextAlign = HorizontalAlignment.Right;
            numFrom.ValueChanged += numContext_ValueChanged;
            // 
            // lblToCaption
            // 
            lblToCaption.Anchor = AnchorStyles.Left;
            lblToCaption.AutoSize = true;
            lblToCaption.Location = new Point(3, 320);
            lblToCaption.Name = "lblToCaption";
            lblToCaption.Size = new Size(72, 19);
            lblToCaption.TabIndex = 13;
            lblToCaption.Text = "Hasta (s):";
            // 
            // numTo
            // 
            numTo.AccessibleName = "Final del elemento en segundos";
            numTo.DecimalPlaces = 3;
            numTo.Dock = DockStyle.Fill;
            numTo.Increment = new decimal(new int[] { 1, 0, 0, 131072 });
            numTo.Location = new Point(123, 317);
            numTo.Name = "numTo";
            numTo.Size = new Size(168, 25);
            numTo.TabIndex = 14;
            numTo.TextAlign = HorizontalAlignment.Right;
            numTo.ValueChanged += numContext_ValueChanged;
            // 
            // lblLengthCaption
            // 
            lblLengthCaption.Anchor = AnchorStyles.Left;
            lblLengthCaption.AutoSize = true;
            lblLengthCaption.Location = new Point(3, 348);
            lblLengthCaption.Name = "lblLengthCaption";
            lblLengthCaption.Size = new Size(67, 19);
            lblLengthCaption.TabIndex = 15;
            lblLengthCaption.Text = "Duración:";
            // 
            // lblLengthValue
            // 
            lblLengthValue.Anchor = AnchorStyles.Right;
            lblLengthValue.AutoSize = true;
            lblLengthValue.Location = new Point(240, 348);
            lblLengthValue.Name = "lblLengthValue";
            lblLengthValue.Size = new Size(51, 19);
            lblLengthValue.TabIndex = 16;
            lblLengthValue.Text = "0.000 s";
            // 
            // lblCurveCaption
            // 
            lblCurveCaption.Anchor = AnchorStyles.Left;
            lblCurveCaption.AutoSize = true;
            lblCurveCaption.Location = new Point(3, 377);
            lblCurveCaption.Name = "lblCurveCaption";
            lblCurveCaption.Size = new Size(49, 19);
            lblCurveCaption.TabIndex = 17;
            lblCurveCaption.Text = "Curva:";
            // 
            // cmbCurve
            // 
            cmbCurve.AccessibleName = "Curva del fundido";
            cmbCurve.Dock = DockStyle.Fill;
            cmbCurve.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbCurve.FormattingEnabled = true;
            cmbCurve.Location = new Point(123, 374);
            cmbCurve.Name = "cmbCurve";
            cmbCurve.Size = new Size(168, 25);
            cmbCurve.TabIndex = 18;
            cmbCurve.SelectedIndexChanged += cmbCurve_SelectedIndexChanged;
            // 
            // pnlCurve
            // 
            pnlCurve.AccessibleName = "Forma de la curva del fundido";
            pnlCurve.AccessibleRole = AccessibleRole.Graphic;
            pnlCurve.BackColor = Color.White;
            pnlCurve.BorderStyle = BorderStyle.FixedSingle;
            pnlCurve.Dock = DockStyle.Fill;
            pnlCurve.Location = new Point(3, 405);
            pnlCurve.Name = "pnlCurve";
            pnlCurve.Size = new Size(288, 90);
            pnlCurve.TabIndex = 19;
            pnlCurve.Paint += pnlCurve_Paint;
            // 
            // btnContextAction
            // 
            btnContextAction.AutoSize = true;
            btnContextAction.Location = new Point(3, 501);
            btnContextAction.Name = "btnContextAction";
            btnContextAction.Size = new Size(130, 29);
            btnContextAction.TabIndex = 20;
            btnContextAction.Text = "Quitar fundido";
            btnContextAction.UseVisualStyleBackColor = true;
            btnContextAction.Click += btnContextAction_Click;
            // 
            // flowButtons
            // 
            flowButtons.AutoSize = true;
            flowButtons.Controls.Add(btnCancel);
            flowButtons.Controls.Add(btnAccept);
            flowButtons.Controls.Add(btnSave);
            flowButtons.Dock = DockStyle.Fill;
            flowButtons.FlowDirection = FlowDirection.RightToLeft;
            flowButtons.Location = new Point(9, 704);
            flowButtons.Margin = new Padding(0, 3, 0, 0);
            flowButtons.Name = "flowButtons";
            flowButtons.Size = new Size(1166, 42);
            flowButtons.TabIndex = 3;
            // 
            // btnCancel
            // 
            btnCancel.AutoSize = true;
            btnCancel.Location = new Point(1057, 3);
            btnCancel.MinimumSize = new Size(105, 29);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(106, 29);
            btnCancel.TabIndex = 2;
            btnCancel.Text = "Cancelar";
            toolTip.SetToolTip(btnCancel, "Cerrar la ventana; si hay cambios sin aplicar, pregunta qué hacer con ellos");
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;
            // 
            // btnAccept
            // 
            btnAccept.AutoSize = true;
            btnAccept.Location = new Point(946, 3);
            btnAccept.MinimumSize = new Size(105, 29);
            btnAccept.Name = "btnAccept";
            btnAccept.Size = new Size(105, 29);
            btnAccept.TabIndex = 1;
            btnAccept.Text = "Aceptar";
            toolTip.SetToolTip(btnAccept, "Llevar los ajustes a la fila y cerrar (Ctrl+Entrar)");
            btnAccept.UseVisualStyleBackColor = true;
            btnAccept.Click += btnAccept_Click;
            // 
            // btnSave
            // 
            btnSave.AccessibleDescription = "Aplica los ajustes a la fila y escribe la copia en la carpeta «Recortados», sin cerrar la ventana.";
            btnSave.AutoSize = true;
            btnSave.Location = new Point(835, 3);
            btnSave.MinimumSize = new Size(105, 29);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(105, 29);
            btnSave.TabIndex = 0;
            btnSave.Text = "💾 Guardar";
            toolTip.SetToolTip(btnSave, "Llevar los ajustes a la fila y escribir la copia en «Recortados» (Ctrl+S)");
            btnSave.UseVisualStyleBackColor = true;
            btnSave.Click += btnSave_Click;
            // 
            // playheadTimer
            // 
            playheadTimer.Interval = 30;
            playheadTimer.Tick += playheadTimer_Tick;
            // 
            // WaveformEditor
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnCancel;
            ClientSize = new Size(1184, 800);
            Controls.Add(tableMain);
            Controls.Add(statusStrip);
            Controls.Add(toolStrip);
            Font = new Font("Segoe UI", 10F);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MinimumSize = new Size(1000, 640);
            Name = "WaveformEditor";
            StartPosition = FormStartPosition.WindowsDefaultLocation;
            Text = "Forma de onda, recorte y fundidos";
            FormClosing += WaveformEditor_FormClosing;
            FormClosed += WaveformEditor_FormClosed;
            Shown += WaveformEditor_Shown;
            toolStrip.ResumeLayout(false);
            toolStrip.PerformLayout();
            statusStrip.ResumeLayout(false);
            statusStrip.PerformLayout();
            tableMain.ResumeLayout(false);
            tableMain.PerformLayout();
            tableViews.ResumeLayout(false);
            tableViews.PerformLayout();
            tableInspector.ResumeLayout(false);
            tableInspector.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numStart).EndInit();
            ((System.ComponentModel.ISupportInitialize)numEnd).EndInit();
            flowCopyButtons.ResumeLayout(false);
            flowCopyButtons.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numFrom).EndInit();
            ((System.ComponentModel.ISupportInitialize)numTo).EndInit();
            flowButtons.ResumeLayout(false);
            flowButtons.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ToolStrip toolStrip;
        private ToolStripButton btnPlay;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripButton btnFadeIn;
        private ToolStripButton btnFadeOut;
        private ToolStripButton btnDelete;
        private ToolStripButton btnRestore;
        private ToolStripSeparator toolStripSeparator2;
        private ToolStripButton btnUndo;
        private ToolStripButton btnRedo;
        private ToolStripButton btnShortcuts;
        private ToolStripButton btnDecibels;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel lblStatus;
        private ToolStripStatusLabel lblFinalLength;
        private ToolStripStatusLabel lblFadeCount;
        private ToolStripStatusLabel lblDeletedTotal;
        private ToolStripStatusLabel lblEncoding;
        private TableLayoutPanel tableMain;
        private Label lblTrack;
        private TableLayoutPanel tableViews;
        private Label lblOverview;
        private EchoCut.Controls.WaveformView viewOverview;
        private Label lblStartView;
        private Label lblEndView;
        private EchoCut.Controls.WaveformView viewStart;
        private EchoCut.Controls.WaveformView viewEnd;
        private TableLayoutPanel tableInspector;
        private Label lblCopyHeader;
        private Label lblStartCaption;
        private NumericUpDown numStart;
        private Label lblEndCaption;
        private NumericUpDown numEnd;
        private Label lblFinalCaption;
        private Label lblFinalValue;
        private FlowLayoutPanel flowCopyButtons;
        private Button btnPlayStart;
        private Button btnPlayEnd;
        private Button btnReset;
        private Label lblDivider;
        private Label lblContextHeader;
        private Label lblContextHint;
        private Label lblFromCaption;
        private NumericUpDown numFrom;
        private Label lblToCaption;
        private NumericUpDown numTo;
        private Label lblLengthCaption;
        private Label lblLengthValue;
        private Label lblCurveCaption;
        private ComboBox cmbCurve;
        private Panel pnlCurve;
        private Button btnContextAction;
        private FlowLayoutPanel flowButtons;
        private Button btnCancel;
        private Button btnAccept;
        private Button btnSave;
        private System.Windows.Forms.Timer playheadTimer;
        private ToolTip toolTip;
    }
}
