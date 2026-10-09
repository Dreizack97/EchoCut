using EchoCut.Audio;
using EchoCut.Controls;
using EchoCut.Objects;
using EchoCut.Processing;
using EchoCut.Waveforms;
using System.ComponentModel;

namespace EchoCut
{
    /// <summary>
    /// Ventana que muestra la forma de onda de una pista —completa y en detalle sus dos bordes— con
    /// el recorte propuesto superpuesto, y permite corregirlo a mano, añadir fundidos y borrar
    /// fragmentos.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Es una ventana sin modo: se pueden tener varias abiertas, una por pista, y seguir usando la
    /// ventana principal. Por eso no toca la fila por su cuenta: avisa con <see cref="Applied"/> y
    /// expone sus decisiones en <see cref="ManualRange"/> y <see cref="Edits"/>, y quien la abre las
    /// aplica, igual que con el diálogo de propiedades.
    /// </para>
    /// <para>
    /// Las ediciones siguen el modelo de Audacity: se selecciona un tramo en cualquier vista y
    /// después se actúa sobre él (aparición, desaparición, borrar o restaurar). Los campos numéricos
    /// de los fundidos ofrecen lo mismo desde el teclado.
    /// </para>
    /// </remarks>
    public partial class WaveformEditor : Form
    {
        /// <summary>Duración mínima de cada vista de detalle: lo bastante para ver el silencio y la música que lo rodea.</summary>
        private const double MinimumEdgeWindowSeconds = 10.0;

        /// <summary>Música que se muestra más allá del corte en cada vista de detalle.</summary>
        private const double EdgeContextSeconds = 5.0;

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

        private double _startSeconds;
        private double _endSeconds;
        private bool _manual;
        private bool _syncing;

        /// <summary>Si hay cambios que todavía no se llevaron a la fila.</summary>
        private bool _dirty;

        /// <summary>Prepara la ventana para una pista, partiendo de lo que ya tenga decidido su fila.</summary>
        /// <param name="song">Fila de la pista a mostrar.</param>
        /// <param name="service">Servicio que carga las formas de onda.</param>
        /// <param name="ffmpegPath">Ruta de FFmpeg para escuchar.</param>
        /// <param name="previewSeconds">Segundos que se escuchan de cada borde.</param>
        public WaveformEditor(
            Song song,
            WaveformService service,
            string ffmpegPath,
            double previewSeconds)
        {
            ArgumentNullException.ThrowIfNull(song);
            InitializeComponent();

            Song = song;
            _durationSeconds = song.DurationSeconds;
            _analysisRange = song.Analysis?.Range;
            _service = service;
            _ffmpegPath = ffmpegPath;
            _previewSeconds = previewSeconds;
            _manual = song.ManualRange is not null;

            TrimRange current = song.TrimRange ?? new TrimRange(0.0, _durationSeconds);
            _fadeIn = song.Edits?.Fades?.FadeIn;
            _fadeOut = song.Edits?.Fades?.FadeOut;
            _deletions = song.Edits?.Deletions ?? DeletedRegions.Empty;

            ShowTrackName();
            song.PropertyChanged += Song_PropertyChanged;
            btnReset.Text = _analysisRange is null ? "Quitar ajuste" : "Restablecer análisis";

            foreach (NumericUpDown field in (NumericUpDown[])[numStart, numEnd, numFadeInStart, numFadeInEnd, numFadeOutStart, numFadeOutEnd])
            {
                field.Maximum = (decimal)_durationSeconds;
            }

            foreach (ComboBox combo in (ComboBox[])[cmbFadeInCurve, cmbFadeOutCurve])
            {
                combo.Items.AddRange([.. FadeText.Curves.Select(FadeText.Name)]);
            }

            cmbFadeInCurve.SelectedIndex = CurveIndex(_fadeIn?.Curve ?? FadeCurve.Linear);
            cmbFadeOutCurve.SelectedIndex = CurveIndex(_fadeOut?.Curve ?? FadeCurve.Linear);

            // Cada vista de detalle abarca su silencio y unos segundos de música, para juzgar el corte
            // en contexto aunque la pista arrastre silencios largos, y el fundido que ya tuviera.
            double headSeconds = Math.Min(_durationSeconds, Math.Max(MinimumEdgeWindowSeconds, Math.Max(current.StartSeconds, _fadeIn?.EndSeconds ?? 0.0) + EdgeContextSeconds));
            double tailSeconds = Math.Min(_durationSeconds, Math.Max(MinimumEdgeWindowSeconds, _durationSeconds - Math.Min(current.EndSeconds, _fadeOut?.StartSeconds ?? _durationSeconds) + EdgeContextSeconds));

            viewOverview.SetView(_durationSeconds, 0.0, _durationSeconds);
            viewStart.SetView(_durationSeconds, 0.0, headSeconds);
            viewEnd.SetView(_durationSeconds, _durationSeconds - tailSeconds, _durationSeconds);

            foreach (WaveformView view in Views)
            {
                view.StatusText = "Calculando forma de onda…";
            }

            ApplyRange(current.StartSeconds, current.EndSeconds);
            ApplyFades();
            ApplyDeletions();
            ApplySelection(null);
            _dirty = false;
        }

        /// <summary>Se produce cuando el usuario pide llevar sus decisiones a la fila, al aceptar.</summary>
        public event EventHandler? Applied;

