using EchoCut.Audio;
using EchoCut.Controls;
using EchoCut.Processing;
using EchoCut.Waveforms;
using System.Globalization;

namespace EchoCut
{
    /// <summary>
    /// La parte de <see cref="WaveformEditor"/> que enseña y analiza el resultado: cómo quedará la
    /// copia tras los fundidos y los borrados, y qué silencios tendrá.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Se edita siempre sobre el original, donde lo borrado sigue a la vista para poder restaurarlo;
    /// la onda ya se pinta con los fundidos aplicados. «Ver resultado» cambia las tres vistas a la
    /// línea de tiempo de la copia: lo borrado desaparece y los empalmes quedan marcados. Es una
    /// vista para mirar y escuchar: para editar se vuelve al original.
    /// </para>
    /// <para>
    /// «Detectar silencios» analiza ese resultado con el mismo algoritmo que la ventana principal y
    /// propone el comienzo y el final de la copia. Es lo que permite recortar silencios que solo
    /// aparecen al editar: una desaparición que deja la cola en silencio o un fragmento borrado que
    /// deja el final sin música.
    /// </para>
    /// </remarks>
    public partial class WaveformEditor
    {
        private bool _showingResult;
        private bool _detecting;

        /// <summary>Análisis hecho desde el editor, que sustituye al que tenía la fila.</summary>
        private TrackAnalysis? _analysis;

        /// <summary>Tramo inicial de cada vista en el original, guardado mientras se ve el resultado.</summary>
        private Dictionary<WaveformView, (double Start, double End)>? _sourceHomes;

        private Waveform? _fullWaveform;
        private Waveform? _tailWaveform;

        /// <summary>
        /// Análisis del resultado hecho desde el editor, si sigue describiendo las ediciones vigentes.
        /// </summary>
        /// <value>
        /// El análisis, para que la fila muestre los silencios de la copia; <c>null</c> si no se analizó
        /// o si las ediciones cambiaron después.
        /// </value>
        public TrackAnalysis? ResultAnalysis => _analysis is { } analysis && analysis.Describes(Edits) ? analysis : null;

        /// <summary>Tramo que propone el análisis vigente: el del editor si se hizo, o el de la fila.</summary>
        private TrimRange? AnalysisRange => _analysis?.Range ?? _initialAnalysisRange;

        /// <summary>Instante que muestran las vistas para un instante del original.</summary>
        /// <remarks>En la vista del resultado, todo lo que se dibuja se traslada a su línea de tiempo.</remarks>
        private double? ToViewSeconds(double? sourceSeconds) =>
            _showingResult && sourceSeconds is { } seconds ? _deletions.OutputSecondsAt(seconds) : sourceSeconds;

        private double ToViewSeconds(double sourceSeconds) =>
            _showingResult ? _deletions.OutputSecondsAt(sourceSeconds) : sourceSeconds;

        private void UpdateResetButton() =>
            btnReset.Text = AnalysisRange is null ? "Quitar ajuste" : "Restablecer análisis";

        /// <summary>Recuerda qué forma de onda es la completa y cuál la cola, para cambiar de vista sin volver a cargar.</summary>
        private void RememberWaveform(Waveform waveform, WaveformView[] views)
        {
            if (views.Contains(viewOverview))
            {
                _fullWaveform = waveform;
            }
            else if (views.Contains(viewEnd))
            {
                _tailWaveform = waveform;
            }
        }

        // ------------------------------------------------------------------- Vista del resultado

        private void btnResult_CheckedChanged(object? sender, EventArgs e)
        {
            if (btnResult.Checked == _showingResult)
            {
                return;
            }

            StopPlayback();
            if (btnResult.Checked)
            {
                ShowResult();
            }
            else
            {
                ShowSource();
            }
        }

        /// <summary>Pasa las tres vistas a la línea de tiempo de la copia, en modo de solo lectura.</summary>
        private void ShowResult()
        {
            ApplySelection(null);
            ShowInspector(InspectorTarget.None);
            _showingResult = true;
            _sourceHomes = new Dictionary<WaveformView, (double Start, double End)>(_homes);

            double resultSeconds = Math.Max(WaveformView.MinimumSpanSeconds, _durationSeconds - _deletions.TotalSeconds);
            double headSpan = _homes[viewStart].End - _homes[viewStart].Start;
            double tailSpan = _homes[viewEnd].End - _homes[viewEnd].Start;

            viewOverview.SetView(resultSeconds, 0.0, resultSeconds);
            viewStart.SetView(resultSeconds, 0.0, Math.Min(resultSeconds, headSpan));
            viewEnd.SetView(resultSeconds, Math.Max(0.0, resultSeconds - tailSpan), resultSeconds);
            if (_fullWaveform is not null)
            {
                viewEnd.Waveform = _fullWaveform;
            }

            IReadOnlyList<double> splices = [.. _deletions.Regions.Select(region => _deletions.OutputSecondsAt(region.StartSeconds)).Distinct()];
            foreach (WaveformView view in Views)
            {
                _homes[view] = (view.ViewStartSeconds, view.ViewEndSeconds);
                view.ReadOnly = true;
                view.CollapseDeletions = true;
                view.Splices = splices;
            }

            RefreshViews();
            SetEditingEnabled(false);
            lblOverview.Text = "Resultado completo — así quedará la copia (solo lectura)";
            lblStartView.Text = "Inicio del resultado";
            lblEndView.Text = "Final del resultado";
            SetStatus(_deletions.IsEmpty
                ? "Vista del resultado: la onda ya incluye los fundidos. Desactiva «Ver resultado» para editar."
                : $"Vista del resultado: {_deletions.Regions.Count} {(_deletions.Regions.Count == 1 ? "empalme marcado" : "empalmes marcados")} en morado. Desactiva «Ver resultado» para editar.");
        }

