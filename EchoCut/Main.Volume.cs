using EchoCut.Library;
using EchoCut.Processing;

namespace EchoCut
{
    /// <summary>
    /// La parte de <see cref="Main"/> que abre la ventana «Regularizar volumen» y recoge lo que cambió.
    /// </summary>
    /// <remarks>
    /// La ventana es modal y lleva su propio lote, su barra y su «Detener»: mientras está abierta, la
    /// ventana principal no puede lanzar otra operación sobre los mismos archivos. Al cerrarla se
    /// sustituyen las filas de los archivos que se reescribieron.
    /// </remarks>
    public partial class Main
    {
        private void mnuVolume_Click(object? sender, EventArgs e)
        {
            if (IsBusy || _songs.Count == 0)
            {
                return;
            }

            List<TrackInfo> tracks = [.. _songs.Select(song => song.Track).Where(LoudnessService.CanAdjust)];
            if (tracks.Count == 0)
            {
                MessageBox.Show(this, "Ninguna de las canciones cargadas es MP3. El volumen solo se puede regularizar sin pérdida de calidad en ese formato.", "Regularizar volumen", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!EnsureFFmpeg())
            {
                return;
            }

            // La previsualización mantiene el archivo abierto y no podría sustituirse.
            StopPreview();

            IReadOnlyCollection<TrackInfo> updated;
            using (VolumeDialog dialog = new(tracks, _locator, (int)numericThreads.Value, _settings.VolumeTargetDb))
            {
                dialog.ShowDialog(this);
                _settings.VolumeTargetDb = dialog.TargetDb;
                _settings.Save();
                updated = [.. dialog.UpdatedTracks];
            }

            foreach (TrackInfo track in updated)
            {
                if (RowFor(track) is { } song)
                {
                    ReplaceTrack(song, track.FilePath, track);
                }
            }

            if (updated.Count > 0)
            {
                _songs.ReapplySort();
            }

            int skipped = _songs.Count - tracks.Count;
            SetStatus(updated.Count == 0
                ? "Regularización de volumen cerrada sin cambios."
                : $"Volumen regularizado: {(updated.Count == 1 ? "1 canción modificada" : $"{updated.Count} canciones modificadas")}"
                    + (skipped > 0 ? $"; {skipped} no MP3 se omitieron." : "."));
        }
    }
}
