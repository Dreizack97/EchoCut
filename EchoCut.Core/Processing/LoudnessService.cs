using EchoCut.Audio;
using EchoCut.Library;
using EchoCut.Loudness;

namespace EchoCut.Processing;

/// <summary>Sonoridad medida de una pista y lo que ya se le había ajustado.</summary>
/// <param name="Track">Pista medida.</param>
/// <param name="Scan">Frecuencia y rango de <c>global_gain</c> de sus tramas.</param>
/// <param name="Measured">Medida ReplayGain del audio tal como está ahora.</param>
/// <param name="AdjustedSteps">
/// Pasos de 1.5 dB que ya tiene aplicados respecto a su volumen original, según sus etiquetas.
/// </param>
public sealed record LoudnessMeasurement(TrackInfo Track, Mp3GainScan Scan, ReplayGainResult Measured, int AdjustedSteps);

/// <summary>Cambio de volumen pedido para una pista.</summary>
/// <param name="Track">Pista a modificar.</param>
/// <param name="Steps">Pasos de 1.5 dB; negativos para bajar el volumen.</param>
/// <param name="Measured">Medida previa al cambio, para anotar la ganancia que queda.</param>
public sealed record GainRequest(TrackInfo Track, int Steps, ReplayGainResult Measured);

/// <summary>Cambio de volumen hecho en una pista.</summary>
/// <param name="Track">La pista releída del disco; la misma si no hubo cambio.</param>
/// <param name="Steps">Pasos aplicados; 0 si no se tocó el archivo.</param>
public sealed record GainChange(TrackInfo Track, int Steps);

/// <summary>
/// Regulariza el volumen de una biblioteca de MP3: mide cada pista con ReplayGain y cambia su
/// <c>global_gain</c> sin recodificar, en el archivo original, de forma reversible.
/// </summary>
/// <remarks>
/// <para>
/// Medir y aplicar van por separado para que quien llama pueda enseñar la propuesta, dejar elegir
/// el objetivo y qué pistas tocar, y solo entonces modificar archivos. Ninguna operación vuelve a
/// medir tras el cambio: <see cref="ReplayGainResult.AfterSteps"/> lo calcula de forma exacta.
/// </para>
/// <para>
/// Solo admite MP3: el ajuste de <c>global_gain</c> es propio de MPEG Layer III, y cualquier otro
/// formato exigiría recodificar.
/// </para>
/// </remarks>
/// <param name="locator">Localizador de FFmpeg ya compartido con el resto de la aplicación.</param>
public sealed class LoudnessService(FFmpegLocator locator)
{
    /// <summary>Si se puede regularizar el volumen de una pista.</summary>
    /// <param name="track">Pista a comprobar.</param>
    /// <returns><c>true</c> si es un MP3.</returns>
    public static bool CanAdjust(TrackInfo track) => Mp3Converter.IsMp3(track.FilePath);

    /// <summary>Mide en paralelo la sonoridad de las pistas, sin modificarlas.</summary>
    /// <param name="tracks">Pistas MP3 a medir.</param>
    /// <param name="maxDegreeOfParallelism">Archivos procesados a la vez.</param>
    /// <param name="progress">Canal de avisos por pista, o <c>null</c> si no interesa el detalle.</param>
    /// <param name="cancellationToken">Token de cancelación del lote.</param>
    /// <returns>El recuento final del lote.</returns>
    /// <remarks>
    /// Cada pista se decodifica entera, en estéreo y a su frecuencia original: ReplayGain mide todo
    /// el tema, y remuestrear cambiaría el pico que decide si se puede subir sin saturar.
    /// </remarks>
    /// <exception cref="FFmpegException">Se lanza si FFmpeg no está resuelto.</exception>
    /// <exception cref="OperationCanceledException">Se lanza si se cancela el lote.</exception>
    public Task<BatchSummary> MeasureAsync(
        IReadOnlyList<TrackInfo> tracks,
        int maxDegreeOfParallelism,
        IProgress<TrackProgress<LoudnessMeasurement>>? progress,
        CancellationToken cancellationToken)
    {
        (string ffmpeg, string ffprobe) = locator.Require();

        return BatchRunner.RunAsync(
            tracks,
            maxDegreeOfParallelism,
            static track => track,
            async (track, token) =>
            {
                Mp3GainScan scan = Mp3GainEditor.Scan(track.FilePath, token);
                int adjusted = Mp3GainEditor.ReadAdjustment(track.FilePath);

                AudioDecoder decoder = new(ffmpeg, ffprobe, ReplayGainAnalyzer.AnalysisRateFor(scan.SampleRate), channels: 2);
                ReplayGainAnalyzer analyzer = new(decoder.SampleRate, decoder.Channels);
                await decoder.DecodeAllIntoAsync(track.FilePath, analyzer, token).ConfigureAwait(false);

                return new LoudnessMeasurement(track, scan, analyzer.GetResult(), adjusted);
            },
            progress,
            cancellationToken);
    }

