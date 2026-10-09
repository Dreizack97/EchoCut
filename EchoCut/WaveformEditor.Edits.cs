using EchoCut.Audio;
using EchoCut.Controls;
using EchoCut.Objects;
using System.Drawing.Drawing2D;
using System.Globalization;

namespace EchoCut
{
    /// <summary>
    /// La parte de <see cref="WaveformEditor"/> que edita el audio —la selección y lo que se hace
    /// con ella: fundidos y borrados— y el inspector que muestra y ajusta cada elemento.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Es el modelo de Audacity: seleccionar primero y actuar después. Todo se guarda en tiempo del
    /// original, igual que el recorte, así que ninguna edición desplaza a las demás.
    /// </para>
    /// <para>
    /// El inspector tiene una parte fija, la copia, y una contextual que cambia con lo último que el
    /// usuario tocó: la selección, un fundido o un fragmento borrado. Los mismos campos sirven a
    /// todos, de modo que el panel no crece con cada tipo de elemento.
    /// </para>
    /// </remarks>
    public partial class WaveformEditor
    {
        /// <summary>Color de la curva en el inspector: el mismo marrón ámbar de las envolventes de la onda.</summary>
        private static readonly Color CurveLineColor = Color.FromArgb(0x8A, 0x3B, 0x00);

        /// <summary>Relleno bajo la curva en el inspector, el mismo ámbar translúcido de la onda.</summary>
        private static readonly Color CurveFillColor = Color.FromArgb(56, 0xE6, 0x9F, 0x00);

        private Fade? _fadeIn;
        private Fade? _fadeOut;
        private DeletedRegions _deletions = DeletedRegions.Empty;
        private TimeRegion? _selection;

        /// <summary>Curvas con las que nace el próximo fundido de cada sentido: la última elegida.</summary>
        private FadeCurve _lastCurveIn;
        private FadeCurve _lastCurveOut;

        private InspectorTarget _inspected;
        private TimeRegion? _inspectedDeletion;

        /// <summary>Lo que muestra la parte contextual del inspector.</summary>
        private enum InspectorTarget
        {
            None,
            Selection,
            FadeIn,
            FadeOut,
            Deletion,
        }

        /// <summary>Fundidos vigentes, tal como se dibujan, se escuchan y se devuelven.</summary>
        private TrackFades? CurrentFades => _fadeIn is null && _fadeOut is null ? null : new TrackFades(_fadeIn, _fadeOut);

        /// <summary>Fundido que muestra el inspector, si muestra uno.</summary>
        private Fade? InspectedFade => _inspected switch
        {
            InspectorTarget.FadeIn => _fadeIn,
            InspectorTarget.FadeOut => _fadeOut,
            _ => null,
        };

        private static string Time(double seconds) => seconds.ToString("0.000", CultureInfo.CurrentCulture);

        // ------------------------------------------------------------------------------ Selección

        /// <summary>La selección hecha en una vista se lleva a las otras dos, a la barra y al inspector.</summary>
        private void View_SelectionChanged(object? sender, EventArgs e)
        {
            if (sender is WaveformView view)
            {
                ApplySelection(view.Selection);
                ShowInspector(view.Selection is null ? InspectorTarget.None : InspectorTarget.Selection);
            }
        }

        /// <summary>
        /// Un clic sin arrastre: sobre lo borrado lo selecciona entero; sobre un fundido lo muestra en
        /// el inspector; en cualquier otro sitio marca desde dónde se escuchará, como en Audacity.
        /// </summary>
        private void View_WaveformClicked(object? sender, WaveformClickEventArgs e)
        {
            if (_deletions.RegionAt(e.Seconds) is { } deleted)
            {
                ApplySelection(deleted);
                ShowInspector(InspectorTarget.Deletion, deleted);
                return;
            }

            ApplySelection(null);
            if (_fadeIn is { } fadeIn && e.Seconds >= fadeIn.StartSeconds && e.Seconds <= fadeIn.EndSeconds)
            {
                ShowInspector(InspectorTarget.FadeIn);
            }
            else if (_fadeOut is { } fadeOut && e.Seconds >= fadeOut.StartSeconds && e.Seconds <= fadeOut.EndSeconds)
            {
                ShowInspector(InspectorTarget.FadeOut);
            }
            else
            {
                ShowInspector(InspectorTarget.None);
            }

            SetCursor(e.Seconds);
            SetStatus($"Se escuchará desde {Time(e.Seconds)} s; pulsa Espacio para reproducir.");
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
            btnFadeIn.Enabled = usable;
            btnFadeOut.Enabled = usable;
            btnDelete.Enabled = usable;
            btnRestore.Enabled = selection is { } region && _deletions.Regions.Any(region.Overlaps);

            if (_inspected == InspectorTarget.Selection)
            {
                SyncInspector();
            }
        }

