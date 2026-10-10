using EchoCut.Audio;
using EchoCut.Library;

namespace EchoCut.Processing;

/// <summary>Dónde quedó la copia en MP3 de una pista.</summary>
/// <param name="OutputPath">Ruta absoluta del archivo escrito.</param>
public sealed record ConversionOutcome(string OutputPath);

/// <summary>
/// Convierte pistas a MP3 en lote. Los originales no se modifican nunca: cada copia va a la
/// subcarpeta <see cref="OutputFolderName"/> de la carpeta de su original.
/// </summary>
public sealed class ConversionService
{
    /// <summary>Subcarpeta de salida, junto a cada original, como «Recortados».</summary>
    public const string OutputFolderName = "MP3";

    private readonly FFmpegLocator _locator;

    /// <summary>Crea el servicio contra un localizador de FFmpeg ya compartido con el resto de la aplicación.</summary>
    /// <param name="locator">Localizador de los ejecutables de FFmpeg.</param>
    public ConversionService(FFmpegLocator locator) => _locator = locator;

    /// <summary>Carpeta en la que se escribe la copia de una pista.</summary>
    /// <param name="track">Pista a convertir.</param>
    /// <returns>La subcarpeta <see cref="OutputFolderName"/> de la carpeta del original.</returns>
    public static string GetOutputDirectory(TrackInfo track) => Path.Combine(track.Directory, OutputFolderName);

    /// <summary>Convierte las pistas indicadas en paralelo.</summary>
    /// <param name="tracks">Pistas a convertir; las que ya son MP3 deberían quedarse fuera.</param>
    /// <param name="quality">Calidad del MP3.</param>
    /// <param name="maxDegreeOfParallelism">Archivos convertidos a la vez.</param>
    /// <param name="progress">Canal de avisos por pista, o <c>null</c> si no interesa el detalle.</param>
    /// <param name="cancellationToken">Token de cancelación del lote.</param>
    /// <returns>El recuento final del lote.</returns>
    /// <remarks>
    /// Tras escribir cada MP3 se le copian las etiquetas y la carátula del original con TagLibSharp,
    /// porque FFmpeg hereda el texto pero no la carátula.
    /// </remarks>
    /// <exception cref="FFmpegException">Se lanza si FFmpeg no está resuelto.</exception>
    /// <exception cref="OperationCanceledException">Se lanza si se cancela el lote.</exception>
    public async Task<BatchSummary> ConvertAsync(
        IReadOnlyList<TrackInfo> tracks,
        Mp3Quality quality,
        int maxDegreeOfParallelism,
        IProgress<TrackProgress<ConversionOutcome>>? progress,
        CancellationToken cancellationToken)
    {
        Mp3Converter converter = new(_locator.Require().FFmpeg);

        return await BatchRunner.RunAsync(
            tracks,
            maxDegreeOfParallelism,
            static track => track,
            async (track, token) =>
            {
                string written = await converter
                    .ConvertAsync(track.FilePath, GetOutputDirectory(track), quality, token)
                    .ConfigureAwait(false);

                TrackEditor.CopyTags(track.FilePath, written);
                return new ConversionOutcome(written);
            },
            progress,
            cancellationToken).ConfigureAwait(false);
    }
}
