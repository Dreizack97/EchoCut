using EchoCut.Audio;
using EchoCut.Library;

namespace EchoCut.Processing;

/// <summary>Una pista a analizar y las ediciones que ya lleva.</summary>
/// <param name="Track">Pista a analizar.</param>
/// <param name="Edits">
/// Fundidos y borrados de la pista, o <c>null</c> si no lleva ninguno. Con ediciones se analiza el
/// resultado editado, que es lo que quedará en la copia.
/// </param>
public sealed record AnalysisRequest(TrackInfo Track, AudioEdits? Edits = null);

/// <summary>
/// Analiza un lote de pistas. Es el orquestador que antes vivía dentro del formulario: sabe de
/// FFmpeg, de paralelismo y de cancelación, y no sabe nada de rejillas ni de ventanas.
/// </summary>
public sealed class AnalysisService
{
    private readonly FFmpegLocator _locator;

    /// <summary>Crea el servicio contra un localizador de FFmpeg ya compartido con el resto de la aplicación.</summary>
    /// <param name="locator">Localizador de los ejecutables de FFmpeg.</param>
    public AnalysisService(FFmpegLocator locator) => _locator = locator;

    /// <summary>Analiza las pistas indicadas en paralelo, tal como están en disco.</summary>
    /// <param name="tracks">Pistas a analizar.</param>
    /// <param name="options">Parámetros del algoritmo de detección.</param>
    /// <param name="maxDegreeOfParallelism">Archivos analizados a la vez.</param>
    /// <param name="progress">Canal de avisos por pista, o <c>null</c> si no interesa el detalle.</param>
    /// <param name="cancellationToken">Token de cancelación del lote.</param>
    /// <returns>El recuento final del lote.</returns>
    /// <exception cref="FFmpegException">Se lanza si FFmpeg no está resuelto.</exception>
    /// <exception cref="OperationCanceledException">Se lanza si se cancela el lote.</exception>
    public Task<BatchSummary> AnalyzeAsync(
        IReadOnlyList<TrackInfo> tracks,
        SilenceOptions options,
        int maxDegreeOfParallelism,
        IProgress<TrackProgress<TrackAnalysis>>? progress,
        CancellationToken cancellationToken) =>
        AnalyzeAsync(
            [.. tracks.Select(track => new AnalysisRequest(track))],
            options,
            maxDegreeOfParallelism,
            progress,
            cancellationToken);

    /// <summary>Analiza las pistas indicadas en paralelo; las que llevan ediciones, sobre su resultado.</summary>
    /// <param name="requests">Pistas a analizar, con sus ediciones.</param>
    /// <param name="options">Parámetros del algoritmo de detección.</param>
    /// <param name="maxDegreeOfParallelism">Archivos analizados a la vez.</param>
    /// <param name="progress">Canal de avisos por pista, o <c>null</c> si no interesa el detalle.</param>
    /// <param name="cancellationToken">Token de cancelación del lote.</param>
    /// <returns>El recuento final del lote.</returns>
    /// <remarks>
    /// Las pistas sin ediciones siguen con el sondeo rápido de los bordes; solo las editadas pagan
    /// la decodificación completa que exige su resultado.
    /// </remarks>
    /// <exception cref="FFmpegException">Se lanza si FFmpeg no está resuelto.</exception>
    /// <exception cref="OperationCanceledException">Se lanza si se cancela el lote.</exception>
    public async Task<BatchSummary> AnalyzeAsync(
        IReadOnlyList<AnalysisRequest> requests,
        SilenceOptions options,
        int maxDegreeOfParallelism,
        IProgress<TrackProgress<TrackAnalysis>>? progress,
        CancellationToken cancellationToken)
    {
        SilenceAnalyzer analyzer = CreateAnalyzer();

        return await BatchRunner.RunAsync(
            requests,
            maxDegreeOfParallelism,
            static request => request.Track,
            (request, token) => AnalyzeAsync(analyzer, request, options, token),
            progress,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Analiza una sola pista, sobre su resultado si lleva ediciones.</summary>
    /// <param name="request">Pista a analizar, con sus ediciones.</param>
    /// <param name="options">Parámetros del algoritmo de detección.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>El análisis, con los cortes en la línea de tiempo del original.</returns>
    /// <exception cref="FFmpegException">Se lanza si FFmpeg no está resuelto o falla la decodificación.</exception>
    /// <exception cref="OperationCanceledException">Se lanza si se cancela.</exception>
    public Task<TrackAnalysis> AnalyzeOneAsync(AnalysisRequest request, SilenceOptions options, CancellationToken cancellationToken) =>
        AnalyzeAsync(CreateAnalyzer(), request, options, cancellationToken);

    private SilenceAnalyzer CreateAnalyzer()
    {
        (string ffmpeg, string ffprobe) = _locator.Require();
        return new SilenceAnalyzer(new AudioDecoder(ffmpeg, ffprobe));
    }

    private static async Task<TrackAnalysis> AnalyzeAsync(
        SilenceAnalyzer analyzer,
        AnalysisRequest request,
        SilenceOptions options,
        CancellationToken cancellationToken)
    {
        TrackInfo track = request.Track;

        if (request.Edits is { HasAny: true } edits)
        {
            AnalysisReport edited = await analyzer
                .AnalyzeEditedAsync(track.FilePath, edits, options, cancellationToken)
                .ConfigureAwait(false);

            return TrackAnalysis.FromEdited(edited, edits);
        }

        // La duración ya se leyó con TagLibSharp al escanear la carpeta; reutilizarla evita
        // levantar un proceso de ffprobe por archivo.
        AnalysisReport report = await analyzer
            .AnalyzeAsync(track.FilePath, options, cancellationToken, track.DurationSeconds)
            .ConfigureAwait(false);

        return TrackAnalysis.From(report);
    }
}
