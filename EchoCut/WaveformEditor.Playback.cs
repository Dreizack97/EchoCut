using EchoCut.Audio;
using EchoCut.Controls;
using EchoCut.Playback;

namespace EchoCut
{
    /// <summary>
    /// La parte de <see cref="WaveformEditor"/> que escucha: los bordes y la selección, tal como
    /// sonarán en la copia, con el cursor de reproducción sobre las vistas.
    /// </summary>
    /// <remarks>
    /// Lo que suena pasa por las mismas ediciones que la copia —fundidos y borrados—, así que se
    /// juzga de oído exactamente lo que se va a guardar.
    /// </remarks>
    public partial class WaveformEditor
    {
        /// <summary>Texto del botón de escucha mientras su tramo suena.</summary>
        private const string StopPlaybackText = "⏹ Detener";

        /// <summary>
        /// Lo más que se escucha de una selección. El reproductor retiene el tramo en memoria, y
        /// medio minuto basta para juzgar un fundido o un empalme sin cargar una canción entera.
        /// </summary>
        private const double MaximumSelectionPlaybackSeconds = 30.0;

        private AudioPreviewPlayer? _player;
        private Button? _playingButton;
        private string _playingButtonText = string.Empty;
        private int _playRequest;

        private async void btnPlayStart_Click(object? sender, EventArgs e) =>
            await TogglePlaybackAsync(
                btnPlayStart,
                new PreviewWindow(_startSeconds, Math.Min(_previewSeconds, _endSeconds - _startSeconds))).ConfigureAwait(true);

        private async void btnPlayEnd_Click(object? sender, EventArgs e)
        {
            double from = Math.Max(_startSeconds, _endSeconds - _previewSeconds);
            await TogglePlaybackAsync(btnPlayEnd, new PreviewWindow(from, _endSeconds - from)).ConfigureAwait(true);
        }

        private async void btnSelPlay_Click(object? sender, EventArgs e)
        {
            if (_selection is { } selection)
            {
                PreviewWindow window = new(selection.StartSeconds, Math.Min(selection.DurationSeconds, MaximumSelectionPlaybackSeconds));
                await TogglePlaybackAsync(btnSelPlay, window).ConfigureAwait(true);
            }
        }

        /// <summary>
        /// Reproduce un tramo tal como sonaría en la copia, o lo detiene si es el que ya suena.
        /// Mientras suena, el botón ofrece detenerlo y las vistas muestran el cursor.
        /// </summary>
        /// <remarks>Nunca lanza: es el cuerpo de manejadores <c>async void</c>.</remarks>
        private async Task TogglePlaybackAsync(Button button, PreviewWindow window)
        {
            bool stopRequested = ReferenceEquals(button, _playingButton);
            StopPlayback();
            if (stopRequested)
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

                _playingButton = button;
                _playingButtonText = button.Text;
                button.Text = StopPlaybackText;
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

            if (_playingButton is { } button)
            {
                button.Text = _playingButtonText;
                _playingButton = null;
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
    }
}
