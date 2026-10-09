using EchoCut.Audio;
using EchoCut.Library;

namespace EchoCut.Processing;

/// <summary>Una pista y el tramo que debe conservar su copia recortada.</summary>
/// <param name="Track">Pista a recortar.</param>
/// <param name="Range">Tramo del original que se conserva.</param>
/// <param name="Fades">Fundidos a aplicar en la copia, o <c>null</c> si no lleva ninguno.</param>
public sealed record TrimRequest(TrackInfo Track, TrimRange Range, TrackFades? Fades = null);

/// <summary>Dónde quedó la copia recortada de una pista.</summary>
/// <param name="OutputPath">Ruta absoluta del archivo escrito.</param>
public sealed record TrimOutcome(string OutputPath);

/// <summary>
/// Recorta pistas, de una en una o en lote. Los archivos originales no se modifican nunca: la
/// copia va siempre a la subcarpeta de salida.
/// </summary>
/// <remarks>
/// Cada pista sale por el camino más fiel posible: por copia de flujo, sin pérdida, salvo que algún
/// fundido llegue a la copia; solo entonces se recodifica con <see cref="AudioFader"/>.
/// </remarks>
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

    /// <summary>Recorta las pistas indicadas en paralelo usando una carpeta de salida común.</summary>
    /// <param name="requests">Pistas a recortar con su punto de corte.</param>
    /// <param name="outputDirectory">Carpeta donde escribir las copias; se crea si no existe.</param>
    /// <param name="maxDegreeOfParallelism">Archivos recortados a la vez.</param>
    /// <param name="progress">Canal de avisos por pista, o <c>null</c> si no interesa el detalle.</param>
    /// <param name="cancellationToken">Token de cancelación del lote.</param>
    /// <returns>El recuento final del lote.</returns>
    /// <exception cref="FFmpegException">Se lanza si FFmpeg no está resuelto.</exception>
    /// <exception cref="OperationCanceledException">Se lanza si se cancela el lote.</exception>
    public Task<BatchSummary> TrimAsync(
        IReadOnlyList<TrimRequest> requests,
        string outputDirectory,
        int maxDegreeOfParallelism,
        IProgress<TrackProgress<TrimOutcome>>? progress,
        CancellationToken cancellationToken) =>
        TrimAsync(
            requests,
            _ => outputDirectory,
            maxDegreeOfParallelism,
            progress,
            cancellationToken);

    /// <summary>Recorta las pistas indicadas resolviendo dinámicamente la carpeta de salida para cada una.</summary>
    /// <param name="requests">Pistas a recortar con su punto de corte.</param>
    /// <param name="outputDirectoryResolver">Función que calcula la carpeta de salida para cada petición.</param>
    /// <param name="maxDegreeOfParallelism">Archivos recortados a la vez.</param>
    /// <param name="progress">Canal de avisos por pista, o <c>null</c> si no interesa el detalle.</param>
    /// <param name="cancellationToken">Token de cancelación del lote.</param>
    /// <returns>El recuento final del lote.</returns>
    /// <exception cref="FFmpegException">Se lanza si FFmpeg no está resuelto.</exception>
    /// <exception cref="OperationCanceledException">Se lanza si se cancela el lote.</exception>
    public async Task<BatchSummary> TrimAsync(
        IReadOnlyList<TrimRequest> requests,
        Func<TrimRequest, string> outputDirectoryResolver,
        int maxDegreeOfParallelism,
        IProgress<TrackProgress<TrimOutcome>>? progress,
        CancellationToken cancellationToken)
    {
        (string ffmpeg, string ffprobe) = _locator.Require();
        AudioTrimmer trimmer = new(ffmpeg);
        AudioFader fader = new(ffmpeg, ffprobe);

        return await BatchRunner.RunAsync(
            requests,
            maxDegreeOfParallelism,
            static request => request.Track,
            (request, token) => WriteAsync(trimmer, fader, request, outputDirectoryResolver(request), token),
            progress,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Recorta las pistas indicadas guardando cada copia en la subcarpeta «Recortados» de su propio directorio de origen.
    /// </summary>
    /// <param name="requests">Pistas a recortar con su punto de corte.</param>
    /// <param name="maxDegreeOfParallelism">Archivos recortados a la vez.</param>
    /// <param name="progress">Canal de avisos por pista, o <c>null</c> si no interesa el detalle.</param>
    /// <param name="cancellationToken">Token de cancelación del lote.</param>
    /// <returns>El recuento final del lote.</returns>
    /// <exception cref="FFmpegException">Se lanza si FFmpeg no está resuelto.</exception>
    /// <exception cref="OperationCanceledException">Se lanza si se cancela el lote.</exception>
    public Task<BatchSummary> TrimAsync(
        IReadOnlyList<TrimRequest> requests,
        int maxDegreeOfParallelism,
        IProgress<TrackProgress<TrimOutcome>>? progress,
        CancellationToken cancellationToken) =>
        TrimAsync(
            requests,
            static request => GetOutputDirectory(request.Track.Directory),
            maxDegreeOfParallelism,
            progress,
            cancellationToken);

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
        (string ffmpeg, string ffprobe) = _locator.Require();

        return await WriteAsync(new AudioTrimmer(ffmpeg), new AudioFader(ffmpeg, ffprobe), request, outputDirectory, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>Escribe la copia de una pista por copia de flujo o, si lleva fundidos, recodificándola.</summary>
    /// <remarks>
    /// Los fundidos que el recorte deja fuera no cuentan: si ninguno llega a la copia, recodificar
    /// solo costaría calidad. Tras recodificar se copian las etiquetas y la carátula del original,
    /// que la copia de flujo ya conserva por sí sola.
    /// </remarks>
    private static async Task<TrimOutcome> WriteAsync(
        AudioTrimmer trimmer,
        AudioFader fader,
        TrimRequest request,
        string outputDirectory,
        CancellationToken cancellationToken)
    {
        string source = request.Track.FilePath;

        if (request.Fades?.Within(request.Range) is not { } fades)
        {
            return new TrimOutcome(await trimmer
                .TrimAsync(source, request.Range, outputDirectory, cancellationToken)
                .ConfigureAwait(false));
        }

        string written = await fader
            .RenderAsync(source, request.Range, fades, outputDirectory, cancellationToken)
            .ConfigureAwait(false);

        TrackEditor.CopyTags(source, written);
        return new TrimOutcome(written);
    }
}
