using EchoCut.Audio;
using EchoCut.Export;
using EchoCut.Library;
using EchoCut.Objects;
using EchoCut.Options;
using EchoCut.Playback;
using EchoCut.Processing;
using EchoCut.Shell;
using System.ComponentModel;

namespace EchoCut
{
    /// <summary>Ventana principal de EchoCut: escaneo de carpeta, análisis, recorte y exportación.</summary>
    /// <remarks>
    /// Es el único formulario de la aplicación y, desde la separación del núcleo, su única
    /// responsabilidad es de presentación: recoge lo que el usuario pide, se lo encarga a
    /// <see cref="AnalysisService"/> y <see cref="TrimService"/>, y vuelca de vuelta a la rejilla
    /// lo que esos servicios van notificando. El algoritmo, FFmpeg y el paralelismo viven en
    /// EchoCut.Core y este archivo no sabe nada de ellos.
    /// </remarks>
    public partial class Main : Form
    {
        private readonly AppSettings _settings = AppSettings.Load();
        private readonly FFmpegLocator _locator = new();
        private readonly SortableBindingList<Song> _songs = new();

        /// <summary>Fila de cada pista, por ruta. Es cómo se vuelve del aviso del lote a la rejilla.</summary>
        private readonly Dictionary<string, Song> _rows = new(StringComparer.OrdinalIgnoreCase);

        private readonly AnalysisService _analysis;
        private readonly TrimService _trimmer;

        private CancellationTokenSource? _analysisCts;
        private CancellationTokenSource? _trimCts;
        private CancellationTokenSource? _cleanCts;
        private CancellationTokenSource? _normalizeCts;

        /// <summary>Reproducción de previsualización, creada al primer uso porque necesita FFmpeg.</summary>
        private AudioPreviewPlayer? _preview;
        private CancellationTokenSource? _previewCts;
        private System.Windows.Forms.Timer? _previewTimer;
        private Song? _playingSong;

        /// <summary>Carpetas escaneadas, origen de las pistas cargadas.</summary>
        private readonly List<string> _sourceDirectories = [];
        private readonly ToolTip _pathToolTip = new();

        /// <summary>
        /// Lote en curso. Cerrar la ventana debe esperarlo: cancelar sin esperar deja procesos de
        /// ffmpeg.exe huérfanos y acciones ya encoladas que se ejecutarían sobre controles destruidos.
        /// </summary>
        private Task? _running;

        /// <summary>Inicializa la ventana, restaura los parámetros guardados y enlaza la rejilla.</summary>
        public Main()
        {
            InitializeComponent();

            _analysis = new AnalysisService(_locator);
            _trimmer = new TrimService(_locator);

            numericTolerance.Value = Clamp(numericTolerance, _settings.Silence.ToleranceSeconds);

            numericThreads.Value = Clamp(
                numericThreads,
                _settings.ThreadCount > 0 ? _settings.ThreadCount : AppSettings.DefaultThreadCount);

            dataGrid.DefaultCellStyle.SelectionBackColor = SongPresentation.Selection;
            dataGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGrid.DataSource = _songs;

            _songs.ListChanged += Songs_ListChanged;

            UpdateButtons();
        }

        /// <summary>
        /// Fuerza el repintado de toda la fila cuando cambia <see cref="Song.Estatus"/>.
        /// </summary>
        /// <remarks>
        /// El color de texto de cada columna depende del estado, pero <c>ListChanged</c> solo marca
        /// como sucia la celda de la propiedad que cambió: sin este repintado explícito, el resto de
        /// la fila se queda pintada con el color del estado anterior hasta que algo más —como
        /// seleccionarla— fuerza un repintado completo.
        /// </remarks>
        private void Songs_ListChanged(object? sender, ListChangedEventArgs e)
        {
            if (!IsAlive
                || e.ListChangedType != ListChangedType.ItemChanged
                || e.PropertyDescriptor?.Name != nameof(Song.Estatus)
                || e.NewIndex < 0 || e.NewIndex >= dataGrid.Rows.Count)
            {
                return;
            }

            dataGrid.InvalidateRow(e.NewIndex);
        }

        private bool IsBusy => _analysisCts is not null || _trimCts is not null || _cleanCts is not null || _normalizeCts is not null;

        /// <summary>
        /// Si la ventana sigue viva. Un lote que ya había terminado hace que la espera del cierre
        /// continúe de forma síncrona y destruya el formulario antes de que corra el <c>finally</c>
        /// del manejador del lote, que entonces tocaría controles ya liberados.
        /// </summary>
        private bool IsAlive => !IsDisposed && !Disposing;

        // ---------------------------------------------------------------- Selección de carpetas

        private void btnPath_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            string[] selected = folderBrowserDialog.SelectedPaths;
            IReadOnlyList<string> selectedPaths = selected.Length > 0
                ? selected
                : string.IsNullOrWhiteSpace(folderBrowserDialog.SelectedPath)
                    ? []
                    : [folderBrowserDialog.SelectedPath];

            if (selectedPaths.Count > 0)
            {
                LoadDirectories(selectedPaths);
            }
        }

        private void btnFile_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            string[] selected = openFileDialog.FileNames;
            IReadOnlyList<string> selectedFiles = selected.Length > 0
                ? selected
                : string.IsNullOrWhiteSpace(openFileDialog.FileName)
                    ? []
                    : [openFileDialog.FileName];

