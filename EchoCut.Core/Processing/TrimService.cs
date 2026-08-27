using EchoCut.Audio;
using EchoCut.Library;

namespace EchoCut.Processing;

/// <summary>Una pista y el punto por el que hay que cortarla.</summary>
/// <param name="Track">Pista a recortar.</param>
/// <param name="CutSeconds">Instante de corte, en segundos desde el principio del archivo.</param>
public sealed record TrimRequest(TrackInfo Track, double CutSeconds);

/// <summary>Dónde quedó la copia recortada de una pista.</summary>
/// <param name="OutputPath">Ruta absoluta del archivo escrito.</param>
public sealed record TrimOutcome(string OutputPath);

/// <summary>
/// Recorta pistas, de una en una o en lote. Los archivos originales no se modifican nunca: la
/// copia va siempre a la subcarpeta de salida.
/// </summary>
public sealed class TrimService
{
    /// <summary>Subcarpeta de salida, creada dentro de la carpeta analizada.</summary>
    public const string OutputFolderName = AudioTrimmer.OutputFolderName;

    private readonly FFmpegLocator _locator;

    /// <summary>Crea el servicio contra un localizador de FFmpeg ya compartido con el resto de la aplicación.</summary>
    /// <param name="locator">Localizador de los ejecutables de FFmpeg.</param>
    public TrimService(FFmpegLocator locator) => _locator = locator;

    /// <summary>Calcula la carpeta de salida para una carpeta de origen dada.</summary>
    /// <param name="sourceDirectory">Carpeta que contiene los archivos originales.</param>
    /// <returns>La subcarpeta donde se escribirán las copias recortadas.</returns>
    public static string GetOutputDirectory(string sourceDirectory) =>
        AudioTrimmer.GetOutputDirectory(sourceDirectory);

    /// <summary>Recorta las pistas indicadas en paralelo.</summary>
    /// <param name="requests">Pistas a recortar con su punto de corte.</param>
    /// <param name="outputDirectory">Carpeta donde escribir las copias; se crea si no existe.</param>
    /// <param name="maxDegreeOfParallelism">Archivos recortados a la vez.</param>
    /// <param name="progress">Canal de avisos por pista, o <c>null</c> si no interesa el detalle.</param>
    /// <param name="cancellationToken">Token de cancelación del lote.</param>
    /// <returns>El recuento final del lote.</returns>
    /// <exception cref="FFmpegException">Se lanza si FFmpeg no está resuelto.</exception>
    /// <exception cref="OperationCanceledException">Se lanza si se cancela el lote.</exception>
    public async Task<BatchSummary> TrimAsync(
        IReadOnlyList<TrimRequest> requests,
        string outputDirectory,
        int maxDegreeOfParallelism,
        IProgress<TrackProgress<TrimOutcome>>? progress,
        CancellationToken cancellationToken)
    {
        AudioTrimmer trimmer = new(_locator.Require().FFmpeg);

        return await BatchRunner.RunAsync(
            requests,
            maxDegreeOfParallelism,
            static request => request.Track,
            async (request, token) => new TrimOutcome(await trimmer
                .TrimAsync(request.Track.FilePath, request.CutSeconds, outputDirectory, token)
                .ConfigureAwait(false)),
            progress,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Recorta una sola pista.</summary>
    /// <remarks>
    /// Sin canal de avisos a propósito: quien recorta una única pista está esperando a esta tarea y
    /// puede reaccionar a su desenlace directamente, sin pasar por un aviso que llegaría después.
    /// </remarks>
    /// <param name="request">Pista a recortar con su punto de corte.</param>
    /// <param name="outputDirectory">Carpeta donde escribir la copia; se crea si no existe.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Dónde quedó la copia recortada.</returns>
    /// <exception cref="FFmpegException">Se lanza si FFmpeg no está resuelto o si falla el recorte.</exception>
    /// <exception cref="OperationCanceledException">Se lanza si se cancela el recorte.</exception>
    public async Task<TrimOutcome> TrimOneAsync(
        TrimRequest request,
        string outputDirectory,
        CancellationToken cancellationToken)
    {
        AudioTrimmer trimmer = new(_locator.Require().FFmpeg);

        return new TrimOutcome(await trimmer
            .TrimAsync(request.Track.FilePath, request.CutSeconds, outputDirectory, cancellationToken)
            .ConfigureAwait(false));
    }
}
