using EchoCut.Audio;
using EchoCut.Controls;
using EchoCut.Library;
using EchoCut.Objects;
using EchoCut.Playback;
using EchoCut.Processing;
using EchoCut.Waveforms;

namespace EchoCut
{
    /// <summary>
    /// Ventana que muestra la forma de onda de una pista —completa y en detalle sus dos bordes— con
    /// el recorte propuesto superpuesto, y permite corregirlo a mano y añadir fundidos.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Solo devuelve decisiones: el tramo ajustado en <see cref="ManualRange"/> y los fundidos en
    /// <see cref="Fades"/>. Aplicarlos a la fila es cosa de quien la abre, igual que con el diálogo de
    /// propiedades.
    /// </para>
    /// <para>
    /// Los fundidos se eligen como en Audacity: arrastrando sobre el detalle del inicio se selecciona
    /// el tramo de la aparición, y sobre el del final, el de la desaparición. Los campos numéricos
    /// ofrecen lo mismo desde el teclado.
    /// </para>
    /// </remarks>
    public partial class WaveformEditor : Form
    {
        /// <summary>Duración mínima de cada vista de detalle: lo bastante para ver el silencio y la música que lo rodea.</summary>
        private const double MinimumEdgeWindowSeconds = 10.0;

        /// <summary>Música que se muestra más allá del corte en cada vista de detalle.</summary>
        private const double EdgeContextSeconds = 5.0;

        /// <summary>Texto del botón de escucha mientras su tramo suena.</summary>
        private const string StopPlaybackText = "⏹ Detener";

        /// <summary>
        /// Aparición que se propone al activarla sin haber seleccionado tramo: un segundo, como la
        /// macro «Fade Ends» de Audacity.
        /// </summary>
        private const double DefaultFadeInSeconds = 1.0;

        /// <summary>
        /// Desaparición que se propone al activarla sin haber seleccionado tramo: más larga que la
        /// aparición, porque un final que se apaga en un segundo suena cortado.
        /// </summary>
        private const double DefaultFadeOutSeconds = 3.0;

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
        private Fade? _fadeIn;
        private Fade? _fadeOut;

        /// <summary>Prepara la ventana para una pista.</summary>
        /// <param name="track">Pista a mostrar.</param>
        /// <param name="durationSeconds">Duración con la que se calcularon los cortes.</param>
        /// <param name="current">Tramo vigente: el ajuste manual o el del análisis.</param>
        /// <param name="analysisRange">Tramo del análisis, o <c>null</c> si la pista no se ha analizado.</param>
        /// <param name="isManual">Si <paramref name="current"/> es un ajuste manual previo.</param>
        /// <param name="fades">Fundidos elegidos antes para esta pista, o <c>null</c> si no tiene.</param>
        /// <param name="service">Servicio que carga las formas de onda.</param>
        /// <param name="ffmpegPath">Ruta de FFmpeg para escuchar los bordes.</param>
        /// <param name="previewSeconds">Segundos que se escuchan de cada borde.</param>
        public WaveformEditor(
            TrackInfo track,
            double durationSeconds,
            TrimRange current,
            TrimRange? analysisRange,
            bool isManual,
            TrackFades? fades,
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
            _fadeIn = fades?.FadeIn;
            _fadeOut = fades?.FadeOut;

            foreach (NumericUpDown field in (NumericUpDown[])[numStart, numEnd, numFadeInStart, numFadeInEnd, numFadeOutStart, numFadeOutEnd])
            {
                field.Maximum = (decimal)durationSeconds;
            }

            foreach (ComboBox combo in (ComboBox[])[cmbFadeInCurve, cmbFadeOutCurve])
            {
                combo.Items.AddRange([.. FadeText.Curves.Select(FadeText.Name)]);
            }

            cmbFadeInCurve.SelectedIndex = CurveIndex(_fadeIn?.Curve ?? FadeCurve.Linear);
            cmbFadeOutCurve.SelectedIndex = CurveIndex(_fadeOut?.Curve ?? FadeCurve.Linear);
            viewStart.SelectionTarget = FadeDirection.In;
            viewEnd.SelectionTarget = FadeDirection.Out;

            // Cada vista de detalle abarca su silencio y unos segundos de música, para juzgar el corte
            // en contexto aunque la pista arrastre silencios largos, y el fundido que ya tuviera.
            double headSeconds = Math.Min(durationSeconds, Math.Max(MinimumEdgeWindowSeconds, Math.Max(current.StartSeconds, _fadeIn?.EndSeconds ?? 0.0) + EdgeContextSeconds));
            double tailSeconds = Math.Min(durationSeconds, Math.Max(MinimumEdgeWindowSeconds, durationSeconds - Math.Min(current.EndSeconds, _fadeOut?.StartSeconds ?? durationSeconds) + EdgeContextSeconds));

            viewOverview.SetView(durationSeconds, 0.0, durationSeconds);
            viewStart.SetView(durationSeconds, 0.0, headSeconds);
            viewEnd.SetView(durationSeconds, durationSeconds - tailSeconds, durationSeconds);

            foreach (WaveformView view in Views)
            {
                view.StatusText = "Calculando forma de onda…";
            }

            ApplyRange(current.StartSeconds, current.EndSeconds);
            ApplyFades();
        }

