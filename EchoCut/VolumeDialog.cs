using EchoCut.Audio;
using EchoCut.Library;
using EchoCut.Loudness;
using EchoCut.Objects;
using EchoCut.Processing;
using System.Globalization;

namespace EchoCut
{
    /// <summary>
    /// Ventana «Regularizar volumen»: mide el volumen percibido de las canciones MP3, propone el
    /// ajuste para llevarlas a un mismo nivel, lo aplica sin recodificar y enseña cómo quedó cada una.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Nada se modifica hasta pulsar «Aplicar ajuste»: «Analizar» solo mide, y cambiar el objetivo
    /// recalcula las propuestas al instante sin volver a decodificar. Las operaciones actúan sobre
    /// las canciones marcadas.
    /// </para>
    /// <para>
    /// Sigue el patrón de los lotes de la ventana principal: un <see cref="CancellationTokenSource"/>
    /// propio, la barra de progreso por pista y un aviso final con lo que falló, sin que un archivo
    /// ilegible detenga a los demás. Cerrar con una operación en marcha la detiene y la espera.
    /// </para>
    /// </remarks>
    public partial class VolumeDialog : Form
    {
        private readonly FFmpegLocator _locator;
        private readonly int _parallelism;
        private readonly Dictionary<string, ListViewItem> _items = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, TrackInfo> _updated = new(StringComparer.OrdinalIgnoreCase);
        private CancellationTokenSource? _cts;
        private Task? _running;
        private bool _populating;
        private bool _buttonsQueued;
        private int _sortColumn = -1;
        private bool _sortDescending;

        /// <summary>Crea la ventana con las canciones a regularizar.</summary>
        /// <param name="tracks">Canciones MP3.</param>
        /// <param name="locator">Localizador de FFmpeg, ya resuelto.</param>
        /// <param name="parallelism">Archivos procesados a la vez.</param>
        /// <param name="targetDb">Objetivo propuesto al abrir; normalmente, el de la última vez.</param>
        public VolumeDialog(IReadOnlyList<TrackInfo> tracks, FFmpegLocator locator, int parallelism, double targetDb)
        {
            ArgumentNullException.ThrowIfNull(tracks);
            InitializeComponent();

            _locator = locator;
            _parallelism = parallelism;

            numTarget.Minimum = (decimal)GainPlanner.MinimumTargetDb;
            numTarget.Maximum = (decimal)GainPlanner.MaximumTargetDb;
            numTarget.Value = Math.Clamp((decimal)targetDb, numTarget.Minimum, numTarget.Maximum);
            btnDefault.Text = $"&Restablecer {GainPlanner.DefaultTargetDb:0} dB";

            Populate(tracks);
            SetStatus($"{Plural(tracks.Count, "canción MP3", "canciones MP3")}. Pulsa «Analizar» para medir su volumen; nada se modifica hasta aplicar el ajuste.");
            UpdateButtons();
        }

        /// <summary>Objetivo elegido, para recordarlo la próxima vez.</summary>
        /// <value>Decibelios en la escala de ReplayGain.</value>
        public double TargetDb => (double)numTarget.Value;

        /// <summary>Canciones cuyo archivo se modificó, releídas del disco, para actualizar sus filas.</summary>
        /// <value>Una por archivo, con su estado más reciente.</value>
        public IReadOnlyCollection<TrackInfo> UpdatedTracks => _updated.Values;

        private bool IsBusy => _cts is not null;

        private IEnumerable<ListViewItem> Items => lvwTracks.Items.OfType<ListViewItem>();

        private List<VolumeRow> CheckedRows(Func<VolumeRow, bool> predicate) =>
            [.. Items.Where(item => item.Checked).Select(RowOf).Where(predicate)];

        private static VolumeRow RowOf(ListViewItem item) => (VolumeRow)item.Tag!;

        // -------------------------------------------------------------------------- Lista

