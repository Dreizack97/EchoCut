using EchoCut.Audio;
using EchoCut.Library;
using EchoCut.Processing;

namespace EchoCut
{
    /// <summary>
    /// La parte de <see cref="Main"/> con las utilidades que no tocan los originales, como convertir
    /// a MP3.
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