    /// <summary>Aplica en paralelo los cambios de volumen pedidos.</summary>
    /// <param name="requests">Pista y pasos de cada cambio.</param>
    /// <param name="maxDegreeOfParallelism">Archivos procesados a la vez.</param>
    /// <param name="progress">Canal de avisos por pista, o <c>null</c> si no interesa el detalle.</param>
    /// <param name="cancellationToken">Token de cancelación del lote.</param>
    /// <returns>El recuento final del lote.</returns>
    /// <remarks>
    /// Cada archivo se reescribe en una copia temporal que solo sustituye al original al terminar:
    /// detener el lote no deja ninguno a medias.
    /// </remarks>
    /// <exception cref="OperationCanceledException">Se lanza si se cancela el lote.</exception>
    public static Task<BatchSummary> ApplyAsync(
        IReadOnlyList<GainRequest> requests,
        int maxDegreeOfParallelism,
        IProgress<TrackProgress<GainChange>>? progress,
        CancellationToken cancellationToken) =>
        BatchRunner.RunAsync(
            requests,
            maxDegreeOfParallelism,
            static request => request.Track,
            static (request, token) => Task.Run(
                () =>
                {
                    if (request.Steps == 0)
                    {
                        return new GainChange(request.Track, 0);
                    }

                    Mp3GainEditor.ChangeGain(request.Track.FilePath, request.Steps, request.Measured, token);
                    return new GainChange(TrackScanner.Read(request.Track.FilePath), request.Steps);
                },
                token),
            progress,
            cancellationToken);

    /// <summary>Devuelve en paralelo las pistas a su volumen original.</summary>
    /// <param name="tracks">Pistas MP3; las que no tengan cambios anotados se dejan como están.</param>
    /// <param name="maxDegreeOfParallelism">Archivos procesados a la vez.</param>
    /// <param name="progress">Canal de avisos por pista, o <c>null</c> si no interesa el detalle.</param>
    /// <param name="cancellationToken">Token de cancelación del lote.</param>
    /// <returns>El recuento final del lote.</returns>
    /// <remarks>No necesita FFmpeg: solo lee la etiqueta del ajuste y reescribe las tramas.</remarks>
    /// <exception cref="OperationCanceledException">Se lanza si se cancela el lote.</exception>
    public static Task<BatchSummary> RestoreAsync(
        IReadOnlyList<TrackInfo> tracks,
        int maxDegreeOfParallelism,
        IProgress<TrackProgress<GainChange>>? progress,
        CancellationToken cancellationToken) =>
        BatchRunner.RunAsync(
            tracks,
            maxDegreeOfParallelism,
            static track => track,
            static (track, token) => Task.Run(
                () =>
                {
                    int steps = Mp3GainEditor.Undo(track.FilePath, token);
                    return new GainChange(steps == 0 ? track : TrackScanner.Read(track.FilePath), steps);
                },
                token),
            progress,
            cancellationToken);
}
