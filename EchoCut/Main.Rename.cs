using EchoCut.Library;
using EchoCut.Objects;

namespace EchoCut
{
    /// <summary>
    /// La parte de <see cref="Main"/> que renombra archivos: una pista desde la propia rejilla con
    /// F2, o varias a la vez anteponiendo un consecutivo o quitando caracteres del principio.
    /// </summary>
    public partial class Main
    {
        /// <summary>Cuadro de edición sobre la celda «Nombre» mientras se renombra con F2.</summary>
        private TextBox? _renameBox;

        /// <summary>Pista que se está renombrando con F2.</summary>
        private Song? _renamingSong;

        // ------------------------------------------------------------- Renombrar desde la rejilla

        /// <summary>
        /// Abre un cuadro de edición sobre la celda «Nombre» de la fila actual, como F2 en el Explorador.
        /// </summary>
        /// <param name="initialText">
        /// Texto con el que abrir; por defecto, el nombre actual. Tras un error se reabre con el
        /// nombre que se intentó para corregirlo sin volver a escribirlo.
        /// </param>
        /// <remarks>
        /// <para>
        /// No se usa la edición propia de la rejilla porque «Nombre» está enlazada a una propiedad
        /// de solo lectura: el nombre no se cambia asignándolo, sino renombrando el archivo, que
        /// puede fallar y debe validarse antes de tocar la fila.
        /// </para>
        /// <para>
        /// El cuadro se coloca sobre el formulario y no dentro de la rejilla: la rejilla interpreta
        /// Entrar y Esc para su propia navegación y se los quitaría al cuadro.
        /// </para>
        /// </remarks>
        private void BeginRename(string? initialText = null)
        {
            if (IsBusy
                || _renameBox is not null
                || dataGrid.CurrentRow is not { Visible: true } row
                || row.DataBoundItem is not Song song
                || dataGrid.Columns[nameof(Song.Name)] is not { Visible: true } column)
            {
                return;
            }

            // Fijar la celda actual la desplaza a la vista antes de medirla.
            dataGrid.CurrentCell = row.Cells[column.Index];
            Rectangle cell = dataGrid.GetCellDisplayRectangle(column.Index, row.Index, cutOverflow: true);
            if (cell.IsEmpty)
            {
                return;
            }

            if (ReferenceEquals(song, _playingSong))
            {
                StopPreview();
            }

            Point location = PointToClient(dataGrid.PointToScreen(cell.Location));
            TextBox box = new()
            {
                BorderStyle = BorderStyle.FixedSingle,
                Font = dataGrid.Font,
                Text = initialText ?? song.Name,
                Location = location,
                Width = Math.Max(cell.Width, 240),
            };

            box.KeyDown += RenameBox_KeyDown;
            box.Leave += (_, _) => CommitRename();

            // Si la celda se mueve, el cuadro quedaría flotando sobre otra fila.
            dataGrid.Scroll += RenameHost_Moved;
            dataGrid.SizeChanged += RenameHost_Moved;

            _renameBox = box;
            _renamingSong = song;

            Controls.Add(box);
            box.BringToFront();
            box.Focus();
            box.SelectAll();
        }

        private void RenameBox_KeyDown(object? sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Enter:
                    e.SuppressKeyPress = true;
                    CommitRename();
                    break;

                case Keys.Escape:
                    e.SuppressKeyPress = true;
                    EndRename();
                    break;
            }
        }

