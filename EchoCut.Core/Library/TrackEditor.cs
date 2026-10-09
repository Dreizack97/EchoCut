namespace EchoCut.Library;

/// <summary>Permite actualizar metadatos y renombrar archivos de pistas de audio.</summary>
public static class TrackEditor
{
    /// <summary>
    /// Carga el conjunto completo de propiedades y metadatos del archivo indicado.
    /// </summary>
    /// <param name="filePath">Ruta del archivo a inspeccionar.</param>
    /// <returns>Estructura con las propiedades del archivo y de los metadatos.</returns>
    /// <exception cref="FileNotFoundException">Si el archivo no existe.</exception>
    public static TrackProperties LoadProperties(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("El archivo de audio no existe en disco.", filePath);
        }

        FileInfo file = new(filePath);
        using TagLib.File tagFile = TagLib.File.Create(filePath);

        return new TrackProperties
        {
            FilePath = filePath,
            FileName = Path.GetFileNameWithoutExtension(file.Name),
            Extension = file.Extension,
            Directory = file.DirectoryName ?? string.Empty,
            SizeBytes = file.Length,
            CreationTime = file.CreationTime,
            LastWriteTime = file.LastWriteTime,
            LastAccessTime = file.LastAccessTime,
            Attributes = file.Attributes,

            Title = tagFile.Tag.Title ?? string.Empty,
            Subtitle = tagFile.Tag.Subtitle ?? string.Empty,
            Comment = tagFile.Tag.Comment ?? string.Empty,

            Performers = string.Join("; ", tagFile.Tag.Performers ?? []),
            AlbumArtist = string.Join("; ", tagFile.Tag.AlbumArtists ?? []),
            Album = tagFile.Tag.Album ?? string.Empty,
            Year = tagFile.Tag.Year,
            Track = tagFile.Tag.Track,
            Genre = string.Join("; ", tagFile.Tag.Genres ?? []),
            Duration = tagFile.Properties.Duration,

            BitrateKbps = tagFile.Properties.AudioBitrate,
            Channels = tagFile.Properties.AudioChannels,
            SampleRateHz = tagFile.Properties.AudioSampleRate,

            Composers = string.Join("; ", tagFile.Tag.Composers ?? []),
            Copyright = tagFile.Tag.Copyright ?? string.Empty,
        };
    }

    /// <summary>
    /// Guarda los cambios de metadatos y opcionalmente renombra el archivo según las propiedades indicadas.
    /// </summary>
    /// <param name="properties">Propiedades modificadas.</param>
    /// <returns>La información de pista actualizada a partir del archivo en disco.</returns>
    /// <exception cref="ArgumentNullException">Si <paramref name="properties"/> es <c>null</c>.</exception>
    /// <exception cref="FileNotFoundException">Si el archivo original no existe en disco.</exception>
    /// <exception cref="ArgumentException">Si el nuevo nombre contiene caracteres no válidos o está vacío.</exception>
    /// <exception cref="IOException">Si ya existe otro archivo con el nuevo nombre o si falla la escritura.</exception>
    public static TrackInfo SaveProperties(TrackProperties properties)
    {
        ArgumentNullException.ThrowIfNull(properties);

        if (!File.Exists(properties.FilePath))
        {
            throw new FileNotFoundException("El archivo de audio no existe en disco.", properties.FilePath);
        }

        string cleanName = properties.FileName.Trim();
        if (string.IsNullOrWhiteSpace(cleanName))
        {
            throw new ArgumentException("El nombre del archivo no puede estar vacío.", nameof(properties));
        }

        if (cleanName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new ArgumentException("El nombre del archivo contiene caracteres no válidos.", nameof(properties));
        }

        string currentPath = properties.FilePath;

        // 1. Guardar etiquetas en el archivo
        using (TagLib.File tagFile = TagLib.File.Create(currentPath))
        {
            tagFile.Tag.Title = properties.Title.Trim();
            tagFile.Tag.Subtitle = properties.Subtitle.Trim();
            tagFile.Tag.Comment = properties.Comment.Trim();

            tagFile.Tag.Performers = SplitList(properties.Performers);
            tagFile.Tag.AlbumArtists = SplitList(properties.AlbumArtist);
            tagFile.Tag.Album = properties.Album.Trim();
            tagFile.Tag.Year = properties.Year;
            tagFile.Tag.Track = properties.Track;
            tagFile.Tag.Genres = SplitList(properties.Genre);

            tagFile.Tag.Composers = SplitList(properties.Composers);
            tagFile.Tag.Copyright = properties.Copyright.Trim();

            tagFile.Save();
        }

        // 2. Renombrar archivo si el nombre cambió
        string newFilePath = Rename(currentPath, cleanName);
        if (!string.Equals(currentPath, newFilePath, StringComparison.Ordinal))
        {
            currentPath = newFilePath;
            properties.FilePath = newFilePath;
            properties.FileName = cleanName;
        }

        // 3. Volver a leer la pista actualizada desde el disco
        return TrackScanner.Read(currentPath);
    }

    /// <summary>
    /// Renombra un archivo dentro de su misma carpeta, conservando la extensión.
    /// </summary>
    /// <param name="currentPath">Ruta actual del archivo.</param>
    /// <param name="newBaseName">Nombre nuevo, sin extensión.</param>
    /// <returns>La ruta resultante; la misma si el nombre no cambia.</returns>
    /// <exception cref="IOException">Si ya existe otro archivo con el nombre nuevo.</exception>
    /// <remarks>
    /// Un cambio que solo afecta a mayúsculas se permite aunque <see cref="File.Exists"/> dé por
    /// existente el destino: en NTFS ambos nombres son el mismo archivo, y normalizar a TitleCase
    /// produce justo ese tipo de cambio.
    /// </remarks>
    private static string Rename(string currentPath, string newBaseName)
    {
        string newFileName = newBaseName + Path.GetExtension(currentPath);
        string newPath = Path.Combine(Path.GetDirectoryName(currentPath) ?? string.Empty, newFileName);

        if (string.Equals(currentPath, newPath, StringComparison.Ordinal))
        {
            return currentPath;
        }

        if (!string.Equals(currentPath, newPath, StringComparison.OrdinalIgnoreCase) && File.Exists(newPath))
        {
            throw new IOException($"Ya existe un archivo llamado «{newFileName}» en la misma carpeta.");
        }

        File.Move(currentPath, newPath);
        return newPath;
    }

    private static string[] SplitList(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        return text.Split([';', '/'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    /// <summary>
    /// Elimina todos los metadatos y etiquetas del archivo de audio indicado en disco.
    /// </summary>
    /// <param name="filePath">Ruta absoluta del archivo de audio.</param>
    /// <returns>La información de pista actualizada a partir del archivo en disco sin metadatos.</returns>
    /// <exception cref="ArgumentException">Si <paramref name="filePath"/> es nulo o está en blanco.</exception>
    /// <exception cref="FileNotFoundException">Si el archivo no existe en disco.</exception>
    public static TrackInfo StripMetadata(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("El archivo de audio no existe en disco.", filePath);
        }

        using (TagLib.File tagFile = TagLib.File.Create(filePath))
        {
            tagFile.RemoveTags(TagLib.TagTypes.AllTags);
            tagFile.Save();
        }

        return TrackScanner.Read(filePath);
    }

    /// <summary>
    /// Normaliza los metadatos y el nombre de archivo de una pista de audio (eliminando acentos diacríticos,
    /// preservando la «ñ» / «Ñ» y aplicando TitleCase), y actualiza el archivo en disco.
    /// </summary>
    /// <param name="filePath">Ruta absoluta del archivo a normalizar.</param>
    /// <returns>La información de pista actualizada tras la normalización y posible renombrado.</returns>
    /// <exception cref="ArgumentException">Si <paramref name="filePath"/> es nulo o está en blanco.</exception>
    /// <exception cref="FileNotFoundException">Si el archivo no existe en disco.</exception>
    public static TrackInfo NormalizeTrack(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("El archivo de audio no existe en disco.", filePath);
        }

        TrackProperties properties = LoadProperties(filePath);

        properties.FileName = TextNormalizer.NormalizeTitleCase(properties.FileName);
        properties.Title = TextNormalizer.NormalizeTitleCase(properties.Title);
        properties.Subtitle = TextNormalizer.NormalizeTitleCase(properties.Subtitle);
        properties.Comment = TextNormalizer.NormalizeTitleCase(properties.Comment);
        properties.Performers = TextNormalizer.NormalizeTitleCase(properties.Performers);
        properties.AlbumArtist = TextNormalizer.NormalizeTitleCase(properties.AlbumArtist);
        properties.Album = TextNormalizer.NormalizeTitleCase(properties.Album);
        properties.Genre = TextNormalizer.NormalizeTitleCase(properties.Genre);
        properties.Composers = TextNormalizer.NormalizeTitleCase(properties.Composers);
        properties.Copyright = TextNormalizer.NormalizeTitleCase(properties.Copyright);

        return SaveProperties(properties);
    }
}