        private void Populate(IReadOnlyList<TrackInfo> tracks)
        {
            _populating = true;
            lvwTracks.BeginUpdate();
            try
            {
                foreach (TrackInfo track in tracks.OrderBy(track => track.Name, StringComparer.CurrentCultureIgnoreCase))
                {
                    ListViewItem item = new(track.Name) { Tag = new VolumeRow(track), Checked = true };
                    item.SubItems.AddRange(["", "", "", "", "", "", ""]);
                    Render(item);
                    lvwTracks.Items.Add(item);
                    _items[track.FilePath] = item;
                }
            }
            finally
            {
                lvwTracks.EndUpdate();
                _populating = false;
            }
        }

        /// <summary>Vuelca el estado de la fila en sus columnas y su color.</summary>
        private static void Render(ListViewItem item)
        {
            VolumeRow row = RowOf(item);
            item.Text = row.Track.Name;
            item.SubItems[1].Text = row.StatusText;
            item.SubItems[2].Text = row.VolumeText;
            item.SubItems[3].Text = row.AdjustmentText;
            item.SubItems[4].Text = row.ResultText;
            item.SubItems[5].Text = row.PeakText;
            item.SubItems[6].Text = row.ClippingText;
            item.SubItems[7].Text = row.AccumulatedText;
            item.ForeColor = row.ForeColor();
            item.ToolTipText = row.Error is { } error ? $"{row.Track.FilePath}{Environment.NewLine}{error}" : row.Track.FilePath;
        }

        private void RenderAll()
        {
            lvwTracks.BeginUpdate();
            try
            {
                foreach (ListViewItem item in Items)
                {
                    Render(item);
                }
            }
            finally
            {
                lvwTracks.EndUpdate();
            }
        }

        /// <summary>Encola la actualización de los botones en lugar de hacerla a mitad del aviso.</summary>
        /// <remarks>
        /// El control avisa de cada casilla también mientras crea su ventana nativa e inserta los
        /// elementos, con la colección todavía a medias; esperar a que termine el mensaje en curso
        /// evita leerla así y hace una sola cuenta cuando cambian muchas casillas seguidas.
        /// </remarks>
        private void lvwTracks_ItemChecked(object? sender, ItemCheckedEventArgs e)
        {
            if (_populating || _buttonsQueued || !IsHandleCreated)
            {
                return;
            }

            _buttonsQueued = true;
            BeginInvoke(() =>
            {
                _buttonsQueued = false;
                if (!IsDisposed)
                {
                    UpdateButtons();
                }
            });
        }

        private void btnCheckAll_Click(object? sender, EventArgs e) => CheckAll(true);

        private void btnCheckNone_Click(object? sender, EventArgs e) => CheckAll(false);

        private void CheckAll(bool value)
        {
            _populating = true;
            lvwTracks.BeginUpdate();
            try
            {
                foreach (ListViewItem item in Items)
                {
                    item.Checked = value;
                }
            }
            finally
            {
                lvwTracks.EndUpdate();
                _populating = false;
            }

            UpdateButtons();
        }

        private void lvwTracks_ColumnClick(object? sender, ColumnClickEventArgs e)
        {
            _sortDescending = e.Column == _sortColumn && !_sortDescending;
            _sortColumn = e.Column;
            lvwTracks.ListViewItemSorter = new ColumnComparer(_sortColumn, _sortDescending);
            lvwTracks.Sort();
        }

        // ------------------------------------------------------------------------ Objetivo

        private void numTarget_ValueChanged(object? sender, EventArgs e)
        {
            foreach (ListViewItem item in Items)
            {
                RowOf(item).Retarget(TargetDb);
            }

            RenderAll();
            UpdateButtons();
            if (!IsBusy)
            {
                SetStatus(Summary());
            }
        }

        private void btnDefault_Click(object? sender, EventArgs e) =>
            numTarget.Value = (decimal)GainPlanner.DefaultTargetDb;