        /// <value>Fila de la pista que se edita.</value>
        public Song Song { get; }

        /// <summary>Tramo elegido a mano.</summary>
        /// <value>
        /// El tramo ajustado, o <c>null</c> si manda el análisis, ya sea porque no se movió nada o
        /// porque se restableció.
        /// </value>
        public TrimRange? ManualRange => _manual ? new TrimRange(_startSeconds, _endSeconds) : null;

        /// <summary>Fundidos y borrados elegidos.</summary>
        /// <value>Las ediciones de la pista, o <c>null</c> si no lleva ninguna.</value>
        public AudioEdits? Edits => AudioEdits.From(CurrentFades, _deletions);

        private IEnumerable<WaveformView> Views => [viewOverview, viewStart, viewEnd];

        /// <summary>Tramo que conservará la copia; nunca vacío, aunque las marcas lleguen a tocarse.</summary>
        private TrimRange KeptRange => new(_startSeconds, Math.Max(_endSeconds, _startSeconds + WaveformView.MinimumGapSeconds));

        /// <summary>Cierra la ventana descartando los cambios sin preguntar.</summary>
        /// <remarks>Para cuando la fila deja de existir: preguntar si aplicar a algo que ya no está no tiene sentido.</remarks>
        public void Discard()
        {
            _dirty = false;
            Close();
        }

        /// <inheritdoc/>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // Supr y Esc actúan sobre la selección salvo mientras se escribe en un campo, donde
            // conservan su significado de siempre.
            if (ActiveControl is not (NumericUpDown or TextBoxBase or ComboBox))
            {
                if (keyData == Keys.Delete && btnSelDelete.Enabled)
                {
                    DeleteSelection();
                    return true;
                }

                if (keyData == Keys.Escape && _selection is not null)
                {
                    ApplySelection(null);
                    return true;
                }
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private async void WaveformEditor_Shown(object? sender, EventArgs e)
        {
            CancellationToken token = _closing.Token;
            string filePath = Song.FilePath;

            // La pista completa alimenta la vista general y el detalle del principio; el final se
            // carga aparte porque debe situarse con la misma referencia que el corte final, y llega
            // mucho antes que la pista entera.
            await Task.WhenAll(
                LoadAsync(_service.LoadAsync(filePath, token), viewOverview, viewStart),
                LoadAsync(_service.LoadTailAsync(filePath, _durationSeconds - viewEnd.ViewStartSeconds, _durationSeconds, token), viewEnd))
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

        /// <summary>El nombre de la pista va también en el título: con varias ventanas abiertas, es lo que las distingue en la barra de tareas.</summary>
        private void ShowTrackName()
        {
            lblTrack.Text = Song.Name;
            Text = $"{Song.Name} — Forma de onda, recorte y fundidos";
        }

        /// <summary>La fila puede renombrarse mientras la ventana sigue abierta.</summary>
        private void Song_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Song.Name) && !IsDisposed)
            {
                ShowTrackName();
            }
        }

        private void View_MarkersChanged(object? sender, EventArgs e)
        {
            if (sender is WaveformView view)
            {
                _manual = true;
                ApplyRange(view.StartMarkerSeconds, view.EndMarkerSeconds);
                MarkDirty();
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
            MarkDirty();
        }

        private void numEnd_ValueChanged(object? sender, EventArgs e)
        {
            if (_syncing)
            {
                return;
            }

            _manual = true;
            ApplyRange(_startSeconds, Math.Max((double)numEnd.Value, _startSeconds + WaveformView.MinimumGapSeconds));
            MarkDirty();
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
            MarkDirty();
        }

        private void btnAccept_Click(object? sender, EventArgs e)
        {
            Apply();
            Close();
        }

        private void btnCancel_Click(object? sender, EventArgs e) => Close();

        /// <summary>Avisa a quien abrió la ventana para que lleve las decisiones a la fila.</summary>
        internal void Apply()
        {
            Applied?.Invoke(this, EventArgs.Empty);
            _dirty = false;
        }

        /// <summary>Anota que hay cambios sin llevar a la fila.</summary>
        private void MarkDirty()
        {
            _dirty = true;
            UpdateSummary();
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
                ? $"No se recorta nada en los bordes. {origin}"
                : $"Se eliminarán {removedStart:0.00} s al inicio y {removedEnd:0.00} s al final. {origin}";

            // Dos líneas: el recorte con lo borrado, y los fundidos con su consecuencia.
            List<string> lines = [trim + DescribeDeletions(), string.Join(" ", DescribeEdits())];
            lblSummary.Text = string.Join(Environment.NewLine, lines.Where(line => line.Length > 0));
        }

        /// <summary>Si hay cambios sin aplicar, pregunta qué hacer con ellos antes de cerrar.</summary>
        private void WaveformEditor_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (_dirty)
            {
                DialogResult answer = MessageBox.Show(
                    this,
                    $"¿Aplicar a «{Song.Name}» los cambios hechos en esta ventana?",
                    "Cambios sin aplicar",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                if (answer == DialogResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }

                if (answer == DialogResult.Yes)
                {
                    Apply();
                }
            }

            _closing.Cancel();
            StopPlayback();
        }

        private void WaveformEditor_FormClosed(object? sender, FormClosedEventArgs e)
        {
            Song.PropertyChanged -= Song_PropertyChanged;

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
