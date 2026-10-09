namespace EchoCut.Library;

/// <summary>
/// Propiedades de texto de una pista que <see cref="TrackEditor.NormalizeTrack"/> puede normalizar.
/// </summary>
/// <remarks>
/// Es un conjunto de banderas y no una lista porque se guarda en la configuración: como número,
/// una versión anterior que no conozca un campo añadido después solo ve un bit que descarta al
/// acotar, en vez de un nombre que haría fallar la lectura del JSON entero.
/// Los valores son fijos y no deben reordenarse: el número guardado depende de ellos.
/// </remarks>
[Flags]
public enum NormalizableFields
{
    /// <summary>Ninguna propiedad.</summary>
    None = 0,

    /// <summary>Nombre del archivo en disco, sin la extensión. Normalizarlo puede renombrar el archivo.</summary>
    FileName = 1 << 0,

    /// <summary>Título de la etiqueta.</summary>
    Title = 1 << 1,

    /// <summary>Subtítulo de la etiqueta.</summary>
    Subtitle = 1 << 2,

    /// <summary>Comentario de la etiqueta.</summary>
    Comment = 1 << 3,

    /// <summary>Intérpretes de la pista.</summary>
    Performers = 1 << 4,

    /// <summary>Artistas del álbum.</summary>
    AlbumArtists = 1 << 5,

    /// <summary>Nombre del álbum.</summary>
    Album = 1 << 6,

    /// <summary>Géneros.</summary>
    Genres = 1 << 7,

    /// <summary>Compositores.</summary>
    Composers = 1 << 8,

    /// <summary>Aviso de derechos de autor.</summary>
    Copyright = 1 << 9,

    /// <summary>Todas las propiedades; es lo que normalizaba la acción antes de poder elegir.</summary>
    All = FileName | Title | Subtitle | Comment | Performers | AlbumArtists | Album | Genres | Composers | Copyright,
}
