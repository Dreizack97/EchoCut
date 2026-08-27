using System.Buffers;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

namespace EchoCut.Audio;

/// <summary>
/// Decodificación vía FFmpeg. El audio se pide en mono, a baja frecuencia y en punto flotante:
/// para métricas de energía sobra, y reduce el PCM que atraviesa la tubería a ~88 KB/s.
/// </summary>
public sealed class AudioDecoder
{
    /// <summary>Frecuencia de análisis. Muy por encima de lo que exige una medida de energía.</summary>
    public const int AnalysisSampleRate = 22050;

    private readonly string _ffmpegPath;
    private readonly string _ffprobePath;

    /// <summary>Crea el decodificador contra los ejecutables de FFmpeg indicados.</summary>
    /// <param name="ffmpegPath">Ruta absoluta a <c>ffmpeg.exe</c>, ya resuelta.</param>
    /// <param name="ffprobePath">Ruta absoluta a <c>ffprobe.exe</c>, ya resuelta.</param>
    public AudioDecoder(string ffmpegPath, string ffprobePath)
    {
        _ffmpegPath = ffmpegPath;
        _ffprobePath = ffprobePath;
    }

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
    public async Task DecodeTailIntoAsync(
        string filePath,
        double windowSeconds,
        double totalDurationSeconds,
        ISampleSink sink,
        CancellationToken cancellationToken)
    {
        bool wholeFile = windowSeconds >= totalDurationSeconds;

        List<string> arguments = ["-v", "error"];
        if (!wholeFile)
        {
            arguments.Add("-sseof");
            arguments.Add((-windowSeconds).ToString("0.###", CultureInfo.InvariantCulture));
        }

        arguments.AddRange(
        [
            "-i", filePath,
            "-map", "0:a:0",
            "-vn",
            "-ac", "1",
            "-ar", AnalysisSampleRate.ToString(CultureInfo.InvariantCulture),
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
