using EchoCut.Audio;
using EchoCut.Fingerprints;
using EchoCut.Library;
using EchoCut.Objects;
using EchoCut.Processing;

namespace EchoCut
{
    /// <summary>
    /// La parte de <see cref="Main"/> con las utilidades que no tocan los originales: convertir a MP3
    /// y buscar canciones duplicadas por su audio.
    /// </summary>
    /// <remarks>
    /// Siguen el patrón de los demás lotes: un <see cref="CancellationTokenSource"/> propio que entra en
    /// <see cref="IsBusy"/>, la barra de progreso por pista y un aviso final con lo que falló, sin que un
    /// archivo ilegible detenga a los demás.
    /// </remarks>
    public partial class Main
    {
        // --------------------------------------------------------------------- Convertir a MP3

        private async void mnuConvertMp3_Click(object? sender, EventArgs e)
        {
            if (IsBusy || _songs.Count == 0 || !EnsureFFmpeg())
            {
                return;
            }

            List<TrackInfo> targets = [.. _songs.Select(song => song.Track).Where(track => !Mp3Converter.IsMp3(track.FilePath))];
            int skipped = _songs.Count - targets.Count;
            if (targets.Count == 0)
            {
                MessageBox.Show(this, "Todas las canciones cargadas ya son MP3.", "Convertir a MP3", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (ConvertMp3Dialog dialog = new(_settings.Mp3Quality, targets.Count, skipped))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                _settings.Mp3Quality = dialog.Selected;
                _settings.Save();
            }

            StopPreview();
            using CancellationTokenSource cts = new();
            _convertCts = cts;
            UpdateButtons();
            StartProgress(targets.Count, "Convirtiendo a MP3…");

            List<string> errors = [];
            try
            {
                Task<BatchSummary> batch = new ConversionService(_locator).ConvertAsync(
                    targets,
                    _settings.Mp3Quality,
                    (int)numericThreads.Value,
                    CreateProgress<ConversionOutcome>(progress => TrackProgressOf(progress, errors)),
                    cts.Token);

                _running = batch;
                BatchSummary summary = await batch.ConfigureAwait(true);

                SetStatus(summary.Failed == 0
                    ? $"Conversión completada: {summary.Succeeded} {(summary.Succeeded == 1 ? "canción" : "canciones")} en la subcarpeta «{ConversionService.OutputFolderName}» ({_settings.Mp3Quality.DisplayName()})."
                    : $"Conversión completada con {summary.Failed} error(es) de {summary.Total}.");
            }
            catch (OperationCanceledException)
            {
                SetStatus("Conversión detenida.");
            }
            catch (Exception exception)
            {
                ShowError("No se pudo completar la conversión.", exception);
            }
            finally
            {
                _convertCts = null;
                _running = null;
                if (IsAlive)
                {
                    EndProgress();
                    UpdateButtons();
                }
            }

            ReportFailures(errors, "Convertir a MP3", "No se pudieron convertir algunas canciones:");
        }

        // ------------------------------------------------------------------ Buscar duplicados

        private async void mnuFindDuplicates_Click(object? sender, EventArgs e)
        {
            if (IsBusy || _songs.Count < 2 || !EnsureFFmpeg())
            {
                return;
            }

            List<TrackInfo> tracks = [.. _songs.Select(song => song.Track)];
            StopPreview();
            using CancellationTokenSource cts = new();
            _duplicatesCts = cts;
            UpdateButtons();
            StartProgress(tracks.Count, "Calculando las huellas acústicas…");

            List<string> errors = [];
            IReadOnlyList<DuplicateGroup>? groups = null;
            try
            {
                Task<IReadOnlyList<DuplicateGroup>> search = new DuplicateFinder(_locator).FindAsync(
                    tracks,
                    (int)numericThreads.Value,
                    CreateProgress<AudioFingerprint>(progress => TrackProgressOf(progress, errors)),
                    cts.Token);

                _running = search;
                groups = await search.ConfigureAwait(true);
                SetStatus(groups.Count == 0
                    ? $"No hay canciones duplicadas por su audio entre las {tracks.Count} cargadas."
                    : $"{groups.Count} {(groups.Count == 1 ? "grupo" : "grupos")} de canciones duplicadas por su audio.");
            }
            catch (OperationCanceledException)
            {
                SetStatus("Búsqueda de duplicados detenida.");
            }
            catch (Exception exception)
            {
                ShowError("No se pudo completar la búsqueda de duplicados.", exception);
            }
            finally
            {
                _duplicatesCts = null;
                _running = null;
                if (IsAlive)
                {
                    EndProgress();
                    UpdateButtons();
                }
            }

            ReportFailures(errors, "Buscar duplicados", "No se pudo calcular la huella de algunas canciones, que quedaron fuera de la búsqueda:");

            if (groups is null || !IsAlive)
            {
                return;
            }

            if (groups.Count == 0)
            {
                MessageBox.Show(this, $"No se encontraron canciones duplicadas por su audio entre las {tracks.Count} cargadas.", "Buscar duplicados", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using DuplicatesDialog dialog = new(groups, _locator.Require().FFmpeg);
            dialog.ShowDialog(this);
            RemoveDeletedRows(dialog.DeletedPaths);
        }

        /// <summary>Retira las filas de los archivos enviados a la Papelera desde la ventana de duplicados.</summary>
        private void RemoveDeletedRows(IReadOnlyList<string> paths)
        {
            if (paths.Count == 0)
            {
                return;
            }

            HashSet<string> deleted = new(paths, StringComparer.OrdinalIgnoreCase);
            List<Song> removed = [.. _songs.Where(song => deleted.Contains(song.FilePath))];

            DiscardEditors(removed);
            foreach (Song song in removed)
            {
                _rows.Remove(song.FilePath);
                _songs.Remove(song);
            }

            UpdateButtons();
            SetStatus($"Se {(removed.Count == 1 ? "envió 1 canción" : $"enviaron {removed.Count} canciones")} a la Papelera de reciclaje.");
        }

        /// <summary>Avanza la barra con cada pista terminada y anota las que fallaron.</summary>
        private void TrackProgressOf<T>(TrackProgress<T> progress, List<string> errors)
            where T : class
        {
            if (progress is { State: TrackState.Failed, Error: { } error })
            {
                errors.Add($"{progress.Track.Name}: {error.Message}");
            }

            AdvanceProgress(progress.CompletedCount);
        }

        /// <summary>Muestra, al terminar un lote, la lista de archivos que fallaron, si los hubo.</summary>
        private void ReportFailures(List<string> errors, string title, string intro)
        {
            if (errors.Count > 0 && IsAlive)
            {
                MessageBox.Show(this, $"{intro}\n\n{string.Join(Environment.NewLine, errors)}", title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
