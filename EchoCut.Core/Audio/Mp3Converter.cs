namespace EchoCut.Audio;

/// <summary>
/// Escribe una copia en MP3 de una pista. Como el recorte, nunca modifica el original: la copia va
/// a una carpeta de salida.
/// </summary>
/// <remarks>
/// <para>
/// Se convierte el original entero, tal cual: la conversión es una utilidad de biblioteca, y el
/// recorte y las ediciones siguen siendo cosa de «Recortar».
/// </para>
/// <para>
/// FFmpeg negocia por su cuenta lo que LAME no admite: remuestrea por encima de 48 kHz y mezcla a
/// estéreo lo que tenga más de dos canales. La carátula no se pide aquí, porque tendría que tratarse
/// como flujo de vídeo; quien llama la copia después con TagLibSharp, como en la copia editada.
/// </para>
/// </remarks>
/// <param name="ffmpegPath">Ruta absoluta a <c>ffmpeg.exe</c>, ya resuelta.</param>
public sealed class Mp3Converter(string ffmpegPath)
{
    /// <summary>Extensión de las copias convertidas.</summary>
    public const string Extension = ".mp3";

    /// <summary>Si un archivo ya es MP3 y convertirlo solo costaría calidad.</summary>
    /// <param name="filePath">Ruta del archivo.</param>
    /// <returns><c>true</c> si su extensión es <c>.mp3</c>, sin distinguir mayúsculas.</returns>
    public static bool IsMp3(string filePath) =>
        string.Equals(Path.GetExtension(filePath), Extension, StringComparison.OrdinalIgnoreCase);

    /// <summary>Escribe la copia en MP3 y devuelve su ruta.</summary>
    /// <param name="filePath">Ruta del archivo original, que no se modifica.</param>
    /// <param name="outputDirectory">Carpeta donde escribir la copia; se crea si no existe.</param>
    /// <param name="quality">Calidad del MP3.</param>
    /// <param name="cancellationToken">Token de cancelación para el proceso de FFmpeg.</param>
    /// <returns>Ruta completa del MP3 escrito.</returns>
    /// <remarks>Si algo falla o se cancela, se borra la copia a medio escribir.</remarks>
    /// <exception cref="FFmpegException">Se lanza si el destino coincide con el original o si FFmpeg falla.</exception>
    public async Task<string> ConvertAsync(
        string filePath,
        string outputDirectory,
        Mp3Quality quality,
        CancellationToken cancellationToken)
    {
        string destination = AudioTrimmer.PrepareDestination(filePath, outputDirectory, Extension);

        List<string> arguments =
        [
            "-v", "error",
            "-y",
            "-i", filePath,
            "-map", "0:a:0",
            "-vn",
            "-c:a", "libmp3lame",
            .. quality.EncoderArguments(),
            "-map_metadata", "0",
            "-id3v2_version", "3",
            destination,
        ];

        try
        {
            await FFmpegRunner.RunCheckedAsync(ffmpegPath, arguments, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            DeleteQuietly(destination);
            throw;
        }

        return destination;
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
}
