using System.Diagnostics;

namespace EchoCut.Audio;

/// <summary>Análisis de un archivo completo: decodificación, detección y métricas de rendimiento.</summary>
/// <param name="Trailing">Resultado del algoritmo de detección sobre el final de la pista.</param>
/// <param name="Leading">
/// Resultado sobre el principio de la pista, o <c>null</c> si
/// <see cref="SilenceOptions.TrimLeadingSilence"/> está desactivado.
/// </param>
/// <param name="DurationSeconds">Duración total del archivo analizado, en segundos.</param>
/// <param name="DecodedSeconds">
/// Segundos de audio decodificados por FFmpeg en todas las pasadas, incluidas las ampliaciones de
/// ventana y el sondeo del principio.
/// </param>
/// <param name="ElapsedMilliseconds">Tiempo de reloj que tardó el análisis completo, en milisegundos.</param>
public sealed record AnalysisReport(
    SilenceResult Trailing,
    SilenceResult? Leading,
    double DurationSeconds,
    double DecodedSeconds,
    long ElapsedMilliseconds)
{
    /// <summary>Segundos de audio procesados por segundo de reloj. Métrica del objetivo 5.</summary>
    /// <value>Cociente entre <see cref="DecodedSeconds"/> y el tiempo transcurrido; <c>0</c> si el tiempo fue nulo o negativo.</value>
    public double RealTimeFactor =>
        ElapsedMilliseconds <= 0 ? 0 : DecodedSeconds / (ElapsedMilliseconds / 1000.0);
}

/// <summary>
/// Une el decodificador con el detector aplicando sondeo progresivo en cada borde: analizar una
/// canción entera para mirar solo su final o su principio es desperdicio, así que se empieza por
/// una ventana corta y solo se amplía cuando resulta insuficiente.
/// </summary>
public sealed class SilenceAnalyzer
{
    /// <summary>Ventana inicial de sondeo del final. Cubre la cola de la inmensa mayoría de las pistas.</summary>
    public const double InitialWindowSeconds = 30.0;

    /// <summary>
    /// Ventana inicial de sondeo del principio. Es más corta que la del final porque una entrada
    /// muerta dura segundos, no decenas, y basta con alcanzar la música y unos segundos de ella para
    /// estimar el nivel de programa. Medido sobre 25 MP3 reales, pasar de 30 s a 10 s reduce a un
    /// tercio el audio decodificado por el sondeo del principio sin cambiar ningún resultado.
    /// </summary>
    public const double InitialLeadingWindowSeconds = 10.0;

    /// <summary>
    /// Factor de ampliación cuando la ventana se queda corta. Cada intento vuelve a decodificar
    /// desde cero, así que duplicar sale caro: para llegar a 240 s hacen falta cuatro pasadas que
    /// suman 450 s de audio decodificado, mientras que cuadruplicar llega en dos y suma 270 s.
    /// </summary>
    private const double WindowGrowthFactor = 4.0;

    private readonly AudioDecoder _decoder;

    /// <summary>Crea el analizador sobre el decodificador indicado.</summary>
    /// <param name="decoder">Decodificador de FFmpeg que se usará para leer los bordes de cada archivo.</param>
    public SilenceAnalyzer(AudioDecoder decoder) => _decoder = decoder;

