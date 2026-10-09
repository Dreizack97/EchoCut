using EchoCut.Audio;
using EchoCut.Controls;
using EchoCut.Playback;

namespace EchoCut
{
    /// <summary>
    /// La parte de <see cref="WaveformEditor"/> que escucha: la reproducción general (Espacio), los
    /// bordes de la copia y el cursor que recorre las vistas mientras suena.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Lo que suena pasa por las mismas ediciones que la copia —fundidos y borrados—, así que se
    /// juzga de oído exactamente lo que se va a guardar.
    /// </para>
    /// <para>
    /// Como en Audacity, la reproducción general suena la selección si la hay y, si no, desde el
    /// punto marcado con un clic; sin ninguno de los dos, desde el comienzo de la copia.
    /// </para>
    /// </remarks>
    public partial class WaveformEditor
    {
        /// <summary>Texto de los botones de escucha mientras su tramo suena.</summary>
        private const string StopPlaybackText = "⏹ Detener";

        /// <summary>
        /// Lo más que suena de una vez. El reproductor retiene el tramo en memoria, y medio minuto
        /// basta para juzgar un fundido o un empalme sin cargar una canción entera.
        /// </summary>
        private const double MaximumPlaybackSeconds = 30.0;

        private AudioPreviewPlayer? _player;
        private object? _playingItem;
        private string _playingItemText = string.Empty;
        private int _playRequest;
        private double? _cursorSeconds;

        /// <summary>Marca en todas las vistas el punto desde el que sonará la reproducción general.</summary>
        private void SetCursor(double? seconds)
        {
            _cursorSeconds = seconds;
            foreach (WaveformView view in Views)
            {
                view.CursorSeconds = seconds;
            }
        }

        private async void btnPlay_Click(object? sender, EventArgs e) =>
            await TogglePlaybackAsync(btnPlay, PlaybackWindow()).ConfigureAwait(true);

        /// <summary>Qué suena con la reproducción general: la selección, o desde el cursor, o desde el comienzo de la copia.</summary>
        private PreviewWindow PlaybackWindow()
        {
            if (_selection is { } selection)
            {
                return new PreviewWindow(selection.StartSeconds, Math.Min(selection.DurationSeconds, MaximumPlaybackSeconds));
            }

            double from = _cursorSeconds is { } cursor && cursor < _endSeconds ? cursor : _startSeconds;
            return new PreviewWindow(from, Math.Min(MaximumPlaybackSeconds, Math.Max(WaveformView.MinimumGapSeconds, _endSeconds - from)));
        }

        private async void btnPlayStart_Click(object? sender, EventArgs e) =>
            await TogglePlaybackAsync(
                btnPlayStart,
                new PreviewWindow(_startSeconds, Math.Min(_previewSeconds, _endSeconds - _startSeconds))).ConfigureAwait(true);

        private async void btnPlayEnd_Click(object? sender, EventArgs e)
        {
            double from = Math.Max(_startSeconds, _endSeconds - _previewSeconds);
            await TogglePlaybackAsync(btnPlayEnd, new PreviewWindow(from, _endSeconds - from)).ConfigureAwait(true);
        }

        /// <summary>
        /// Reproduce un tramo tal como sonaría en la copia, o lo detiene si es el que ya suena.
        /// Mientras suena, el botón que lo pidió ofrece detenerlo y las vistas muestran el cursor.
        /// </summary>
        /// <param name="item">Botón o elemento de la barra que pidió la escucha.</param>
        /// <param name="window">Tramo a reproducir.</param>
        /// <remarks>
        /// Pulsar cualquier botón de escucha mientras algo suena lo detiene, sin empezar otra cosa: es
        /// lo que se espera de Espacio, que alterna entre sonar y callar.
        /// </remarks>
        private async Task TogglePlaybackAsync(object item, PreviewWindow window)
        {
            bool wasPlaying = _playingItem is not null;
            bool sameItem = ReferenceEquals(item, _playingItem);
            StopPlayback();
            if (sameItem || (wasPlaying && ReferenceEquals(item, btnPlay)))
            {
                return;
            }

            // Un clic posterior invalida este: solo la última petición toca la interfaz al terminar
            // de decodificar.
            int request = ++_playRequest;

            try
            {
                _player ??= CreatePlayer();
                await _player.PlayAsync(Song.FilePath, window, Edits, _closing.Token).ConfigureAwait(true);

                if (IsDisposed || request != _playRequest || !_player.IsPlaying)
                {
                    return;
                }

                _playingItem = item;
                _playingItemText = TextOf(item);
                SetText(item, StopPlaybackText);
                playheadTimer.Start();
            }
            catch (OperationCanceledException)
            {
                // La ventana se cerró mientras se preparaba el audio.
            }
            catch (Exception exception)
            {
                if (!IsDisposed)
                {
                    MessageBox.Show(this, exception.Message, "No se pudo reproducir", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        /// <summary>
        /// Crea el reproductor en el hilo de la interfaz, donde avisará de que el tramo dejó de sonar
        /// —por llegar al final o porque otra ventana empezó a sonar— para devolver el botón y
        /// retirar el cursor.
        /// </summary>
        private AudioPreviewPlayer CreatePlayer()
        {
            AudioPreviewPlayer player = new(_ffmpegPath);
            player.PlaybackCompleted += (_, _) => StopPlayback();
            return player;
        }

        /// <summary>Detiene lo que suene, retira el cursor y devuelve el botón a su texto.</summary>
        private void StopPlayback()
        {
            _playRequest++;
            _player?.Stop();
            playheadTimer.Stop();

            foreach (WaveformView view in Views)
            {
                view.PlayheadSeconds = null;
            }

            if (_playingItem is { } item)
            {
                SetText(item, _playingItemText);
                _playingItem = null;
            }
        }

        /// <summary>Lleva a las vistas el instante que está sonando, según el propio dispositivo.</summary>
        private void playheadTimer_Tick(object? sender, EventArgs e)
        {
            double? position = _player?.PositionSeconds;
            foreach (WaveformView view in Views)
            {
                view.PlayheadSeconds = position;
            }
        }

        private static string TextOf(object item) => item switch
        {
            ToolStripItem toolItem => toolItem.Text ?? string.Empty,
            Control control => control.Text,
            _ => string.Empty,
        };

        private static void SetText(object item, string text)
        {
            switch (item)
            {
                case ToolStripItem toolItem:
                    toolItem.Text = text;
                    break;

                case Control control:
                    control.Text = text;
                    break;
            }
        }
    }
}