        private void btnFadeIn_Click(object? sender, EventArgs e) => FadeSelection(FadeDirection.In);

        private void btnFadeOut_Click(object? sender, EventArgs e) => FadeSelection(FadeDirection.Out);

        /// <summary>Convierte la selección en un fundido con la última curva usada en ese sentido.</summary>
        private void FadeSelection(FadeDirection direction)
        {
            if (_selection is not { } selection)
            {
                return;
            }

            BeginChange();
            SetFade(direction, selection.StartSeconds, selection.EndSeconds, direction == FadeDirection.In ? _lastCurveIn : _lastCurveOut);
            EndChange();

            ApplySelection(null);
            ShowInspector(direction == FadeDirection.In ? InspectorTarget.FadeIn : InspectorTarget.FadeOut);
            SetStatus($"{FadeText.Name(direction)} de {Time(selection.DurationSeconds)} s. Cambia su curva en el inspector; Ctrl+Z la deshace.");
        }

        private void btnDelete_Click(object? sender, EventArgs e) => DeleteSelection();

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

            BeginChange();
            _deletions = deletions;
            ApplyDeletions();
            EndChange();

            ApplySelection(null);
            ShowInspector(InspectorTarget.None);
            SetStatus($"Se borraron {Time(selection.DurationSeconds)} s. Haz clic en lo borrado para restaurarlo; Ctrl+Z lo deshace.");
        }

        /// <summary>Devuelve a la copia lo borrado que cae dentro de la selección.</summary>
        private void btnRestore_Click(object? sender, EventArgs e)
        {
            if (_selection is { } selection)
            {
                RestoreDeleted(selection);
            }
        }