        /// <summary>Tramo elegido a mano al aceptar.</summary>
        /// <value>
        /// El tramo ajustado, o <c>null</c> si al aceptar mandaba el análisis, ya fuera porque no se
        /// movió nada o porque se restableció.
        /// </value>
        public TrimRange? ManualRange { get; private set; }

        /// <summary>Fundidos elegidos al aceptar.</summary>
        /// <value>Los fundidos de la pista, o <c>null</c> si no lleva ninguno.</value>
        public TrackFades? Fades { get; private set; }

        private IEnumerable<WaveformView> Views => [viewOverview, viewStart, viewEnd];

        /// <summary>Fundidos vigentes, tal como se dibujan, se escuchan y se devolverán.</summary>
        private TrackFades? CurrentFades => _fadeIn is null && _fadeOut is null ? null : new TrackFades(_fadeIn, _fadeOut);

        private static int CurveIndex(FadeCurve curve) => Math.Max(0, FadeText.Curves.ToList().IndexOf(curve));

        private static FadeCurve CurveAt(ComboBox combo) =>
            FadeText.Curves[Math.Clamp(combo.SelectedIndex, 0, FadeText.Curves.Count - 1)];

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

        private void btnAccept_Click(object? sender, EventArgs e)
        {
            ManualRange = _manual ? new TrimRange(_startSeconds, _endSeconds) : null;
            Fades = CurrentFades;
        }

        /// <summary>Convierte en fundido el tramo que el usuario arrastra sobre una vista de detalle.</summary>
        /// <remarks>
        /// Un tramo más corto que <see cref="Fade.MinimumSeconds"/> no se aplica: suele ser el
        /// principio de un arrastre, y aplicarlo haría parpadear el fundido anterior.
        /// </remarks>
        private void View_FadeSelected(object? sender, FadeSelectionEventArgs e)
        {
            if (e.EndSeconds - e.StartSeconds < Fade.MinimumSeconds)
            {
                return;
            }

            SetFade(e.Direction, e.StartSeconds, e.EndSeconds);
        }

        private void chkFadeIn_CheckedChanged(object? sender, EventArgs e)
        {
            if (_syncing)
            {
                return;
            }

            if (chkFadeIn.Checked)
            {
                SetFade(FadeDirection.In, _startSeconds, Math.Min(_endSeconds, _startSeconds + DefaultFadeInSeconds));
            }
            else
            {
                _fadeIn = null;
                ApplyFades();
            }
        }

        private void chkFadeOut_CheckedChanged(object? sender, EventArgs e)
        {
            if (_syncing)
            {
                return;
            }

            if (chkFadeOut.Checked)
            {
                SetFade(FadeDirection.Out, Math.Max(_startSeconds, _endSeconds - DefaultFadeOutSeconds), _endSeconds);
            }
            else
            {
                _fadeOut = null;
                ApplyFades();
            }
        }

        private void numFadeIn_ValueChanged(object? sender, EventArgs e)
        {
            if (!_syncing)
            {
                SetFade(FadeDirection.In, (double)numFadeInStart.Value, (double)numFadeInEnd.Value, keepStart: sender == numFadeInStart);
            }
        }

        private void numFadeOut_ValueChanged(object? sender, EventArgs e)
        {
            if (!_syncing)
            {
                SetFade(FadeDirection.Out, (double)numFadeOutStart.Value, (double)numFadeOutEnd.Value, keepStart: sender == numFadeOutStart);
            }
        }

        private void cmbFadeInCurve_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (!_syncing && _fadeIn is { } fade)
            {
                _fadeIn = fade with { Curve = CurveAt(cmbFadeInCurve) };
                ApplyFades();
            }
        }

