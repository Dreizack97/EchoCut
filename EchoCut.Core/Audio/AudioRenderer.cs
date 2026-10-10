using System.Buffers;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

namespace EchoCut.Audio;

/// <summary>
/// Escribe la copia recortada de una pista con sus ediciones aplicadas: fundidos y fragmentos
/// borrados. Como <see cref="AudioTrimmer"/>, nunca modifica el original.
/// </summary>
/// <remarks>
/// <para>
/// El audio atraviesa dos procesos de FFmpeg unidos por este: uno decodifica el tramo a PCM
/// flotante, <see cref="FadeEnvelope"/> lo atenúa en bloques, se descartan las tramas borradas y
/// otro lo codifica en el formato del original. Es lo mismo que se dibuja y se escucha en el
/// editor, y nunca hay más de un bloque en memoria.
/// </para>
/// <para>
/// Al decodificar, <c>-ss</c> y <c>-t</c> son exactos a la muestra: la copia empieza y termina donde
/// se pidió, sin el redondeo a paquete del recorte por copia de flujo.
/// </para>
/// <para>
/// El orden importa: primero se aplican los fundidos y después se borra, porque ambos se expresan en
/// tiempo del original. Así un fundido que cruza un borrado conserva la ganancia que tenía cada
/// muestra que sobrevive, como en Audacity al borrar dentro de un tramo ya atenuado.
/// </para>
/// </remarks>
/// <param name="ffmpegPath">Ruta absoluta a <c>ffmpeg.exe</c>, ya resuelta.</param>
/// <param name="ffprobePath">Ruta absoluta a <c>ffprobe.exe</c>, ya resuelta.</param>
public sealed class AudioRenderer(string ffmpegPath, string ffprobePath)
{
    /// <summary>Audio mínimo que debe quedar en la copia tras borrar: una copia vacía no es un archivo de audio.</summary>
    private const double MinimumKeptSeconds = 0.01;

    /// <summary>
    /// Escribe la copia de <paramref name="filePath"/> que conserva <paramref name="range"/> con
    /// <paramref name="edits"/> aplicadas, y devuelve la ruta escrita.
    /// </summary>
    /// <param name="filePath">Ruta del archivo original, que no se modifica.</param>
    /// <param name="range">Tramo del original que conserva la copia.</param>
    /// <param name="edits">Fundidos y borrados a aplicar, en tiempo del original.</param>
    /// <param name="outputDirectory">Carpeta donde escribir la copia; se crea si no existe.</param>
    /// <param name="cancellationToken">Token de cancelación para los procesos de FFmpeg.</param>
    /// <returns>Ruta completa del archivo escrito.</returns>
    /// <remarks>
    /// Si algo falla o se cancela, se borra la copia a medio escribir: un archivo truncado en la
    /// carpeta de salida pasaría por bueno.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Se lanza si el tramo o los fundidos no son válidos, o si lo borrado no deja audio en la copia.
    /// </exception>
    /// <exception cref="FFmpegException">
    /// Se lanza si el destino coincide con el original, si no hay codificador para el formato o si
    /// alguno de los procesos de FFmpeg falla.
    /// </exception>
    public async Task<string> RenderAsync(
        string filePath,
        TrimRange range,
        AudioEdits edits,
        string outputDirectory,
        CancellationToken cancellationToken)
    {
        range.ThrowIfInvalid();
        ArgumentNullException.ThrowIfNull(edits);
        edits.ThrowIfInvalid();

        if (edits.KeptSeconds(range) < MinimumKeptSeconds)
        {
            throw new ArgumentOutOfRangeException(nameof(edits), "Lo borrado no deja audio que escribir en la copia.");
        }

        string destination = AudioTrimmer.PrepareDestination(filePath, outputDirectory);
        AudioStreamInfo source = await AudioStreamInfo.ProbeAsync(ffprobePath, filePath, cancellationToken).ConfigureAwait(false);
        IReadOnlyList<string> encoding = AudioEncoding.ArgumentsFor(source, Path.GetExtension(filePath));
        PcmEditor editor = new(edits, source.SampleRate, range.StartSeconds, source.Channels);

        try
        {
            await PipeAsync(
                BuildDecodeArguments(filePath, range, source),
                BuildEncodeArguments(filePath, source, encoding, destination),
                editor,
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
        PcmEditor editor,
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
                await PumpAsync(decoder.StandardOutput.BaseStream, encoder.StandardInput.BaseStream, editor, cancellationToken)
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
                $"No se pudo codificar la copia editada: {FFmpegRunner.FirstLine(error)}",
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

    /// <summary>Copia el PCM del decodificador al codificador, aplicando las ediciones por el camino.</summary>
    /// <remarks>
    /// Una lectura puede partir una trama; los bytes sobrantes encabezan la siguiente vuelta para que
    /// el editor nunca vea una trama incompleta ni pierda la cuenta de su posición en el original.
    /// </remarks>
    private static async Task PumpAsync(
        Stream input,
        Stream output,
        PcmEditor editor,
        CancellationToken cancellationToken)
    {
        int frameBytes = editor.Channels * sizeof(float);
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

                // El editor compacta lo que sobrevive al principio del búfer; los bytes sobrantes de
                // la trama partida quedan detrás, intactos.
                int kept = Edit(buffer, usable, editor, frame);
                await output.WriteAsync(buffer.AsMemory(0, kept * frameBytes), cancellationToken).ConfigureAwait(false);
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

    /// <summary>Aplica las ediciones a las tramas completas del búfer y devuelve cuántas se conservan.</summary>
    /// <remarks>Va aparte porque un <see cref="Span{T}"/> no puede cruzar el <c>await</c> del bombeo.</remarks>
    private static int Edit(byte[] buffer, int count, PcmEditor editor, long firstFrame) =>
        editor.Process(MemoryMarshal.Cast<byte, float>(buffer.AsSpan(0, count)), firstFrame);

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
