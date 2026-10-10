using System.Buffers;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

namespace EchoCut.Audio;

/// <summary>
/// Decodificación vía FFmpeg. El audio se pide en mono, a baja frecuencia y en punto flotante:
/// para métricas de energía sobra, y reduce el PCM que atraviesa la tubería a ~88 KB/s.
/// </summary>
/// <remarks>
/// La medida de sonoridad es la excepción: ReplayGain filtra cada canal por separado y promedia
/// después, así que pide los dos canales intercalados y la frecuencia original de la pista.
/// </remarks>
public sealed class AudioDecoder
{
    /// <summary>Frecuencia de análisis. Muy por encima de lo que exige una medida de energía.</summary>
    public const int AnalysisSampleRate = 22050;

    private readonly string _ffmpegPath;
    private readonly string _ffprobePath;

    /// <summary>Crea el decodificador contra los ejecutables de FFmpeg indicados.</summary>
    /// <param name="ffmpegPath">Ruta absoluta a <c>ffmpeg.exe</c>, ya resuelta.</param>
    /// <param name="ffprobePath">Ruta absoluta a <c>ffprobe.exe</c>, ya resuelta.</param>
    /// <param name="sampleRate">
    /// Frecuencia a la que FFmpeg entrega el PCM. El análisis de silencio usa
    /// <see cref="AnalysisSampleRate"/>; el espectrograma necesita más para mostrar las altas
    /// frecuencias donde viven el hiss y las colas de platillos.
    /// </param>
    /// <param name="channels">
    /// Canales del PCM. Con 1, FFmpeg mezcla a mono; con más, las muestras llegan intercaladas.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Se lanza si <paramref name="sampleRate"/> o <paramref name="channels"/> no son positivos.
    /// </exception>
    public AudioDecoder(string ffmpegPath, string ffprobePath, int sampleRate = AnalysisSampleRate, int channels = 1)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sampleRate);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(channels);

        _ffmpegPath = ffmpegPath;
        _ffprobePath = ffprobePath;
        SampleRate = sampleRate;
        Channels = channels;
    }

    /// <value>Frecuencia de muestreo del PCM que entrega este decodificador, en Hz.</value>
    public int SampleRate { get; }

    /// <value>Canales del PCM que entrega este decodificador; 1 salvo que se pidan más.</value>
    public int Channels { get; }

    /// <summary>Duración total del archivo según ffprobe, en segundos.</summary>
    /// <param name="filePath">Ruta del archivo de audio a consultar.</param>
    /// <param name="cancellationToken">Token de cancelación para el proceso de ffprobe.</param>
    /// <returns>Duración del archivo, en segundos, siempre mayor que cero.</returns>
    /// <exception cref="FFmpegException">
    /// Se lanza si ffprobe termina con error, o si su salida no se puede interpretar como una
    /// duración positiva y finita.
    /// </exception>
    public async Task<double> GetDurationAsync(string filePath, CancellationToken cancellationToken)
    {
        string[] arguments =
        [
            "-v", "error",
            "-show_entries", "format=duration",
            "-of", "default=noprint_wrappers=1:nokey=1",
            filePath,
        ];

        FFmpegResult result = await FFmpegRunner
            .RunAsync(_ffprobePath, arguments, cancellationToken)
            .ConfigureAwait(false);

        if (result.ExitCode != 0)
        {
            throw new FFmpegException(
                $"ffprobe no pudo leer el archivo: {FFmpegRunner.FirstLine(result.StandardError)}",
                result.ExitCode,
                result.StandardError);
        }

        string raw = result.StandardOutput.Trim();
        if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double duration)
            || double.IsNaN(duration)
            || duration <= 0)
        {
            throw new FFmpegException($"ffprobe devolvió una duración no utilizable ('{raw}').");
        }

        return duration;
    }

    /// <summary>
    /// Decodifica los últimos <paramref name="windowSeconds"/> segundos del archivo como PCM mono y
    /// se los entrega a <paramref name="sink"/> según van llegando, sin retener el audio completo.
    /// </summary>
    /// <param name="filePath">Ruta del archivo de audio a decodificar.</param>
    /// <param name="windowSeconds">
    /// Duración de la cola a decodificar. Si es mayor o igual que <paramref name="totalDurationSeconds"/>,
    /// se decodifica el archivo completo desde el principio en lugar de saltar con <c>-sseof</c>.
    /// </param>
    /// <param name="totalDurationSeconds">Duración total conocida del archivo, en segundos.</param>
    /// <param name="sink">Destino que recibe las muestras decodificadas, trozo a trozo.</param>
    /// <param name="cancellationToken">
    /// Token de cancelación. Si se activa mientras el proceso de FFmpeg sigue vivo, se mata antes de
    /// propagar la cancelación.
    /// </param>
    /// <remarks>
    /// <c>-sseof</c> puede aterrizar impreciso en MP3 VBR sin cabecera Xing. Da igual: quien llama
    /// mide el silencio hacia atrás desde la última muestra, así que la posición exacta del salto
    /// no interviene en el resultado; solo determina cuánto audio se decodifica de más o de menos.
    /// </remarks>
    /// <exception cref="FFmpegException">
    /// Se lanza si el proceso de FFmpeg termina con un código de salida distinto de cero.
    /// </exception>
    public Task DecodeTailIntoAsync(
        string filePath,
        double windowSeconds,
        double totalDurationSeconds,
        ISampleSink sink,
        CancellationToken cancellationToken)
    {
        List<string> seek = windowSeconds >= totalDurationSeconds
            ? []
            : ["-sseof", Seconds(-windowSeconds)];

        return DecodeIntoAsync(filePath, seek, [], sink, cancellationToken);
    }

    /// <summary>
    /// Decodifica <paramref name="durationSeconds"/> segundos a partir de
    /// <paramref name="startSeconds"/> como PCM mono y se los entrega a <paramref name="sink"/> según
    /// van llegando, sin retener el audio completo.
    /// </summary>
    /// <param name="filePath">Ruta del archivo de audio a decodificar.</param>
    /// <param name="startSeconds">Instante inicial del tramo, en segundos desde el principio del archivo.</param>
    /// <param name="durationSeconds">Duración del tramo, en segundos. Si excede el archivo, se decodifica hasta el final.</param>
    /// <param name="sink">Destino que recibe las muestras decodificadas, trozo a trozo.</param>
    /// <param name="cancellationToken">
    /// Token de cancelación. Si se activa mientras el proceso de FFmpeg sigue vivo, se mata antes de
    /// propagar la cancelación.
    /// </param>
    /// <remarks>
    /// A diferencia del recorte por copia de flujo, aquí <c>-ss</c> es exacto a la muestra: al
    /// decodificar, FFmpeg salta al paquete anterior y descarta lo que sobra. Desde el principio se
    /// omite el salto para que la primera trama del análisis coincida con la primera muestra del
    /// archivo, que es la referencia de tiempo del recorte del inicio.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Se lanza si <paramref name="startSeconds"/> es negativo o <paramref name="durationSeconds"/> no es positiva.
    /// </exception>
    /// <exception cref="FFmpegException">
    /// Se lanza si el proceso de FFmpeg termina con un código de salida distinto de cero.
    /// </exception>
    public Task DecodeRangeIntoAsync(
        string filePath,
        double startSeconds,
        double durationSeconds,
        ISampleSink sink,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(startSeconds);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(durationSeconds);

        List<string> seek = startSeconds > 0 ? ["-ss", Seconds(startSeconds)] : [];
        return DecodeIntoAsync(filePath, seek, ["-t", Seconds(durationSeconds)], sink, cancellationToken);
    }

    /// <summary>
    /// Decodifica el archivo completo como PCM mono y se lo entrega a <paramref name="sink"/> según
    /// va llegando, sin retener el audio completo.
    /// </summary>
    /// <param name="filePath">Ruta del archivo de audio a decodificar.</param>
    /// <param name="sink">Destino que recibe las muestras decodificadas, trozo a trozo.</param>
    /// <param name="cancellationToken">
    /// Token de cancelación. Si se activa mientras el proceso de FFmpeg sigue vivo, se mata antes de
    /// propagar la cancelación.
    /// </param>
    /// <remarks>
    /// No se acota con la duración de los metadatos: la que declara TagLibSharp puede discrepar de
    /// la real, y la última muestra que entregue FFmpeg es la única referencia fiable del final.
    /// </remarks>
    /// <exception cref="FFmpegException">
    /// Se lanza si el proceso de FFmpeg termina con un código de salida distinto de cero.
    /// </exception>
    public Task DecodeAllIntoAsync(string filePath, ISampleSink sink, CancellationToken cancellationToken) =>
        DecodeIntoAsync(filePath, [], [], sink, cancellationToken);

    /// <summary>Lanza FFmpeg con las opciones de posición indicadas y vuelca su salida PCM en el destino.</summary>
    /// <param name="filePath">Ruta del archivo de audio a decodificar.</param>
    /// <param name="inputSeek">Opciones de entrada que sitúan el inicio, colocadas antes de <c>-i</c>.</param>
    /// <param name="outputLimit">Opciones de salida que acotan la duración, colocadas después de <c>-i</c>.</param>
    /// <param name="sink">Destino que recibe las muestras decodificadas, trozo a trozo.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <exception cref="FFmpegException">
    /// Se lanza si el proceso de FFmpeg termina con un código de salida distinto de cero.
    /// </exception>
    private async Task DecodeIntoAsync(
        string filePath,
        List<string> inputSeek,
        List<string> outputLimit,
        ISampleSink sink,
        CancellationToken cancellationToken)
    {
        List<string> arguments = ["-v", "error", .. inputSeek, "-i", filePath, .. outputLimit];
        arguments.AddRange(
        [
            "-map", "0:a:0",
            "-vn",
            "-ac", Channels.ToString(CultureInfo.InvariantCulture),
            "-ar", SampleRate.ToString(CultureInfo.InvariantCulture),
            "-f", "f32le",
            "-",
        ]);

        using Process process = FFmpegRunner.Start(_ffmpegPath, arguments, redirectStandardOutput: true);
        Task<string> stderr = FFmpegRunner.DrainStandardErrorAsync(process);

        try
        {
            await ReadSamplesAsync(process.StandardOutput.BaseStream, sink, cancellationToken)
                .ConfigureAwait(false);

            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

            if (process.ExitCode != 0)
            {
                string error = await stderr.ConfigureAwait(false);
                throw new FFmpegException(
                    $"No se pudo decodificar el audio: {FFmpegRunner.FirstLine(error)}",
                    process.ExitCode,
                    error);
            }
        }
        catch (OperationCanceledException)
        {
            FFmpegRunner.KillQuietly(process);
            throw;
        }
    }

    private static string Seconds(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    /// <summary>Lee la salida de FFmpeg como floats de 32 bits y los reenvía al sumidero en trozos.</summary>
    /// <param name="stream">Salida estándar del proceso de FFmpeg, en formato <c>f32le</c> crudo.</param>
    /// <param name="sink">Destino que consume cada trozo de muestras completas.</param>
    /// <param name="cancellationToken">Token de cancelación para las lecturas asíncronas del flujo.</param>
    private static async Task ReadSamplesAsync(
        Stream stream,
        ISampleSink sink,
        CancellationToken cancellationToken)
    {
        byte[] buffer = ArrayPool<byte>.Shared.Rent(64 * 1024);
        int carry = 0;

        try
        {
            while (true)
            {
                int read = await stream
                    .ReadAsync(buffer.AsMemory(carry, buffer.Length - carry), cancellationToken)
                    .ConfigureAwait(false);

                if (read == 0)
                {
                    break;
                }

                int available = carry + read;
                int usable = available - (available % sizeof(float));

                sink.Write(MemoryMarshal.Cast<byte, float>(buffer.AsSpan(0, usable)));

                // Una lectura puede cortar un float por la mitad; esos bytes sueltos encabezan
                // la siguiente iteración en lugar de descartarse.
                carry = available - usable;
                if (carry > 0)
                {
                    buffer.AsSpan(usable, carry).CopyTo(buffer);
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
