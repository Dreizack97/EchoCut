using EchoCut.Audio;
using EchoCut.Controls;
using EchoCut.Library;
using EchoCut.Playback;
using EchoCut.Processing;
using EchoCut.Waveforms;

namespace EchoCut
{
    /// <summary>
    /// Ventana que muestra la forma de onda de una pista —completa y en detalle sus dos bordes— con
    /// el recorte propuesto superpuesto, y permite corregirlo a mano.
    /// </summary>
    /// <remarks>
    /// Solo devuelve una decisión: el tramo ajustado en <see cref="ManualRange"/>. Aplicarlo a la
    /// fila es cosa de quien la abre, igual que con el diálogo de propiedades.
    /// </remarks>
    public partial class WaveformEditor : Form
    {
        /// <summary>Duración mínima de cada vista de detalle: lo bastante para ver el silencio y la música que lo rodea.</summary>
        private const double MinimumEdgeWindowSeconds = 10.0;

        /// <summary>Música que se muestra más allá del corte en cada vista de detalle.</summary>
        private const double EdgeContextSeconds = 5.0;

        /// <summary>Texto del botón de escucha mientras su tramo suena.</summary>
        private const string StopPlaybackText = "⏹ Detener";

        private readonly TrackInfo _track;
        private readonly double _durationSeconds;
        private readonly TrimRange? _analysisRange;
        private readonly WaveformService _service;
        private readonly string _ffmpegPath;
        private readonly double _previewSeconds;

        /// <summary>
        /// Cancela la carga y la reproducción al cerrar. No se libera: los procesos de FFmpeg que lo
        /// observan pueden seguir terminando tras el cierre, y liberarlo antes los haría fallar al
        /// registrarse en vez de cancelarse.
        /// </summary>
        private readonly CancellationTokenSource _closing = new();

        private readonly List<Waveform> _waveforms = [];
        private AudioPreviewPlayer? _player;
        private Button? _playingButton;
        private string _playingButtonText = string.Empty;
        private int _playRequest;

        private double _startSeconds;
        private double _endSeconds;
        private bool _manual;
        private bool _syncing;

        /// <summary>Prepara la ventana para una pista.</summary>
        /// <param name="track">Pista a mostrar.</param>
        /// <param name="durationSeconds">Duración con la que se calcularon los cortes.</param>
        /// <param name="current">Tramo vigente: el ajuste manual o el del análisis.</param>
        /// <param name="analysisRange">Tramo del análisis, o <c>null</c> si la pista no se ha analizado.</param>
        /// <param name="isManual">Si <paramref name="current"/> es un ajuste manual previo.</param>
        /// <param name="service">Servicio que carga las formas de onda.</param>
        /// <param name="ffmpegPath">Ruta de FFmpeg para escuchar los bordes.</param>
        /// <param name="previewSeconds">Segundos que se escuchan de cada borde.</param>
        public WaveformEditor(
            TrackInfo track,
            double durationSeconds,
            TrimRange current,
            TrimRange? analysisRange,
            bool isManual,
            WaveformService service,
            string ffmpegPath,
            double previewSeconds)
        {
            InitializeComponent();

            _track = track;
            _durationSeconds = durationSeconds;
            _analysisRange = analysisRange;
            _service = service;
            _ffmpegPath = ffmpegPath;
            _previewSeconds = previewSeconds;
            _manual = isManual;

            lblTrack.Text = track.Name;
            btnReset.Text = analysisRange is null ? "Quitar ajuste" : "Restablecer análisis";
            numStart.Maximum = (decimal)durationSeconds;
            numEnd.Maximum = (decimal)durationSeconds;

            // Cada vista de detalle abarca su silencio y unos segundos de música, para juzgar el corte
            // en contexto aunque la pista arrastre silencios largos.
            double headSeconds = Math.Min(durationSeconds, Math.Max(MinimumEdgeWindowSeconds, current.StartSeconds + EdgeContextSeconds));
            double tailSeconds = Math.Min(durationSeconds, Math.Max(MinimumEdgeWindowSeconds, durationSeconds - current.EndSeconds + EdgeContextSeconds));

            viewOverview.SetView(durationSeconds, 0.0, durationSeconds);
            viewStart.SetView(durationSeconds, 0.0, headSeconds);
            viewEnd.SetView(durationSeconds, durationSeconds - tailSeconds, durationSeconds);

            foreach (WaveformView view in Views)
            {
                view.StatusText = "Calculando forma de onda…";
            }

            ApplyRange(current.StartSeconds, current.EndSeconds);
        }

        /// <summary>Tramo elegido a mano al aceptar.</summary>
        /// <value>
        /// El tramo ajustado, o <c>null</c> si al aceptar mandaba el análisis, ya fuera porque no se
        /// movió nada o porque se restableció.
        /// </value>
        public TrimRange? ManualRange { get; private set; }

        private IEnumerable<WaveformView> Views => [viewOverview, viewStart, viewEnd];

        private async void WaveformEditor_Shown(object? sender, EventArgs e)
        {
            CancellationToken token = _closing.Token;

            // La pista completa alimenta la vista general y el detalle del principio; el final se
            // carga aparte porque debe situarse con la misma referencia que el corte final, y llega
            // mucho antes que la pista entera.
            await Task.WhenAll(
                LoadAsync(_service.LoadAsync(_track.FilePath, token), viewOverview, viewStart),
                LoadAsync(_service.LoadTailAsync(_track.FilePath, _durationSeconds - viewEnd.ViewStartSeconds, _durationSeconds, token), viewEnd))
                .ConfigureAwait(true);
        }

