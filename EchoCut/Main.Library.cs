using EchoCut.Library;
using EchoCut.Objects;

namespace EchoCut
{
    /// <summary>
    /// La parte de <see cref="Main"/> que agrupa las operaciones de biblioteca: las que modifican o
    /// renombran el archivo original en lugar de escribir una copia.
    /// </summary>
    /// <remarks>
    /// Comparten el mismo esqueleto —recorrer el listado en paralelo, sustituir cada fila por la
    /// pista releída, contar fallos sin abortar el lote— y solo difieren en qué le hacen a cada
    /// archivo y en cómo lo cuentan, de ahí <see cref="RunLibraryBatchAsync"/>.
    /// </remarks>
    public partial class Main
    {
        /// <summary>Textos con los que una operación de biblioteca informa de su avance y resultado.</summary>
        /// <param name="Progress">Mensaje mientras el lote está en marcha.</param>
        /// <param name="Completed">Mensaje final a partir de las pistas correctas y del total.</param>
        /// <param name="Cancelled">Mensaje si el usuario detiene el lote.</param>
        /// <param name="Failed">Mensaje si el lote entero falla.</param>
        /// <param name="WarningTitle">Título del aviso con los archivos que fallaron.</param>
        /// <param name="WarningIntro">Primera línea de ese aviso, antes de la lista de archivos.</param>
        private sealed record LibraryBatch(
            string Progress,
            Func<int, int, string> Completed,
            string Cancelled,
            string Failed,
            string WarningTitle,
            string WarningIntro);