        /// <summary>Renombra el archivo con el texto escrito y actualiza la fila.</summary>
        /// <remarks>
        /// Se confirma al pulsar Entrar y también al perder el foco, como en el Explorador: pulsar en
        /// otra fila o desplazar la rejilla no debe descartar lo escrito.
        /// </remarks>
        private void CommitRename()
        {
            if (_renameBox is not { } box || _renamingSong is not { } song)
            {
                return;
            }

            string newName = box.Text.Trim();
            EndRename();

            if (string.Equals(newName, song.Name, StringComparison.Ordinal) || IsBusy)
            {
                return;
            }

            // Se valida aquí, y no solo en el núcleo, para explicar el problema con su propio texto
            // y no con el de la excepción, que arrastra el nombre del parámetro.
            if (TrackNaming.GetProblem(newName) is { } problem)
            {
                MessageBox.Show(this, problem, "Nombre no válido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                BeginRename(newName);
                return;
            }

            string oldPath = song.FilePath;
            string oldName = song.Name;

            try
            {
                TrackInfo updated = TrackEditor.RenameTrack(oldPath, newName);
                ReplaceTrack(song, oldPath, updated);
                _songs.ReapplySort();
                SetStatus($"Renombrado: «{oldName}» → «{song.Name}».");
            }
            catch (Exception exception) when (exception is ArgumentException or IOException or UnauthorizedAccessException)
            {
                ShowError("No se pudo renombrar la canción.", exception);

                // Se reabre con lo escrito para corregirlo, si la fila sigue siendo la actual.
                if (IsAlive && ReferenceEquals(dataGrid.CurrentRow?.DataBoundItem, song))
                {
                    BeginRename(newName);
                }
            }
        }

        /// <summary>Retira el cuadro de edición sin renombrar.</summary>
        /// <remarks>
        /// Los campos se vacían antes de quitar el cuadro: retirarlo le quita el foco y dispara
        /// <c>Leave</c>, que de otro modo volvería a intentar confirmar.
        /// </remarks>
        private void EndRename()
        {
            if (_renameBox is not { } box)
            {
                return;
            }

            _renameBox = null;
            _renamingSong = null;

            dataGrid.Scroll -= RenameHost_Moved;
            dataGrid.SizeChanged -= RenameHost_Moved;
            Controls.Remove(box);
            box.Dispose();

            if (IsAlive)
            {
                dataGrid.Focus();
            }
        }

        private void RenameHost_Moved(object? sender, EventArgs e) => CommitRename();

        private void mnuRenameSong_Click(object? sender, EventArgs e) => BeginRename();

        // ----------------------------------------------------------------- Renombrar en lote

        private async void mnuAddSequence_Click(object? sender, EventArgs e) =>
            await RenameBatchAsync(RenameMode.Sequence).ConfigureAwait(true);

        private async void mnuRemoveLeading_Click(object? sender, EventArgs e) =>
            await RenameBatchAsync(RenameMode.RemoveLeading).ConfigureAwait(true);

        /// <summary>
        /// Calcula los nombres nuevos con <see cref="RenameSongsDialog"/> y renombra las pistas que cambian.
        /// </summary>
        /// <remarks>
        /// Con varias filas seleccionadas se renombran solo esas; con una o ninguna, todo el listado.
        /// En ambos casos en el orden de la rejilla, que es el que el usuario ve y en el que espera
        /// que se numere. La vista previa del diálogo hace de confirmación: muestra cada nombre
        /// antes y después, y no deja aceptar si alguno no es válido.
        /// </remarks>
        private async Task RenameBatchAsync(RenameMode mode)
        {
            if (IsBusy || _songs.Count == 0)
            {
                return;
            }

            List<Song> selected = GetSelectedSongs();
            bool selectionOnly = selected.Count > 1;
            List<Song> targets = selectionOnly ? selected : [.. _songs];

            using RenameSongsDialog dialog = new(
                mode,
                [.. targets.Select(song => new RenameItem(song.Track.Directory, song.Name, song.Extension))],
                selectionOnly);

            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            Dictionary<string, string> newNames = new(StringComparer.OrdinalIgnoreCase);
            List<Song> changed = [];

            for (int i = 0; i < targets.Count; i++)
            {
                if (!string.Equals(dialog.NewNames[i], targets[i].Name, StringComparison.Ordinal))
                {
                    newNames[targets[i].FilePath] = dialog.NewNames[i];
                    changed.Add(targets[i]);
                }
            }

            if (changed.Count == 0)
            {
                return;
            }

            await RunLibraryBatchAsync(
                changed,
                new LibraryBatch(
                    "Renombrando canciones…",
                    (success, total) => success == 1 && total == 1
                        ? "1 canción renombrada."
                        : $"{success} de {total} canciones renombradas.",
                    "Renombrado cancelado.",
                    "No se pudo completar el renombrado.",
                    "Aviso de renombrado",
                    "No se pudieron renombrar algunos archivos:"),
                path => TrackEditor.RenameTrack(path, newNames[path]),
                cts => _renameCts = cts).ConfigureAwait(true);
        }
    }
}
