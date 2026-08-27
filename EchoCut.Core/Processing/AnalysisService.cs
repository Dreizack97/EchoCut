using EchoCut.Audio;
using EchoCut.Library;

namespace EchoCut.Processing;

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

    /// <summary>Analiza las pistas indicadas en paralelo.</summary>
    /// <param name="tracks">Pistas a analizar.</param>
    /// <param name="options">Parámetros del algoritmo de detección.</param>
    /// <param name="maxDegreeOfParallelism">Archivos analizados a la vez.</param>
    /// <param name="progress">Canal de avisos por pista, o <c>null</c> si no interesa el detalle.</param>
    /// <param name="cancellationToken">Token de cancelación del lote.</param>
    /// <returns>El recuento final del lote.</returns>
    /// <exception cref="FFmpegException">Se lanza si FFmpeg no está resuelto.</exception>
    /// <exception cref="OperationCanceledException">Se lanza si se cancela el lote.</exception>
    public async Task<BatchSummary> AnalyzeAsync(
        IReadOnlyList<TrackInfo> tracks,
        SilenceOptions options,
        int maxDegreeOfParallelism,
        IProgress<TrackProgress<TrackAnalysis>>? progress,
        CancellationToken cancellationToken)
    {
        (string ffmpeg, string ffprobe) = _locator.Require();
        SilenceAnalyzer analyzer = new(new AudioDecoder(ffmpeg, ffprobe));

        return await BatchRunner.RunAsync(
            tracks,
            maxDegreeOfParallelism,
            static track => track,
            async (track, token) =>
            {
                // La duración ya se leyó con TagLibSharp al escanear la carpeta; reutilizarla
                // evita levantar un proceso de ffprobe por archivo.
                AnalysisReport report = await analyzer
                    .AnalyzeAsync(track.FilePath, options, token, track.DurationSeconds)
                    .ConfigureAwait(false);

                return TrackAnalysis.From(report);
            },
            progress,
            cancellationToken).ConfigureAwait(false);
    }
}
