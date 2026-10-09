using EchoCut.Library;

namespace EchoCut
{
    /// <summary>
    /// La parte de <see cref="Main"/> que recibe pistas desde fuera de la ventana: por línea de
    /// comandos o desde otra instancia de la aplicación.
    /// </summary>
    public partial class Main
    {
        /// <summary>
        /// Espera tras la última ruta recibida antes de cargarlas. El Explorador lanza un proceso por
        /// archivo seleccionado y sus rutas llegan escalonadas: agruparlas carga una selección de una
        /// vez, en lugar de reordenar y reajustar la rejilla por cada archivo.
        /// </summary>
        private const int IncomingDelayMilliseconds = 400;

        /// <summary>Rutas recibidas pendientes de cargar. Se protege con su propio bloqueo.</summary>
        private readonly List<string> _incomingPaths = [];

        /// <summary>Temporizador que agrupa las rutas recibidas; se crea con la ventana.</summary>
        private System.Windows.Forms.Timer? _incomingTimer;

        /// <summary>
        /// Recibe rutas de archivos o carpetas para añadirlas al listado. Se puede llamar desde
        /// cualquier hilo, incluso antes de que la ventana exista.
        /// </summary>
        /// <param name="paths">Rutas a añadir; vacía si solo se volvió a abrir la aplicación.</param>
        /// <remarks>
        /// Aunque no lleguen rutas, la ventana pasa al primer plano: abrir EchoCut con otra ventana
        /// ya abierta debe llevar a ella, no parecer que no ha pasado nada.
        /// </remarks>
        public void EnqueuePaths(IReadOnlyList<string> paths)
        {
            lock (_incomingPaths)
            {
                _incomingPaths.AddRange(paths);
            }

            if (!IsHandleCreated || !IsAlive)
            {
                // Aún no hay ventana: OnShown recogerá las rutas pendientes.
                return;
            }

            try
            {
                BeginInvoke(() =>
                {
                    if (IsAlive)
                    {
                        ComeToFront();
                        RestartIncomingTimer();
                    }
                });
            }
            catch (InvalidOperationException)
            {
                // La ventana se está cerrando; no queda dónde cargarlas.
            }
        }

        /// <inheritdoc/>
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            RestartIncomingTimer();
        }

        private void RestartIncomingTimer()
        {
            if (_incomingTimer is null)
            {
                _incomingTimer = new System.Windows.Forms.Timer(components) { Interval = IncomingDelayMilliseconds };
                _incomingTimer.Tick += IncomingTimer_Tick;
            }

            _incomingTimer.Stop();
            _incomingTimer.Start();
        }

        /// <summary>
        /// Carga las rutas pendientes, o vuelve a intentarlo más tarde si hay un lote en curso.
        /// </summary>
        /// <remarks>
        /// Durante un lote no se toca el listado: cambiarlo bajo un análisis o un renombrado haría
        /// que sus avisos buscaran filas que ya no están. El temporizador sigue en marcha y las
        /// rutas se cargan en cuanto el lote termina.
        /// </remarks>
        private void IncomingTimer_Tick(object? sender, EventArgs e)
        {
            if (IsBusy)
            {
                return;
            }

            _incomingTimer?.Stop();

            List<string> paths;
            lock (_incomingPaths)
            {
                paths = [.. _incomingPaths.Distinct(StringComparer.OrdinalIgnoreCase)];
                _incomingPaths.Clear();
            }

            if (paths.Count > 0)
            {
                AppendPaths(paths);
            }
        }

        /// <summary>
        /// Añade al listado las pistas de las rutas indicadas, sin quitar las que ya estaban.
        /// </summary>
        /// <param name="paths">Archivos de audio o carpetas.</param>
        /// <remarks>
        /// A diferencia de «Abrir carpeta(s)» y «Abrir archivo(s)», que sustituyen el listado, lo que
        /// llega del Explorador se suma: es la forma de reunir canciones de varias carpetas.
        /// </remarks>
        private void AppendPaths(IReadOnlyList<string> paths)
        {
            List<string> directories = [.. paths.Where(Directory.Exists)];
            List<string> files = [.. paths.Where(File.Exists)];

            if (directories.Count == 0 && files.Count == 0)
            {
                SetStatus("No se encontraron los archivos recibidos desde el Explorador.");
                return;
            }

            List<TrackInfo> tracks = [];
            int skipped = 0;

            if (directories.Count > 0)
            {
                ScanResult scan = TrackScanner.Scan(directories);
                tracks.AddRange(scan.Tracks);
                skipped += scan.SkippedCount;
            }

            if (files.Count > 0)
            {
                ScanResult scan = TrackScanner.ScanFiles(files);
                tracks.AddRange(scan.Tracks);
                skipped += scan.SkippedCount;
            }

            IEnumerable<string> origins = directories.Concat(files.Select(Path.GetDirectoryName).OfType<string>());
            foreach (string origin in origins)
            {
                if (!_sourceDirectories.Contains(origin, StringComparer.OrdinalIgnoreCase))
                {
                    _sourceDirectories.Add(origin);
                }
            }

            ShowSourceDirectories();
            int added = AddTracks(tracks, replace: false);

            string summary = added == 1
                ? $"1 archivo agregado; {_songs.Count} en el listado."
                : $"{added} archivos agregados; {_songs.Count} en el listado.";

            SetStatus(skipped == 0 ? summary : $"{summary} {skipped} ilegible(s) o no compatible(s) omitido(s).");
            UpdateButtons();
        }

        /// <summary>Lleva la ventana al primer plano, restaurándola si estaba minimizada.</summary>
        private void ComeToFront()
        {
            if (WindowState == FormWindowState.Minimized)
            {
                WindowState = FormWindowState.Normal;
            }

            Activate();
        }
    }
}
