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

            // Ordenar o recargar la lista regenera las filas, y con ellas se pierde su visibilidad.
            ApplyFilter();
        }

        // ---------------------------------------------------------------------------- Filtro

        /// <summary>Si se está aplicando el filtro; evita reentrar desde el reenlace que provoca.</summary>
        private bool _applyingFilter;

        /// <summary>Texto vigente del filtro, sin espacios en los extremos.</summary>
        private string FilterText => txtFilter.Text.Trim();

        private void txtFilter_TextChanged(object? sender, EventArgs e) => ApplyFilter();

        /// <summary>Si el nombre de una pista contiene el texto del filtro, sin distinguir mayúsculas.</summary>
        /// <remarks>
        /// Se compara con la cultura actual y no con la invariante para que las mayúsculas de los
        /// caracteres propios del español —«Ñ», vocales acentuadas— se plieguen igual que las
        /// demás. Un filtro vacío deja pasar todo.
        /// </remarks>
        private static bool MatchesFilter(Song song, string filter) =>
            filter.Length == 0 || song.Name.Contains(filter, StringComparison.CurrentCultureIgnoreCase);

        /// <summary>
        /// Muestra solo las filas cuyo nombre coincide con el filtro.
        /// </summary>
        /// <remarks>
        /// El filtro es solo de presentación: oculta filas, pero <see cref="_songs"/> sigue siendo la
        /// lista completa, así que los lotes y la exportación actúan sobre todas las pistas cargadas.
        /// La rejilla prohíbe ocultar la fila en la que está posicionado el enlace, de ahí que se
        /// suspenda mientras se cambia la visibilidad.
        /// </remarks>
        private void ApplyFilter()
        {
            if (_applyingFilter || !IsAlive || BindingContext?[_songs] is not CurrencyManager manager)
            {
                return;
            }

            _applyingFilter = true;
            string filter = FilterText;

            // Suspender el enlace descarta la celda actual; se recuerda para devolverla si su fila
            // sigue a la vista, y que el teclado no pierda su sitio en cada pulsación del filtro.
            Point current = dataGrid.CurrentCellAddress;

            manager.SuspendBinding();
            try
            {
                foreach (DataGridViewRow row in dataGrid.Rows)
                {
                    SetRowVisibility(row, filter);
                }
            }
            finally
            {
                manager.ResumeBinding();
                _applyingFilter = false;
            }

            RestoreCurrentCell(current);

            RefreshSummary();

            // El aviso de «sin coincidencias» se pinta sobre la rejilla.
            dataGrid.Invalidate();
        }

        /// <summary>
        /// Reevalúa el filtro para una sola fila, cuando cambia el nombre de su pista al normalizarla
        /// o editar sus propiedades.
        /// </summary>
        private void ApplyFilter(int rowIndex)
        {
            if (FilterText.Length == 0
                || rowIndex < 0 || rowIndex >= dataGrid.Rows.Count
                || BindingContext?[_songs] is not CurrencyManager manager)
            {
                return;
            }

            Point current = dataGrid.CurrentCellAddress;

            manager.SuspendBinding();
            try
            {
                SetRowVisibility(dataGrid.Rows[rowIndex], FilterText);
            }
            finally
            {
                manager.ResumeBinding();
            }

            RestoreCurrentCell(current);
            QueueSummaryRefresh();
        }

        /// <summary>Devuelve la celda actual a la dirección indicada si su fila sigue visible.</summary>
        private void RestoreCurrentCell(Point address)
        {
            if (address.Y < 0 || address.Y >= dataGrid.Rows.Count
                || address.X < 0 || address.X >= dataGrid.Columns.Count
                || !dataGrid.Rows[address.Y].Visible
                || !dataGrid.Columns[address.X].Visible
                || dataGrid.CurrentCellAddress == address)
            {
                return;
            }

            // Fijar la celda actual selecciona su fila en modo de fila completa; se conserva la
            // selección que hubiera para no deshacer una multiselección al teclear en el filtro.
            List<DataGridViewRow> selected = [.. dataGrid.SelectedRows.Cast<DataGridViewRow>()];
            dataGrid.CurrentCell = dataGrid.Rows[address.Y].Cells[address.X];

            foreach (DataGridViewRow row in selected)
            {
                row.Selected = true;
            }
        }

        /// <summary>
        /// Muestra u oculta una fila. Una fila que se oculta deja de estar seleccionada: si no, las
        /// acciones sobre la selección —eliminar, sobre todo— alcanzarían pistas que no se ven.
        /// </summary>
        private static void SetRowVisibility(DataGridViewRow row, string filter)
        {
            bool visible = row.DataBoundItem is not Song song || MatchesFilter(song, filter);
            if (row.Visible == visible)
            {
                return;
            }

            if (!visible)
            {
                row.Selected = false;
            }

            row.Visible = visible;
        }

        /// <summary>Vacía el filtro y devuelve el foco a la rejilla.</summary>
        private void ClearFilter()
        {
            txtFilter.Clear();
            dataGrid.Focus();
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
                PaintNoFilterMatches(e.Graphics);
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

        /// <summary>
        /// Avisa, cuando el filtro oculta todas las filas, de que la lista no está vacía: sin el
        /// aviso, una rejilla en blanco parece una carga fallida.
        /// </summary>
        private void PaintNoFilterMatches(Graphics graphics)
        {
            if (FilterText.Length == 0 || dataGrid.Rows.GetRowCount(DataGridViewElementStates.Visible) > 0)
            {
                return;
            }

            int top = dataGrid.ColumnHeadersVisible ? dataGrid.ColumnHeadersHeight : 0;
            Rectangle bounds = Rectangle.FromLTRB(
                DropZoneMargin,
                top + DropZoneMargin,
                dataGrid.ClientSize.Width - DropZoneMargin,
                dataGrid.ClientSize.Height - DropZoneMargin);

            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                return;
            }

            string message = $"Ninguna pista coincide con «{FilterText}».\n"
                + "Esc vacía el filtro y vuelve a mostrar "
                + (_songs.Count == 1 ? "la pista cargada." : $"las {_songs.Count} pistas cargadas.");

            TextRenderer.DrawText(
                graphics,
                message,
                dataGrid.Font,
                bounds,
                SongPresentation.Inconclusive,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
        }
    }
}
