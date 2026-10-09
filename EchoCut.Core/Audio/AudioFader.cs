using System.Buffers;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

namespace EchoCut.Audio;

/// <summary>
/// Escribe la copia recortada de una pista con sus fundidos aplicados. Como
/// <see cref="AudioTrimmer"/>, nunca modifica el original.
/// </summary>
/// <remarks>
/// <para>
/// El audio atraviesa dos procesos de FFmpeg unidos por este: uno decodifica el tramo a PCM
/// flotante, <see cref="FadeEnvelope"/> lo atenúa en bloques y otro lo codifica en el formato del
/// original. La curva es la misma que se dibuja y se escucha en el editor, y nunca hay más de un
/// bloque en memoria.
/// </para>
/// <para>
/// Al decodificar, <c>-ss</c> y <c>-t</c> son exactos a la muestra: la copia con fundido empieza y
/// termina donde se pidió, sin el redondeo a paquete del recorte por copia de flujo.
/// </para>
/// </remarks>
/// <param name="ffmpegPath">Ruta absoluta a <c>ffmpeg.exe</c>, ya resuelta.</param>
/// <param name="ffprobePath">Ruta absoluta a <c>ffprobe.exe</c>, ya resuelta.</param>
public sealed class AudioFader(string ffmpegPath, string ffprobePath)
{
    /// <summary>
    /// Escribe la copia de <paramref name="filePath"/> que conserva <paramref name="range"/> con
    /// <paramref name="fades"/> aplicados, y devuelve la ruta escrita.
    /// </summary>
    /// <param name="filePath">Ruta del archivo original, que no se modifica.</param>
    /// <param name="range">Tramo del original que conserva la copia.</param>
    /// <param name="fades">Fundidos a aplicar, en tiempo del original.</param>
    /// <param name="outputDirectory">Carpeta donde escribir la copia; se crea si no existe.</param>
    /// <param name="cancellationToken">Token de cancelación para los procesos de FFmpeg.</param>
    /// <returns>Ruta completa del archivo escrito.</returns>
    /// <remarks>
    /// Si algo falla o se cancela, se borra la copia a medio escribir: un archivo truncado en la
    /// carpeta de salida pasaría por bueno.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Se lanza si el tramo o los fundidos no son válidos.</exception>
    /// <exception cref="FFmpegException">
    /// Se lanza si el destino coincide con el original, si no hay codificador para el formato o si
    /// alguno de los procesos de FFmpeg falla.
    /// </exception>
    public async Task<string> RenderAsync(
        string filePath,
        TrimRange range,
        TrackFades fades,
        string outputDirectory,
        CancellationToken cancellationToken)
    {
        range.ThrowIfInvalid();
        ArgumentNullException.ThrowIfNull(fades);
        fades.ThrowIfInvalid();

        string destination = AudioTrimmer.PrepareDestination(filePath, outputDirectory);
        AudioStreamInfo source = await AudioStreamInfo.ProbeAsync(ffprobePath, filePath, cancellationToken).ConfigureAwait(false);
        IReadOnlyList<string> encoding = AudioEncoding.ArgumentsFor(source, Path.GetExtension(filePath));
        FadeEnvelope envelope = new(fades, source.SampleRate, range.StartSeconds);

        try
        {
            await PipeAsync(
                BuildDecodeArguments(filePath, range, source),
                BuildEncodeArguments(filePath, source, encoding, destination),
                envelope,
                source.Channels,
                cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            DeleteQuietly(destination);
            throw;
        }

        return destination;
    }

    /// <summary>Decodificación del tramo a PCM flotante entrelazado con la frecuencia y canales del original.</summary>
    /// <remarks>
    /// Frecuencia y canales se fijan aunque coincidan con los del original: el codificador recibe PCM
    /// sin cabecera, y si FFmpeg decidiera otra cosa la copia sonaría acelerada o con los canales
    /// cruzados sin que nada lo delatara.
    /// </remarks>
    private static List<string> BuildDecodeArguments(string filePath, TrimRange range, AudioStreamInfo source)
    {
        List<string> arguments = ["-v", "error"];
        if (range.TrimsStart)
        {
            arguments.AddRange(["-ss", Seconds(range.StartSeconds)]);
        }

        arguments.AddRange(
        [
            "-i", filePath,
            "-t", Seconds(range.DurationSeconds),
            "-map", "0:a:0",
            "-vn",
            "-ac", Integer(source.Channels),
            "-ar", Integer(source.SampleRate),
            "-f", "f32le",
            "-",
        ]);

        return arguments;
    }

    /// <summary>Codificación del PCM recibido por la entrada estándar, con las etiquetas del original.</summary>
    /// <remarks>
    /// El original entra como segunda entrada solo para heredar sus metadatos; de él no se toma
    /// ningún flujo. La carátula no se pide aquí: no todos los contenedores la aceptan como flujo de
    /// vídeo, y quien llama la copia después con TagLibSharp.
    /// </remarks>
    private static List<string> BuildEncodeArguments(
        string filePath,
        AudioStreamInfo source,
        IReadOnlyList<string> encoding,
        string destination) =>
    [
        "-v", "error",
        "-y",
        "-f", "f32le",
        "-ar", Integer(source.SampleRate),
        "-ac", Integer(source.Channels),
        "-i", "pipe:0",
        "-i", filePath,
        "-map", "0:a",
        "-map_metadata", "1",
        .. encoding,
        destination,
    ];

    /// <summary>Lanza ambos procesos, bombea el audio de uno a otro y comprueba cómo terminaron.</summary>
    private async Task PipeAsync(
        List<string> decodeArguments,
        List<string> encodeArguments,
        FadeEnvelope envelope,
        int channels,
        CancellationToken cancellationToken)
    {
        using Process decoder = FFmpegRunner.Start(ffmpegPath, decodeArguments, redirectStandardOutput: true);
        using Process encoder = FFmpegRunner.Start(ffmpegPath, encodeArguments, redirectStandardOutput: false, redirectStandardInput: true);
        Task<string> decoderErrors = FFmpegRunner.DrainStandardErrorAsync(decoder);
        Task<string> encoderErrors = FFmpegRunner.DrainStandardErrorAsync(encoder);

        bool encoderStopped = false;
        try
        {
            try
            {
                await PumpAsync(decoder.StandardOutput.BaseStream, encoder.StandardInput.BaseStream, envelope, channels, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (IOException)
            {
                // El codificador cerró su entrada antes de tiempo, casi siempre porque falló. El
                // decodificador quedaría bloqueado escribiendo en una tubería que ya nadie lee.
                encoderStopped = true;
                FFmpegRunner.KillQuietly(decoder);
            }

            // Cerrar la entrada es lo que le dice al codificador que el audio terminó.
            CloseQuietly(encoder.StandardInput);

            await encoder.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            await decoder.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            FFmpegRunner.KillQuietly(decoder);
            FFmpegRunner.KillQuietly(encoder);
            throw;
        }

        if (encoderStopped || encoder.ExitCode != 0)
        {
            string error = await encoderErrors.ConfigureAwait(false);
            throw new FFmpegException(
                $"No se pudo codificar la copia con fundido: {FFmpegRunner.FirstLine(error)}",
                encoder.ExitCode,
                error);
        }

        if (decoder.ExitCode != 0)
        {
            string error = await decoderErrors.ConfigureAwait(false);
            throw new FFmpegException(
                $"No se pudo decodificar el audio: {FFmpegRunner.FirstLine(error)}",
                decoder.ExitCode,
                error);
        }
    }

    /// <summary>Copia el PCM del decodificador al codificador, aplicando los fundidos por el camino.</summary>
    /// <remarks>
    /// Una lectura puede partir una trama; los bytes sobrantes encabezan la siguiente vuelta para que
    /// la envolvente nunca vea una trama incompleta ni pierda la cuenta de su posición.
    /// </remarks>
    private static async Task PumpAsync(
        Stream input,
        Stream output,
        FadeEnvelope envelope,
        int channels,
        CancellationToken cancellationToken)
    {
        int frameBytes = channels * sizeof(float);
        byte[] buffer = ArrayPool<byte>.Shared.Rent(64 * 1024);
        int carry = 0;
        long frame = 0;

        try
        {
            while (true)
            {
                int read = await input
                    .ReadAsync(buffer.AsMemory(carry, buffer.Length - carry), cancellationToken)
                    .ConfigureAwait(false);

                if (read == 0)
                {
                    break;
                }

                int available = carry + read;
                int usable = available - (available % frameBytes);

                Attenuate(buffer, usable, envelope, channels, frame);
                await output.WriteAsync(buffer.AsMemory(0, usable), cancellationToken).ConfigureAwait(false);
                frame += usable / frameBytes;

                carry = available - usable;
                if (carry > 0)
                {
                    buffer.AsSpan(usable, carry).CopyTo(buffer);
                }
            }

            await output.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>Aplica la envolvente a las tramas completas del búfer.</summary>
    /// <remarks>Va aparte porque un <see cref="Span{T}"/> no puede cruzar el <c>await</c> del bombeo.</remarks>
    private static void Attenuate(byte[] buffer, int count, FadeEnvelope envelope, int channels, long firstFrame) =>
        envelope.Apply(MemoryMarshal.Cast<byte, float>(buffer.AsSpan(0, count)), channels, firstFrame);

    private static void CloseQuietly(StreamWriter input)
    {
        try
        {
            input.Close();
        }
        catch (IOException)
        {
            // La tubería ya estaba rota: el código de salida del codificador dirá por qué.
        }
    }

    private static void DeleteQuietly(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Si no se puede borrar, el error original sigue siendo lo que hay que contar.
        }
    }

    /// <summary>Segundos con precisión de microsegundo: el tramo se decodifica exacto a la muestra.</summary>
    private static string Seconds(double value) => value.ToString("0.######", CultureInfo.InvariantCulture);

    private static string Integer(int value) => value.ToString(CultureInfo.InvariantCulture);
}
