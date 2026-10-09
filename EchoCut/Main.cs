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
        private readonly WaveformService _waveforms;

        private CancellationTokenSource? _analysisCts;
        private CancellationTokenSource? _trimCts;
        private CancellationTokenSource? _cleanCts;
        private CancellationTokenSource? _normalizeCts;
        private CancellationTokenSource? _tagCts;
        private CancellationTokenSource? _renameCts;

        /// <summary>Reproducción de previsualización, creada al primer uso porque necesita FFmpeg.</summary>
        private AudioPreviewPlayer? _preview;
        private CancellationTokenSource? _previewCts;
        private Song? _playingSong;

        /// <summary>Carpetas escaneadas, origen de las pistas cargadas.</summary>
        private readonly List<string> _sourceDirectories = [];

        /// <summary>Si se está arrastrando encima algo que se puede soltar; resalta la zona de arrastre.</summary>
        private bool _dropHighlighted;

        /// <summary>Fuente del título de la zona de arrastre, creada al primer repintado de la lista vacía.</summary>
        private Font? _dropZoneTitleFont;

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
            _waveforms = new WaveformService(_locator);

            numericTolerance.Value = Clamp(numericTolerance, _settings.Silence.ToleranceSeconds);

            numericThreads.Value = Clamp(
                numericThreads,
                _settings.ThreadCount > 0 ? _settings.ThreadCount : AppSettings.DefaultThreadCount);

            dataGrid.DefaultCellStyle.SelectionBackColor = SongPresentation.Selection;
            dataGrid.DataSource = _songs;

            _songs.ListChanged += Songs_ListChanged;

            // El submenú necesita algún elemento para mostrar su flecha y abrirse; al abrirse se
            // reconstruye con las columnas ya generadas.
            BuildColumnItems(mnuColumns.DropDownItems);
            Disposed += (_, _) => _dropZoneTitleFont?.Dispose();

            UpdateButtons();
            RefreshSummary();
        }

        /// <summary>
        /// Mantiene al día el resumen de la barra de estado y fuerza el repintado de toda la fila
        /// cuando cambia <see cref="Song.Estatus"/>.
        /// </summary>
        /// <remarks>
        /// El color de texto de cada columna depende del estado, pero <c>ListChanged</c> solo marca
        /// como sucia la celda de la propiedad que cambió: sin este repintado explícito, el resto de
        /// la fila se queda pintada con el color del estado anterior hasta que algo más —como
        /// seleccionarla— fuerza un repintado completo.
        /// </remarks>
        private void Songs_ListChanged(object? sender, ListChangedEventArgs e)
        {
            if (!IsAlive)
            {
                return;
            }

            string? property = e.PropertyDescriptor?.Name;

            // El resumen depende de cuántas filas hay y de su estado o recorte; el resto de cambios
            // (metadatos renombrados, por ejemplo) no lo alteran.
            if (e.ListChangedType is not ListChangedType.ItemChanged
                || property is nameof(Song.Estatus) or nameof(Song.Crop))
            {
                QueueSummaryRefresh();
            }

            if (e.ListChangedType == ListChangedType.ItemChanged
                && property == nameof(Song.Estatus)
                && e.NewIndex >= 0 && e.NewIndex < dataGrid.Rows.Count)
            {
                dataGrid.InvalidateRow(e.NewIndex);
            }

            // Normalizar o editar una pista puede cambiar su nombre y, con él, si pasa el filtro.
            if (e.ListChangedType == ListChangedType.ItemChanged && property == nameof(Song.Name))
            {
                ApplyFilter(e.NewIndex);
            }
        }

        private bool IsBusy => _analysisCts is not null || _trimCts is not null || _cleanCts is not null || _normalizeCts is not null || _tagCts is not null || _renameCts is not null;

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

        /// <remarks>
        /// La rejilla ocupa casi toda la ventana y cada control decide por sí mismo si acepta lo que
        /// se le suelta, así que comparte estos manejadores con el formulario: de lo contrario solo
        /// funcionaría soltar sobre la franja de la ruta.
        /// </remarks>
        private void Main_DragEnter(object? sender, DragEventArgs e)
        {
            bool accepted = !IsBusy && e.Data?.GetDataPresent(DataFormats.FileDrop) == true;
            e.Effect = accepted ? DragDropEffects.Copy : DragDropEffects.None;
            SetDropHighlight(accepted);
        }

        private void Main_DragLeave(object? sender, EventArgs e) => SetDropHighlight(false);

        private void SetDropHighlight(bool highlighted)
        {
            if (_dropHighlighted == highlighted)
            {
                return;
            }

            _dropHighlighted = highlighted;

            if (_songs.Count == 0)
            {
                dataGrid.Invalidate();
            }
        }

        private void Main_DragDrop(object? sender, DragEventArgs e)
        {
            SetDropHighlight(false);

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

            ShowSourceDirectories();

            ScanResult scan = TrackScanner.Scan(_sourceDirectories);

            AddTracks(scan.Tracks, replace: true);

            string folderSummary = _sourceDirectories.Count == 1
                ? "1 carpeta"
                : $"{_sourceDirectories.Count} carpetas";

            SetStatus(scan.SkippedCount == 0
                ? $"{_songs.Count} archivo(s) encontrados en {folderSummary}."
                : $"{_songs.Count} archivo(s) encontrados en {folderSummary}; {scan.SkippedCount} ilegible(s) omitido(s).");

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

            ShowSourceDirectories();

            ScanResult scan = TrackScanner.ScanFiles(validFiles);

            AddTracks(scan.Tracks, replace: true);

            string originSummary = _sourceDirectories.Count == 1
                ? $"de la carpeta «{Path.GetFileName(_sourceDirectories[0])}»"
                : $"de {_sourceDirectories.Count} carpetas";

            SetStatus(scan.SkippedCount == 0
                ? $"{_songs.Count} archivo(s) cargados {originSummary}."
                : $"{_songs.Count} archivo(s) cargados {originSummary}; {scan.SkippedCount} ilegible(s) o no compatible(s) omitido(s).");

            UpdateButtons();
        }

        /// <summary>Muestra en el campo «Origen» las carpetas de las pistas cargadas.</summary>
        private void ShowSourceDirectories()
        {
            txtPath.Text = _sourceDirectories.Count switch
            {
                0 => string.Empty,
                1 => _sourceDirectories[0],
                _ => string.Join("; ", _sourceDirectories)
            };
            toolTip.SetToolTip(txtPath, string.Join(Environment.NewLine, _sourceDirectories));
        }

        /// <summary>
        /// Añade a la rejilla las pistas indicadas que aún no estén en ella.
        /// </summary>
        /// <param name="tracks">Pistas escaneadas.</param>
        /// <param name="replace">Si se vacía antes el listado, como al abrir carpetas o archivos.</param>
        /// <returns>Cuántas pistas se añadieron.</returns>
        /// <remarks>
        /// Los avisos de la lista se suspenden mientras se añade: con cientos de pistas, repintar la
        /// rejilla por cada una tarda más que el propio escaneo. Al terminar se reaplica el orden,
        /// porque cargar no debe dejar la cabecera marcada con un orden obsoleto, y se ajustan las
        /// columnas a los nombres nuevos.
        /// </remarks>
        private int AddTracks(IEnumerable<TrackInfo> tracks, bool replace)
        {
            int added = 0;
            _songs.RaiseListChangedEvents = false;

            if (replace)
            {
                _songs.Clear();
                _rows.Clear();
            }

            foreach (TrackInfo track in tracks)
            {
                if (_rows.ContainsKey(track.FilePath))
                {
                    continue;
                }

                Song song = new(track);
                _songs.Add(song);
                _rows[track.FilePath] = song;
                added++;
            }

            _songs.RaiseListChangedEvents = true;
            _songs.ResetBindings();
            _songs.ReapplySort();
            AutoSizeColumns();

            return added;
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
                SetStatus($"Análisis completado. {trimmable} de {_songs.Count} con silencio recortable.");
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

        private void btnStop_Click(object sender, EventArgs e)
        {
            _analysisCts?.Cancel();
            _trimCts?.Cancel();
            _cleanCts?.Cancel();
            _normalizeCts?.Cancel();
            _tagCts?.Cancel();
            _renameCts?.Cancel();
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
                _preview ??= CreatePreviewPlayer();
                SetStatus($"Preparando el final de {song.Name}…");

                await _preview
                    .PlayAsync(song.FilePath, window, cts.Token)
                    .ConfigureAwait(true);

                if (!IsAlive || cts.IsCancellationRequested)
                {
                    return;
                }

                _playingSong = song;
                SetStatus($"Reproduciendo el final de {song.Name}…");
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

        /// <summary>
        /// Crea el reproductor en el hilo de la interfaz, que es donde avisará de que el tramo
        /// terminó: así el glifo vuelve a su sitio justo cuando deja de sonar.
        /// </summary>
        private AudioPreviewPlayer CreatePreviewPlayer()
        {
            AudioPreviewPlayer player = new(_locator.Require().FFmpeg);
            player.PlaybackCompleted += (_, _) => StopPreview();
            return player;
        }

        private void StopPreview()
        {
            _previewCts?.Cancel();
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
                .Where(x => x.ShouldTrim && x.TrimRange is not null)
                .Select(x => new TrimRequest(x.Track, x.TrimRange!.Value))
                .ToList();

            if (targets.Count == 0)
            {
                MessageBox.Show(
                    this,
                    "No hay pistas con silencio recortable. Ejecuta primero el análisis.",
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
            if (IsBusy || !EnsureFFmpeg() || song.TrimRange is not { } range)
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
                    new TrimRequest(song.Track, range),
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

                SetStatus($"Resultados exportados a {Path.GetFileName(saveFileDialog.FileName)}.");
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
                SetStatus("Parámetros avanzados actualizados.");

                // Activar o desactivar el principio, o cambiar sus mínimos, debe reflejarse ya en
                // las pistas analizadas en lugar de esperar a un nuevo análisis.
                if (!IsBusy)
                {
                    ApplyOptionsDynamically();
                }
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

            _settings.Silence.ToleranceSeconds = (double)numericTolerance.Value;
            ApplyOptionsDynamically();
        }

        /// <summary>
        /// Recalcula dinámicamente el recorte y la decisión de corte para todas las pistas ya analizadas
        /// en memoria, sin necesidad de redecodificar el audio con FFmpeg.
        /// </summary>
        private void ApplyOptionsDynamically()
        {
            if (_songs.Count == 0)
            {
                return;
            }

            SilenceOptions options = CurrentOptions();
            bool anyUpdated = false;
            foreach (Song song in _songs)
            {
                if (song.Analysis is not { } analysis)
                {
                    continue;
                }

                song.Complete(analysis.WithOptions(options));
                anyUpdated = true;
            }

            if (anyUpdated)
            {
                int trimmable = _songs.Count(x => x.ShouldTrim);
                SetStatus($"Tolerancia de {options.ToleranceSeconds:0.0} s. {trimmable} de {_songs.Count} con silencio recortable.");
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
                    SetStatus($"Error al actualizar la interfaz: {exception.Message}");
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

        private void UpdateButtons()
        {
            bool analyzing = _analysisCts is not null;
            bool trimming = _trimCts is not null;
            bool cleaning = _cleanCts is not null;
            bool normalizing = _normalizeCts is not null;
            bool tagging = _tagCts is not null;
            bool renaming = _renameCts is not null;
            bool hasSongs = _songs.Count > 0;

            btnPath.Enabled = !IsBusy;
            btnFile.Enabled = !IsBusy;
            btnAnalyze.Enabled = !IsBusy && hasSongs;
            btnCropAll.Enabled = !IsBusy && hasSongs;
            btnStop.Enabled = analyzing || cleaning || normalizing || tagging || renaming || trimming;
            btnExport.Enabled = !IsBusy && hasSongs;
            ddbUtilities.Enabled = !IsBusy;
            mnuMetadata.Enabled = hasSongs;
            mnuNormalize.Enabled = hasSongs;
            mnuRename.Enabled = hasSongs;
            btnAdvanced.Enabled = !IsBusy;
            numericThreads.Enabled = !IsBusy;
            numericTolerance.Enabled = !IsBusy;
            mnuEditSong.Enabled = !IsBusy && hasSongs;
            mnuRenameSong.Enabled = !IsBusy && hasSongs;
            mnuDeleteSong.Enabled = !IsBusy && hasSongs;
        }

        private void ShowError(string message, Exception exception)
        {
            if (!IsAlive)
            {
                return;
            }

            SetStatus(message);
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
            _tagCts?.Cancel();
            _renameCts?.Cancel();
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

        /// <summary>
        /// Atajos de teclado de la barra de herramientas, anunciados en el tooltip de cada botón.
        /// </summary>
        /// <remarks>
        /// <see cref="ToolStripButton"/> no admite <c>ShortcutKeys</c>, así que se resuelven aquí.
        /// Pasar por <see cref="ToolStripItem.PerformClick"/> respeta el estado habilitado: un atajo
        /// nunca dispara lo que el botón, deshabilitado, no permitiría. Esc solo se consume si hay
        /// algo que detener, para no robárselo a la edición de los controles numéricos.
        /// Ctrl+F lleva al filtro y, dentro de él, Esc lo vacía antes que detener un lote: es la
        /// acción más cercana a donde está el foco.
        /// </remarks>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.F))
            {
                txtFilter.Focus();
                txtFilter.SelectAll();
                return true;
            }

            if (keyData == Keys.Escape && txtFilter.Focused && txtFilter.TextLength > 0)
            {
                ClearFilter();
                return true;
            }

            ToolStripItem? target = keyData switch
            {
                Keys.Control | Keys.O => btnFile,
                Keys.Control | Keys.Shift | Keys.O => btnPath,
                Keys.F5 => btnAnalyze,
                Keys.Control | Keys.R => btnCropAll,
                Keys.Control | Keys.E => btnExport,
                Keys.Escape when btnStop.Enabled => btnStop,
                _ => null,
            };

            if (target is { Enabled: true })
            {
                target.PerformClick();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private static decimal Clamp(NumericUpDown control, double value) =>
            Math.Clamp((decimal)value, control.Minimum, control.Maximum);

        // ------------------------------------------------------------------- Menú contextual

        private void dataGrid_CellMouseDown(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                // Si la fila pulsada con clic derecho no forma parte de la selección actual, la
                // selección pasa a ser solo esa fila; si forma parte, se conserva la multiselección
                // para que el menú actúe sobre todas, como en el Explorador.
                DataGridViewCell cell = dataGrid.Rows[e.RowIndex].Cells[e.ColumnIndex];
                if (dataGrid.Rows[e.RowIndex].Selected)
                {
                    SetCurrentCellKeepingSelection(cell);
                }
                else
                {
                    dataGrid.ClearSelection();
                    dataGrid.CurrentCell = cell;
                }
                mnuEditSong.Enabled = !IsBusy && dataGrid.SelectedRows.Count == 1;
                mnuRenameSong.Enabled = !IsBusy && dataGrid.SelectedRows.Count == 1;
                mnuDeleteSong.Enabled = !IsBusy && dataGrid.SelectedRows.Count > 0;
            }
        }

        /// <summary>Pistas seleccionadas y visibles, en el orden de la rejilla.</summary>
        /// <remarks>
        /// Se descartan las filas ocultas por el filtro: una acción sobre la selección, como
        /// eliminar del disco, nunca debe alcanzar una pista que el usuario no está viendo.
        /// </remarks>
        private List<Song> GetSelectedSongs() =>
            dataGrid.SelectedRows
                .Cast<DataGridViewRow>()
                .Where(r => r.Visible)
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

        /// <summary>
        /// Abre la forma de onda de la pista seleccionada para ver sus silencios y corregir el
        /// recorte a mano.
        /// </summary>
        /// <remarks>
        /// Funciona también con pistas sin analizar: el tramo parte de la pista completa y el ajuste
        /// manual basta para recortarla.
        /// </remarks>
        private void mnuWaveform_Click(object? sender, EventArgs e)
        {
            if (IsBusy || GetSelectedSong() is not { } song || !EnsureFFmpeg())
            {
                return;
            }

            StopPreview();

            double duration = song.DurationSeconds;
            using WaveformEditor dialog = new(
                song.Track,
                duration,
                song.TrimRange ?? new TrimRange(0.0, duration),
                song.Analysis?.Range,
                song.ManualRange is not null,
                _waveforms,
                _locator.Require().FFmpeg,
                _settings.Silence.PreviewSeconds);

            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            song.AdjustManually(dialog.ManualRange);
            SetStatus(dialog.ManualRange is null
                ? $"{song.Name}: se usa el recorte del análisis."
                : $"{song.Name}: recorte ajustado a mano, {song.Crop:0.00} s a eliminar.");
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
            else if (e.KeyCode == Keys.F2)
            {
                e.Handled = true;
                BeginRename();
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
    }
}
