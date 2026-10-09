using EchoCut.Audio;
using EchoCut.Controls;
using EchoCut.Objects;
using System.Globalization;

namespace EchoCut
{
    /// <summary>
    /// La parte de <see cref="WaveformEditor"/> que edita el audio: la selección y lo que se hace con
    /// ella —fundidos y borrados—, además de los controles numéricos de los fundidos.
    /// </summary>
    /// <remarks>
    /// Va aparte del ciclo de vida de la ventana porque es donde vive el modelo de Audacity:
    /// seleccionar primero y actuar después. Todo se guarda en tiempo del original, igual que el
    /// recorte, así que ninguna edición desplaza a las demás.
    /// </remarks>
    public partial class WaveformEditor
    {
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

        private Fade? _fadeIn;
        private Fade? _fadeOut;
        private DeletedRegions _deletions = DeletedRegions.Empty;
        private TimeRegion? _selection;

        /// <summary>Fundidos vigentes, tal como se dibujan, se escuchan y se devuelven.</summary>
        private TrackFades? CurrentFades => _fadeIn is null && _fadeOut is null ? null : new TrackFades(_fadeIn, _fadeOut);

        private static int CurveIndex(FadeCurve curve) => Math.Max(0, FadeText.Curves.ToList().IndexOf(curve));

        private static FadeCurve CurveAt(ComboBox combo) =>
            FadeText.Curves[Math.Clamp(combo.SelectedIndex, 0, FadeText.Curves.Count - 1)];

        private static string Time(double seconds) => seconds.ToString("0.000", CultureInfo.CurrentCulture);

        // ------------------------------------------------------------------------------ Selección

        /// <summary>La selección hecha en una vista se lleva a las otras dos y a la barra de acciones.</summary>
        private void View_SelectionChanged(object? sender, EventArgs e)
        {
            if (sender is WaveformView view)
            {
                ApplySelection(view.Selection);
            }
        }

        /// <summary>Fija la selección en todas las vistas y habilita lo que se puede hacer con ella.</summary>
        private void ApplySelection(TimeRegion? selection)
        {
            _selection = selection;
            foreach (WaveformView view in Views)
            {
                view.Selection = selection;
            }

            bool usable = selection is { DurationSeconds: >= Fade.MinimumSeconds };
            btnSelFadeIn.Enabled = usable;
            btnSelFadeOut.Enabled = usable;
            btnSelDelete.Enabled = usable;
            btnSelPlay.Enabled = usable;
            btnSelRestore.Enabled = selection is { } region && _deletions.Regions.Any(region.Overlaps);

            lblSelection.Text = selection is { } current
                ? $"Selección: {Time(current.StartSeconds)} a {Time(current.EndSeconds)} s ({Time(current.DurationSeconds)} s)"
                : "Sin selección: arrastra sobre la forma de onda.";
        }

        private void btnSelFadeIn_Click(object? sender, EventArgs e)
        {
            if (_selection is { } selection)
            {
                SetFade(FadeDirection.In, selection.StartSeconds, selection.EndSeconds);
            }
        }

        private void btnSelFadeOut_Click(object? sender, EventArgs e)
        {
            if (_selection is { } selection)
            {
                SetFade(FadeDirection.Out, selection.StartSeconds, selection.EndSeconds);
            }
        }

        private void btnSelDelete_Click(object? sender, EventArgs e) => DeleteSelection();

        /// <summary>
        /// Borra la selección como el «Borrar» de Audacity: el audio sale de la copia y lo anterior se
        /// une con lo posterior. Después, como en Audacity, la selección desaparece.
        /// </summary>
        private void DeleteSelection()
        {
            if (_selection is not { } selection)
            {
                return;
            }

            DeletedRegions deletions = _deletions.Add(selection);
            if (KeptRange.DurationSeconds - deletions.Within(KeptRange).TotalSeconds < WaveformView.MinimumGapSeconds)
            {
                MessageBox.Show(this, "No se puede borrar todo lo que conserva la copia.", "Borrar selección", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _deletions = deletions;
            ApplyDeletions();
            ApplySelection(null);
            MarkDirty();
        }

        /// <summary>Devuelve a la copia lo borrado que cae dentro de la selección.</summary>
        private void btnSelRestore_Click(object? sender, EventArgs e)
        {
            if (_selection is not { } selection)
            {
                return;
            }

            _deletions = _deletions.Remove(selection);
            ApplyDeletions();
            ApplySelection(selection);
            MarkDirty();
        }

        /// <summary>Lleva lo borrado a las vistas y al resumen.</summary>
        private void ApplyDeletions()
        {
            foreach (WaveformView view in Views)
            {
                view.Deletions = _deletions;
            }

            UpdateSummary();
        }

        /// <summary>Resume lo borrado dentro de la copia; lo que el recorte ya elimina no se cuenta.</summary>
        private string DescribeDeletions()
        {
            DeletedRegions effective = _deletions.Within(KeptRange);
            return effective.IsEmpty
                ? string.Empty
                : $" Se borrarán {effective.TotalSeconds:0.00} s en {effective.Regions.Count} {(effective.Regions.Count == 1 ? "fragmento" : "fragmentos")}.";
        }

        // ------------------------------------------------------------------------------- Fundidos

        /// <summary>Lleva al fundido el borde que el usuario arrastra en cualquier vista.</summary>
        /// <remarks>
        /// Un tramo más corto que <see cref="Fade.MinimumSeconds"/> no se aplica: suele ser el
        /// principio de un arrastre, y aplicarlo haría parpadear el fundido anterior.
        /// </remarks>
        private void View_FadeAdjusted(object? sender, FadeAdjustedEventArgs e)
        {
            if (e.EndSeconds - e.StartSeconds >= Fade.MinimumSeconds)
            {
                SetFade(e.Direction, e.StartSeconds, e.EndSeconds);
            }
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
                MarkDirty();
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
                MarkDirty();
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
                MarkDirty();
            }
        }

        private void cmbFadeOutCurve_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (!_syncing && _fadeOut is { } fade)
            {
                _fadeOut = fade with { Curve = CurveAt(cmbFadeOutCurve) };
                ApplyFades();
                MarkDirty();
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
            MarkDirty();
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
        /// curva sigue disponible, para elegirla antes de aplicarla a una selección.
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

        /// <summary>
        /// Resume los fundidos y avisa de lo que implican las ediciones: la copia se recodifica, y lo
        /// que el recorte deja fuera no tendrá efecto.
        /// </summary>
        private IEnumerable<string> DescribeEdits()
        {
            if (CurrentFades is { } fades)
            {
                List<string> parts = [];
                foreach (Fade? candidate in (Fade?[])[fades.FadeIn, fades.FadeOut])
                {
                    if (candidate is { } fade)
                    {
                        string outside = fade.Overlaps(KeptRange) ? string.Empty : ", fuera de la copia";
                        parts.Add($"{FadeText.Name(fade.Direction).ToLowerInvariant()} de {fade.DurationSeconds:0.00} s ({FadeText.Name(fade.Curve).ToLowerInvariant()}{outside})");
                    }
                }

                yield return $"Fundidos: {string.Join(", ", parts)}.";
            }

            if (Edits is { } edits)
            {
                yield return edits.Within(KeptRange) is null
                    ? "Las ediciones no afectan a la copia, que se escribirá sin recodificar."
                    : "La copia se volverá a codificar para aplicar las ediciones.";
            }
        }
    }
}
