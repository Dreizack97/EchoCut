using EchoCut.Objects;

namespace EchoCut
{
    /// <summary>
    /// La mitad de <see cref="Main"/> que solo se ocupa de la rejilla: qué columnas hay, cómo se
    /// reparten el ancho y de qué color va cada celda.
    /// </summary>
    /// <remarks>
    /// Está separada del resto del formulario porque no comparte nada con él salvo el control:
    /// no toca el lote, ni la configuración, ni la cancelación. Tenerla aparte deja el archivo
    /// principal en lo que el usuario pide y lo que los servicios contestan.
    /// </remarks>
    public partial class Main
    {
        /// <summary>Nombre de la columna de acción que reproduce la previsualización.</summary>
        internal const string PlayColumnName = "colPlay";

        /// <summary>Nombre de la columna de acción que recorta una sola pista.</summary>
        internal const string TrimColumnName = "colTrim";

        private void dataGrid_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            EnsureActionColumns();

            FormatNumericColumn(nameof(Song.LeadingSilence));
            FormatNumericColumn(nameof(Song.Silence));
            FormatNumericColumn(nameof(Song.Crop));

            ApplyColumnLayout();
        }

        /// <summary>
        /// Inserta las dos columnas de acción al principio de la rejilla.
        /// </summary>
        /// <remarks>
        /// Van primero, y no al final, por una razón práctica: la suma de anchos supera el de una
        /// ventana normal, así que puestas al final obligarían a desplazarse horizontalmente cada vez
        /// que se quisiera reproducir o recortar una fila.
        /// Las columnas de datos se siguen generando solas desde las propiedades de <see cref="Song"/>;
        /// solo estas dos se añaden a mano, y de ahí la guarda para no duplicarlas en cada reenlace.
        /// </remarks>
        private void EnsureActionColumns()
        {
            if (dataGrid.Columns.Contains(PlayColumnName))
            {
                return;
            }

            dataGrid.Columns.Insert(0, CreateActionColumn(PlayColumnName, SongPresentation.PlayGlyph));
            dataGrid.Columns.Insert(1, CreateActionColumn(TrimColumnName, SongPresentation.TrimGlyph));
        }

        private static DataGridViewButtonColumn CreateActionColumn(string name, string header) => new()
        {
            Name = name,
            HeaderText = header,
            FlatStyle = FlatStyle.Flat,

            // El texto lo pone CellFormatting por fila: el glifo depende del estado de cada pista,
            // no de la columna.
            UseColumnTextForButtonValue = false,
            SortMode = DataGridViewColumnSortMode.NotSortable,
            Resizable = DataGridViewTriState.False,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
            Width = 36,
            MinimumWidth = 32,
        };

        private void FormatNumericColumn(string columnName)
        {
            if (dataGrid.Columns[columnName] is { } column)
            {
                column.DefaultCellStyle.Format = "N2";
                column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
        }

        /// <summary>
        /// Proporciones, mínimos y alineación de cada columna, portados del proyecto de referencia.
        /// </summary>
        /// <remarks>
        /// La rejilla reparte el ancho disponible entre las columnas según su peso, en vez de dar
        /// 100 px a cada una. Con trece columnas, asignarles anchos fijos las hace sumar bastante más
        /// de lo que cabe en la ventana, así que aparecería barra horizontal desde el primer momento
        /// y las últimas —«Recorte» y «Estado», justo las que se consultan— quedarían fuera de vista.
        /// Repartiendo por peso, todas caben y las proporciones se conservan al redimensionar.
        /// El usuario puede seguir arrastrando los separadores; el reparto solo fija el punto de
        /// partida y qué columna cede espacio a cuál.
        /// </remarks>
        private void ApplyColumnLayout()
        {
            // Los mínimos no son estéticos: por debajo de ellos se corta el propio título de la
            // columna, y una cabecera que pone «Silencio» donde debería poner «Silencio (s)» hace
            // dudar de en qué unidad está el número.
            SetColumnLayout(nameof(Song.Name), 200, 90);
            SetColumnLayout(nameof(Song.Title), 150, 70);
            SetColumnLayout(nameof(Song.Artist), 120, 70);
            SetColumnLayout(nameof(Song.Album), 120, 70);
            SetColumnLayout(nameof(Song.Duration), 75, 74, DataGridViewContentAlignment.MiddleRight);
            SetColumnLayout(nameof(Song.Extension), 55, 50, DataGridViewContentAlignment.MiddleCenter);
            SetColumnLayout(nameof(Song.Bitrate), 80, 64, DataGridViewContentAlignment.MiddleRight);
            SetColumnLayout(nameof(Song.Size), 80, 70, DataGridViewContentAlignment.MiddleRight);
            SetColumnLayout(nameof(Song.LeadingSilence), 130, 127);
            SetColumnLayout(nameof(Song.Silence), 120, 119);
            SetColumnLayout(nameof(Song.Crop), 90, 88);
            SetColumnLayout(nameof(Song.Estatus), 120, 72);
        }

        private void SetColumnLayout(
            string columnName,
            int weight,
            int minimumWidth,
            DataGridViewContentAlignment? alignment = null)
        {
            if (dataGrid.Columns[columnName] is not { } column)
            {
                return;
            }

            column.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            column.FillWeight = weight;
            column.MinimumWidth = minimumWidth;

            if (alignment is { } value)
            {
                column.DefaultCellStyle.Alignment = value;
            }
        }

        /// <summary>
        /// Colorea el texto según el estado y resuelve los glifos de las columnas de acción.
        /// </summary>
        /// <remarks>
        /// Se hace aquí y no al mutar la fila porque el evento solo se dispara para las celdas
        /// visibles: con una carpeta de varios cientos de pistas, pintar solo lo que se ve es la
        /// diferencia entre una rejilla fluida y una que se arrastra al desplazarse.
        /// </remarks>
        private void dataGrid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= dataGrid.Rows.Count)
            {
                return;
            }

