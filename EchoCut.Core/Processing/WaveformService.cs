using EchoCut.Audio;
using EchoCut.Waveforms;

namespace EchoCut.Processing;

/// <summary>
/// Carga la forma de onda de una pista: completa, para la vista general y el detalle del
/// principio, y su final por separado. Sabe de FFmpeg; no sabe nada de controles.
/// </summary>
public sealed class WaveformService
{
    /// <summary>
    /// Frecuencia de decodificación. A diferencia del análisis, que solo mide energía, aquí importan
    /// los picos: a 44.1 kHz se ven tal como los muestra Audacity en la gran mayoría de las pistas.
    /// </summary>
    public const int SampleRate = 44100;

    private readonly FFmpegLocator _locator;

    /// <summary>Crea el servicio contra un localizador de FFmpeg ya compartido con el resto de la aplicación.</summary>
    /// <param name="locator">Localizador de los ejecutables de FFmpeg.</param>
    public WaveformService(FFmpegLocator locator) => _locator = locator;

    /// <summary>Carga la forma de onda de la pista entera.</summary>
    /// <param name="filePath">Ruta del archivo de audio.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>La forma de onda, que empieza en el instante 0; quien la recibe debe liberarla.</returns>
    /// <remarks>
    /// Decodificada desde la primera muestra comparte referencia con el corte inicial, así que sirve
    /// también para el detalle del principio. No se acota con la duración de los metadatos para no
    /// perder el final si esta es errónea.
    /// </remarks>
    /// <exception cref="FFmpegException">Se lanza si FFmpeg no está resuelto o falla la decodificación.</exception>
    /// <exception cref="OperationCanceledException">Se lanza si se cancela.</exception>
    public Task<Waveform> LoadAsync(string filePath, CancellationToken cancellationToken) =>
        LoadCoreAsync(
            (decoder, sink) => decoder.DecodeAllIntoAsync(filePath, sink, cancellationToken),
            static _ => 0.0);

    /// <summary>Carga la forma de onda de los últimos segundos de la pista.</summary>
    /// <param name="filePath">Ruta del archivo de audio.</param>
    /// <param name="windowSeconds">Segundos finales a cargar.</param>
    /// <param name="durationSeconds">Duración de la pista con la que se calculó el análisis.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>La forma de onda del final; quien la recibe debe liberarla.</returns>
    /// <remarks>
    /// Se decodifica con <c>-sseof</c> y se sitúa contando hacia atrás desde
    /// <paramref name="durationSeconds"/>, igual que el análisis sitúa el corte final. Contar muestras
    /// desde el principio no serviría: en MP3 sin cabecera Xing, FFmpeg estima la duración por la tasa
    /// de bits y en 16 mezclas reales la cuenta de muestras la superó hasta en 1.56 s, que en la vista
    /// del final desplazaría la onda respecto del corte.
    /// </remarks>
    /// <exception cref="FFmpegException">Se lanza si FFmpeg no está resuelto o falla la decodificación.</exception>
    /// <exception cref="OperationCanceledException">Se lanza si se cancela.</exception>
    public Task<Waveform> LoadTailAsync(
        string filePath,
        double windowSeconds,
        double durationSeconds,
        CancellationToken cancellationToken) =>
        LoadCoreAsync(
            (decoder, sink) => decoder.DecodeTailIntoAsync(filePath, windowSeconds, durationSeconds, sink, cancellationToken),
            decodedSeconds => durationSeconds - decodedSeconds);

    /// <summary>Decodifica en un resumen nuevo y lo entrega situado en el tiempo.</summary>
    /// <param name="decode">Cómo decodificar el tramo en el destino dado.</param>
    /// <param name="startOf">Instante de la primera muestra a partir de los segundos decodificados.</param>
    private async Task<Waveform> LoadCoreAsync(
        Func<AudioDecoder, ISampleSink, Task> decode,
        Func<double, double> startOf)
    {
        (string ffmpeg, string ffprobe) = _locator.Require();
        AudioDecoder decoder = new(ffmpeg, ffprobe, SampleRate);

        using WaveformBuilder builder = new(SampleRate);
        await decode(decoder, builder).ConfigureAwait(false);
        builder.Complete();

        return builder.Build(startOf(builder.WrittenSeconds));
    }
}