        /// <summary>Devuelve las tres vistas al original, donde se edita.</summary>
        private void ShowSource()
        {
            _showingResult = false;
            if (_sourceHomes is { } homes)
            {
                foreach ((WaveformView view, (double start, double end)) in homes)
                {
                    _homes[view] = (start, end);
                }
            }

            (double headStart, double headEnd) = _homes[viewStart];
            (double tailStart, double tailEnd) = _homes[viewEnd];
            viewOverview.SetView(_durationSeconds, 0.0, _durationSeconds);
            viewStart.SetView(_durationSeconds, headStart, headEnd);
            viewEnd.SetView(_durationSeconds, tailStart, tailEnd);
            viewEnd.SetScrollLimits(tailStart, _durationSeconds);
            if (_tailWaveform is not null)
            {
                viewEnd.Waveform = _tailWaveform;
            }

            foreach (WaveformView view in Views)
            {
                view.ReadOnly = false;
                view.CollapseDeletions = false;
                view.Splices = [];
            }

            RefreshViews();
            SetEditingEnabled(true);
            lblOverview.Text = "Pista completa";
            lblStartView.Text = "Inicio de la pista";
            lblEndView.Text = "Final de la pista";
            SetStatus("Vista del original: arrastra sobre la onda para seleccionar.");
        }

        /// <summary>Vuelve a llevar a las vistas todo lo que dibujan, en la línea de tiempo que muestren ahora.</summary>
        private void RefreshViews()
        {
            ApplyRange(_startSeconds, _endSeconds);
            ApplyFades();
            ApplyDeletions();
            SetCursor(_cursorSeconds);
        }

        /// <summary>
        /// En la vista del resultado no se edita: se apagan las acciones y los campos que cambian algo,
        /// pero se sigue pudiendo escuchar los bordes.
        /// </summary>
        private void SetEditingEnabled(bool enabled)
        {
            foreach (Control control in (Control[])[numStart, numEnd, btnReset, numFrom, numTo, cmbCurve, btnContextAction])
            {
                control.Enabled = enabled;
            }

            if (enabled)
            {
                ApplySelection(_selection);
                UpdateHistoryButtons();
                return;
            }

            foreach (ToolStripItem item in (ToolStripItem[])[btnFadeIn, btnFadeOut, btnDelete, btnRestore, btnUndo, btnRedo, btnZoomSelection])
            {
                item.Enabled = false;
            }
        }

        // ----------------------------------------------------------------- Detectar silencios

        /// <summary>
        /// Analiza el resultado editado y propone el comienzo y el final de la copia; se puede deshacer.
        /// </summary>
        /// <remarks>Nunca lanza: es el cuerpo de un manejador <c>async void</c>.</remarks>
        private async void btnDetect_Click(object? sender, EventArgs e)
        {
            if (_detecting || _saving)
            {
                return;
            }

            _detecting = true;
            btnDetect.Enabled = false;
            UseWaitCursor = true;
            AudioEdits? edits = Edits;
            SetStatus(edits is null ? "Analizando los silencios de la pista…" : "Analizando los silencios del resultado editado…");

            try
            {
                TrackAnalysis analysis = await _services.Analysis
                    .AnalyzeOneAsync(new AnalysisRequest(Song.Track, edits), _services.Options(), _closing.Token)
                    .ConfigureAwait(true);

                if (IsDisposed)
                {
                    return;
                }

                BeginChange();
                _analysis = analysis;
                _manual = false;
                ApplyRange(analysis.StartSeconds, analysis.EndSeconds);
                UpdateResetButton();
                EndChange();

                SetStatus(DescribeDetection(analysis));
            }
            catch (OperationCanceledException)
            {
                // La ventana se cerró mientras se analizaba.
            }
            catch (Exception exception)
            {
                if (!IsDisposed)
                {
                    SetStatus("No se pudieron detectar los silencios.");
                    MessageBox.Show(this, exception.Message, "Detectar silencios", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            finally
            {
                _detecting = false;
                if (!IsDisposed)
                {
                    btnDetect.Enabled = true;
                    UseWaitCursor = false;
                }
            }
        }

        /// <summary>Cuenta qué encontró el análisis, en segundos del resultado, que es lo que se oirá.</summary>
        private static string DescribeDetection(TrackAnalysis analysis)
        {
            if (!analysis.ShouldTrim)
            {
                return "El resultado no tiene silencios que recortar con la tolerancia actual.";
            }

            double leading = analysis.Leading.ShouldTrim ? analysis.Leading.RemovedSeconds : 0.0;
            double trailing = analysis.Trailing.ShouldTrim ? analysis.Trailing.RemovedSeconds : 0.0;
            return string.Create(
                CultureInfo.CurrentCulture,
                $"Silencios del resultado: se quitarán {leading:0.00} s al inicio y {trailing:0.00} s al final. Ctrl+Z lo deshace.");
        }
    }
}