        private void RestoreDeleted(TimeRegion region)
        {
            BeginChange();
            _deletions = _deletions.Remove(region);
            ApplyDeletions();
            EndChange();

            ApplySelection(null);
            ShowInspector(InspectorTarget.None);
            SetStatus("Se restauró el audio borrado.");
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

        // ------------------------------------------------------------------------------- Fundidos

        /// <summary>Lleva al fundido el borde que el usuario arrastra en cualquier vista.</summary>
        /// <remarks>
        /// Un tramo más corto que <see cref="Fade.MinimumSeconds"/> no se aplica: suele ser el
        /// principio de un arrastre, y aplicarlo haría parpadear el fundido anterior.
        /// </remarks>
        private void View_FadeAdjusted(object? sender, FadeAdjustedEventArgs e)
        {
            if (e.EndSeconds - e.StartSeconds < Fade.MinimumSeconds)
            {
                return;
            }

            BeginChange();
            Fade? current = e.Direction == FadeDirection.In ? _fadeIn : _fadeOut;
            SetFade(e.Direction, e.StartSeconds, e.EndSeconds, current?.Curve ?? FadeCurve.Linear);
            EndChange();
            ShowInspector(e.Direction == FadeDirection.In ? InspectorTarget.FadeIn : InspectorTarget.FadeOut);
        }

        /// <summary>Fija el tramo y la curva de un fundido y lo lleva a toda la ventana.</summary>
        /// <param name="direction">Fundido a fijar.</param>
        /// <param name="startSeconds">Comienzo pedido.</param>
        /// <param name="endSeconds">Final pedido.</param>
        /// <param name="curve">Curva del fundido.</param>
        /// <param name="keepStart">
        /// Si el tramo es demasiado corto, qué extremo se respeta: el que acaba de tocar el usuario.
        /// </param>
        private void SetFade(FadeDirection direction, double startSeconds, double endSeconds, FadeCurve curve, bool keepStart = true)
        {
            (double start, double end) = Widen(startSeconds, endSeconds, Fade.MinimumSeconds, keepStart);

            if (direction == FadeDirection.In)
            {
                _fadeIn = new Fade(direction, start, end, curve);
            }
            else
            {
                _fadeOut = new Fade(direction, start, end, curve);
            }

            ApplyFades();
        }

        /// <summary>Lleva los fundidos a las vistas, al inspector y al resumen.</summary>
        private void ApplyFades()
        {
            TrackFades? fades = CurrentFades;
            foreach (WaveformView view in Views)
            {
                view.Fades = fades;
            }

            if (_inspected is InspectorTarget.FadeIn or InspectorTarget.FadeOut)
            {
                if (InspectedFade is null)
                {
                    ShowInspector(InspectorTarget.None);
                }
                else
                {
                    SyncInspector();
                }
            }

            UpdateSummary();
        }

        /// <summary>Acota un tramo a la pista y lo ensancha hasta una duración mínima por el lado que no tocó el usuario.</summary>
        private (double Start, double End) Widen(double startSeconds, double endSeconds, double minimum, bool keepStart)
        {
            double start = Math.Clamp(startSeconds, 0.0, _durationSeconds);
            double end = Math.Clamp(endSeconds, 0.0, _durationSeconds);
            if (end - start >= minimum)
            {
                return (start, end);
            }

            if (keepStart)
            {
                end = Math.Min(_durationSeconds, start + minimum);
                return (end - minimum, end);
            }

            start = Math.Max(0.0, end - minimum);
            return (start, start + minimum);
        }

        // ------------------------------------------------------------------------------ Inspector

        /// <summary>Muestra en la parte contextual del inspector un elemento, o la ayuda si no hay ninguno.</summary>
        private void ShowInspector(InspectorTarget target, TimeRegion? deletion = null)
        {
            _inspected = target;
            _inspectedDeletion = target == InspectorTarget.Deletion ? deletion : null;

            (string header, string hint) = target switch
            {
                InspectorTarget.Selection => ("Selección", "Aplícale un fundido o bórrala desde la barra de herramientas; Espacio la escucha."),
                InspectorTarget.FadeIn => ("◢ Aparición", "Ajusta sus bordes arrastrándolos sobre la onda o con estos valores."),
                InspectorTarget.FadeOut => ("◣ Desaparición", "Ajusta sus bordes arrastrándolos sobre la onda o con estos valores."),
                InspectorTarget.Deletion => ("⌫ Fragmento borrado", "Este audio no estará en la copia; lo anterior y lo posterior se unen."),
                _ => ("Detalles", "Selecciona un tramo arrastrando sobre la onda, o haz clic en un fundido o en un fragmento borrado, para ver aquí sus detalles."),
            };

            lblContextHeader.Text = header;
            lblContextHint.Text = hint;

            bool hasRange = target != InspectorTarget.None;
            bool isFade = target is InspectorTarget.FadeIn or InspectorTarget.FadeOut;
            foreach (Control control in (Control[])[lblFromCaption, numFrom, lblToCaption, numTo, lblLengthCaption, lblLengthValue])
            {
                control.Visible = hasRange;
            }

            foreach (Control control in (Control[])[lblCurveCaption, cmbCurve, pnlCurve])
            {
                control.Visible = isFade;
            }

            btnContextAction.Visible = isFade || target == InspectorTarget.Deletion;
            btnContextAction.Text = isFade ? "Quitar fundido" : "↺ Restaurar";
            toolTip.SetToolTip(btnContextAction, isFade ? "Quitar este fundido de la copia" : "Devolver este audio a la copia");

            SyncInspector();
        }

        /// <summary>Lleva a los campos del inspector los valores del elemento que muestra.</summary>
        private void SyncInspector()
        {
            TimeRegion? region = _inspected switch
            {
                InspectorTarget.Selection => _selection,
                InspectorTarget.Deletion => _inspectedDeletion,
                _ => InspectedFade is { } fade ? new TimeRegion(fade.StartSeconds, fade.EndSeconds) : null,
            };

            if (region is not { } value)
            {
                return;
            }

            _syncing = true;
            try
            {
                numFrom.Value = Math.Clamp((decimal)Math.Round(value.StartSeconds, 3), numFrom.Minimum, numFrom.Maximum);
                numTo.Value = Math.Clamp((decimal)Math.Round(value.EndSeconds, 3), numTo.Minimum, numTo.Maximum);
                lblLengthValue.Text = $"{Time(value.DurationSeconds)} s";

                if (InspectedFade is { } fade)
                {
                    cmbCurve.SelectedIndex = Math.Max(0, FadeText.Curves.ToList().IndexOf(fade.Curve));
                    pnlCurve.Invalidate();
                }
            }
            finally
            {
                _syncing = false;
            }
        }

        /// <summary>Los valores escritos en el inspector se llevan al elemento que muestra.</summary>
        private void numContext_ValueChanged(object? sender, EventArgs e)
        {
            if (_syncing)
            {
                return;
            }

            bool keepStart = sender == numFrom;
            double from = (double)numFrom.Value;
            double to = (double)numTo.Value;

            switch (_inspected)
            {
                case InspectorTarget.Selection:
                    (double start, double end) = Widen(from, to, Fade.MinimumSeconds, keepStart);
                    ApplySelection(new TimeRegion(start, end));
                    break;

                case InspectorTarget.FadeIn or InspectorTarget.FadeOut when InspectedFade is { } fade:
                    BeginChange();
                    SetFade(fade.Direction, from, to, fade.Curve, keepStart);
                    EndChange();
                    break;

                case InspectorTarget.Deletion when _inspectedDeletion is { } deleted:
                    (double newStart, double newEnd) = Widen(from, to, WaveformView.MinimumGapSeconds, keepStart);
                    BeginChange();
                    _deletions = _deletions.Remove(deleted).Add(new TimeRegion(newStart, newEnd));
                    _inspectedDeletion = _deletions.RegionAt(newStart);
                    ApplyDeletions();
                    EndChange();
                    ApplySelection(_inspectedDeletion);
                    SyncInspector();
                    break;
            }
        }

        private void cmbCurve_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_syncing || InspectedFade is not { } fade || cmbCurve.SelectedIndex < 0)
            {
                return;
            }

