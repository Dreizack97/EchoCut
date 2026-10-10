using EchoCut.Controls;
using EchoCut.Waveforms;

namespace EchoCut
{
    /// <summary>
    /// La parte de <see cref="WaveformEditor"/> que gobierna qué tramo muestra cada vista: el zoom,
    /// el desplazamiento y la vista sobre la que actúan los botones.
    /// </summary>
    /// <remarks>
    /// La rueda y Ctrl+rueda actúan sobre la vista bajo el puntero, sin más; los botones y atajos de
    /// zoom, sobre la última vista que se tocó, que se distingue por su marco de foco.
    /// </remarks>
    public partial class WaveformEditor
    {
        /// <summary>Cuánto acerca o aleja cada pulsación de los botones de zoom.</summary>
        private const double ButtonZoomFactor = 2.0;

        /// <summary>Margen que se deja a cada lado de la selección al ajustar la vista a ella.</summary>
        private const double SelectionMarginFraction = 0.05;

        /// <summary>Tramo inicial de cada vista, al que vuelve «Ver todo».</summary>
        private readonly Dictionary<WaveformView, (double Start, double End)> _homes = [];

        private WaveformView? _activeView;

        /// <summary>Vista sobre la que actúan los botones de zoom: la última tocada, o la general.</summary>
        private WaveformView ActiveView => _activeView ?? viewOverview;

        /// <summary>Fija el tramo inicial de cada vista y hasta dónde se puede desplazar.</summary>
        /// <param name="headSeconds">Segundos que abarca la vista del inicio.</param>
        /// <param name="tailSeconds">Segundos que abarca la vista del final.</param>
        /// <remarks>
        /// La vista del final solo tiene la cola de la pista: no se puede llevar más atrás de lo que
        /// se decodifica para ella.
        /// </remarks>
        private void InitializeViews(double headSeconds, double tailSeconds)
        {
            viewOverview.SetView(_durationSeconds, 0.0, _durationSeconds);
            viewStart.SetView(_durationSeconds, 0.0, headSeconds);
            viewEnd.SetView(_durationSeconds, _durationSeconds - tailSeconds, _durationSeconds);
            viewEnd.SetScrollLimits(_durationSeconds - tailSeconds, _durationSeconds);

            foreach (WaveformView view in Views)
            {
                _homes[view] = (view.ViewStartSeconds, view.ViewEndSeconds);
                view.StatusText = "Calculando forma de onda…";
            }
        }

        /// <summary>La vista que recibe el foco pasa a ser la de los botones de zoom.</summary>
        private void View_Enter(object? sender, EventArgs e)
        {
            if (sender is WaveformView view)
            {
                _activeView = view;
            }
        }

        private void btnZoomIn_Click(object? sender, EventArgs e) => ActiveView.ZoomBy(ButtonZoomFactor, ZoomAnchor(ActiveView));

        private void btnZoomOut_Click(object? sender, EventArgs e) => ActiveView.ZoomBy(1.0 / ButtonZoomFactor, ZoomAnchor(ActiveView));

        /// <summary>Ajusta la vista activa a la selección, con un poco de margen para ver lo que la rodea.</summary>
        private void btnZoomSelection_Click(object? sender, EventArgs e)
        {
            if (_selection is { } selection)
            {
                double margin = selection.DurationSeconds * SelectionMarginFraction;
                ActiveView.ShowRange(selection.StartSeconds - margin, selection.EndSeconds + margin);
            }
        }

        private void btnZoomFit_Click(object? sender, EventArgs e)
        {
            (double start, double end) = _homes[ActiveView];
            ActiveView.ShowRange(start, end);
        }

        /// <summary>
        /// Punto que no se mueve al acercar con los botones: la selección o el cursor si están a la
        /// vista, para no perder de vista lo que se está mirando; si no, el centro.
        /// </summary>
        private double? ZoomAnchor(WaveformView view)
        {
            double? candidate = _selection is { } selection ? (selection.StartSeconds + selection.EndSeconds) / 2.0 : _cursorSeconds;
            return candidate is { } seconds && seconds >= view.ViewStartSeconds && seconds <= view.ViewEndSeconds ? seconds : null;
        }

        private void btnDecibels_CheckedChanged(object? sender, EventArgs e)
        {
            AmplitudeScale scale = btnDecibels.Checked ? AmplitudeScale.Decibels : AmplitudeScale.Linear;
            foreach (WaveformView view in Views)
            {
                view.AmplitudeScale = scale;
            }
        }
    }
}