        /// <summary>Espera una forma de onda y la muestra en las vistas indicadas, o explica por qué no se pudo.</summary>
        /// <remarks>
        /// Nunca lanza: es el cuerpo de un manejador <c>async void</c>. Si el resultado llega con la
        /// ventana ya cerrada se libera aquí mismo, porque nadie más lo va a reclamar.
        /// </remarks>
        private async Task LoadAsync(Task<Waveform> pending, params WaveformView[] views)
        {
            try
            {
                Waveform waveform = await pending.ConfigureAwait(true);
                if (IsDisposed || _closing.IsCancellationRequested)
                {
                    waveform.Dispose();
                    return;
                }

                _waveforms.Add(waveform);
                foreach (WaveformView view in views)
                {
                    view.Waveform = waveform;
                }
            }
            catch (OperationCanceledException)
            {
                // La ventana se cerró mientras se cargaba.
            }
            catch (Exception exception)
            {
                if (!IsDisposed)
                {
                    foreach (WaveformView view in views)
                    {
                        view.StatusText = $"No se pudo cargar la forma de onda: {exception.Message}";
                    }
                }
            }
        }

        private void View_MarkersChanged(object? sender, EventArgs e)
        {
            if (sender is WaveformView view)
            {
                _manual = true;
                ApplyRange(view.StartMarkerSeconds, view.EndMarkerSeconds);
            }
        }

        private void numStart_ValueChanged(object? sender, EventArgs e)
        {
            if (_syncing)
            {
                return;
            }

            _manual = true;
            ApplyRange(Math.Min((double)numStart.Value, _endSeconds - WaveformView.MinimumGapSeconds), _endSeconds);
        }

        private void numEnd_ValueChanged(object? sender, EventArgs e)
        {
            if (_syncing)
            {
                return;
            }

            _manual = true;
            ApplyRange(_startSeconds, Math.Max((double)numEnd.Value, _startSeconds + WaveformView.MinimumGapSeconds));
        }

        private void chkDecibels_CheckedChanged(object? sender, EventArgs e)
        {
            AmplitudeScale scale = chkDecibels.Checked ? AmplitudeScale.Decibels : AmplitudeScale.Linear;
            foreach (WaveformView view in Views)
            {
                view.AmplitudeScale = scale;
            }
        }

        private void btnReset_Click(object? sender, EventArgs e)
        {
            TrimRange range = _analysisRange ?? new TrimRange(0.0, _durationSeconds);
            _manual = false;
            ApplyRange(range.StartSeconds, range.EndSeconds);
        }

        private void btnAccept_Click(object? sender, EventArgs e) =>
            ManualRange = _manual ? new TrimRange(_startSeconds, _endSeconds) : null;

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
        /// Reproduce un borde tal como sonaría en la copia recortada, o lo detiene si es el que ya
        /// suena. Mientras suena, el botón ofrece detenerlo y las vistas muestran el cursor.
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
                await _player.PlayAsync(_track.FilePath, window, _closing.Token).ConfigureAwait(true);

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
        /// Crea el reproductor en el hilo de la interfaz, donde avisará del final del tramo para
        /// devolver el botón y retirar el cursor.
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

        /// <summary>Lleva el tramo a todas las vistas, a los campos numéricos y al resumen.</summary>
        private void ApplyRange(double startSeconds, double endSeconds)
        {
            _startSeconds = Math.Clamp(startSeconds, 0.0, _durationSeconds);
            _endSeconds = Math.Clamp(endSeconds, _startSeconds, _durationSeconds);

            _syncing = true;
            try
            {
                foreach (WaveformView view in Views)
                {
                    view.SetMarkers(_startSeconds, _endSeconds);
                }

                numStart.Value = Math.Clamp((decimal)Math.Round(_startSeconds, 3), numStart.Minimum, numStart.Maximum);
                numEnd.Value = Math.Clamp((decimal)Math.Round(_endSeconds, 3), numEnd.Minimum, numEnd.Maximum);
            }
            finally
            {
                _syncing = false;
            }

            UpdateSummary();
        }

        private void UpdateSummary()
        {
            double removedStart = _startSeconds;
            double removedEnd = _durationSeconds - _endSeconds;
            string origin = _manual ? "Ajuste manual." : _analysisRange is null ? "Sin análisis." : "Según el análisis.";

            lblSummary.Text = removedStart + removedEnd < 0.0005
                ? $"No se eliminará nada: la pista se conserva completa. {origin}"
                : $"Se eliminarán {removedStart:0.00} s al inicio y {removedEnd:0.00} s al final ({removedStart + removedEnd:0.00} s en total). {origin}";
        }

        private void WaveformEditor_FormClosing(object? sender, FormClosingEventArgs e)
        {
            _closing.Cancel();
            StopPlayback();
        }

        private void WaveformEditor_FormClosed(object? sender, FormClosedEventArgs e)
        {
            foreach (WaveformView view in Views)
            {
                view.Waveform = null;
            }

            foreach (Waveform waveform in _waveforms)
            {
                waveform.Dispose();
            }

            _waveforms.Clear();
            _player?.Dispose();
        }
    }
}
