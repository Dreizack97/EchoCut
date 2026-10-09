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
    /// Escribe la copia de <paramref name="filePath"/> que conserva <paramref name="range"/> y
    /// devuelve la ruta escrita.
    /// </summary>
    /// <param name="filePath">Ruta del archivo original, que no se modifica.</param>
    /// <param name="range">Tramo del original que conserva la copia.</param>
    /// <param name="outputDirectory">Carpeta donde escribir la copia recortada; se crea si no existe.</param>
    /// <param name="cancellationToken">Token de cancelación para el proceso de FFmpeg subyacente.</param>
    /// <returns>Ruta completa del archivo recortado ya escrito.</returns>
    /// <remarks>
    /// Con <c>-c copy</c> no hay recodificación: cero pérdida generacional y una escritura casi
    /// instantánea. A cambio, cada extremo se alinea a un límite de paquete comprimido, y lo hace de
    /// forma distinta según el códec (medido con FFmpeg 8.1):
    /// <list type="bullet">
    ///   <item>WAV y M4A/AAC: inicio exacto; MP3: a medio paquete del pedido (±13 ms).</item>
    ///   <item>FLAC y Vorbis: el inicio retrocede al bloque o página anterior (hasta ~93 ms y ~0.9 s).</item>
    ///   <item>
    ///     MP3, AAC y Opus: los primeros milisegundos de la copia no coinciden con el original
    ///     (~8 ms en MP3 y AAC, ~110 ms atenuados en Opus) porque el decodificador arranca sin el
    ///     estado de los paquetes previos.
    ///   </item>
    /// </list>
    /// Ninguna de esas desviaciones es audible porque el inicio pedido cae dentro del silencio,
    /// separado de la música por la tolerancia. El final, en cambio, se mide siempre desde el
    /// inicio <em>pedido</em> y no desde el paquete donde arrancó la copia.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Se lanza si <paramref name="range"/> no es válido.</exception>
    /// <exception cref="FFmpegException">
    /// Se lanza si el destino calculado coincide con el archivo original, o si el proceso de FFmpeg
    /// termina con un código de salida distinto de cero.
    /// </exception>
    public async Task<string> TrimAsync(
        string filePath,
        TrimRange range,
        string outputDirectory,
        CancellationToken cancellationToken)
    {
        range.ThrowIfInvalid();
        string destination = PrepareDestination(filePath, outputDirectory);

        await FFmpegRunner
            .RunCheckedAsync(_ffmpegPath, BuildArguments(filePath, range, destination), cancellationToken)
            .ConfigureAwait(false);

        return destination;
    }

    /// <summary>Crea la carpeta de salida y calcula la ruta de la copia, sin pisar nunca el original.</summary>
    /// <param name="filePath">Ruta del archivo original.</param>
    /// <param name="outputDirectory">Carpeta donde escribir la copia; se crea si no existe.</param>
    /// <param name="extension">
    /// Extensión de la copia, con el punto, si cambia de formato; <c>null</c> para conservar la del original.
    /// </param>
    /// <returns>Ruta completa de la copia, con el mismo nombre que el original.</returns>
    /// <exception cref="FFmpegException">Se lanza si el destino calculado coincide con el original.</exception>
    internal static string PrepareDestination(string filePath, string outputDirectory, string? extension = null)
    {
        Directory.CreateDirectory(outputDirectory);
        string fileName = extension is null ? Path.GetFileName(filePath) : Path.GetFileNameWithoutExtension(filePath) + extension;
        string destination = Path.Combine(outputDirectory, fileName);

        if (string.Equals(Path.GetFullPath(destination), Path.GetFullPath(filePath), StringComparison.OrdinalIgnoreCase))
        {
            throw new FFmpegException("El archivo de salida coincide con el original; se omite para no sobrescribirlo.");
        }

        return destination;
    }

    /// <summary>Construye la línea de órdenes de FFmpeg para un recorte por copia de flujo.</summary>
    /// <param name="filePath">Archivo original.</param>
    /// <param name="range">Tramo que se conserva, ya validado.</param>
    /// <param name="destination">Archivo a escribir.</param>
    /// <returns>Argumentos en el orden en que FFmpeg los interpreta.</returns>
    /// <remarks>
    /// <c>-ss</c> va <em>antes</em> de <c>-i</c>: como opción de entrada salta directamente al
    /// paquete en vez de leer y descartar todo lo anterior, y reinicia las marcas de tiempo, de modo
    /// que <c>-t</c> se cuenta desde el inicio pedido. Si no se recorta el inicio se omite por
    /// completo para que la copia arranque en el mismo primer paquete que el original.
    /// </remarks>
    private static List<string> BuildArguments(string filePath, TrimRange range, string destination)
    {
        List<string> arguments = ["-v", "error", "-y"];

        if (range.TrimsStart)
        {
            arguments.AddRange(["-ss", Seconds(range.StartSeconds)]);
        }

        arguments.AddRange(
        [
            "-i", filePath,
            "-t", Seconds(range.DurationSeconds),
            "-map", "0",
            "-c", "copy",
            "-map_metadata", "0",
            "-id3v2_version", "3",
            destination,
        ]);

        return arguments;
    }

    private static string Seconds(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
}