        // ----------------------------------------------------------------------- Acciones

        private async void btnAnalyze_Click(object? sender, EventArgs e)
        {
            List<VolumeRow> rows = CheckedRows(_ => true);
            if (IsBusy || rows.Count == 0)
            {
                return;
            }

            List<TrackInfo> tracks = [.. rows.Select(row => row.Track)];
            await RunAsync<LoudnessMeasurement>(
                rows,
                "Midiendo…",
                "Midiendo el volumen…",
                (progress, token) => new LoudnessService(_locator).MeasureAsync(tracks, _parallelism, progress, token),
                (row, measurement) => row.Measure(measurement, TargetDb),
                _ => Summary(),
                "Medición detenida.",
                "No se pudo medir el volumen de algunas canciones:").ConfigureAwait(true);
        }

        private async void btnApply_Click(object? sender, EventArgs e)
        {
            List<VolumeRow> rows = CheckedRows(row => row.CanApply);
            if (IsBusy || rows.Count == 0)
            {
                return;
            }

            DialogResult confirmation = MessageBox.Show(
                this,
                $"¿Aplicar el ajuste a {Plural(rows.Count, "canción", "canciones")} para llevarlas a {TargetDb:0.#} dB?\n\n"
                + "Se modifican los archivos originales, sin recodificar ni perder calidad. "
                + "Puedes devolverlas a su volumen de siempre con «Restaurar original».",
                "Aplicar ajuste de volumen",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button1);

            if (confirmation != DialogResult.Yes)
            {
                return;
            }

            List<GainRequest> requests = [.. rows.Select(row => new GainRequest(row.Track, row.Plan!.AppliedSteps, row.Current!))];
            await RunAsync<GainChange>(
                rows,
                "Ajustando…",
                "Aplicando el ajuste de volumen…",
                (progress, token) => LoudnessService.ApplyAsync(requests, _parallelism, progress, token),
                (row, change) =>
                {
                    row.Apply(change, TargetDb);
                    Remember(change);
                },
                summary => $"Ajuste aplicado a {Plural(summary.Succeeded, "canción", "canciones")} de {summary.Total}. {Summary()}",
                "Ajuste detenido; las canciones ya procesadas conservan el cambio.",
                "No se pudo ajustar el volumen de algunas canciones:").ConfigureAwait(true);
        }

        private async void btnRestore_Click(object? sender, EventArgs e)
        {
            List<VolumeRow> rows = CheckedRows(row => row.CanRestore);
            if (IsBusy || rows.Count == 0)
            {
                return;
            }

            DialogResult confirmation = MessageBox.Show(
                this,
                $"¿Devolver {Plural(rows.Count, "canción", "canciones")} al volumen que tenían antes de cualquier ajuste?\n\n"
                + "Se modifican los archivos originales, sin recodificar. Las que no tengan un ajuste guardado se quedan como están.",
                "Restaurar volumen original",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);

            if (confirmation != DialogResult.Yes)
            {
                return;
            }

            int restored = 0;
            List<TrackInfo> tracks = [.. rows.Select(row => row.Track)];
            await RunAsync<GainChange>(
                rows,
                "Restaurando…",
                "Restaurando el volumen original…",
                (progress, token) => LoudnessService.RestoreAsync(tracks, _parallelism, progress, token),
                (row, change) =>
                {
                    row.Restore(change, TargetDb);
                    Remember(change);
                    restored += change.Steps == 0 ? 0 : 1;
                },
                summary => $"Volumen original restaurado en {Plural(restored, "canción", "canciones")}; {summary.Succeeded - restored} no {(summary.Succeeded - restored == 1 ? "tenía" : "tenían")} ajustes guardados.",
                "Restauración detenida; las canciones ya procesadas recuperaron su volumen.",
                "No se pudo restaurar el volumen de algunas canciones:").ConfigureAwait(true);
        }

        private void btnStop_Click(object? sender, EventArgs e) => _cts?.Cancel();