        private void cmbFadeOutCurve_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (!_syncing && _fadeOut is { } fade)
            {
                _fadeOut = fade with { Curve = CurveAt(cmbFadeOutCurve) };
                ApplyFades();
            }
        }

        /// <summary>Fija el tramo de un fundido con la curva elegida en su lista y lo lleva a toda la ventana.</summary>
        /// <param name="direction">Fundido a fijar.</param>
        /// <param name="startSeconds">Comienzo pedido.</param>
        /// <param name="endSeconds">Final pedido.</param>
        /// <param name="keepStart">
        /// Si el tramo es demasiado corto, qué extremo se respeta: el que acaba de tocar el usuario.
        /// </param>
        private void SetFade(FadeDirection direction, double startSeconds, double endSeconds, bool keepStart = true)
        {
            double start = Math.Clamp(startSeconds, 0.0, _durationSeconds);
            double end = Math.Clamp(endSeconds, 0.0, _durationSeconds);

            if (end - start < Fade.MinimumSeconds)
            {
                if (keepStart)
                {
                    end = Math.Min(_durationSeconds, start + Fade.MinimumSeconds);
                    start = end - Fade.MinimumSeconds;
                }
                else
                {
                    start = Math.Max(0.0, end - Fade.MinimumSeconds);
                    end = start + Fade.MinimumSeconds;
                }
            }

            if (direction == FadeDirection.In)
            {
                _fadeIn = new Fade(direction, start, end, CurveAt(cmbFadeInCurve));
            }
            else
            {
                _fadeOut = new Fade(direction, start, end, CurveAt(cmbFadeOutCurve));
            }

            ApplyFades();
        }

        /// <summary>Lleva los fundidos a las vistas, a sus controles y al resumen.</summary>
        private void ApplyFades()
        {
            TrackFades? fades = CurrentFades;
            foreach (WaveformView view in Views)
            {
                view.Fades = fades;
            }

            _syncing = true;
            try
            {
                SyncFadeControls(_fadeIn, chkFadeIn, numFadeInStart, numFadeInEnd);
                SyncFadeControls(_fadeOut, chkFadeOut, numFadeOutStart, numFadeOutEnd);
            }
            finally
            {
                _syncing = false;
            }

            UpdateSummary();
        }

        /// <summary>
        /// Refleja un fundido en su fila de controles. Sin fundido, los campos se desactivan pero la
        /// curva sigue disponible, para elegirla antes de seleccionar el tramo.
        /// </summary>
        private static void SyncFadeControls(Fade? fade, CheckBox check, NumericUpDown start, NumericUpDown end)
        {
            check.Checked = fade is not null;
            start.Enabled = fade is not null;
            end.Enabled = fade is not null;

            if (fade is { } value)
            {
                start.Value = Math.Clamp((decimal)Math.Round(value.StartSeconds, 3), start.Minimum, start.Maximum);
                end.Value = Math.Clamp((decimal)Math.Round(value.EndSeconds, 3), end.Minimum, end.Maximum);
            }
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
                await _player.PlayAsync(_track.FilePath, window, CurrentFades, _closing.Token).ConfigureAwait(true);

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

            string trim = removedStart + removedEnd < 0.0005
                ? $"No se eliminará nada: la pista se conserva completa. {origin}"
                : $"Se eliminarán {removedStart:0.00} s al inicio y {removedEnd:0.00} s al final ({removedStart + removedEnd:0.00} s en total). {origin}";

            lblSummary.Text = trim + DescribeFades();
        }

        /// <summary>
        /// Resume los fundidos y avisa de lo que implican: la copia se recodifica, y un fundido que el
        /// recorte deja fuera no tendrá efecto.
        /// </summary>
        private string DescribeFades()
        {
            if (CurrentFades is not { } fades)
            {
                return string.Empty;
            }

            TrimRange kept = new(_startSeconds, Math.Max(_endSeconds, _startSeconds + WaveformView.MinimumGapSeconds));
            List<string> parts = [];
            foreach (Fade? candidate in (Fade?[])[fades.FadeIn, fades.FadeOut])
            {
                if (candidate is { } fade)
                {
                    string outside = fade.Overlaps(kept) ? string.Empty : ", fuera de la copia";
                    parts.Add($"{FadeText.Name(fade.Direction).ToLowerInvariant()} de {fade.DurationSeconds:0.00} s ({FadeText.Name(fade.Curve).ToLowerInvariant()}{outside})");
                }
            }

            string encoding = fades.Within(kept) is null
                ? "no afectan a la copia, que se escribirá sin recodificar"
                : "la copia se volverá a codificar";
            return $"{Environment.NewLine}Fundidos: {string.Join(", ", parts)}; {encoding}.";
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
