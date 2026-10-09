using EchoCut.Library;
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

        /// <summary>Texto del filtro ya sin acentos, listo para <see cref="MatchesFilter"/>.</summary>
        /// <remarks>
        /// Se pliega una sola vez por aplicación del filtro y no en cada comparación, que se repite
        /// por cada fila de la lista.
        /// </remarks>
        private string FilterKey => TextNormalizer.RemoveAccents(FilterText);

        private void txtFilter_TextChanged(object? sender, EventArgs e) => ApplyFilter();

        /// <summary>
        /// Si el nombre de una pista contiene el filtro, sin distinguir mayúsculas ni acentos.
        /// </summary>
        /// <param name="song">Pista cuyo nombre se comprueba.</param>
        /// <param name="filterKey">Filtro ya plegado con <see cref="FilterKey"/>.</param>
        /// <remarks>
        /// Los nombres de archivo llegan con y sin tilde según quién los etiquetó, así que «cancion»
        /// debe encontrar «Canción». Se reutiliza <see cref="TextNormalizer.RemoveAccents"/>, que
        /// conserva la «ñ» como letra propia y no como «n» acentuada: «ano» no encuentra «año»,
        /// igual que la normalización de nombres tampoco las confunde.
        /// Se compara con la cultura actual para que las mayúsculas de «Ñ» se plieguen igual que
        /// las demás. Un filtro vacío deja pasar todo.
        /// </remarks>
        private static bool MatchesFilter(Song song, string filterKey) =>
            filterKey.Length == 0
            || TextNormalizer.RemoveAccents(song.Name).Contains(filterKey, StringComparison.CurrentCultureIgnoreCase);

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
            string filter = FilterKey;

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
                SetRowVisibility(dataGrid.Rows[rowIndex], FilterKey);
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
        private static void SetRowVisibility(DataGridViewRow row, string filterKey)
        {
            bool visible = row.DataBoundItem is not Song song || MatchesFilter(song, filterKey);
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
        /// que se quisiera reproducir o recortar una fila. Por lo mismo quedan inmovilizadas: al
        /// desplazarse hacia «Recorte» y «Estado» siguen a la vista junto a la fila que se consulta.
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
            Frozen = true,
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
        /// Fracción del ancho visible que, como mucho, recibe una columna de texto libre al
        /// ajustarse a su contenido.
        /// </summary>
        /// <remarks>
        /// Un solo nombre desmesurado no debe empujar el resto de columnas fuera de la vista. Lo que
        /// quede recortado se lee en el tooltip de la celda, y el usuario puede ensanchar la columna.
        /// </remarks>
        private const double MaxTextColumnRatio = 0.4;

        /// <summary>Columnas de texto libre, las únicas cuyo contenido no tiene un ancho acotado.</summary>
        private static readonly string[] TextColumns =
            [nameof(Song.Name), nameof(Song.Title), nameof(Song.Artist), nameof(Song.Album)];

        /// <summary>Todos los valores que puede mostrar la columna «Estado».</summary>
        private static readonly string[] StatusTexts =
        [
            Song.StatusPending, Song.StatusAnalyzing, Song.StatusAnalyzed, Song.StatusNoSilence,
            Song.StatusAdjusted, Song.StatusTrimming, Song.StatusTrimmed, Song.StatusCancelled,
            Song.StatusError,
        ];

        /// <summary>Si las columnas ya recibieron su primer ajuste al contenido.</summary>
        private bool _columnsAutoSized;

        /// <summary>
        /// Ancho que pedía su contenido, por columna de texto que el tope dejó más estrecha.
        /// </summary>
        /// <remarks>
        /// El tope es una fracción del ancho visible, así que al ensanchar la ventana se recalcula
        /// para devolverles el espacio que les falta. Una columna sale de aquí en cuanto el usuario
        /// la ajusta a mano: a partir de ahí su ancho es decisión suya.
        /// </remarks>
        private readonly Dictionary<string, int> _cappedColumns = [];

        /// <summary>Si los anchos los está cambiando el propio ajuste, y no el usuario.</summary>
        private bool _sizingColumns;

        /// <summary>Alineación del contenido de cada columna.</summary>
        /// <remarks>
        /// Solo decide cómo se presenta el contenido, no el ancho: se aplica en cada reenlace, y
        /// los anchos se fijan aparte en <see cref="AutoSizeColumns"/> para no deshacer los que el
        /// usuario haya ajustado a mano cada vez que ordena.
        /// </remarks>
        private void ApplyColumnLayout()
        {
            SetColumnAlignment(nameof(Song.Duration), DataGridViewContentAlignment.MiddleRight);
            SetColumnAlignment(nameof(Song.Extension), DataGridViewContentAlignment.MiddleCenter);
            SetColumnAlignment(nameof(Song.Bitrate), DataGridViewContentAlignment.MiddleRight);
            SetColumnAlignment(nameof(Song.Size), DataGridViewContentAlignment.MiddleRight);

            if (!_columnsAutoSized)
            {
                AutoSizeColumns();
            }
        }

        private void SetColumnAlignment(string columnName, DataGridViewContentAlignment alignment)
        {
            if (dataGrid.Columns[columnName] is { } column)
            {
                column.DefaultCellStyle.Alignment = alignment;
            }
        }

        /// <summary>
        /// Ajusta cada columna al ancho de su contenido, con su título como mínimo.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Sustituye al reparto por peso (<c>Fill</c>): repartir el ancho de la ventana por
        /// proporciones fijas recortaba los nombres aunque sobrara sitio en columnas de contenido
        /// corto como «Ext.». Y <see cref="DataGridView.AutoResizeColumns()"/> no sirve sobre
        /// columnas en <c>Fill</c>: no falla, pero el reparto vuelve a imponer su ancho.
        /// </para>
        /// <para>
        /// Se mide con todas las filas y no solo con las visibles: con <c>DisplayedCells</c> los
        /// nombres largos que aparecen al desplazarse quedarían cortados. Se hace al cargar y tras
        /// los lotes que reescriben metadatos, nunca al ordenar o filtrar, porque deshacería el
        /// ancho que el usuario haya dado a mano a una columna.
        /// </para>
        /// <para>
        /// El título fija el mínimo al que se puede estrechar una columna: una cabecera que pone
        /// «Silencio» donde debería poner «Silencio (s)» hace dudar de la unidad. «Estado» toma el
        /// espacio sobrante para que la rejilla no termine en un hueco, y nunca baja de su valor más
        /// largo, porque cambia durante los lotes y sin ese mínimo «Recortando…» quedaría cortado.
        /// </para>
        /// </remarks>
        private void AutoSizeColumns()
        {
            if (!IsAlive || dataGrid.Columns.Count == 0)
            {
                return;
            }

            _columnsAutoSized = true;
            _sizingColumns = true;
            _cappedColumns.Clear();

            try
            {
                DataGridViewColumn? status = dataGrid.Columns[nameof(Song.Estatus)];
                int maxTextWidth = MaxTextColumnWidth;

                foreach (DataGridViewColumn column in dataGrid.Columns)
                {
                    // Las columnas de acción tienen un ancho fijo pensado para un único glifo.
                    if (column is DataGridViewButtonColumn)
                    {
                        continue;
                    }

                    column.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                    column.MinimumWidth = column.GetPreferredWidth(DataGridViewAutoSizeColumnMode.ColumnHeader, fixedHeight: true);

                    dataGrid.AutoResizeColumn(column.Index, DataGridViewAutoSizeColumnMode.AllCells);

                    if (TextColumns.Contains(column.Name) && column.Width > maxTextWidth)
                    {
                        _cappedColumns[column.Name] = column.Width;
                        column.Width = Math.Max(column.MinimumWidth, maxTextWidth);
                    }
                }

                if (status is not null)
                {
                    status.MinimumWidth = Math.Max(status.MinimumWidth, LongestStatusWidth(status));
                    status.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                }
            }
            finally
            {
                _sizingColumns = false;
            }
        }

        /// <summary>Tope de ancho de una columna de texto para el ancho visible actual.</summary>
        private int MaxTextColumnWidth => Math.Max(150, (int)(dataGrid.ClientSize.Width * MaxTextColumnRatio));

        /// <summary>
        /// Recalcula el tope de las columnas recortadas cuando cambia el ancho de la rejilla, para
        /// que al maximizar el espacio vaya a los nombres que no cabían y no solo a «Estado».
        /// </summary>
        private void dataGrid_SizeChanged(object? sender, EventArgs e)
        {
            if (_cappedColumns.Count == 0 || !IsAlive)
            {
                return;
            }

            int maxTextWidth = MaxTextColumnWidth;
            _sizingColumns = true;

            try
            {
                foreach ((string name, int preferred) in _cappedColumns)
                {
                    if (dataGrid.Columns[name] is { } column)
                    {
                        column.Width = Math.Max(column.MinimumWidth, Math.Min(preferred, maxTextWidth));
                    }
                }
            }
            finally
            {
                _sizingColumns = false;
            }
        }

        /// <summary>Deja de gobernar el ancho de una columna que el usuario ajusta a mano.</summary>
        private void dataGrid_ColumnWidthChanged(object? sender, DataGridViewColumnEventArgs e)
        {
            if (!_sizingColumns)
            {
                _cappedColumns.Remove(e.Column.Name);
            }
        }

        /// <summary>Ancho que necesita la celda de estado para el valor más largo que puede tomar.</summary>
        private int LongestStatusWidth(DataGridViewColumn status)
        {
            DataGridViewCellStyle? style = status.InheritedStyle;
            Font font = style?.Font ?? dataGrid.Font;
            int text = StatusTexts.Max(value => TextRenderer.MeasureText(value, font).Width);

            // Relleno de la celda más el margen que la propia rejilla deja al pintar el texto.
            return text + (style?.Padding.Horizontal ?? 0) + 8;
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
