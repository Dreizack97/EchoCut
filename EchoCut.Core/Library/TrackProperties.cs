namespace EchoCut.Library;

/// <summary>
/// Modelo detallado de propiedades y metadatos de una pista de audio para visualización y edición.
/// </summary>
public sealed class TrackProperties
{
    /// <summary>Ruta absoluta del archivo en disco.</summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>Nombre del archivo sin extensión.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>Extensión del archivo con el punto incluido.</summary>
    public string Extension { get; set; } = string.Empty;

    /// <summary>Directorio contenedor del archivo.</summary>
    public string Directory { get; set; } = string.Empty;

    /// <summary>Tamaño del archivo en bytes.</summary>
    public long SizeBytes { get; set; }

    /// <summary>Fecha y hora de creación del archivo.</summary>
    public DateTime CreationTime { get; set; }

    /// <summary>Fecha y hora de última modificación del archivo.</summary>
    public DateTime LastWriteTime { get; set; }

    /// <summary>Fecha y hora de último acceso al archivo.</summary>
    public DateTime LastAccessTime { get; set; }

    /// <summary>Atributos del sistema de archivos de Windows.</summary>
    public FileAttributes Attributes { get; set; }

    /// <summary>Título de la obra en los metadatos.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Subtítulo de la pista.</summary>
    public string Subtitle { get; set; } = string.Empty;

    /// <summary>Comentarios adicionales en los metadatos.</summary>
    public string Comment { get; set; } = string.Empty;

    /// <summary>Intérpretes colaboradores o artistas principales (separados por punto y coma).</summary>
    public string Performers { get; set; } = string.Empty;

    /// <summary>Intérprete del álbum (separados por punto y coma).</summary>
    public string AlbumArtist { get; set; } = string.Empty;

    /// <summary>Nombre del álbum al que pertenece la pista.</summary>
    public string Album { get; set; } = string.Empty;

    /// <summary>Año de lanzamiento de la pista o álbum.</summary>
    public uint Year { get; set; }

    /// <summary>Número de pista en el álbum.</summary>
    public uint Track { get; set; }

    /// <summary>Géneros musicales asociados a la pista (separados por punto y coma).</summary>
    public string Genre { get; set; } = string.Empty;

    /// <summary>Duración total del audio.</summary>
    public TimeSpan Duration { get; set; }

    /// <summary>Velocidad de bits del audio en kilobits por segundo.</summary>
    public int BitrateKbps { get; set; }

    /// <summary>Número de canales de audio.</summary>
    public int Channels { get; set; }

    /// <summary>Frecuencia de muestreo en hercios.</summary>
    public int SampleRateHz { get; set; }

    /// <summary>Compositores de la obra musical (separados por punto y coma).</summary>
    public string Composers { get; set; } = string.Empty;

    /// <summary>Información legal de copyright registrada en la pista.</summary>
    public string Copyright { get; set; } = string.Empty;
}
