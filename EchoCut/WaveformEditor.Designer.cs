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
            flowSelection = new FlowLayoutPanel();
            lblSelection = new Label();
            btnSelFadeIn = new Button();
            btnSelFadeOut = new Button();
            btnSelDelete = new Button();
            btnSelRestore = new Button();
            btnSelPlay = new Button();
            tableEdges = new TableLayoutPanel();
            grpStart = new GroupBox();
            tableStart = new TableLayoutPanel();
            viewStart = new EchoCut.Controls.WaveformView();
            flowStart = new FlowLayoutPanel();
            lblStartCaption = new Label();
            numStart = new NumericUpDown();
            btnPlayStart = new Button();
            flowFadeIn = new FlowLayoutPanel();
            chkFadeIn = new CheckBox();
            numFadeInStart = new NumericUpDown();
            lblFadeInTo = new Label();
            numFadeInEnd = new NumericUpDown();
            cmbFadeInCurve = new ComboBox();
            grpEnd = new GroupBox();
            tableEnd = new TableLayoutPanel();
            viewEnd = new EchoCut.Controls.WaveformView();
            flowEnd = new FlowLayoutPanel();
            lblEndCaption = new Label();
            numEnd = new NumericUpDown();
            btnPlayEnd = new Button();
            flowFadeOut = new FlowLayoutPanel();
            chkFadeOut = new CheckBox();
            numFadeOutStart = new NumericUpDown();
            lblFadeOutTo = new Label();
            numFadeOutEnd = new NumericUpDown();
            cmbFadeOutCurve = new ComboBox();
            tableBottom = new TableLayoutPanel();
            lblSummary = new Label();
            chkDecibels = new CheckBox();
            btnReset = new Button();
            btnAccept = new Button();
            btnCancel = new Button();
            playheadTimer = new System.Windows.Forms.Timer(components);
            tableMain.SuspendLayout();
            flowSelection.SuspendLayout();
            tableEdges.SuspendLayout();
            grpStart.SuspendLayout();
            tableStart.SuspendLayout();
            flowStart.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numStart).BeginInit();
            flowFadeIn.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numFadeInStart).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numFadeInEnd).BeginInit();
            grpEnd.SuspendLayout();
            tableEnd.SuspendLayout();
            flowEnd.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numEnd).BeginInit();
            flowFadeOut.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numFadeOutStart).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numFadeOutEnd).BeginInit();
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
            tableMain.Controls.Add(flowSelection, 0, 3);
            tableMain.Controls.Add(tableEdges, 0, 4);
            tableMain.Controls.Add(tableBottom, 0, 5);
            tableMain.Dock = DockStyle.Fill;
            tableMain.Location = new Point(0, 0);
            tableMain.Name = "tableMain";
            tableMain.Padding = new Padding(9);
            tableMain.RowCount = 6;
            tableMain.RowStyles.Add(new RowStyle());
            tableMain.RowStyles.Add(new RowStyle());
            tableMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 170F));
            tableMain.RowStyles.Add(new RowStyle());
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
            viewOverview.AccessibleDescription = "Forma de onda de la pista completa. Flechas izquierda y derecha mueven la marca activa; Inicio y Fin eligen la marca. Arrastrar con el ratón selecciona un tramo para aplicarle un fundido o borrarlo.";
            viewOverview.AccessibleName = "Forma de onda de la pista completa";
            viewOverview.Dock = DockStyle.Fill;
            viewOverview.Location = new Point(12, 67);
            viewOverview.Name = "viewOverview";
            viewOverview.Size = new Size(1160, 164);
            viewOverview.TabIndex = 2;
            viewOverview.MarkersChanged += View_MarkersChanged;
            viewOverview.SelectionChanged += View_SelectionChanged;
            viewOverview.FadeAdjusted += View_FadeAdjusted;
            // 
            // flowSelection
            // 
            flowSelection.AutoSize = true;
            flowSelection.Controls.Add(lblSelection);
            flowSelection.Controls.Add(btnSelFadeIn);
            flowSelection.Controls.Add(btnSelFadeOut);
            flowSelection.Controls.Add(btnSelDelete);
            flowSelection.Controls.Add(btnSelRestore);
            flowSelection.Controls.Add(btnSelPlay);
            flowSelection.Dock = DockStyle.Fill;
            flowSelection.Location = new Point(12, 237);
            flowSelection.Margin = new Padding(3, 3, 3, 0);
            flowSelection.Name = "flowSelection";
            flowSelection.Size = new Size(1160, 35);
            flowSelection.TabIndex = 3;
            flowSelection.WrapContents = false;
            // 
            // lblSelection
            // 
            lblSelection.Anchor = AnchorStyles.Left;
            lblSelection.AutoSize = true;
            lblSelection.Location = new Point(3, 8);
            lblSelection.MinimumSize = new Size(330, 0);
            lblSelection.Name = "lblSelection";
            lblSelection.Size = new Size(330, 19);
            lblSelection.TabIndex = 0;
            lblSelection.Text = "Sin selección: arrastra sobre la forma de onda.";
            // 
            // btnSelFadeIn
            // 
            btnSelFadeIn.AccessibleDescription = "Aplica a la selección un fundido de aparición con la curva elegida para la aparición.";
            btnSelFadeIn.AutoSize = true;
            btnSelFadeIn.Enabled = false;
            btnSelFadeIn.Location = new Point(440, 3);
            btnSelFadeIn.Name = "btnSelFadeIn";
            btnSelFadeIn.Size = new Size(105, 29);
            btnSelFadeIn.TabIndex = 1;
            btnSelFadeIn.Text = "Aparición";
            btnSelFadeIn.UseVisualStyleBackColor = true;
            btnSelFadeIn.Click += btnSelFadeIn_Click;
            // 
            // btnSelFadeOut
            // 
            btnSelFadeOut.AccessibleDescription = "Aplica a la selección un fundido de desaparición con la curva elegida para la desaparición.";
            btnSelFadeOut.AutoSize = true;
            btnSelFadeOut.Enabled = false;
            btnSelFadeOut.Location = new Point(550, 3);
            btnSelFadeOut.Name = "btnSelFadeOut";
            btnSelFadeOut.Size = new Size(105, 29);
            btnSelFadeOut.TabIndex = 2;
            btnSelFadeOut.Text = "Desaparición";
            btnSelFadeOut.UseVisualStyleBackColor = true;
            btnSelFadeOut.Click += btnSelFadeOut_Click;
            // 
            // btnSelDelete
            // 
            btnSelDelete.AccessibleDescription = "Quita de la copia el audio seleccionado y une lo anterior con lo posterior. Atajo: Supr.";
            btnSelDelete.AutoSize = true;
            btnSelDelete.Enabled = false;
            btnSelDelete.Location = new Point(660, 3);
            btnSelDelete.Name = "btnSelDelete";
            btnSelDelete.Size = new Size(105, 29);
            btnSelDelete.TabIndex = 3;
            btnSelDelete.Text = "Borrar selección";
            btnSelDelete.UseVisualStyleBackColor = true;
            btnSelDelete.Click += btnSelDelete_Click;
            // 
            // btnSelRestore
            // 
            btnSelRestore.AccessibleDescription = "Devuelve a la copia el audio borrado que cae dentro de la selección.";
            btnSelRestore.AutoSize = true;
            btnSelRestore.Enabled = false;
            btnSelRestore.Location = new Point(770, 3);
            btnSelRestore.Name = "btnSelRestore";
            btnSelRestore.Size = new Size(105, 29);
            btnSelRestore.TabIndex = 4;
            btnSelRestore.Text = "Restaurar";
            btnSelRestore.UseVisualStyleBackColor = true;
            btnSelRestore.Click += btnSelRestore_Click;
            // 
            // btnSelPlay
            // 
            btnSelPlay.AccessibleDescription = "Escucha la selección tal como sonará en la copia, hasta 30 segundos.";
            btnSelPlay.AutoSize = true;
            btnSelPlay.Enabled = false;
            btnSelPlay.Location = new Point(880, 3);
            btnSelPlay.Name = "btnSelPlay";
            btnSelPlay.Size = new Size(105, 29);
            btnSelPlay.TabIndex = 5;
            btnSelPlay.Text = "▶ Escuchar selección";
            btnSelPlay.UseVisualStyleBackColor = true;
            btnSelPlay.Click += btnSelPlay_Click;
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
            tableEdges.TabIndex = 4;
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
            tableStart.Controls.Add(flowFadeIn, 0, 2);
            tableStart.Dock = DockStyle.Fill;
            tableStart.Location = new Point(3, 21);
            tableStart.Name = "tableStart";
            tableStart.RowCount = 3;
            tableStart.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableStart.RowStyles.Add(new RowStyle());
            tableStart.RowStyles.Add(new RowStyle());
            tableStart.Size = new Size(571, 388);
            tableStart.TabIndex = 0;
            // 
            // viewStart
            // 
            viewStart.AccessibleDescription = "Detalle del inicio de la pista. Flechas izquierda y derecha mueven el comienzo de la copia: 10 ms, 100 ms con Mayús y 1 s con Ctrl. Arrastrar con el ratón selecciona un tramo para aplicarle un fundido o borrarlo.";
            viewStart.AccessibleName = "Forma de onda del inicio";
            viewStart.Dock = DockStyle.Fill;
            viewStart.Location = new Point(3, 3);
            viewStart.Name = "viewStart";
            viewStart.ShowEndMarker = false;
            viewStart.Size = new Size(565, 341);
            viewStart.TabIndex = 0;
            viewStart.MarkersChanged += View_MarkersChanged;
            viewStart.SelectionChanged += View_SelectionChanged;
            viewStart.FadeAdjusted += View_FadeAdjusted;
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
            // flowFadeIn
            // 
            flowFadeIn.AutoSize = true;
            flowFadeIn.Controls.Add(chkFadeIn);
            flowFadeIn.Controls.Add(numFadeInStart);
            flowFadeIn.Controls.Add(lblFadeInTo);
            flowFadeIn.Controls.Add(numFadeInEnd);
            flowFadeIn.Controls.Add(cmbFadeInCurve);
            flowFadeIn.Dock = DockStyle.Fill;
            flowFadeIn.Location = new Point(3, 350);
            flowFadeIn.Name = "flowFadeIn";
            flowFadeIn.Size = new Size(565, 35);
            flowFadeIn.TabIndex = 2;
            flowFadeIn.WrapContents = false;
            // 
            // chkFadeIn
            // 
            chkFadeIn.AccessibleDescription = "Activa la aparición en la copia. También se aplica a una selección con el botón «Aparición».";
            chkFadeIn.Anchor = AnchorStyles.Left;
            chkFadeIn.AutoSize = true;
            chkFadeIn.Location = new Point(3, 6);
            chkFadeIn.Name = "chkFadeIn";
            chkFadeIn.Size = new Size(140, 23);
            chkFadeIn.TabIndex = 0;
            chkFadeIn.Text = "Aparición de (s):";
            chkFadeIn.UseVisualStyleBackColor = true;
            chkFadeIn.CheckedChanged += chkFadeIn_CheckedChanged;
            // 
            // numFadeInStart
            // 
            numFadeInStart.AccessibleName = "Comienzo de la aparición en segundos";
            numFadeInStart.Anchor = AnchorStyles.Left;
            numFadeInStart.DecimalPlaces = 3;
            numFadeInStart.Increment = new decimal(new int[] { 1, 0, 0, 131072 });
            numFadeInStart.Location = new Point(149, 5);
            numFadeInStart.Name = "numFadeInStart";
            numFadeInStart.Size = new Size(95, 25);
            numFadeInStart.TabIndex = 1;
            numFadeInStart.TextAlign = HorizontalAlignment.Right;
            numFadeInStart.ValueChanged += numFadeIn_ValueChanged;
            // 
            // lblFadeInTo
            // 
            lblFadeInTo.Anchor = AnchorStyles.Left;
            lblFadeInTo.AutoSize = true;
            lblFadeInTo.Location = new Point(250, 8);
            lblFadeInTo.Name = "lblFadeInTo";
            lblFadeInTo.Size = new Size(15, 19);
            lblFadeInTo.TabIndex = 2;
            lblFadeInTo.Text = "a";
            // 
            // numFadeInEnd
            // 
            numFadeInEnd.AccessibleName = "Final de la aparición en segundos";
            numFadeInEnd.Anchor = AnchorStyles.Left;
            numFadeInEnd.DecimalPlaces = 3;
            numFadeInEnd.Increment = new decimal(new int[] { 1, 0, 0, 131072 });
            numFadeInEnd.Location = new Point(271, 5);
            numFadeInEnd.Name = "numFadeInEnd";
            numFadeInEnd.Size = new Size(95, 25);
            numFadeInEnd.TabIndex = 3;
            numFadeInEnd.TextAlign = HorizontalAlignment.Right;
            numFadeInEnd.ValueChanged += numFadeIn_ValueChanged;
            // 
            // cmbFadeInCurve
            // 
            cmbFadeInCurve.AccessibleName = "Curva de la aparición";
            cmbFadeInCurve.Anchor = AnchorStyles.Left;
            cmbFadeInCurve.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbFadeInCurve.FormattingEnabled = true;
            cmbFadeInCurve.Location = new Point(372, 4);
            cmbFadeInCurve.Name = "cmbFadeInCurve";
            cmbFadeInCurve.Size = new Size(125, 25);
            cmbFadeInCurve.TabIndex = 4;
            cmbFadeInCurve.SelectedIndexChanged += cmbFadeInCurve_SelectedIndexChanged;
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
            tableEnd.Controls.Add(flowFadeOut, 0, 2);
            tableEnd.Dock = DockStyle.Fill;
            tableEnd.Location = new Point(3, 21);
            tableEnd.Name = "tableEnd";
            tableEnd.RowCount = 3;
            tableEnd.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableEnd.RowStyles.Add(new RowStyle());
            tableEnd.RowStyles.Add(new RowStyle());
            tableEnd.Size = new Size(571, 388);
            tableEnd.TabIndex = 0;
            // 
            // viewEnd
            // 
            viewEnd.AccessibleDescription = "Detalle del final de la pista. Flechas izquierda y derecha mueven el final de la copia: 10 ms, 100 ms con Mayús y 1 s con Ctrl. Arrastrar con el ratón selecciona un tramo para aplicarle un fundido o borrarlo.";
            viewEnd.AccessibleName = "Forma de onda del final";
            viewEnd.Dock = DockStyle.Fill;
            viewEnd.Location = new Point(3, 3);
            viewEnd.Name = "viewEnd";
            viewEnd.ShowStartMarker = false;
            viewEnd.Size = new Size(565, 341);
            viewEnd.TabIndex = 0;
            viewEnd.MarkersChanged += View_MarkersChanged;
            viewEnd.SelectionChanged += View_SelectionChanged;
            viewEnd.FadeAdjusted += View_FadeAdjusted;
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
            // flowFadeOut
            // 
            flowFadeOut.AutoSize = true;
            flowFadeOut.Controls.Add(chkFadeOut);
            flowFadeOut.Controls.Add(numFadeOutStart);
            flowFadeOut.Controls.Add(lblFadeOutTo);
            flowFadeOut.Controls.Add(numFadeOutEnd);
            flowFadeOut.Controls.Add(cmbFadeOutCurve);
            flowFadeOut.Dock = DockStyle.Fill;
            flowFadeOut.Location = new Point(3, 350);
            flowFadeOut.Name = "flowFadeOut";
            flowFadeOut.Size = new Size(565, 35);
            flowFadeOut.TabIndex = 2;
            flowFadeOut.WrapContents = false;
            // 
            // chkFadeOut
            // 
            chkFadeOut.AccessibleDescription = "Activa la desaparición en la copia. También se aplica a una selección con el botón «Desaparición».";
            chkFadeOut.Anchor = AnchorStyles.Left;
            chkFadeOut.AutoSize = true;
            chkFadeOut.Location = new Point(3, 6);
            chkFadeOut.Name = "chkFadeOut";
            chkFadeOut.Size = new Size(140, 23);
            chkFadeOut.TabIndex = 0;
            chkFadeOut.Text = "Desaparición de (s):";
            chkFadeOut.UseVisualStyleBackColor = true;
            chkFadeOut.CheckedChanged += chkFadeOut_CheckedChanged;
            // 
            // numFadeOutStart
            // 
            numFadeOutStart.AccessibleName = "Comienzo de la desaparición en segundos";
            numFadeOutStart.Anchor = AnchorStyles.Left;
            numFadeOutStart.DecimalPlaces = 3;
            numFadeOutStart.Increment = new decimal(new int[] { 1, 0, 0, 131072 });
            numFadeOutStart.Location = new Point(149, 5);
            numFadeOutStart.Name = "numFadeOutStart";
            numFadeOutStart.Size = new Size(95, 25);
            numFadeOutStart.TabIndex = 1;
            numFadeOutStart.TextAlign = HorizontalAlignment.Right;
            numFadeOutStart.ValueChanged += numFadeOut_ValueChanged;
            // 
            // lblFadeOutTo
            // 
            lblFadeOutTo.Anchor = AnchorStyles.Left;
            lblFadeOutTo.AutoSize = true;
            lblFadeOutTo.Location = new Point(250, 8);
            lblFadeOutTo.Name = "lblFadeOutTo";
            lblFadeOutTo.Size = new Size(15, 19);
            lblFadeOutTo.TabIndex = 2;
            lblFadeOutTo.Text = "a";
            // 
            // numFadeOutEnd
            // 
            numFadeOutEnd.AccessibleName = "Final de la desaparición en segundos";
            numFadeOutEnd.Anchor = AnchorStyles.Left;
            numFadeOutEnd.DecimalPlaces = 3;
            numFadeOutEnd.Increment = new decimal(new int[] { 1, 0, 0, 131072 });
            numFadeOutEnd.Location = new Point(271, 5);
            numFadeOutEnd.Name = "numFadeOutEnd";
            numFadeOutEnd.Size = new Size(95, 25);
            numFadeOutEnd.TabIndex = 3;
            numFadeOutEnd.TextAlign = HorizontalAlignment.Right;
            numFadeOutEnd.ValueChanged += numFadeOut_ValueChanged;
            // 
            // cmbFadeOutCurve
            // 
            cmbFadeOutCurve.AccessibleName = "Curva de la desaparición";
            cmbFadeOutCurve.Anchor = AnchorStyles.Left;
            cmbFadeOutCurve.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbFadeOutCurve.FormattingEnabled = true;
            cmbFadeOutCurve.Location = new Point(372, 4);
            cmbFadeOutCurve.Name = "cmbFadeOutCurve";
            cmbFadeOutCurve.Size = new Size(125, 25);
            cmbFadeOutCurve.TabIndex = 4;
            cmbFadeOutCurve.SelectedIndexChanged += cmbFadeOutCurve_SelectedIndexChanged;
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
            tableBottom.SetColumnSpan(lblSummary, 5);
            tableBottom.Controls.Add(chkDecibels, 1, 1);
            tableBottom.Controls.Add(btnReset, 2, 1);
            tableBottom.Controls.Add(btnAccept, 3, 1);
            tableBottom.Controls.Add(btnCancel, 4, 1);
            tableBottom.Dock = DockStyle.Fill;
            tableBottom.Location = new Point(9, 661);
            tableBottom.Margin = new Padding(0, 3, 0, 0);
            tableBottom.Name = "tableBottom";
            tableBottom.RowCount = 2;
            tableBottom.RowStyles.Add(new RowStyle());
            tableBottom.RowStyles.Add(new RowStyle());
            tableBottom.Size = new Size(1166, 51);
            tableBottom.TabIndex = 5;
            // 
            // lblSummary
            // 
            lblSummary.Dock = DockStyle.Fill;
            lblSummary.Location = new Point(3, 0);
            lblSummary.MinimumSize = new Size(0, 60);
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
            ClientSize = new Size(1184, 800);
            Controls.Add(tableMain);
            Font = new Font("Segoe UI", 10F);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MinimizeBox = false;
            MinimumSize = new Size(1000, 640);
            Name = "WaveformEditor";
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Forma de onda, recorte y fundidos";
            FormClosing += WaveformEditor_FormClosing;
            FormClosed += WaveformEditor_FormClosed;
            Shown += WaveformEditor_Shown;
            tableMain.ResumeLayout(false);
            tableMain.PerformLayout();
            flowSelection.ResumeLayout(false);
            flowSelection.PerformLayout();
            tableEdges.ResumeLayout(false);
            grpStart.ResumeLayout(false);
            tableStart.ResumeLayout(false);
            tableStart.PerformLayout();
            flowStart.ResumeLayout(false);
            flowStart.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numStart).EndInit();
            flowFadeIn.ResumeLayout(false);
            flowFadeIn.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numFadeInStart).EndInit();
            ((System.ComponentModel.ISupportInitialize)numFadeInEnd).EndInit();
            grpEnd.ResumeLayout(false);
            tableEnd.ResumeLayout(false);
            tableEnd.PerformLayout();
            flowEnd.ResumeLayout(false);
            flowEnd.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numEnd).EndInit();
            flowFadeOut.ResumeLayout(false);
            flowFadeOut.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numFadeOutStart).EndInit();
            ((System.ComponentModel.ISupportInitialize)numFadeOutEnd).EndInit();
            tableBottom.ResumeLayout(false);
            tableBottom.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private TableLayoutPanel tableMain;
        private Label lblTrack;
        private Label lblOverview;
        private EchoCut.Controls.WaveformView viewOverview;
        private FlowLayoutPanel flowSelection;
        private Label lblSelection;
        private Button btnSelFadeIn;
        private Button btnSelFadeOut;
        private Button btnSelDelete;
        private Button btnSelRestore;
        private Button btnSelPlay;
        private TableLayoutPanel tableEdges;
        private GroupBox grpStart;
        private TableLayoutPanel tableStart;
        private EchoCut.Controls.WaveformView viewStart;
        private FlowLayoutPanel flowStart;
        private Label lblStartCaption;
        private NumericUpDown numStart;
        private Button btnPlayStart;
        private FlowLayoutPanel flowFadeIn;
        private CheckBox chkFadeIn;
        private NumericUpDown numFadeInStart;
        private Label lblFadeInTo;
        private NumericUpDown numFadeInEnd;
        private ComboBox cmbFadeInCurve;
        private GroupBox grpEnd;
        private TableLayoutPanel tableEnd;
        private EchoCut.Controls.WaveformView viewEnd;
        private FlowLayoutPanel flowEnd;
        private Label lblEndCaption;
        private NumericUpDown numEnd;
        private Button btnPlayEnd;
        private FlowLayoutPanel flowFadeOut;
        private CheckBox chkFadeOut;
        private NumericUpDown numFadeOutStart;
        private Label lblFadeOutTo;
        private NumericUpDown numFadeOutEnd;
        private ComboBox cmbFadeOutCurve;
        private TableLayoutPanel tableBottom;
        private Label lblSummary;
        private CheckBox chkDecibels;
        private Button btnReset;
        private Button btnAccept;
        private Button btnCancel;
        private System.Windows.Forms.Timer playheadTimer;
    }
}