            if (selectedFiles.Count > 0)
            {
                LoadFiles(selectedFiles);
            }
        }

        private void Main_DragEnter(object sender, DragEventArgs e)
        {
            if (!IsBusy && e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void Main_DragDrop(object sender, DragEventArgs e)
        {
            if (IsBusy || e.Data?.GetData(DataFormats.FileDrop) is not string[] dropped || dropped.Length == 0)
            {
                return;
            }

            List<string> directories = dropped
                .Where(Directory.Exists)
                .ToList();

            List<string> files = dropped
                .Where(File.Exists)
                .ToList();

            if (directories.Count > 0)
            {
                foreach (string file in files)
                {
                    string? dir = Path.GetDirectoryName(file);
                    if (dir is not null && !directories.Contains(dir, StringComparer.OrdinalIgnoreCase))
                    {
                        directories.Add(dir);
                    }
                }

                LoadDirectories(directories);
            }
            else if (files.Count > 0)
            {
                LoadFiles(files);
            }
        }

        /// <summary>
        /// Escanea y lista las pistas de audio de las carpetas indicadas, evitando duplicados.
        /// </summary>
        /// <param name="directories">Carpetas a escanear.</param>
        private void LoadDirectories(IReadOnlyList<string> directories)
        {
            StopPreview();

            _sourceDirectories.Clear();
            _sourceDirectories.AddRange(directories
                .Where(Directory.Exists)
                .Distinct(StringComparer.OrdinalIgnoreCase));

            if (_sourceDirectories.Count == 0)
            {
                return;
            }

            txtPath.Text = _sourceDirectories.Count switch
            {
                1 => _sourceDirectories[0],
                _ => string.Join("; ", _sourceDirectories)
            };
            _pathToolTip.SetToolTip(txtPath, string.Join(Environment.NewLine, _sourceDirectories));

            ScanResult scan = TrackScanner.Scan(_sourceDirectories);

            _songs.RaiseListChangedEvents = false;
            _songs.Clear();
            _rows.Clear();

            foreach (TrackInfo track in scan.Tracks)
            {
                if (_rows.ContainsKey(track.FilePath))
                {
                    continue;
                }

                Song song = new(track);
                _songs.Add(song);
                _rows[track.FilePath] = song;
            }

            _songs.RaiseListChangedEvents = true;
            _songs.ResetBindings();

            // Cargar otra selección de carpetas no debe dejar la cabecera marcada con un orden obsoleto.
            _songs.ReapplySort();

            string folderSummary = _sourceDirectories.Count == 1
                ? "1 carpeta"
                : $"{_sourceDirectories.Count} carpetas";

            lblStatus.Text = scan.SkippedCount == 0
                ? $"{_songs.Count} archivo(s) encontrados en {folderSummary}."
                : $"{_songs.Count} archivo(s) encontrados en {folderSummary}; {scan.SkippedCount} ilegible(s) omitido(s).";

            progressBar.Value = 0;
            UpdateButtons();
        }

        /// <summary>
        /// Carga y lista una colección de archivos de audio seleccionados individualmente.
        /// </summary>
        /// <param name="filePaths">Rutas de los archivos a cargar.</param>
        private void LoadFiles(IReadOnlyList<string> filePaths)
        {
            StopPreview();

            List<string> validFiles = filePaths
                .Where(File.Exists)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (validFiles.Count == 0)
            {
                return;
            }

            _sourceDirectories.Clear();
            _sourceDirectories.AddRange(validFiles
                .Select(Path.GetDirectoryName)
                .Where(d => !string.IsNullOrWhiteSpace(d))
                .Distinct(StringComparer.OrdinalIgnoreCase)!);

            txtPath.Text = _sourceDirectories.Count switch
            {
                0 => string.Empty,
                1 => _sourceDirectories[0],
                _ => string.Join("; ", _sourceDirectories)
            };
            _pathToolTip.SetToolTip(txtPath, string.Join(Environment.NewLine, _sourceDirectories));

            ScanResult scan = TrackScanner.ScanFiles(validFiles);

            _songs.RaiseListChangedEvents = false;
            _songs.Clear();
            _rows.Clear();

            foreach (TrackInfo track in scan.Tracks)
            {
                if (_rows.ContainsKey(track.FilePath))
                {
                    continue;
                }

                Song song = new(track);
                _songs.Add(song);
                _rows[track.FilePath] = song;
            }

            _songs.RaiseListChangedEvents = true;
            _songs.ResetBindings();

            // Cargar nueva lista de pistas no debe dejar la cabecera marcada con un orden obsoleto.
            _songs.ReapplySort();

            string originSummary = _sourceDirectories.Count == 1
                ? $"de la carpeta «{Path.GetFileName(_sourceDirectories[0])}»"
                : $"de {_sourceDirectories.Count} carpetas";

            lblStatus.Text = scan.SkippedCount == 0
                ? $"{_songs.Count} archivo(s) cargados {originSummary}."
                : $"{_songs.Count} archivo(s) cargados {originSummary}; {scan.SkippedCount} ilegible(s) o no compatible(s) omitido(s).";

            progressBar.Value = 0;
            UpdateButtons();
        }

        // ---------------------------------------------------------------------------- Análisis

        private async void btnAnalyze_Click(object sender, EventArgs e)
        {
            if (IsBusy || _songs.Count == 0 || !EnsureFFmpeg())
            {
                return;
            }

            StopPreview();

            List<TrackInfo> pending = _songs.Select(x => x.Track).ToList();

            using CancellationTokenSource cts = new();
            _analysisCts = cts;
            UpdateButtons();

            StartProgress(pending.Count, "Analizando…");

            try
            {
                _running = _analysis.AnalyzeAsync(
                    pending,
                    CurrentOptions(),
                    (int)numericThreads.Value,
                    CreateProgress<TrackAnalysis>(ApplyAnalysis),
                    cts.Token);

                await _running.ConfigureAwait(true);

                int trimmable = _songs.Count(x => x.ShouldTrim);
                SetStatus($"Análisis completado. {trimmable} de {_songs.Count} con cola recortable.");
            }
            catch (OperationCanceledException)
            {
                SetStatus("Análisis cancelado.");
            }
            catch (Exception exception)
            {
                ShowError("No se pudo completar el análisis.", exception);
            }
            finally
            {
                _analysisCts = null;
                _running = null;

                if (IsAlive)
                {
                    EndProgress();
                    UpdateButtons();
                }
            }
        }

        /// <summary>Vuelca a la rejilla lo que el servicio de análisis va notificando.</summary>
        private void ApplyAnalysis(TrackProgress<TrackAnalysis> progress)
        {
            if (RowFor(progress.Track) is not { } song)
            {
                return;
            }

            switch (progress.State)
            {
                case TrackState.Running:
                    song.Estatus = Song.StatusAnalyzing;
                    break;

                case TrackState.Completed when progress.Result is { } analysis:
                    song.Complete(analysis);
                    break;

                case TrackState.Failed when progress.Error is { } error:
                    song.Fail(error);
                    break;

                case TrackState.Cancelled:
                    song.Estatus = Song.StatusCancelled;
                    break;
            }

            AdvanceProgress(progress.CompletedCount);
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            _analysisCts?.Cancel();
            _cleanCts?.Cancel();
            _normalizeCts?.Cancel();
        }

        // ------------------------------------------------------------------ Acciones de fila

        /// <summary>
        /// Atiende la pulsación de las columnas de acción.
        /// </summary>
        /// <remarks>
        /// <c>CellContentClick</c> y no <c>CellClick</c>: en una columna de botones solo se dispara
        /// al pulsar el botón, no al seleccionar la fila, así que elegir una pista para verla no
        /// desencadena nada.
        /// </remarks>
        private async void dataGrid_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || IsBusy)
            {
                return;
            }

            if (dataGrid.Rows[e.RowIndex].DataBoundItem is not Song song)
            {
                return;
            }

            string columnName = dataGrid.Columns[e.ColumnIndex].Name;

            // El manejador es async void: una excepción que escape de aquí no tiene quién la
            // recoja y termina el proceso. Las rutas internas ya informan de sus propios fallos;
            // esto cubre lo que ocurra antes de entrar en ellas.
            try
            {
                if (columnName == PlayColumnName)
                {
                    await TogglePreviewAsync(song).ConfigureAwait(true);
                }
                else if (columnName == TrimColumnName && SongPresentation.CanTrim(song))
                {
                    await TrimSingleAsync(song).ConfigureAwait(true);
                }
            }
            catch (Exception exception)
            {
                ShowError("No se pudo completar la acción sobre la pista.", exception);
            }
        }

        private void dataGrid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
            {
                return;
            }

            string columnName = dataGrid.Columns[e.ColumnIndex].Name;
            if (columnName is PlayColumnName or TrimColumnName)
            {
                return;
            }

            if (dataGrid.Rows[e.RowIndex].DataBoundItem is not Song song)
            {
                return;
            }

            OpenSongsInAudacity([song]);
        }

        // ------------------------------------------------------------------- Previsualización

        /// <summary>Reproduce el final resultante de una pista, o lo detiene si ya está sonando.</summary>
        private async Task TogglePreviewAsync(Song song)
        {
            if (ReferenceEquals(song, _playingSong))
            {
                StopPreview();
                return;
            }

            if (!EnsureFFmpeg())
            {
                return;
            }

            StopPreview();

            PreviewWindow window = AudioPreview.WindowFor(
                song.DurationSeconds,
                song.CutSeconds,
                song.Silence,
                (double)numericTolerance.Value,
                _settings.Silence.PreviewSeconds);

            using CancellationTokenSource cts = new();
            _previewCts = cts;

            try
            {
                _preview ??= new AudioPreviewPlayer(_locator.Require().FFmpeg);
                SetStatus($"Preparando el final de {song.Name}…");

                TimeSpan duration = await _preview
                    .PlayAsync(song.FilePath, window, cts.Token)
                    .ConfigureAwait(true);

                if (!IsAlive || cts.IsCancellationRequested)
                {
                    return;
                }

                _playingSong = song;
                SetStatus($"Reproduciendo el final de {song.Name}…");

                // SoundPlayer no avisa de que terminó, así que el glifo se devuelve a su sitio con
                // un temporizador de la duración ya conocida.
                _previewTimer = new System.Windows.Forms.Timer
                {
                    Interval = (int)Math.Max(1, duration.TotalMilliseconds) + 100,
                };
                _previewTimer.Tick += (_, _) => StopPreview();
                _previewTimer.Start();

                RefreshPlayColumn();
            }
            catch (OperationCanceledException)
            {
                SetStatus("Previsualización cancelada.");
            }
            catch (Exception exception)
            {
                ShowError("No se pudo reproducir la previsualización.", exception);
            }
            finally
            {
                _previewCts = null;
            }
        }

        private void StopPreview()
        {
            _previewCts?.Cancel();

            if (_previewTimer is { } timer)
            {
                timer.Stop();
                timer.Dispose();
                _previewTimer = null;
            }

            _preview?.Stop();

            if (_playingSong is null)
            {
                return;
            }

            _playingSong = null;
            RefreshPlayColumn();
        }

        /// <summary>Repinta la columna de reproducción para que el glifo refleje qué está sonando.</summary>
        private void RefreshPlayColumn()
        {
            if (IsAlive && dataGrid.Columns[PlayColumnName] is { } column)
            {
                dataGrid.InvalidateColumn(column.Index);
            }
        }

        // ----------------------------------------------------------------------------- Recorte

        private async void btnCropAll_Click(object sender, EventArgs e)
        {
            if (IsBusy || !EnsureFFmpeg())
            {
                return;
            }

            StopPreview();

            List<TrimRequest> targets = _songs
                .Where(x => x.ShouldTrim && x.CutSeconds is not null)
                .Select(x => new TrimRequest(x.Track, x.CutSeconds!.Value))
                .ToList();

            if (targets.Count == 0)
            {
                MessageBox.Show(
                    this,
                    "No hay pistas con cola recortable. Ejecuta primero el análisis.",
                    "Nada que recortar",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            HashSet<string> targetFolders = targets
                .Select(x => TrimService.GetOutputDirectory(x.Track.Directory))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            string folderDetails = targetFolders.Count switch
            {
                1 => $"en la subcarpeta:\n{targetFolders.First()}",
                <= 5 => $"en las subcarpetas «{TrimService.OutputFolderName}» de cada carpeta:\n"
                        + string.Join(Environment.NewLine, targetFolders.Select(f => $" • {f}")),
                _ => $"en las subcarpetas «{TrimService.OutputFolderName}» de {targetFolders.Count} carpetas de origen:\n"
                     + string.Join(Environment.NewLine, targetFolders.Take(5).Select(f => $" • {f}"))
                     + $"{Environment.NewLine} • ... y {targetFolders.Count - 5} carpetas más."
            };

            if (MessageBox.Show(
                    this,
                    $"Se escribirán {targets.Count} archivo(s) recortado(s) {folderDetails}\n\n"
                    + "Los archivos originales no se modificarán.",
                    "Confirmar recorte dinámico",
                    MessageBoxButtons.OKCancel,
                    MessageBoxIcon.Question) != DialogResult.OK)
            {
                return;
            }

            using CancellationTokenSource cts = new();
            _trimCts = cts;
            UpdateButtons();

            StartProgress(targets.Count, "Recortando…");

            try
            {
                Task<BatchSummary> batch = _trimmer.TrimAsync(
                    targets,
                    (int)numericThreads.Value,
                    CreateProgress<TrimOutcome>(ApplyTrim),
                    cts.Token);

                _running = batch;
                BatchSummary summary = await batch.ConfigureAwait(true);

                SetStatus(summary.Failed == 0
                    ? $"Recorte completado: {summary.Total} archivo(s) en «{TrimService.OutputFolderName}» de {targetFolders.Count} carpeta(s)."
                    : $"Recorte completado con {summary.Failed} error(es) de {summary.Total}.");
            }
            catch (OperationCanceledException)
            {
                SetStatus("Recorte detenido.");
            }
            catch (Exception exception)
            {
                ShowError("No se pudo completar el recorte.", exception);
            }
            finally
            {
                _trimCts = null;
                _running = null;

                if (IsAlive)
                {
                    EndProgress();
                    UpdateButtons();
                }
            }
        }

        /// <summary>Vuelca a la rejilla lo que el servicio de recorte va notificando.</summary>
        private void ApplyTrim(TrackProgress<TrimOutcome> progress)
        {
            if (RowFor(progress.Track) is not { } song)
            {
                return;
            }

            song.Estatus = progress.State switch
            {
                TrackState.Running => Song.StatusTrimming,
                TrackState.Completed => Song.StatusTrimmed,
                TrackState.Cancelled => Song.StatusCancelled,
                _ => song.Estatus,
            };

            if (progress is { State: TrackState.Failed, Error: { } error })
            {
                song.Fail(error);
            }

            AdvanceProgress(progress.CompletedCount);
        }

        private void btnStop_Click(object sender, EventArgs e) => _trimCts?.Cancel();

        /// <summary>
        /// Recorta una sola pista, la de la fila pulsada.
        /// </summary>
        /// <remarks>
        /// Comparte el <see cref="_trimCts"/> del lote y se anota en <see cref="_running"/> aunque
        /// sea un solo archivo: así el botón «Detener» la alcanza y, sobre todo, cerrar la ventana
        /// espera a que termine. Un recorte que no pasara por ahí dejaría un ffmpeg.exe huérfano.
        /// Tampoco pasa por <see cref="CreateProgress{T}"/>: aquí no hay hilo de trabajo del que
        /// marshalar, y encolar la mutación la haría llegar después del «Recortado» que se fija
        /// tras el <c>await</c>, dejando la fila clavada en «Recortando…».
        /// </remarks>
        private async Task TrimSingleAsync(Song song)
        {
            if (IsBusy || !EnsureFFmpeg() || song.CutSeconds is not { } cut)
            {
                return;
            }

            // El archivo que se va a reescribir no puede estar sonando.
            if (ReferenceEquals(song, _playingSong))
            {
                StopPreview();
            }

            string originDir = song.Track.Directory is { Length: > 0 } directory
                ? directory
                : (_sourceDirectories.FirstOrDefault() ?? string.Empty);
            string outputDirectory = TrimService.GetOutputDirectory(originDir);

            using CancellationTokenSource cts = new();
            _trimCts = cts;
            UpdateButtons();

            StartProgress(1, $"Recortando {song.Name}…");
            song.Estatus = Song.StatusTrimming;

            try
            {
                Task<TrimOutcome> trim = _trimmer.TrimOneAsync(
                    new TrimRequest(song.Track, cut),
                    outputDirectory,
                    cts.Token);

                _running = trim;
                await trim.ConfigureAwait(true);

                song.Estatus = Song.StatusTrimmed;
                SetStatus($"Recortado en «{outputDirectory}»: {song.Name}.");
            }
            catch (OperationCanceledException)
            {
                song.Estatus = Song.StatusCancelled;
                SetStatus("Recorte detenido.");
            }
            catch (Exception exception)
            {
                song.Fail(exception);
                ShowError("No se pudo recortar la pista.", exception);
            }
            finally
            {
                _trimCts = null;
                _running = null;

                if (IsAlive)
                {
                    EndProgress();
                    UpdateButtons();
                }
            }
        }

        // --------------------------------------------------------------------------- Exportar

        private void btnExport_Click(object sender, EventArgs e)
        {
            if (_songs.Count == 0)
            {
                return;
            }

            saveFileDialog.FileName = $"echocut-{DateTime.Now:yyyyMMdd-HHmm}.csv";
            if (saveFileDialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            try
            {
                File.WriteAllText(
                    saveFileDialog.FileName,
                    CsvExporter.Build(_songs.Select(x => x.ToRecord())),
                    CsvExporter.Encoding);

                lblStatus.Text = $"Resultados exportados a {Path.GetFileName(saveFileDialog.FileName)}.";
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                ShowError("No se pudo escribir el archivo CSV.", exception);
            }
        }

        // ------------------------------------------------------------------- Opciones y estado

        private void btnAdvanced_Click(object sender, EventArgs e)
        {
            using AdvancedOptions dialog = new(_settings.Silence);
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                _settings.Silence = dialog.Result;
                _settings.Save();
                lblStatus.Text = "Parámetros avanzados actualizados.";
            }
        }

        /// <summary>Parámetros efectivos: los avanzados más la tolerancia de la ventana principal.</summary>
        private SilenceOptions CurrentOptions()
        {
            SilenceOptions options = _settings.Silence.Clone();
            options.ToleranceSeconds = (double)numericTolerance.Value;
            return options;
        }

        private void numericTolerance_ValueChanged(object sender, EventArgs e)
        {
            if (IsBusy)
            {
                return;
            }

            double tolerance = (double)numericTolerance.Value;
            _settings.Silence.ToleranceSeconds = tolerance;
            ApplyToleranceDynamically(tolerance);
        }

        /// <summary>
        /// Recalcula dinámicamente el recorte y la decisión de corte para todas las pistas ya analizadas
        /// en memoria, sin necesidad de redecodificar el audio con FFmpeg.
        /// </summary>
        private void ApplyToleranceDynamically(double tolerance)
        {
            if (_songs.Count == 0)
            {
                return;
            }

            bool anyUpdated = false;
            foreach (Song song in _songs)
            {
                if (song.Analysis is not { } analysis)
                {
                    continue;
                }

                double keptSeconds = tolerance + (analysis.FadeDetected ? _settings.Silence.FadeGuardSeconds : 0.0);
                double savedSeconds = Math.Max(0.0, analysis.SilenceSeconds - keptSeconds);
                double cutSeconds = Math.Clamp(analysis.DurationSeconds - savedSeconds, 0.0, analysis.DurationSeconds);
                bool shouldTrim = analysis.SilenceSeconds >= _settings.Silence.MinSilenceSeconds
                                  && savedSeconds >= _settings.Silence.MinSavingsSeconds
                                  && analysis.ReachesSilenceFloor;

                TrackAnalysis updated = analysis with
                {
                    CropSeconds = Math.Round(savedSeconds, 2),
                    CutSeconds = cutSeconds,
                    ShouldTrim = shouldTrim
                };

                song.Analysis = updated;
                song.Estatus = shouldTrim ? Song.StatusAnalyzed : Song.StatusNoSilence;
                anyUpdated = true;
            }

            if (anyUpdated)
            {
                int trimmable = _songs.Count(x => x.ShouldTrim);
                SetStatus($"Tolerancia actualizada a {tolerance:0.0} s. {trimmable} de {_songs.Count} con cola recortable.");
            }
        }

        /// <summary>Localiza la fila de una pista notificada por un lote.</summary>
        private Song? RowFor(TrackInfo track) =>
            _rows.TryGetValue(track.FilePath, out Song? song) ? song : null;

        /// <summary>
        /// Canal para devolver al hilo de interfaz los avisos que nacen en los hilos de trabajo.
        /// <see cref="Progress{T}"/> captura el contexto de sincronización en su construcción, así
        /// que debe crearse aquí, en el hilo de la interfaz, y no dentro del cuerpo paralelo.
        /// </summary>
        /// <remarks>
        /// Los avisos se encolan y se atienden más tarde, así que pueden llegar cuando el
        /// formulario ya se está destruyendo; tocar un control en ese momento lanza
        /// <see cref="ObjectDisposedException"/> sin nadie que la capture. Y una excepción que
        /// escape de aquí viaja por el bucle de mensajes y termina el proceso, de modo que un fallo
        /// al volcar el resultado de un solo archivo tumbaría el lote entero.
        /// </remarks>
        private IProgress<TrackProgress<T>> CreateProgress<T>(Action<TrackProgress<T>> apply)
            where T : class => new Progress<TrackProgress<T>>(progress =>
            {
                if (IsDisposed || Disposing)
                {
                    return;
                }

                try
                {
                    apply(progress);
                }
                catch (Exception exception)
                {
                    lblStatus.Text = $"Error al actualizar la interfaz: {exception.Message}";
                }
            });

        /// <summary>
        /// Resuelve FFmpeg desde la carpeta guardada o el PATH y, si no aparece, deja que el
        /// usuario la indique. No se distribuyen ni se descargan binarios.
        /// </summary>
        private bool EnsureFFmpeg()
        {
            if (_locator.IsResolved || _locator.TryResolve(_settings.FFmpegDirectory))
            {
                return true;
            }

            MessageBox.Show(
                this,
                "No se encontró FFmpeg en el PATH del sistema.\n\n"
                + "Indica a continuación la carpeta que contiene ffmpeg.exe y ffprobe.exe.",
                "FFmpeg no encontrado",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);

            using FolderBrowserDialog picker = new() { Description = "Carpeta de FFmpeg" };
            if (picker.ShowDialog(this) != DialogResult.OK)
            {
                return false;
            }

            if (!_locator.TryUseDirectory(picker.SelectedPath))
            {
                MessageBox.Show(
                    this,
                    "Esa carpeta no contiene ffmpeg.exe y ffprobe.exe.",
                    "FFmpeg no encontrado",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return false;
            }

            _settings.FFmpegDirectory = picker.SelectedPath;
            _settings.Save();
            return true;
        }

        private void StartProgress(int total, string message)
        {
            progressBar.Maximum = Math.Max(1, total);
            progressBar.Value = 0;
            lblStatus.Text = message;
        }

        private void AdvanceProgress(int completed) =>
            progressBar.Value = Math.Min(completed, progressBar.Maximum);

        private void EndProgress() => progressBar.Value = progressBar.Maximum;

        private void UpdateButtons()
        {
            bool analyzing = _analysisCts is not null;
            bool trimming = _trimCts is not null;
            bool cleaning = _cleanCts is not null;
            bool normalizing = _normalizeCts is not null;
            bool hasSongs = _songs.Count > 0;

            btnPath.Enabled = !IsBusy;
            btnFile.Enabled = !IsBusy;
            btnAnalyze.Enabled = !IsBusy && hasSongs;
            btnCancel.Enabled = analyzing || cleaning || normalizing;
            btnCropAll.Enabled = !IsBusy && hasSongs;
            btnStop.Enabled = trimming;
            btnExport.Enabled = !IsBusy && hasSongs;
            btnClean.Enabled = !IsBusy && hasSongs;
            btnNormalize.Enabled = !IsBusy && hasSongs;
            btnAdvanced.Enabled = !IsBusy;
            numericThreads.Enabled = !IsBusy;
            numericTolerance.Enabled = !IsBusy;
            mnuEditSong.Enabled = !IsBusy && hasSongs;
            mnuDeleteSong.Enabled = !IsBusy && hasSongs;
        }

        private void SetStatus(string message)
        {
            if (IsAlive)
            {
                lblStatus.Text = message;
            }
        }

        private void ShowError(string message, Exception exception)
        {
            if (!IsAlive)
            {
                return;
            }

            lblStatus.Text = message;
            MessageBox.Show(
                this,
                $"{message}\n\n{exception.Message}",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        private async void Main_FormClosing(object sender, FormClosingEventArgs e)
        {
            StopPreview();
            _preview?.Dispose();
            _preview = null;
            _pathToolTip.Dispose();

            _settings.Silence.ToleranceSeconds = (double)numericTolerance.Value;
            _settings.ThreadCount = (int)numericThreads.Value;
            _settings.Save();

            if (_running is not { } running)
            {
                return;
            }

            // Cancelar y cerrar sin esperar deja procesos de ffmpeg.exe huérfanos: la señal de
            // cancelación tarda en llegar al bucle de lectura que los mata. Se aborta el cierre,
            // se espera al lote y solo entonces se vuelve a cerrar.
            e.Cancel = true;
            _analysisCts?.Cancel();
            _trimCts?.Cancel();
            _cleanCts?.Cancel();
            _normalizeCts?.Cancel();
            SetStatus("Cerrando: esperando a que terminen las tareas en curso…");

            try
            {
                await running.ConfigureAwait(true);
            }
            catch (Exception)
            {
                // El cierre no debe fallar por cómo terminara el lote; los manejadores del lote ya
                // informaron del error en su propia ruta.
            }

            _running = null;
            Close();
        }

        private static decimal Clamp(NumericUpDown control, double value) =>
            Math.Clamp((decimal)value, control.Minimum, control.Maximum);

        // ------------------------------------------------------------------- Menú contextual

        private void dataGrid_CellMouseDown(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                // Si la fila pulsada con clic derecho no forma parte de la selección actual,
                // se restablece la selección a dicha fila; de lo contrario, se conserva la multiselección.
                if (!dataGrid.Rows[e.RowIndex].Selected)
                {
                    dataGrid.ClearSelection();
                    dataGrid.Rows[e.RowIndex].Selected = true;
                }

                dataGrid.CurrentCell = dataGrid.Rows[e.RowIndex].Cells[e.ColumnIndex];
                mnuEditSong.Enabled = !IsBusy && dataGrid.SelectedRows.Count == 1;
                mnuDeleteSong.Enabled = !IsBusy && dataGrid.SelectedRows.Count > 0;
            }
        }

        private List<Song> GetSelectedSongs() =>
            dataGrid.SelectedRows
                .Cast<DataGridViewRow>()
                .OrderBy(r => r.Index)
                .Select(r => r.DataBoundItem)
                .OfType<Song>()
                .ToList();

        private Song? GetSelectedSong() => GetSelectedSongs().FirstOrDefault();

        /// <summary>
        /// Abre una o varias pistas en Audacity para su inspección acústica.
        /// </summary>
        /// <param name="songs">Colección de pistas a abrir.</param>
        private void OpenSongsInAudacity(IReadOnlyList<Song> songs)
        {
            if (songs.Count == 0)
            {
                return;
            }

            if (ExternalApps.FindAudacity() is not { } audacityPath)
            {
                MessageBox.Show(
                    this,
                    "No se encontró Audacity en las rutas por defecto. Por favor, instálelo o verifique su ubicación.",
                    "Audacity no encontrado",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            try
            {
                ExternalApps.OpenInAudacity(audacityPath, songs.Select(s => s.FilePath));
            }
            catch (Exception exception)
            {
                ShowError("No se pudo abrir Audacity.", exception);
            }
        }

        private void mnuOpenFolder_Click(object? sender, EventArgs e)
        {
            if (GetSelectedSong() is not { } song)
            {
                return;
            }

            try
            {
                ExternalApps.RevealInExplorer(song.FilePath);
            }
            catch (Exception exception)
            {
                ShowError("No se pudo abrir la carpeta contenedora.", exception);
            }
        }

        private void mnuOpenAudacity_Click(object? sender, EventArgs e)
        {
            List<Song> songs = GetSelectedSongs();
            if (songs.Count > 0)
            {
                OpenSongsInAudacity(songs);
            }
        }

        private void mnuEditSong_Click(object? sender, EventArgs e)
        {
            if (IsBusy)
            {
                return;
            }

            if (GetSelectedSong() is not { } song)
            {
                return;
            }

            if (ReferenceEquals(song, _playingSong))
            {
                StopPreview();
            }

            using SongPropertiesDialog dialog = new(song.Track);
            dialog.TrackUpdated += (_, updatedTrack) =>
            {
                string oldPath = song.FilePath;
                string newPath = updatedTrack.FilePath;

                if (!string.Equals(oldPath, newPath, StringComparison.OrdinalIgnoreCase))
                {
                    _rows.Remove(oldPath);
                    _rows[newPath] = song;
                }

                song.UpdateTrack(updatedTrack);
                _songs.ReapplySort();

                SetStatus($"Propiedades actualizadas: {song.Name}.");
            };

            dialog.ShowDialog(this);
        }

        private void dataGrid_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                e.Handled = true;
                DeleteSelectedSongs();
            }
        }

        private void mnuDeleteSong_Click(object? sender, EventArgs e)
        {
            DeleteSelectedSongs();
        }

        /// <summary>
        /// Elimina permanentemente del disco y retira de la lista las canciones actualmente seleccionadas.
        /// </summary>
        private void DeleteSelectedSongs()
        {
            if (IsBusy)
            {
                return;
            }

            List<Song> selectedSongs = GetSelectedSongs();
            if (selectedSongs.Count == 0)
            {
                return;
            }

            string confirmationMessage = selectedSongs.Count == 1
                ? $"¿Está seguro de que desea eliminar permanentemente «{selectedSongs[0].Name}» del disco?"
                : $"¿Está seguro de que desea eliminar permanentemente estas {selectedSongs.Count} canciones del disco?";

            DialogResult result = MessageBox.Show(
                this,
                confirmationMessage,
                "Confirmar eliminación",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);

            if (result != DialogResult.Yes)
            {
                return;
            }

            if (_playingSong is not null && selectedSongs.Any(s => ReferenceEquals(s, _playingSong)))
            {
                StopPreview();
            }

            List<Song> successfullyDeleted = [];
            List<string> errors = [];

            foreach (Song song in selectedSongs)
            {
                try
                {
                    if (File.Exists(song.FilePath))
                    {
                        File.Delete(song.FilePath);
                    }

                    successfullyDeleted.Add(song);
                }
                catch (Exception exception)
                {
                    errors.Add($"{song.Name}: {exception.Message}");
                }
            }

            foreach (Song song in successfullyDeleted)
            {
                _rows.Remove(song.FilePath);
                _songs.Remove(song);
            }

            UpdateButtons();

            if (successfullyDeleted.Count == 1)
            {
                SetStatus($"Canción eliminada del disco: {successfullyDeleted[0].Name}.");
            }
            else if (successfullyDeleted.Count > 1)
            {
                SetStatus($"Se eliminaron {successfullyDeleted.Count} canciones del disco.");
            }

            if (errors.Count > 0)
            {
                MessageBox.Show(
                    this,
                    $"No se pudieron eliminar algunos archivos:\n\n{string.Join(Environment.NewLine, errors)}",
                    "Error al eliminar archivos",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private async void btnClean_Click(object sender, EventArgs e)
        {
            if (IsBusy || _songs.Count == 0)
            {
                return;
            }

            int count = _songs.Count;
            string confirmationMessage = count == 1
                ? $"¿Está seguro de que desea eliminar todos los metadatos de «{_songs[0].Name}»?\n\nEsta operación modificará el archivo directamente en el disco."
                : $"¿Está seguro de que desea eliminar todos los metadatos de las {count} canciones cargadas en la lista?\n\nEsta operación modificará los archivos directamente en el disco.";

            DialogResult confirmation = MessageBox.Show(
                this,
                confirmationMessage,
                "Confirmar limpieza de metadatos",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);

            if (confirmation != DialogResult.Yes)
            {
                return;
            }

            StopPreview();

            List<Song> songsToClean = [.. _songs];
            using CancellationTokenSource cts = new();
            _cleanCts = cts;
            UpdateButtons();

            StartProgress(songsToClean.Count, "Limpiando metadatos…");

            int completed = 0;
            int successCount = 0;
            List<string> errors = [];
            int degreeOfParallelism = Math.Clamp((int)numericThreads.Value, 1, 8);

            try
            {
                _running = Task.Run(async () =>
                {
                    ParallelOptions parallelOptions = new()
                    {
                        MaxDegreeOfParallelism = degreeOfParallelism,
                        CancellationToken = cts.Token,
                    };

                    await Parallel.ForEachAsync(songsToClean, parallelOptions, (song, token) =>
                    {
                        token.ThrowIfCancellationRequested();

                        try
                        {
                            TrackInfo updated = TrackEditor.StripMetadata(song.FilePath);
                            if (IsAlive)
                            {
                                try
                                {
                                    BeginInvoke(() =>
                                    {
                                        if (IsAlive)
                                        {
                                            song.UpdateTrack(updated);
                                        }
                                    });
                                }
                                catch (InvalidOperationException)
                                {
                                }
                            }

                            Interlocked.Increment(ref successCount);
                        }
                        catch (Exception exception)
                        {
                            lock (errors)
                            {
                                errors.Add($"{song.Name}: {exception.Message}");
                            }
                        }
                        finally
                        {
                            int current = Interlocked.Increment(ref completed);
                            if (IsAlive)
                            {
                                try
                                {
                                    BeginInvoke(() =>
                                    {
                                        if (IsAlive)
                                        {
                                            AdvanceProgress(current);
                                        }
                                    });
                                }
                                catch (InvalidOperationException)
                                {
                                }
                            }
                        }

                        return ValueTask.CompletedTask;
                    }).ConfigureAwait(false);
                }, cts.Token);

                await _running.ConfigureAwait(true);

                if (IsAlive)
                {
                    _songs.ReapplySort();

                    if (successCount == 1)
                    {
                        SetStatus("Metadatos eliminados correctamente para 1 canción.");
                    }
                    else
                    {
                        SetStatus($"Metadatos eliminados correctamente en {successCount} de {songsToClean.Count} canciones.");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                SetStatus("Limpieza de metadatos cancelada.");
            }
            catch (Exception exception)
            {
                ShowError("No se pudo completar la limpieza de metadatos.", exception);
            }
            finally
            {
                _cleanCts = null;
                _running = null;

                if (IsAlive)
                {
                    EndProgress();
                    UpdateButtons();
                }
            }

            if (errors.Count > 0 && IsAlive)
            {
                MessageBox.Show(
                    this,
                    $"No se pudieron limpiar los metadatos de algunos archivos:\n\n{string.Join(Environment.NewLine, errors)}",
                    "Aviso de limpieza",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private async void btnNormalize_Click(object sender, EventArgs e)
        {
            if (IsBusy || _songs.Count == 0)
            {
                return;
            }

            int count = _songs.Count;
            string confirmationMessage = count == 1
                ? $"¿Está seguro de que desea normalizar «{_songs[0].Name}»?\n\nEsta operación removerá los acentos (conservando la letra «ñ») y convertirá las palabras a TitleCase tanto en los metadatos como en el nombre del archivo en disco."
                : $"¿Está seguro de que desea normalizar las {count} canciones cargadas en la lista?\n\nEsta operación removerá los acentos (conservando la letra «ñ») y convertirá las palabras a TitleCase tanto en los metadatos como en el nombre del archivo en disco.";

            DialogResult confirmation = MessageBox.Show(
                this,
                confirmationMessage,
                "Confirmar normalización de canciones",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);

            if (confirmation != DialogResult.Yes)
            {
                return;
            }

            StopPreview();

            List<Song> songsToNormalize = [.. _songs];
            using CancellationTokenSource cts = new();
            _normalizeCts = cts;
            UpdateButtons();

            StartProgress(songsToNormalize.Count, "Normalizando información de canciones…");

            int completed = 0;
            int successCount = 0;
            List<string> errors = [];
            int degreeOfParallelism = Math.Clamp((int)numericThreads.Value, 1, 8);

            try
            {
                _running = Task.Run(async () =>
                {
                    ParallelOptions parallelOptions = new()
                    {
                        MaxDegreeOfParallelism = degreeOfParallelism,
                        CancellationToken = cts.Token,
                    };

                    await Parallel.ForEachAsync(songsToNormalize, parallelOptions, (song, token) =>
                    {
                        token.ThrowIfCancellationRequested();

                        try
                        {
                            string oldPath = song.FilePath;
                            TrackInfo updated = TrackEditor.NormalizeTrack(oldPath);

                            if (IsAlive)
                            {
                                try
                                {
                                    BeginInvoke(() =>
                                    {
                                        if (IsAlive)
                                        {
                                            string newPath = updated.FilePath;
                                            if (!string.Equals(oldPath, newPath, StringComparison.Ordinal))
                                            {
                                                _rows.Remove(oldPath);
                                                _rows[newPath] = song;
                                            }

                                            song.UpdateTrack(updated);
                                        }
                                    });
                                }
                                catch (InvalidOperationException)
                                {
                                }
                            }

                            Interlocked.Increment(ref successCount);
                        }
                        catch (Exception exception)
                        {
                            lock (errors)
                            {
                                errors.Add($"{song.Name}: {exception.Message}");
                            }
                        }
                        finally
                        {
                            int current = Interlocked.Increment(ref completed);
                            if (IsAlive)
                            {
                                try
                                {
                                    BeginInvoke(() =>
                                    {
                                        if (IsAlive)
                                        {
                                            AdvanceProgress(current);
                                        }
                                    });
                                }
                                catch (InvalidOperationException)
                                {
                                }
                            }
                        }

                        return ValueTask.CompletedTask;
                    }).ConfigureAwait(false);
                }, cts.Token);

                await _running.ConfigureAwait(true);

                if (IsAlive)
                {
                    _songs.ReapplySort();

                    if (successCount == 1)
                    {
                        SetStatus("Normalización completada para 1 canción.");
                    }
                    else
                    {
                        SetStatus($"Normalización completada en {successCount} de {songsToNormalize.Count} canciones.");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                SetStatus("Normalización de canciones cancelada.");
            }
            catch (Exception exception)
            {
                ShowError("No se pudo completar la normalización de canciones.", exception);
            }
            finally
            {
                _normalizeCts = null;
                _running = null;

                if (IsAlive)
                {
                    EndProgress();
                    UpdateButtons();
                }
            }

            if (errors.Count > 0 && IsAlive)
            {
                MessageBox.Show(
                    this,
                    $"No se pudieron normalizar algunos archivos:\n\n{string.Join(Environment.NewLine, errors)}",
                    "Aviso de normalización",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }
    }
}
