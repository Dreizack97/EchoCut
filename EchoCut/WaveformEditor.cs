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
    /// Sigue el estilo de la ventana principal: una barra de herramientas con las acciones, una barra
    /// de estado que describe cada opción y resume el resultado, y atajos anunciados en cada tooltip
    /// y reunidos en «Atajos» (F1). A la derecha, un inspector muestra la copia y los detalles de lo
    /// que se haya seleccionado: la selección, un fundido o un fragmento borrado.
    /// </para>
    /// <para>
    /// Es una ventana sin modo: se pueden tener varias abiertas, una por pista, y seguir usando la
    /// ventana principal. Por eso no toca la fila por su cuenta: avisa con <see cref="Applied"/> y
    /// expone sus decisiones en <see cref="ManualRange"/> y <see cref="Edits"/>, y quien la abre las
    /// aplica, igual que con el diálogo de propiedades.
    /// </para>
    /// <para>
    /// El resto vive en archivos parciales: la edición y el inspector, el historial de deshacer, la
    /// escucha, el zoom y la ayuda.
    /// </para>
    /// </remarks>
    public partial class WaveformEditor : Form
    {
        /// <summary>Duración mínima de cada vista de detalle: lo bastante para ver el silencio y la música que lo rodea.</summary>
        private const double MinimumEdgeWindowSeconds = 10.0;

        /// <summary>Música que se muestra más allá del corte en cada vista de detalle.</summary>
        private const double EdgeContextSeconds = 5.0;

        private readonly double _durationSeconds;
        private readonly WaveformEditorServices _services;
        private readonly Func<WaveformEditor, Task<string?>> _save;

        /// <summary>Tramo del análisis que tenía la fila al abrir la ventana, o <c>null</c> si no estaba analizada.</summary>
        private readonly TrimRange? _initialAnalysisRange;

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

        /// <summary>Si se está guardando: la ventana no se puede cerrar ni editar a medias.</summary>
        private bool _saving;

        /// <summary>Prepara la ventana para una pista, partiendo de lo que ya tenga decidido su fila.</summary>
        /// <param name="song">Fila de la pista a mostrar.</param>
        /// <param name="services">Lo que el editor toma de la ventana principal: carga, análisis y escucha.</param>
        /// <param name="save">
        /// Aplica las decisiones a la fila y escribe la copia; devuelve la ruta escrita, o <c>null</c>
        /// si no se pudo guardar (quien guarda ya habrá explicado por qué).
        /// </param>
        public WaveformEditor(
            Song song,
            WaveformEditorServices services,
            Func<WaveformEditor, Task<string?>> save)
        {
            ArgumentNullException.ThrowIfNull(song);
            ArgumentNullException.ThrowIfNull(services);
            InitializeComponent();

            Song = song;
            _durationSeconds = song.DurationSeconds;
            _initialAnalysisRange = song.Analysis?.Range;
            _services = services;
            _save = save;
            _manual = song.ManualRange is not null;

            TrimRange current = song.TrimRange ?? new TrimRange(0.0, _durationSeconds);
            _fadeIn = song.Edits?.Fades?.FadeIn;
            _fadeOut = song.Edits?.Fades?.FadeOut;
            _deletions = song.Edits?.Deletions ?? DeletedRegions.Empty;
            _lastCurveIn = _fadeIn?.Curve ?? FadeCurve.Linear;
            _lastCurveOut = _fadeOut?.Curve ?? FadeCurve.Linear;

            ShowTrackName();
            song.PropertyChanged += Song_PropertyChanged;
            UpdateResetButton();

            foreach (NumericUpDown field in (NumericUpDown[])[numStart, numEnd, numFrom, numTo])
            {
                field.Maximum = (decimal)_durationSeconds;
            }

            cmbCurve.Items.AddRange([.. FadeText.Curves.Select(FadeText.Name)]);

            // Cada vista de detalle abarca su silencio y unos segundos de música, para juzgar el corte
            // en contexto aunque la pista arrastre silencios largos, y el fundido que ya tuviera.
            double headSeconds = Math.Min(_durationSeconds, Math.Max(MinimumEdgeWindowSeconds, Math.Max(current.StartSeconds, _fadeIn?.EndSeconds ?? 0.0) + EdgeContextSeconds));
            double tailSeconds = Math.Min(_durationSeconds, Math.Max(MinimumEdgeWindowSeconds, _durationSeconds - Math.Min(current.EndSeconds, _fadeOut?.StartSeconds ?? _durationSeconds) + EdgeContextSeconds));
            InitializeViews(headSeconds, tailSeconds);

            ApplyRange(current.StartSeconds, current.EndSeconds);
            ApplyFades();
            ApplyDeletions();
            ApplySelection(null);
            ShowInspector(InspectorTarget.None);
            InitializeHelp();
            MarkApplied();
        }

        /// <summary>Se produce cuando el usuario pide llevar sus decisiones a la fila, al aceptar o al guardar.</summary>
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
            MarkApplied();
            Close();
        }

        private async void WaveformEditor_Shown(object? sender, EventArgs e)
        {
            CancellationToken token = _closing.Token;
            string filePath = Song.FilePath;

            // La pista completa alimenta la vista general y el detalle del principio; el final se
            // carga aparte porque debe situarse con la misma referencia que el corte final, y llega
            // mucho antes que la pista entera.
            await Task.WhenAll(
                LoadAsync(_services.Waveforms.LoadAsync(filePath, token), viewOverview, viewStart),
                LoadAsync(_services.Waveforms.LoadTailAsync(filePath, _durationSeconds - viewEnd.ViewStartSeconds, _durationSeconds, token), viewEnd))
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
                RememberWaveform(waveform, views);
                foreach (WaveformView view in views)
                {
                    // En la vista del resultado, el detalle del final usa la pista completa: su cola
                    // no basta para juntar lo que queda a ambos lados de un borrado.
                    if (!(_showingResult && view == viewEnd))
                    {
                        view.Waveform = waveform;
                    }
                }

                if (_showingResult && views.Contains(viewOverview))
                {
                    viewEnd.Waveform = waveform;
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

        // ------------------------------------------------------------------------------- Recorte

        private void View_MarkersChanged(object? sender, EventArgs e)
        {
            if (sender is WaveformView view)
            {
                BeginChange();
                _manual = true;
                ApplyRange(view.StartMarkerSeconds, view.EndMarkerSeconds);
                EndChange();
            }
        }

        private void numStart_ValueChanged(object? sender, EventArgs e)
        {
            if (_syncing)
            {
                return;
            }

            BeginChange();
            _manual = true;
            ApplyRange(Math.Min((double)numStart.Value, _endSeconds - WaveformView.MinimumGapSeconds), _endSeconds);
            EndChange();
        }

        private void numEnd_ValueChanged(object? sender, EventArgs e)
        {
            if (_syncing)
            {
                return;
            }

            BeginChange();
            _manual = true;
            ApplyRange(_startSeconds, Math.Max((double)numEnd.Value, _startSeconds + WaveformView.MinimumGapSeconds));
            EndChange();
        }

        private void btnReset_Click(object? sender, EventArgs e)
        {
            BeginChange();
            TrimRange range = AnalysisRange ?? new TrimRange(0.0, _durationSeconds);
            _manual = false;
            ApplyRange(range.StartSeconds, range.EndSeconds);
            EndChange();
            SetStatus(AnalysisRange is null ? "Se quitó el ajuste: la copia conserva la pista completa." : "Se restableció el recorte del análisis.");
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
                    view.SetMarkers(ToViewSeconds(_startSeconds), ToViewSeconds(_endSeconds));
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

        // -------------------------------------------------------------------- Aplicar y guardar

        private void btnAccept_Click(object? sender, EventArgs e)
        {
            Apply();
            Close();
        }

        private void btnCancel_Click(object? sender, EventArgs e) => Close();

        /// <summary>Lleva las decisiones a la fila y escribe la copia sin cerrar la ventana.</summary>
        /// <remarks>Nunca lanza: es el cuerpo de un manejador <c>async void</c>.</remarks>
        private async void btnSave_Click(object? sender, EventArgs e)
        {
            if (_saving)
            {
                return;
            }

            if (Edits is { } edits && edits.KeptSeconds(KeptRange) < WaveformView.MinimumGapSeconds)
            {
                MessageBox.Show(this, "Lo borrado no deja audio en la copia: restaura algo antes de guardar.", "Nada que guardar", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            StopPlayback();
            SetSaving(true);
            try
            {
                string? written = await _save(this).ConfigureAwait(true);
                if (IsDisposed)
                {
                    return;
                }

                SetStatus(written is not null
                    ? $"Guardado en «{Path.GetFileName(Path.GetDirectoryName(written))}{Path.DirectorySeparatorChar}{Path.GetFileName(written)}»."
                    : "No se guardó la copia; el motivo está en la ventana principal.");
            }
            catch (Exception exception)
            {
                if (!IsDisposed)
                {
                    SetStatus($"No se guardó la copia: {exception.Message}");
                }
            }
            finally
            {
                if (!IsDisposed)
                {
                    SetSaving(false);
                }
            }
        }

        /// <summary>Avisa a quien abrió la ventana para que lleve las decisiones a la fila.</summary>
        internal void Apply()
        {
            Applied?.Invoke(this, EventArgs.Empty);
            MarkApplied();
        }

        /// <summary>Mientras se guarda, nada de la ventana se puede tocar.</summary>
        private void SetSaving(bool saving)
        {
            _saving = saving;
            tableMain.Enabled = !saving;
            toolStrip.Enabled = !saving;
            UseWaitCursor = saving;
            if (saving)
            {
                SetStatus("Guardando la copia…");
            }
        }

        /// <summary>Si hay cambios sin aplicar, pregunta qué hacer con ellos antes de cerrar.</summary>
        private void WaveformEditor_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (_saving)
            {
                e.Cancel = true;
                return;
            }

            if (IsDirty)
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
