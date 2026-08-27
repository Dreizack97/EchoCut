using System.Globalization;

namespace EchoCut.Audio;

/// <summary>
/// Escribe la versión recortada en una subcarpeta. Los archivos originales no se modifican nunca.
/// </summary>
public sealed class AudioTrimmer
{
    /// <summary>Subcarpeta de salida, creada dentro de la carpeta analizada.</summary>
    public const string OutputFolderName = "Recortados";

    private readonly string _ffmpegPath;

    /// <summary>Crea el recortador contra el ejecutable de FFmpeg indicado.</summary>
    /// <param name="ffmpegPath">Ruta absoluta a <c>ffmpeg.exe</c>, ya resuelta.</param>
    public AudioTrimmer(string ffmpegPath) => _ffmpegPath = ffmpegPath;

    /// <summary>Calcula la carpeta de salida para una carpeta de origen dada.</summary>
    /// <param name="sourceDirectory">Carpeta que contiene los archivos originales.</param>
    /// <returns><paramref name="sourceDirectory"/> combinada con <see cref="OutputFolderName"/>.</returns>
    public static string GetOutputDirectory(string sourceDirectory) =>
        Path.Combine(sourceDirectory, OutputFolderName);

    /// <summary>
    /// Recorta <paramref name="filePath"/> en <paramref name="cutSeconds"/> y devuelve la ruta escrita.
    /// </summary>
    /// <param name="filePath">Ruta del archivo original, que no se modifica.</param>
    /// <param name="cutSeconds">Instante de corte, en segundos desde el principio del archivo.</param>
    /// <param name="outputDirectory">Carpeta donde escribir la copia recortada; se crea si no existe.</param>
    /// <param name="cancellationToken">Token de cancelación para el proceso de FFmpeg subyacente.</param>
    /// <returns>Ruta completa del archivo recortado ya escrito.</returns>
    /// <remarks>
    /// Con <c>-c copy</c> no hay recodificación: cero pérdida generacional y una escritura casi
    /// instantánea. A cambio, el corte se alinea al límite del paquete comprimido más cercano
    /// (desviación típica por debajo de 26 ms en MP3), lo cual queda holgadamente dentro del
    /// margen de tolerancia que se conserva al recortar.
    /// </remarks>
    /// <exception cref="FFmpegException">
    /// Se lanza si el destino calculado coincide con el archivo original, o si el proceso de FFmpeg
    /// termina con un código de salida distinto de cero.
    /// </exception>
    public async Task<string> TrimAsync(
        string filePath,
        double cutSeconds,
        string outputDirectory,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(outputDirectory);
        string destination = Path.Combine(outputDirectory, Path.GetFileName(filePath));

        if (string.Equals(Path.GetFullPath(destination), Path.GetFullPath(filePath), StringComparison.OrdinalIgnoreCase))
        {
            throw new FFmpegException("El archivo de salida coincide con el original; se omite para no sobrescribirlo.");
        }

        string[] arguments =
        [
            "-v", "error",
            "-y",
            "-i", filePath,
            "-t", cutSeconds.ToString("0.###", CultureInfo.InvariantCulture),
            "-map", "0",
            "-c", "copy",
            "-map_metadata", "0",
            "-id3v2_version", "3",
            destination,
        ];

        await FFmpegRunner.RunCheckedAsync(_ffmpegPath, arguments, cancellationToken).ConfigureAwait(false);
        return destination;
    }
}