        /// <summary>Esqueleto común de medir, aplicar y restaurar.</summary>
        /// <typeparam name="T">Resultado por canción.</typeparam>
        /// <param name="rows">Filas afectadas.</param>
        /// <param name="workingText">Estado de cada fila mientras se procesa.</param>
        /// <param name="progressText">Mensaje mientras el lote está en marcha.</param>
        /// <param name="start">Lanza el lote con el canal de avisos y el token.</param>
        /// <param name="completed">Lo que se hace con la fila cuando su canción termina bien.</param>
        /// <param name="done">Mensaje final a partir del recuento.</param>
        /// <param name="cancelledText">Mensaje si se detiene.</param>
        /// <param name="failureIntro">Primera línea del aviso con las canciones que fallaron.</param>
        private async Task RunAsync<T>(
            List<VolumeRow> rows,
            string workingText,
            string progressText,
            Func<IProgress<TrackProgress<T>>, CancellationToken, Task<BatchSummary>> start,
            Action<VolumeRow, T> completed,
            Func<BatchSummary, string> done,
            string cancelledText,
            string failureIntro)
            where T : class
        {
            foreach (VolumeRow row in rows)
            {
                row.StartWork(workingText);
            }

            RenderAll();

            using CancellationTokenSource cts = new();
            _cts = cts;
            UpdateButtons();
            progressBar.Maximum = Math.Max(rows.Count, 1);
            progressBar.Value = 0;
            progressBar.Visible = true;
            SetStatus(progressText);

            List<string> errors = [];
            Progress<TrackProgress<T>> progress = new(update =>
            {
                if (IsDisposed || !_items.TryGetValue(update.Track.FilePath, out ListViewItem? item))
                {
                    return;
                }

                VolumeRow row = RowOf(item);
                switch (update)
                {
                    case { State: TrackState.Completed, Result: { } result }:
                        completed(row, result);
                        break;

                    case { State: TrackState.Failed, Error: { } error }:
                        row.Fail(error.Message);
                        errors.Add($"{row.Track.Name}: {error.Message}");
                        break;

                    case { State: TrackState.Cancelled }:
                        row.Cancel();
                        break;
                }

                Render(item);
                progressBar.Value = Math.Min(update.CompletedCount, progressBar.Maximum);
            });

            try
            {
                Task<BatchSummary> batch = start(progress, cts.Token);
                _running = batch;
                BatchSummary summary = await batch.ConfigureAwait(true);
                SetStatus(done(summary));
            }
            catch (OperationCanceledException)
            {
                SetStatus(cancelledText);
            }
            catch (Exception exception)
            {
                SetStatus(exception.Message);
                errors.Add(exception.Message);
            }
            finally
            {
                _cts = null;
                _running = null;
                if (!IsDisposed)
                {
                    foreach (VolumeRow row in rows)
                    {
                        row.Cancel();
                    }

                    RenderAll();
                    progressBar.Visible = false;
                    UpdateButtons();
                }
            }

            if (errors.Count > 0 && !IsDisposed && Visible)
            {
                MessageBox.Show(this, $"{failureIntro}\n\n{string.Join(Environment.NewLine, errors)}", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>Anota la canción releída si su archivo cambió.</summary>
        private void Remember(GainChange change)
        {
            if (change.Steps != 0)
            {
                _updated[change.Track.FilePath] = change.Track;
            }
        }

        // ------------------------------------------------------------------------- Estado

        private void UpdateButtons()
        {
            bool busy = IsBusy;
            List<VolumeRow> marked = CheckedRows(_ => true);

            btnAnalyze.Enabled = !busy && marked.Count > 0;
            btnApply.Enabled = !busy && marked.Any(row => row.CanApply);
            btnRestore.Enabled = !busy && marked.Any(row => row.CanRestore);
            btnStop.Enabled = busy;
            btnCheckAll.Enabled = !busy;
            btnCheckNone.Enabled = !busy;
            numTarget.Enabled = !busy;
            btnDefault.Enabled = !busy;
        }

        /// <summary>Recuento de la lista para la barra de estado.</summary>
        private string Summary()
        {
            List<VolumeRow> rows = [.. Items.Select(RowOf)];
            int measured = rows.Count(row => row.Current is not null);
            if (measured == 0)
            {
                return $"{Plural(rows.Count, "canción", "canciones")} sin medir.";
            }

            int pending = rows.Count(row => row.CanApply);
            int clipping = rows.Count(row => row.ClippingText.StartsWith('⚠'));
            int limited = rows.Count(row => row.ClippingText.StartsWith('▲'));

            string text = $"{measured} de {rows.Count} medidas · {pending} por ajustar a {TargetDb:0.#} dB";
            if (limited > 0)
            {
                text += $" · {limited} con la subida limitada para no saturar";
            }

            if (clipping > 0)
            {
                text += $" · {clipping} saturan";
            }

            return text + ".";
        }

        private void SetStatus(string text) => lblStatus.Text = text;

        private static string Plural(int value, string singular, string plural) =>
            value == 1 ? $"1 {singular}" : $"{value} {plural}";

        // ------------------------------------------------------------------ Teclado y cierre

        private void VolumeDialog_KeyDown(object? sender, KeyEventArgs e)
        {
            switch (e.KeyData)
            {
                case Keys.F5 when btnAnalyze.Enabled:
                    btnAnalyze.PerformClick();
                    break;

                case Keys.Control | Keys.Enter when btnApply.Enabled:
                    btnApply.PerformClick();
                    break;

                case Keys.Escape when IsBusy:
                    // Con una operación en marcha, Esc la detiene en lugar de cerrar la ventana.
                    _cts?.Cancel();
                    break;

                default:
                    return;
            }

            e.Handled = true;
            e.SuppressKeyPress = true;
        }

        /// <summary>Cerrar con una operación en marcha la detiene y espera a que termine.</summary>
        /// <remarks>
        /// Cerrar sin esperar dejaría procesos de FFmpeg huérfanos y avisos encolados que tocarían
        /// controles ya destruidos. Ningún archivo queda a medias: cada uno se reescribe en una copia
        /// que solo sustituye al original al terminar.
        /// </remarks>
        private async void VolumeDialog_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (_running is not { } running)
            {
                return;
            }

            e.Cancel = true;
            _cts?.Cancel();
            SetStatus("Cerrando: esperando a que termine la operación en curso…");

            try
            {
                await running.ConfigureAwait(true);
            }
            catch (Exception)
            {
                // El desenlace ya lo atendió RunAsync; aquí solo importa que haya terminado.
            }

            if (!IsDisposed)
            {
                Close();
            }
        }

        /// <summary>Ordena por una columna: numérica si su texto empieza por un número, alfabética si no.</summary>
        private sealed class ColumnComparer(int column, bool descending) : System.Collections.IComparer
        {
            public int Compare(object? x, object? y)
            {
                string left = ((ListViewItem)x!).SubItems[column].Text;
                string right = ((ListViewItem)y!).SubItems[column].Text;

                int result = (Number(left), Number(right)) switch
                {
                    ({ } a, { } b) => a.CompareTo(b),
                    (null, { }) => 1,
                    ({ }, null) => -1,
                    _ => string.Compare(left, right, StringComparison.CurrentCultureIgnoreCase),
                };

                return descending ? -result : result;
            }

            private static double? Number(string text)
            {
                int end = 0;
                while (end < text.Length && (char.IsDigit(text[end]) || text[end] is '+' or '-' or '.' or ','))
                {
                    end++;
                }

                return end > 0 && double.TryParse(text.AsSpan(0, end), NumberStyles.Float, CultureInfo.CurrentCulture, out double value)
                    ? value
                    : null;
            }
        }
    }
}
