using System.Diagnostics;

namespace EchoCut.Audio;

/// <summary>Análisis de un archivo completo: decodificación, detección y métricas de rendimiento.</summary>
/// <param name="Result">Resultado del algoritmo de detección de silencio.</param>
/// <param name="DurationSeconds">Duración total del archivo analizado, en segundos.</param>
/// <param name="DecodedSeconds">Segundos de audio efectivamente decodificados por FFmpeg.</param>
/// <param name="ElapsedMilliseconds">Tiempo de reloj que tardó el análisis completo, en milisegundos.</param>
public sealed record AnalysisReport(
    SilenceResult Result,
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
/// Une el decodificador con el detector aplicando sondeo de cola progresivo: analizar una canción
/// entera para mirar solo su final es desperdicio, así que se empieza por una ventana corta y solo
/// se amplía cuando resulta insuficiente.
/// </summary>
public sealed class SilenceAnalyzer
{
    /// <summary>Ventana inicial de sondeo. Cubre la cola de la inmensa mayoría de las pistas.</summary>
    public const double InitialWindowSeconds = 30.0;

    /// <summary>
    /// Factor de ampliación cuando la ventana se queda corta. Cada intento vuelve a decodificar
    /// desde cero, así que duplicar sale caro: para llegar a 240 s hacen falta cuatro pasadas que
    /// suman 450 s de audio decodificado, mientras que cuadruplicar llega en dos y suma 270 s.
    /// </summary>
    private const double WindowGrowthFactor = 4.0;

    private readonly AudioDecoder _decoder;

    /// <summary>Crea el analizador sobre el decodificador indicado.</summary>
    /// <param name="decoder">Decodificador de FFmpeg que se usará para leer la cola de cada archivo.</param>
    public SilenceAnalyzer(AudioDecoder decoder) => _decoder = decoder;

    /// <summary>Analiza el silencio final de un archivo, ampliando la ventana de sondeo si hace falta.</summary>
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
        double window = Math.Min(InitialWindowSeconds, duration);
        double decodedSeconds = 0;
        SilenceResult result;

        while (true)
        {
            int expectedFrames =
                (int)(window * 1000.0 / Math.Max(1.0, options.FrameMilliseconds)) + 1;

            using LevelFramer framer = new(
                AudioDecoder.AnalysisSampleRate,
                options.FrameMilliseconds,
                expectedFrames,
                options.HighPassHz);

            await _decoder
                .DecodeTailIntoAsync(filePath, window, duration, framer, cancellationToken)
                .ConfigureAwait(false);

            framer.Complete();

            decodedSeconds = framer.AnalyzedSeconds;
            result = SilenceDetector.AnalyzeFrames(
                framer.Levels,
                framer.FrameSeconds,
                framer.LastFrameSeconds,
                framer.PeakDbfs,
                duration,
                options);

            // Si toda la ventana resultó silenciosa, el silencio real empieza antes de donde
            // miramos y la medida sería un recorte por defecto. Se amplía y se repite.
            if (!result.EntireBufferSilent || window >= duration)
            {
                break;
            }

            window = Math.Min(window * WindowGrowthFactor, duration);
        }

        return new AnalysisReport(
            result,
            duration,
            decodedSeconds,
            (long)Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);
    }
}