            FadeCurve curve = FadeText.Curves[cmbCurve.SelectedIndex];
            if (fade.Direction == FadeDirection.In)
            {
                _lastCurveIn = curve;
            }
            else
            {
                _lastCurveOut = curve;
            }

            BeginChange();
            SetFade(fade.Direction, fade.StartSeconds, fade.EndSeconds, curve);
            EndChange();
        }

        /// <summary>Quita el fundido que se muestra, o restaura el fragmento borrado.</summary>
        private void btnContextAction_Click(object? sender, EventArgs e)
        {
            if (InspectedFade is { } fade)
            {
                BeginChange();
                if (fade.Direction == FadeDirection.In)
                {
                    _fadeIn = null;
                }
                else
                {
                    _fadeOut = null;
                }

                ApplyFades();
                EndChange();
                ShowInspector(InspectorTarget.None);
                SetStatus($"Se quitó la {FadeText.Name(fade.Direction).ToLowerInvariant()}; Ctrl+Z la recupera.");
            }
            else if (_inspectedDeletion is { } deleted)
            {
                RestoreDeleted(deleted);
            }
        }

        /// <summary>
        /// Dibuja la curva del fundido que se muestra y su nivel a mitad de camino, que es lo que de
        /// verdad distingue una curva de otra al oído.
        /// </summary>
        private void pnlCurve_Paint(object? sender, PaintEventArgs e)
        {
            if (InspectedFade is not { } fade)
            {
                return;
            }

            Graphics g = e.Graphics;
            Rectangle area = pnlCurve.ClientRectangle;
            int padding = LogicalToDeviceUnits(8);
            Rectangle plot = Rectangle.Inflate(area, -padding, -padding);
            if (plot.Width <= 1 || plot.Height <= 1)
            {
                return;
            }

            PointF[] points = new PointF[plot.Width + 1];
            for (int i = 0; i <= plot.Width; i++)
            {
                double progress = i / (double)plot.Width;
                double gain = FadeShape.FadeInGain(fade.Curve, fade.Direction == FadeDirection.In ? progress : 1.0 - progress);
                points[i] = new PointF(plot.Left + i, (float)(plot.Bottom - (gain * plot.Height)));
            }

            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (SolidBrush fill = new(CurveFillColor))
            {
                g.FillPolygon(fill, [new PointF(plot.Left, plot.Bottom), .. points, new PointF(plot.Right, plot.Bottom)]);
            }

            using (Pen line = new(CurveLineColor, LogicalToDeviceUnits(2)))
            {
                g.DrawLines(line, points);
            }

            double middle = 20.0 * Math.Log10(Math.Max(1e-6, FadeShape.FadeInGain(fade.Curve, 0.5)));
            string label = $"A mitad: {middle.ToString("0.0", CultureInfo.CurrentCulture)} dB";
            TextRenderer.DrawText(g, label, Font, new Point(fade.Direction == FadeDirection.In ? plot.Left : plot.Right - TextRenderer.MeasureText(label, Font).Width, plot.Top), SystemColors.GrayText);
        }
    }
}
