using EchoCut.Objects;

namespace EchoCut
{
    /// <summary>
    /// La parte de <see cref="Main"/> que gobierna la barra de estado: el mensaje de la última
    /// acción, el progreso del lote en curso y el resumen permanente de la lista.
    /// </summary>
    /// <remarks>
    /// Está aparte por la misma razón que la rejilla: no decide nada del lote, solo lo refleja.
    /// Los manejadores de <c>Main.cs</c> le dicen qué ha pasado y aquí se resuelve cómo se ve.
    /// </remarks>
    public partial class Main
    {
        /// <summary>
        /// Si ya hay un recálculo del resumen encolado. Un lote cambia el estado de cientos de filas
        /// en ráfaga; recontar la lista en cada aviso es cuadrático, y basta con hacerlo una vez
        /// cuando el bucle de mensajes vuelve a quedar libre.
        /// </summary>
        private bool _summaryQueued;

        /// <summary>Muestra un mensaje en la barra de estado, si la ventana sigue viva.</summary>
        /// <remarks>
        /// La etiqueta se estira para ocupar el hueco libre y recorta lo que no cabe, así que el
        /// mensaje completo se repite en su tooltip: las rutas largas de los avisos de recorte
        /// suelen ser justo la parte que queda fuera.
        /// </remarks>
        private void SetStatus(string message)
        {
            if (!IsAlive)
            {
                return;
            }

            lblStatus.ToolTipText = message;

            // Mientras la barra muestra la descripción de una opción, el mensaje nuevo espera a
            // que el ratón la deje; de lo contrario, al salir se restauraría uno ya obsoleto.
            if (_statusBeforeHint is not null)
            {
                _statusBeforeHint = message;
                return;
            }

            lblStatus.Text = message;
        }

        /// <summary>Hace visible la barra de progreso y el contador para un lote de <paramref name="total"/> pistas.</summary>
        /// <remarks>
        /// Con una sola pista la barra no avanzaría hasta el final —pasaría de vacía a llena—, así
        /// que se anima en modo marquesina para que se note que hay trabajo en marcha.
        /// </remarks>
        private void StartProgress(int total, string message)
        {
            progressBar.Style = total == 1 ? ProgressBarStyle.Marquee : ProgressBarStyle.Blocks;
            progressBar.Maximum = Math.Max(1, total);
            progressBar.Value = 0;
            progressBar.Visible = true;

            lblProgress.Visible = total > 1;
            UpdateProgressCounter(0);

            SetStatus(message);
        }

        private void AdvanceProgress(int completed)
        {
            progressBar.Value = Math.Min(completed, progressBar.Maximum);
            UpdateProgressCounter(progressBar.Value);
        }

        /// <summary>
        /// Oculta el progreso al terminar. Una barra que se queda llena tras el lote no dice nada
        /// que no diga ya el mensaje, y confunde cuando se vuelve a la ventana más tarde.
        /// </summary>
        private void EndProgress()
        {
            progressBar.Visible = false;
            lblProgress.Visible = false;
            progressBar.Style = ProgressBarStyle.Blocks;
        }

        private void UpdateProgressCounter(int completed) =>
            lblProgress.Text = $"{completed} / {progressBar.Maximum}";

        /// <summary>Encola un recálculo del resumen de la lista, coalesciendo los avisos en ráfaga.</summary>
        private void QueueSummaryRefresh()
        {
            if (_summaryQueued || !IsHandleCreated || !IsAlive)
            {
                return;
            }

            _summaryQueued = true;
            BeginInvoke(() =>
            {
                _summaryQueued = false;

                if (IsAlive)
                {
                    RefreshSummary();
                }
            });
        }

        /// <summary>
        /// Recuenta la lista y actualiza las etiquetas del resumen.
        /// </summary>
        /// <remarks>
        /// Cada cifra lleva el mismo glifo y el mismo color que la columna de acción y el estado de
        /// la fila, para que el resumen se lea con el vocabulario de la rejilla. El color nunca va
        /// solo: el glifo y la palabra dicen lo mismo a quien no distinga el tono.
        /// Los errores solo aparecen cuando los hay; un «0 errores» permanente es ruido.
        /// </remarks>
        private void RefreshSummary()
        {
            int total = _songs.Count;
            int trimmable = 0;
            int trimmed = 0;
            int failed = 0;

            foreach (Song song in _songs)
            {
                switch (song.Estatus)
                {
                    case Song.StatusTrimmed:
                        trimmed++;
                        break;

                    case Song.StatusError:
                        failed++;
                        break;

                    default:
                        if (song.ShouldTrim)
                        {
                            trimmable++;
                        }

                        break;
                }
            }

            // Con el filtro activo se dice cuántas se ven de cuántas hay: los lotes actúan sobre todas,
            // y es lo único que delata que hay filas ocultas que también se procesarán.
            string filter = FilterKey;
            int visible = filter.Length == 0 ? total : _songs.Count(song => MatchesFilter(song, filter));

            lblTotal.Text = visible == total
                ? (total == 1 ? "1 pista" : $"{total} pistas")
                : $"{visible} de {total} pistas";
            lblTotal.ToolTipText = visible == total
                ? "Pistas cargadas en la lista"
                : "Pistas que pasan el filtro de las cargadas; los lotes actúan sobre todas";

            lblTrimmable.Text = $"{SongPresentation.TrimGlyph} {trimmable} {(trimmable == 1 ? "recortable" : "recortables")}";
            lblTrimmable.ForeColor = trimmable > 0 ? SongPresentation.Trimmable : SongPresentation.Inconclusive;

            lblTrimmed.Text = $"{SongPresentation.TrimmedGlyph} {trimmed} {(trimmed == 1 ? "recortada" : "recortadas")}";
            lblTrimmed.ForeColor = trimmed > 0 ? SongPresentation.Trimmed : SongPresentation.Inconclusive;

            lblErrors.Text = $"⚠ {failed} {(failed == 1 ? "error" : "errores")}";
            lblErrors.ForeColor = SongPresentation.Failed;
            lblErrors.Visible = failed > 0;

            // La guía de lista vacía se pinta sobre la rejilla, que no se repinta sola al quitar la
            // última fila.
            if (total == 0)
            {
                dataGrid.Invalidate();
            }
        }
    }
}