            if (dataGrid.Rows[e.RowIndex].DataBoundItem is not Song song)
            {
                return;
            }

            string columnName = dataGrid.Columns[e.ColumnIndex].Name;

            if (columnName == PlayColumnName)
            {
                e.Value = SongPresentation.PlayGlyphFor(ReferenceEquals(song, _playingSong));
                e.FormattingApplied = true;
                return;
            }

            if (columnName == TrimColumnName)
            {
                e.Value = SongPresentation.TrimGlyphFor(song);
                e.FormattingApplied = true;
                return;
            }

            e.CellStyle.ForeColor = SongPresentation.ForeColorFor(song.Estatus);

            // La selección utiliza la paleta clara semántica para mantener el indicio de estado
            // y a la vez garantizar un alto contraste con el fondo oscuro de la selección.
            e.CellStyle.SelectionForeColor = SongPresentation.SelectionForeColorFor(song.Estatus);
        }

        /// <summary>Margen entre el borde de la rejilla y el recuadro de la zona de arrastre.</summary>
        private const int DropZoneMargin = 24;

        /// <summary>
        /// Pinta, con la lista vacía, la guía de qué hacer a continuación.
        /// </summary>
        /// <remarks>
        /// Una rejilla vacía con trece cabeceras no dice por dónde empezar, y arrastrar carpetas a la
        /// ventana —el camino más rápido— no se descubre si nadie lo anuncia. El recuadro discontinuo
        /// se resalta mientras se arrastra algo aceptable encima, para confirmar que soltar ahí sirve.
        /// </remarks>
        private void dataGrid_Paint(object? sender, PaintEventArgs e)
        {
            if (_songs.Count > 0)
            {
                return;
            }

            int top = dataGrid.ColumnHeadersVisible ? dataGrid.ColumnHeadersHeight : 0;
            Rectangle zone = Rectangle.FromLTRB(
                DropZoneMargin,
                top + DropZoneMargin,
                dataGrid.ClientSize.Width - DropZoneMargin,
                dataGrid.ClientSize.Height - DropZoneMargin);

            if (zone.Width <= 0 || zone.Height <= 0)
            {
                return;
            }

            Color accent = _dropHighlighted ? SystemColors.Highlight : SystemColors.ControlDark;

            if (_dropHighlighted)
            {
                using SolidBrush fill = new(Color.FromArgb(24, SystemColors.Highlight));
                e.Graphics.FillRectangle(fill, zone);
            }

            using (Pen border = new(accent, _dropHighlighted ? 2f : 1f) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash })
            {
                e.Graphics.DrawRectangle(border, zone);
            }

            const string title = "Arrastra aquí carpetas o archivos de audio";
            const string hint = "o usa «Abrir carpeta(s)» y «Abrir archivo(s)» en la barra de herramientas.\n"
                + "Los originales nunca se modifican al recortar: las copias van a la subcarpeta «Recortados».";

            const TextFormatFlags flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix;

            Font titleFont = _dropZoneTitleFont ??= new Font(dataGrid.Font.FontFamily, dataGrid.Font.Size * 1.3f, FontStyle.Bold);
            Rectangle textBounds = Rectangle.Inflate(zone, -DropZoneMargin, -DropZoneMargin);

            Size titleSize = TextRenderer.MeasureText(e.Graphics, title, titleFont, textBounds.Size, flags);
            Size hintSize = TextRenderer.MeasureText(e.Graphics, hint, dataGrid.Font, textBounds.Size, flags);
            int y = textBounds.Top + ((textBounds.Height - titleSize.Height - hintSize.Height - 8) / 2);

            TextRenderer.DrawText(
                e.Graphics,
                title,
                titleFont,
                new Rectangle(textBounds.Left, y, textBounds.Width, titleSize.Height),
                _dropHighlighted ? SystemColors.Highlight : SystemColors.ControlText,
                flags);

            TextRenderer.DrawText(
                e.Graphics,
                hint,
                dataGrid.Font,
                new Rectangle(textBounds.Left, y + titleSize.Height + 8, textBounds.Width, hintSize.Height),
                SongPresentation.Inconclusive,
                flags);
        }
    }
}