    /// <summary>
    /// Analiza el silencio final y, si está activado, el inicial de un archivo, ampliando la ventana
    /// de sondeo de cada borde si hace falta.
    /// </summary>
    /// <param name="filePath">Ruta del archivo de audio a analizar.</param>
    /// <param name="options">Parámetros del algoritmo de detección.</param>
    /// <param name="cancellationToken">Token de cancelación para la decodificación subyacente.</param>
    /// <param name="knownDurationSeconds">
    /// Duración ya conocida por el llamador, si la tiene. La interfaz la obtiene de TagLibSharp al
    /// escanear la carpeta, así que pasarla evita lanzar ffprobe y ahorra un proceso por archivo.
    /// Medido sobre 43 MP3 reales, TagLibSharp y ffprobe difieren como mucho 23 ms: por debajo de
    /// la granularidad de trama de MP3 y muy lejos del margen que se conserva al recortar.
    /// </param>
    /// <returns>Resultado del análisis junto con las métricas de rendimiento de la decodificación.</returns>
    /// <remarks>
    /// Si la ventana del final acaba cubriendo el archivo entero —pistas de menos de
    /// <see cref="InitialWindowSeconds"/> o con colas muy largas—, el principio se analiza
    /// sobre las mismas tramas en lugar de decodificarlo otra vez.
    /// </remarks>
    public async Task<AnalysisReport> AnalyzeAsync(
        string filePath,
        SilenceOptions options,
        CancellationToken cancellationToken,
        double? knownDurationSeconds = null)
    {
        long startedAt = Stopwatch.GetTimestamp();

        double duration = knownDurationSeconds is > 0
            ? knownDurationSeconds.Value
            : await _decoder.GetDurationAsync(filePath, cancellationToken).ConfigureAwait(false);

        ((SilenceResult trailing, SilenceResult? leading), double decodedSeconds) = await ProbeAsync(
            duration,
            InitialWindowSeconds,
            options,
            (window, sink) => _decoder.DecodeTailIntoAsync(filePath, window, duration, sink, cancellationToken),
            (framer, window) => (
                Trailing: SilenceDetector.AnalyzeFrames(
                    framer.Levels,
                    framer.FrameSeconds,
                    framer.LastFrameSeconds,
                    framer.PeakDbfs,
                    duration,
                    options),
                Leading: options.TrimLeadingSilence && window >= duration ? AnalyzeLeading(framer, options) : null),
            static edges => edges.Trailing.EntireBufferSilent).ConfigureAwait(false);

        if (options.TrimLeadingSilence && leading is null)
        {
            (leading, double leadingSeconds) = await ProbeAsync(
                duration,
                InitialLeadingWindowSeconds,
                options,
                (window, sink) => _decoder.DecodeRangeIntoAsync(filePath, 0.0, window, sink, cancellationToken),
                (framer, _) => AnalyzeLeading(framer, options),
                static result => result.EntireBufferSilent).ConfigureAwait(false);

            decodedSeconds += leadingSeconds;
        }

        return new AnalysisReport(
            trailing,
            leading,
            duration,
            decodedSeconds,
            (long)Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);
    }

    /// <summary>
    /// Decodifica un borde con una ventana creciente hasta que el análisis encuentra música o la
    /// ventana cubre el archivo entero.
    /// </summary>
    /// <typeparam name="TResult">Tipo del resultado que produce el análisis de cada ventana.</typeparam>
    /// <param name="durationSeconds">Duración total del archivo, que acota la ventana.</param>
    /// <param name="initialWindowSeconds">Duración de la primera ventana.</param>
    /// <param name="options">Parámetros del algoritmo, de los que se toman la trama y el paso alto.</param>
    /// <param name="decodeWindow">Cómo decodificar una ventana de la duración indicada en el destino dado.</param>
    /// <param name="analyze">Cómo analizar las tramas de una ventana ya decodificada, junto con su duración.</param>
    /// <param name="isWindowSilent">
    /// Si el resultado declara que toda la ventana era silencio. En ese caso el silencio real se
    /// extiende más allá de donde se miró y la medida sería un recorte por defecto, así que se amplía
    /// la ventana y se repite.
    /// </param>
    /// <returns>El resultado de la última ventana y los segundos decodificados en todas las pasadas.</returns>
    private static async Task<(TResult Result, double DecodedSeconds)> ProbeAsync<TResult>(
        double durationSeconds,
        double initialWindowSeconds,
        SilenceOptions options,
        Func<double, ISampleSink, Task> decodeWindow,
        Func<LevelFramer, double, TResult> analyze,
        Func<TResult, bool> isWindowSilent)
    {
        double window = Math.Min(initialWindowSeconds, durationSeconds);
        double decodedSeconds = 0;

        while (true)
        {
            int expectedFrames =
                (int)(window * 1000.0 / Math.Max(1.0, options.FrameMilliseconds)) + 1;

            using LevelFramer framer = new(
                AudioDecoder.AnalysisSampleRate,
                options.FrameMilliseconds,
                expectedFrames,
                options.HighPassHz);

            await decodeWindow(window, framer).ConfigureAwait(false);
            framer.Complete();

            decodedSeconds += framer.AnalyzedSeconds;
            TResult result = analyze(framer, window);

            if (!isWindowSilent(result) || window >= durationSeconds)
            {
                return (result, decodedSeconds);
            }

            window = Math.Min(window * WindowGrowthFactor, durationSeconds);
        }
    }

    private static SilenceResult AnalyzeLeading(LevelFramer framer, SilenceOptions options) =>
        SilenceDetector.AnalyzeLeadingFrames(framer.Levels, framer.FrameSeconds, framer.PeakDbfs, options);
}
