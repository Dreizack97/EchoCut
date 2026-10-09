using EchoCut.Controls;
using EchoCut.Waveforms;

namespace EchoCut
{
    /// <summary>
    /// La parte de <see cref="WaveformEditor"/> que gobierna qué tramo muestra cada vista y con qué
    /// escala vertical.
    /// </summary>
    /// <remarks>
    /// La rueda y Ctrl+rueda desplazan y acercan la vista bajo el puntero; de eso se ocupa la propia
    /// vista, dentro de los límites que se fijan aquí.
    /// </remarks>
    public partial class WaveformEditor
    {
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
                view.StatusText = "Calculando forma de onda…";
            }
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
