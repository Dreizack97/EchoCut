namespace EchoCut.Library;

/// <summary>Lo que dio de sí el escaneo de una carpeta.</summary>
/// <param name="Tracks">Pistas leídas correctamente, en el orden en que aparecieron en la carpeta.</param>
/// <param name="SkippedCount">Archivos con extensión admitida que no se pudieron leer.</param>
public sealed record ScanResult(IReadOnlyList<TrackInfo> Tracks, int SkippedCount);

/// <summary>Lee los metadatos de los archivos de audio de una carpeta.</summary>
public static class TrackScanner
{
    /// <summary>Escanea una carpeta y devuelve sus pistas de audio legibles.</summary>
    /// <param name="directory">Carpeta a escanear. No se recorre en profundidad.</param>
    /// <returns>
    /// Las pistas leídas y cuántos archivos se omitieron. Un archivo corrupto, bloqueado o de un
    /// formato que TagLibSharp no reconozca se cuenta como omitido en vez de abortar el escaneo:
    /// una sola pista dañada no debe impedir trabajar con el resto de la carpeta.
    /// </returns>
    public static ScanResult Scan(string directory)
    {
        List<TrackInfo> tracks = [];
        int skipped = 0;

        foreach (string path in Directory.GetFiles(directory).Where(AudioFileTypes.IsSupported))
        {
            try
            {
                tracks.Add(Read(path));
            }
            catch (Exception exception) when (exception is IOException
                                                  or UnauthorizedAccessException
                                                  or TagLib.CorruptFileException
                                                  or TagLib.UnsupportedFormatException)
            {
                skipped++;
            }
        }

        return new ScanResult(tracks, skipped);
    }

    /// <summary>Escanea múltiples carpetas y devuelve la combinación de sus pistas de audio legibles.</summary>
    /// <param name="directories">Colección de carpetas a escanear.</param>
    /// <returns>Las pistas leídas de todas las carpetas y el total de archivos omitidos.</returns>
    public static ScanResult Scan(IEnumerable<string> directories)
    {
        List<TrackInfo> tracks = [];
        int skipped = 0;

        foreach (string directory in directories)
        {
            if (!Directory.Exists(directory))
            {
                continue;
            }

            ScanResult result = Scan(directory);
            tracks.AddRange(result.Tracks);
            skipped += result.SkippedCount;
        }

        return new ScanResult(tracks, skipped);
    }

    /// <summary>Lee los metadatos de una colección de archivos de audio seleccionados.</summary>
    /// <param name="filePaths">Rutas de los archivos a leer.</param>
    /// <returns>Las pistas leídas y cuántos archivos no se pudieron leer o no son compatibles.</returns>
    /// <exception cref="IOException">Se lanza si el archivo no se puede leer.</exception>
    /// <exception cref="UnauthorizedAccessException">Se lanza si no hay permiso para leerlo.</exception>
    /// <exception cref="TagLib.CorruptFileException">Se lanza si los metadatos están dañados.</exception>
    /// <exception cref="TagLib.UnsupportedFormatException">Se lanza si TagLibSharp no reconoce el formato.</exception>
    public static ScanResult ScanFiles(IEnumerable<string> filePaths)
    {
        List<TrackInfo> tracks = [];
        int skipped = 0;

        foreach (string path in filePaths)
        {
            if (!AudioFileTypes.IsSupported(path))
            {
                skipped++;
                continue;
            }

            try
            {
                tracks.Add(Read(path));
            }
            catch (Exception exception) when (exception is IOException
                                                  or UnauthorizedAccessException
                                                  or TagLib.CorruptFileException
                                                  or TagLib.UnsupportedFormatException)
            {
                skipped++;
            }
        }

        return new ScanResult(tracks, skipped);
    }

    /// <summary>Lee los metadatos de un solo archivo.</summary>
    /// <param name="filePath">Ruta del archivo de audio.</param>
    /// <returns>Los metadatos del archivo.</returns>
    /// <exception cref="IOException">Se lanza si el archivo no se puede leer.</exception>
    /// <exception cref="UnauthorizedAccessException">Se lanza si no hay permiso para leerlo.</exception>
    /// <exception cref="TagLib.CorruptFileException">Se lanza si los metadatos están dañados.</exception>
    /// <exception cref="TagLib.UnsupportedFormatException">Se lanza si TagLibSharp no reconoce el formato.</exception>
    public static TrackInfo Read(string filePath)
    {
        using TagLib.File tagFile = TagLib.File.Create(filePath);
        FileInfo file = new(filePath);

        return new TrackInfo(
            FilePath: filePath,
            Name: Path.GetFileNameWithoutExtension(file.Name),
            Title: tagFile.Tag.Title ?? string.Empty,
            Artist: tagFile.Tag.FirstAlbumArtist ?? tagFile.Tag.FirstPerformer ?? string.Empty,
            Album: tagFile.Tag.Album ?? string.Empty,
            Duration: tagFile.Properties.Duration,
            Extension: file.Extension,
            BitrateKbps: tagFile.Properties.AudioBitrate,
            SizeBytes: file.Length);
    }
}
