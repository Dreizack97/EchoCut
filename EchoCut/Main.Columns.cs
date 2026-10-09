using EchoCut.Objects;
using System.ComponentModel;

namespace EchoCut
{
    /// <summary>
    /// La parte de <see cref="Main"/> que deja elegir qué columnas de la rejilla se ven y lo
    /// recuerda entre sesiones.
    /// </summary>
    /// <remarks>
    /// Trece columnas no caben en una ventana normal, y no todas interesan a todo el mundo: quien
    /// solo recorta no necesita «Título» ni «Bitrate». Ocultar las que sobran devuelve el ancho a
    /// las que se consultan sin obligar a desplazarse en horizontal.
    /// </remarks>
    public partial class Main
    {
        /// <summary>
        /// Columna que no se puede ocultar: es la que identifica cada fila y sobre la que filtra el
        /// cuadro de búsqueda. Sin ella la rejilla mostraría datos sin decir de qué pista son.
        /// </summary>
        private const string AlwaysVisibleColumn = nameof(Song.Name);

        /// <summary>Oculta las columnas guardadas en la configuración.</summary>
        /// <remarks>
        /// Se llama en cada reenlace y solo oculta, nunca muestra: las columnas autogeneradas
        /// sobreviven a los reenlaces con la visibilidad que el usuario les dio, y la configuración
        /// se guarda en cuanto la cambia, así que ambas coinciden siempre. El JSON puede venir
        /// editado a mano, de ahí que se vuelva a comprobar qué columnas admiten ocultarse.
        /// </remarks>
        private void ApplyHiddenColumns()
        {
            foreach (string name in _settings.HiddenColumns)
            {
                if (dataGrid.Columns[name] is { } column && IsToggleable(column))
                {
                    column.Visible = false;
                }
            }
        }

        /// <summary>Si el usuario puede ocultar una columna.</summary>
        /// <remarks>
        /// Las columnas de acción no son datos sino botones de la fila, y reproducir o recortar una
        /// pista no debe poder desaparecer de la vista por descuido; «Nombre», por
        /// <see cref="AlwaysVisibleColumn"/>, tampoco.
        /// </remarks>
        private static bool IsToggleable(DataGridViewColumn column) =>
            column is not DataGridViewButtonColumn && column.Name != AlwaysVisibleColumn;

        /// <summary>Asigna el menú de columnas a las cabeceras; el resto de celdas conserva el de la fila.</summary>
        private void dataGrid_CellContextMenuStripNeeded(object? sender, DataGridViewCellContextMenuStripNeededEventArgs e)
        {
            if (e.RowIndex == -1)
            {
                e.ContextMenuStrip = columnsMenuStrip;
            }
        }

        private void columnsMenuStrip_Opening(object? sender, CancelEventArgs e) =>
            BuildColumnItems(columnsMenuStrip.Items);

        private void mnuColumns_DropDownOpening(object? sender, EventArgs e) =>
            BuildColumnItems(mnuColumns.DropDownItems);

        /// <summary>
        /// Mantiene abierto el menú de las cabeceras al marcar o desmarcar una columna.
        /// </summary>
        /// <remarks>
        /// Ocultar varias columnas es lo habitual, y que el menú se cierre tras cada una obliga a
        /// reabrirlo otras tantas veces. Las acciones del pie del menú sí lo cierran.
        /// </remarks>
        private void columnsMenuStrip_Closing(object? sender, ToolStripDropDownClosingEventArgs e)
        {
            if (e.CloseReason == ToolStripDropDownCloseReason.ItemClicked
                && columnsMenuStrip.GetItemAt(columnsMenuStrip.PointToClient(Cursor.Position)) is { Tag: DataGridViewColumn })
            {
                e.Cancel = true;
            }
        }

        /// <summary>
        /// Rellena un menú con una casilla por columna y las acciones comunes sobre columnas.
        /// </summary>
        /// <remarks>
        /// Se reconstruye cada vez que se abre, en vez de mantenerse sincronizado, porque las columnas
        /// se generan al enlazar y su visibilidad puede haber cambiado desde el otro menú.
        /// </remarks>
        private void BuildColumnItems(ToolStripItemCollection items)
        {
            foreach (ToolStripItem old in items.Cast<ToolStripItem>().ToList())
            {
                old.Dispose();
            }

            bool anyHidden = false;

            foreach (DataGridViewColumn column in dataGrid.Columns.Cast<DataGridViewColumn>().OrderBy(c => c.DisplayIndex))
            {
                if (column is DataGridViewButtonColumn)
                {
                    continue;
                }

                anyHidden |= !column.Visible;

                ToolStripMenuItem item = new(column.HeaderText)
                {
                    Checked = column.Visible,
                    Enabled = IsToggleable(column),
                    Tag = column,
                };
                item.Click += ColumnItem_Click;
                items.Add(item);
            }

            ToolStripMenuItem showAll = new("Mostrar todas") { Enabled = anyHidden };
            showAll.Click += (_, _) => ShowAllColumns();

            ToolStripMenuItem fit = new("Ajustar anchos al contenido");
            fit.Click += (_, _) => AutoSizeColumns();

            items.Add(new ToolStripSeparator());
            items.Add(showAll);
            items.Add(fit);
        }

        private void ColumnItem_Click(object? sender, EventArgs e)
        {
            if (sender is not ToolStripMenuItem { Tag: DataGridViewColumn column } item
                || !IsToggleable(column))
            {
                return;
            }

            column.Visible = !column.Visible;
            item.Checked = column.Visible;
            SaveHiddenColumns();
        }

        private void ShowAllColumns()
        {
            foreach (DataGridViewColumn column in dataGrid.Columns)
            {
                if (IsToggleable(column))
                {
                    column.Visible = true;
                }
            }

            SaveHiddenColumns();
        }

        /// <summary>
        /// Guarda las columnas ocultas en cuanto cambian, sin esperar al cierre de la ventana, para
        /// que un cierre inesperado no las pierda.
        /// </summary>
        /// <remarks>
        /// Se conservan los nombres guardados que esta versión no conoce: si una versión posterior
        /// añadió una columna y el usuario la ocultó, volver a abrir esta no debe olvidarlo.
        /// </remarks>
        private void SaveHiddenColumns()
        {
            IEnumerable<string> unknown = _settings.HiddenColumns.Where(name => !dataGrid.Columns.Contains(name));
            IEnumerable<string> hidden = dataGrid.Columns
                .Cast<DataGridViewColumn>()
                .Where(column => IsToggleable(column) && !column.Visible)
                .Select(column => column.Name);

            _settings.HiddenColumns = [.. unknown.Concat(hidden).Distinct(StringComparer.Ordinal)];
            _settings.Save();
        }
    }
}
