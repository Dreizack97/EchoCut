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

        string cleanName = ValidFileName(properties.FileName, nameof(properties));

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
    /// Renombra el archivo de una pista dentro de su carpeta, conservando la extensión y sin tocar
    /// sus etiquetas.
    /// </summary>
    /// <param name="filePath">Ruta absoluta del archivo de audio.</param>
    /// <param name="newBaseName">Nombre nuevo, sin extensión; se recortan los espacios de los extremos.</param>
    /// <returns>La información de pista releída desde la ruta nueva.</returns>
    /// <exception cref="ArgumentException">Si el nombre nuevo está vacío, contiene caracteres no válidos o termina en punto.</exception>
    /// <exception cref="FileNotFoundException">Si el archivo no existe en disco.</exception>
    /// <exception cref="IOException">Si ya existe otro archivo con el nombre nuevo en la carpeta.</exception>
    public static TrackInfo RenameTrack(string filePath, string newBaseName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("El archivo de audio no existe en disco.", filePath);
        }

        string cleanName = ValidFileName(newBaseName, nameof(newBaseName));
        return TrackScanner.Read(Rename(filePath, cleanName));
    }

    /// <summary>Recorta y valida un nombre de archivo sin extensión con <see cref="TrackNaming.GetProblem"/>.</summary>
    /// <param name="name">Nombre propuesto.</param>
    /// <param name="paramName">Parámetro del llamador al que atribuir el error.</param>
    /// <returns>El nombre sin espacios en los extremos.</returns>
    /// <exception cref="ArgumentException">Si el nombre no es válido.</exception>
    private static string ValidFileName(string? name, string paramName) =>
        TrackNaming.GetProblem(name) is { } problem
            ? throw new ArgumentException(problem, paramName)
            : name!.Trim();

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
    /// Aplica a una pista las etiquetas indicadas, reemplazando las que tuviera.
    /// </summary>
    /// <param name="filePath">Ruta absoluta del archivo de audio.</param>
    /// <param name="patch">Etiquetas a aplicar; las propiedades en blanco no se tocan.</param>
    /// <returns>La información de pista releída del disco.</returns>
    /// <exception cref="ArgumentException">Si <paramref name="filePath"/> es nulo o está en blanco.</exception>
    /// <exception cref="ArgumentNullException">Si <paramref name="patch"/> es <c>null</c>.</exception>
    /// <exception cref="FileNotFoundException">Si el archivo no existe en disco.</exception>
    /// <remarks>
    /// <para>
    /// Los intérpretes y géneros se separan solo por «;», y no también por «/» como en
    /// <see cref="SaveProperties"/>: un intérprete como «AC/DC» debe llegar entero.
    /// </para>
    /// <para>
    /// El resto de etiquetas no se reescribe, y si las elegidas ya tenían esos valores el archivo
    /// no se guarda, para no alterar su fecha de modificación.
    /// </para>
    /// </remarks>
    public static TrackInfo ApplyTags(string filePath, TagPatch patch)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(patch);
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("El archivo de audio no existe en disco.", filePath);
        }

        string? title = patch.TitleFromFileName ? Path.GetFileNameWithoutExtension(filePath) : patch.Title;

        using (TagLib.File tagFile = TagLib.File.Create(filePath))
        {
            TagLib.Tag tag = tagFile.Tag;
            bool changed = false;

            changed |= TryAssign(tag.Performers, SplitBySemicolon(patch.Artist), values => tag.Performers = values);
            changed |= TryAssign(tag.Title, title, value => tag.Title = value);
            changed |= TryAssign(tag.Album, patch.Album, value => tag.Album = value);
            changed |= TryAssign(tag.Genres, SplitBySemicolon(patch.Genre), values => tag.Genres = values);
            changed |= TryAssign(tag.Comment, patch.Comment, value => tag.Comment = value);

            if (changed)
            {
                tagFile.Save();
            }
        }

        return TrackScanner.Read(filePath);
    }

    /// <summary>Asigna un valor de texto si no está en blanco y difiere del actual.</summary>
    /// <returns><c>true</c> si se asignó.</returns>
    private static bool TryAssign(string? current, string? value, Action<string> assign)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string trimmed = value.Trim();
        if (string.Equals(current, trimmed, StringComparison.Ordinal))
        {
            return false;
        }

        assign(trimmed);
        return true;
    }

    /// <summary>Asigna una lista si no está vacía y difiere de la actual.</summary>
    /// <returns><c>true</c> si se asignó.</returns>
    private static bool TryAssign(string[]? current, string[] values, Action<string[]> assign)
    {
        if (values.Length == 0 || (current ?? []).SequenceEqual(values, StringComparer.Ordinal))
        {
            return false;
        }

        assign(values);
        return true;
    }

    private static string[] SplitBySemicolon(string? text) =>
        string.IsNullOrWhiteSpace(text)
            ? []
            : text.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

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
    /// Normaliza las propiedades indicadas de una pista —quita los acentos diacríticos conservando
    /// la «ñ» / «Ñ» y aplica TitleCase— y actualiza el archivo en disco.
    /// </summary>
    /// <param name="filePath">Ruta absoluta del archivo a normalizar.</param>
    /// <param name="fields">
    /// Propiedades a normalizar. Por defecto, todas; con <see cref="NormalizableFields.FileName"/>
    /// el archivo puede quedar renombrado.
    /// </param>
    /// <returns>La información de pista actualizada tras la normalización y posible renombrado.</returns>
    /// <exception cref="ArgumentException">Si <paramref name="filePath"/> es nulo o está en blanco.</exception>
    /// <exception cref="FileNotFoundException">Si el archivo no existe en disco.</exception>
    /// <exception cref="IOException">Si el nombre normalizado ya lo usa otro archivo de la carpeta.</exception>
    /// <remarks>
    /// <para>
    /// Solo se escriben las etiquetas elegidas, y no todas a través de
    /// <see cref="SaveProperties"/>: aquella vuelve a partir las listas por «;» y «/», lo que
    /// rompería un intérprete como «AC/DC» aunque el usuario solo hubiera pedido normalizar el
    /// título. Las listas se normalizan elemento a elemento, conservando su estructura.
    /// </para>
    /// <para>
    /// Si ninguna etiqueta cambia, el archivo no se reescribe: guardar con TagLib reserializa
    /// todas las etiquetas y altera la fecha de modificación de un archivo que ya estaba bien.
    /// </para>
    /// </remarks>
    public static TrackInfo NormalizeTrack(string filePath, NormalizableFields fields = NormalizableFields.All)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("El archivo de audio no existe en disco.", filePath);
        }

        NormalizeTags(filePath, fields);

        string currentPath = filePath;
        if (fields.HasFlag(NormalizableFields.FileName))
        {
            string normalized = TextNormalizer.NormalizeTitleCase(Path.GetFileNameWithoutExtension(filePath));
            if (!string.IsNullOrWhiteSpace(normalized))
            {
                currentPath = Rename(filePath, normalized);
            }
        }

        return TrackScanner.Read(currentPath);
    }

    /// <summary>Normaliza las etiquetas elegidas y guarda el archivo solo si alguna cambió.</summary>
    private static void NormalizeTags(string filePath, NormalizableFields fields)
    {
        if ((fields & ~NormalizableFields.FileName) == NormalizableFields.None)
        {
            return;
        }

        using TagLib.File tagFile = TagLib.File.Create(filePath);
        TagLib.Tag tag = tagFile.Tag;
        bool changed = false;

        if (fields.HasFlag(NormalizableFields.Title))
        {
            changed |= TryNormalize(tag.Title, value => tag.Title = value);
        }

        if (fields.HasFlag(NormalizableFields.Subtitle))
        {
            changed |= TryNormalize(tag.Subtitle, value => tag.Subtitle = value);
        }

        if (fields.HasFlag(NormalizableFields.Comment))
        {
            changed |= TryNormalize(tag.Comment, value => tag.Comment = value);
        }

        if (fields.HasFlag(NormalizableFields.Performers))
        {
            changed |= TryNormalize(tag.Performers, values => tag.Performers = values);
        }

        if (fields.HasFlag(NormalizableFields.AlbumArtists))
        {
            changed |= TryNormalize(tag.AlbumArtists, values => tag.AlbumArtists = values);
        }

        if (fields.HasFlag(NormalizableFields.Album))
        {
            changed |= TryNormalize(tag.Album, value => tag.Album = value);
        }

        if (fields.HasFlag(NormalizableFields.Genres))
        {
            changed |= TryNormalize(tag.Genres, values => tag.Genres = values);
        }

        if (fields.HasFlag(NormalizableFields.Composers))
        {
            changed |= TryNormalize(tag.Composers, values => tag.Composers = values);
        }

        if (fields.HasFlag(NormalizableFields.Copyright))
        {
            changed |= TryNormalize(tag.Copyright, value => tag.Copyright = value);
        }

        if (changed)
        {
            tagFile.Save();
        }
    }

    /// <summary>Normaliza un valor de texto y lo asigna solo si cambia.</summary>
    /// <returns><c>true</c> si el valor cambió y se asignó.</returns>
    private static bool TryNormalize(string? value, Action<string> assign)
    {
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        string normalized = TextNormalizer.NormalizeTitleCase(value);
        if (string.Equals(value, normalized, StringComparison.Ordinal))
        {
            return false;
        }

        assign(normalized);
        return true;
    }

    /// <summary>Normaliza cada elemento de una lista y la asigna solo si alguno cambia.</summary>
    /// <returns><c>true</c> si la lista cambió y se asignó.</returns>
    private static bool TryNormalize(string[]? values, Action<string[]> assign)
    {
        if (values is not { Length: > 0 })
        {
            return false;
        }

        string[] normalized = [.. values.Select(value => TextNormalizer.NormalizeTitleCase(value))];
        if (values.SequenceEqual(normalized, StringComparer.Ordinal))
        {
            return false;
        }

        assign(normalized);
        return true;
    }
}