        /// <summary>
        /// Aplica <paramref name="edit"/> a cada pista del listado y sustituye su fila por la pista
        /// que devuelve.
        /// </summary>
        /// <param name="batch">Textos de la operación.</param>
        /// <param name="edit">
        /// Operación sobre el archivo, a partir de su ruta. Corre en un hilo de trabajo y devuelve la
        /// pista releída del disco, quizá con otra ruta si la renombró.
        /// </param>
        /// <param name="track">
        /// Anota o retira el <see cref="CancellationTokenSource"/> en el campo propio de la operación,
        /// del que dependen <see cref="IsBusy"/> y el botón «Detener».
        /// </param>
        /// <remarks>
        /// Un fallo por archivo no aborta el lote: se anota y se muestra al final, porque un archivo
        /// de solo lectura no debe impedir procesar los cientos restantes. Los avisos al hilo de la
        /// interfaz se encolan con <see cref="Control.BeginInvoke(Delegate)"/> y comprueban
        /// <see cref="IsAlive"/>, porque pueden llegar con la ventana ya cerrándose.
        /// </remarks>
        private async Task RunLibraryBatchAsync(
            LibraryBatch batch,
            Func<string, TrackInfo> edit,
            Action<CancellationTokenSource?> track)
        {
            StopPreview();

            List<Song> songs = [.. _songs];
            using CancellationTokenSource cts = new();
            track(cts);
            UpdateButtons();

            StartProgress(songs.Count, batch.Progress);

            int completed = 0;
            int successCount = 0;
            List<string> errors = [];
            int degreeOfParallelism = Math.Clamp((int)numericThreads.Value, 1, 8);

            try
            {
                _running = Task.Run(async () =>
                {
                    ParallelOptions parallelOptions = new()
                    {
                        MaxDegreeOfParallelism = degreeOfParallelism,
                        CancellationToken = cts.Token,
                    };

                    await Parallel.ForEachAsync(songs, parallelOptions, (song, token) =>
                    {
                        token.ThrowIfCancellationRequested();

                        try
                        {
                            string oldPath = song.FilePath;
                            TrackInfo updated = edit(oldPath);

                            PostToUi(() =>
                            {
                                // La fila se localiza por ruta: si la operación renombró el
                                // archivo, los avisos posteriores deben encontrarla con la nueva.
                                string newPath = updated.FilePath;
                                if (!string.Equals(oldPath, newPath, StringComparison.Ordinal))
                                {
                                    _rows.Remove(oldPath);
                                    _rows[newPath] = song;
                                }

                                song.UpdateTrack(updated);
                            });

                            Interlocked.Increment(ref successCount);
                        }
                        catch (Exception exception)
                        {
                            lock (errors)
                            {
                                errors.Add($"{song.Name}: {exception.Message}");
                            }
                        }
                        finally
                        {
                            int current = Interlocked.Increment(ref completed);
                            PostToUi(() => AdvanceProgress(current));
                        }

                        return ValueTask.CompletedTask;
                    }).ConfigureAwait(false);
                }, cts.Token);

                await _running.ConfigureAwait(true);

                if (IsAlive)
                {
                    _songs.ReapplySort();

                    // Los metadatos reescritos cambian el ancho que necesitan sus columnas.
                    AutoSizeColumns();

                    SetStatus(batch.Completed(successCount, songs.Count));
                }
            }
            catch (OperationCanceledException)
            {
                SetStatus(batch.Cancelled);
            }
            catch (Exception exception)
            {
                ShowError(batch.Failed, exception);
            }
            finally
            {
                track(null);
                _running = null;

                if (IsAlive)
                {
                    EndProgress();
                    UpdateButtons();
                }
            }

            if (errors.Count > 0 && IsAlive)
            {
                MessageBox.Show(
                    this,
                    $"{batch.WarningIntro}\n\n{string.Join(Environment.NewLine, errors)}",
                    batch.WarningTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// Encola una acción en el hilo de la interfaz desde un hilo de trabajo, si la ventana sigue viva.
        /// </summary>
        /// <remarks>
        /// <see cref="InvalidOperationException"/> llega si el identificador de la ventana se destruyó
        /// entre la comprobación y el encolado; en ese caso no queda nada que actualizar.
        /// </remarks>
        private void PostToUi(Action action)
        {
            if (!IsAlive)
            {
                return;
            }

            try
            {
                BeginInvoke(() =>
                {
                    if (IsAlive)
                    {
                        action();
                    }
                });
            }
            catch (InvalidOperationException)
            {
            }
        }

        // ------------------------------------------------------------------- Limpiar metadatos

        private async void mnuCleanMetadata_Click(object sender, EventArgs e)
        {
            if (IsBusy || _songs.Count == 0)
            {
                return;
            }

            int count = _songs.Count;
            string confirmationMessage = count == 1
                ? $"¿Está seguro de que desea eliminar todos los metadatos de «{_songs[0].Name}»?\n\nEsta operación modificará el archivo directamente en el disco."
                : $"¿Está seguro de que desea eliminar todos los metadatos de las {count} canciones cargadas en la lista?\n\nEsta operación modificará los archivos directamente en el disco.";

            DialogResult confirmation = MessageBox.Show(
                this,
                confirmationMessage,
                "Confirmar limpieza de metadatos",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);

            if (confirmation != DialogResult.Yes)
            {
                return;
            }

            await RunLibraryBatchAsync(
                new LibraryBatch(
                    "Limpiando metadatos…",
                    (success, total) => success == 1
                        ? "Metadatos eliminados correctamente para 1 canción."
                        : $"Metadatos eliminados correctamente en {success} de {total} canciones.",
                    "Limpieza de metadatos cancelada.",
                    "No se pudo completar la limpieza de metadatos.",
                    "Aviso de limpieza",
                    "No se pudieron limpiar los metadatos de algunos archivos:"),
                TrackEditor.StripMetadata,
                cts => _cleanCts = cts).ConfigureAwait(true);
        }

        // ------------------------------------------------------------------- Agregar metadatos

        /// <summary>
        /// Pide las etiquetas a aplicar y confirma antes de escribirlas en todas las pistas.
        /// </summary>
        /// <param name="count">Pistas a las que afectará el lote.</param>
        /// <returns>El parche a aplicar, o <c>null</c> si el usuario canceló en cualquiera de los dos pasos.</returns>
        /// <remarks>
        /// La confirmación repite los valores escritos porque reemplazan los de todo el listado: es
        /// el último momento para ver una errata antes de que se copie en cientos de archivos.
        /// </remarks>
        private TagPatch? ChooseTagPatch(int count)
        {
            using AddMetadataDialog dialog = new(count);
            if (dialog.ShowDialog(this) != DialogResult.OK || dialog.Patch.IsEmpty)
            {
                return null;
            }

            TagPatch patch = dialog.Patch;

            string target = count == 1
                ? $"«{_songs[0].Name}»"
                : $"las {count} canciones cargadas en la lista";

            string changes = string.Join(
                Environment.NewLine,
                AddMetadataDialog.Describe(patch).Select(line => $" • {line}"));

            DialogResult confirmation = MessageBox.Show(
                this,
                $"¿Aplicar estos metadatos a {target}?\n\n{changes}\n\n"
                + "Reemplazarán los valores que ya tengan; las propiedades que dejaste en blanco no se modifican. "
                + "Los archivos originales se modifican directamente y el cambio no se puede deshacer.",
                "Confirmar metadatos",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);

            return confirmation == DialogResult.Yes ? patch : null;
        }

        private async void mnuAddMetadata_Click(object sender, EventArgs e)
        {
            if (IsBusy || _songs.Count == 0)
            {
                return;
            }

            if (ChooseTagPatch(_songs.Count) is not { } patch)
            {
                return;
            }

            await RunLibraryBatchAsync(
                new LibraryBatch(
                    "Aplicando metadatos…",
                    (success, total) => success == 1 && total == 1
                        ? "Metadatos aplicados a 1 canción."
                        : $"Metadatos aplicados a {success} de {total} canciones.",
                    "Aplicación de metadatos cancelada.",
                    "No se pudieron aplicar los metadatos.",
                    "Aviso de metadatos",
                    "No se pudieron aplicar los metadatos a algunos archivos:"),
                path => TrackEditor.ApplyTags(path, patch),
                cts => _tagCts = cts).ConfigureAwait(true);
        }

        // -------------------------------------------------------------------------- Normalizar

        /// <summary>
        /// Pregunta qué propiedades normalizar y pide confirmación antes de tocar los archivos.
        /// </summary>
        /// <param name="count">Pistas a las que afectará el lote.</param>
        /// <returns>Las propiedades elegidas, o <c>null</c> si el usuario canceló en cualquiera de los dos pasos.</returns>
        /// <remarks>
        /// La elección se guarda en cuanto se acepta el diálogo, aunque luego se cancele la
        /// confirmación: es una preferencia sobre qué normalizar, no una orden de hacerlo ya.
        /// La confirmación enumera las propiedades porque el diálogo ya se cerró, y lo que se va a
        /// modificar —sobre todo si incluye renombrar archivos— debe leerse justo antes de aceptar.
        /// </remarks>
        private NormalizableFields? ChooseNormalizeFields(int count)
        {
            using NormalizeOptions dialog = new(_settings.NormalizeFields, count);
            if (dialog.ShowDialog(this) != DialogResult.OK || dialog.Selected == NormalizableFields.None)
            {
                return null;
            }

            NormalizableFields fields = dialog.Selected;
            _settings.NormalizeFields = fields;
            _settings.Save();

            string target = count == 1
                ? $"«{_songs[0].Name}»"
                : $"las {count} canciones cargadas en la lista";

            string properties = string.Join(
                Environment.NewLine,
                NormalizeOptions.Describe(fields).Select(name => $" • {name}"));

            string rename = fields.HasFlag(NormalizableFields.FileName)
                ? "Los archivos se renombrarán en disco si su nombre cambia. "
                : string.Empty;

            DialogResult confirmation = MessageBox.Show(
                this,
                $"¿Normalizar {target}?\n\n"
                + $"Se quitarán los acentos (conservando la «ñ») y cada palabra empezará con mayúscula en:\n{properties}\n\n"
                + $"{rename}Los archivos originales se modifican directamente y el cambio no se puede deshacer.",
                "Confirmar normalización de canciones",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);

            return confirmation == DialogResult.Yes ? fields : null;
        }

        private async void mnuNormalize_Click(object sender, EventArgs e)
        {
            if (IsBusy || _songs.Count == 0)
            {
                return;
            }

            if (ChooseNormalizeFields(_songs.Count) is not { } fields)
            {
                return;
            }

            await RunLibraryBatchAsync(
                new LibraryBatch(
                    "Normalizando información de canciones…",
                    (success, total) => success == 1
                        ? "Normalización completada para 1 canción."
                        : $"Normalización completada en {success} de {total} canciones.",
                    "Normalización de canciones cancelada.",
                    "No se pudo completar la normalización de canciones.",
                    "Aviso de normalización",
                    "No se pudieron normalizar algunos archivos:"),
                path => TrackEditor.NormalizeTrack(path, fields),
                cts => _normalizeCts = cts).ConfigureAwait(true);
        }
    }
}
