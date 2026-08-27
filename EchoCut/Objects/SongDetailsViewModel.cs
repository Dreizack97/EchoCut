using EchoCut.Library;
using System.ComponentModel;

namespace EchoCut.Objects;

/// <summary>
/// Modelo de presentación para el PropertyGrid de la pestaña «Detalles»,
/// replicando las categorías y propiedades de la ventana de propiedades de audio de Windows.
/// </summary>
public sealed class SongDetailsViewModel
{
    private const string CatDesc = "Descripción";
    private const string CatMulti = "Multimedia";
    private const string CatAudio = "Audio";
    private const string CatOrigen = "Origen";
    private const string CatCont = "Contenido";

    private readonly TrackProperties _properties;

    /// <summary>Inicializa el modelo de vista a partir de las propiedades de la pista.</summary>
    /// <param name="properties">Instancia de propiedades compartida con el diálogo.</param>
    public SongDetailsViewModel(TrackProperties properties)
    {
        _properties = properties;
    }

    // ========================================== Descripción ==========================================

    /// <summary>Título de la canción.</summary>
    [Category(CatDesc)]
    [DisplayName("Título")]
    [Description("Título de la pista musical.")]
    public string Título
    {
        get => _properties.Title;
        set => _properties.Title = value ?? string.Empty;
    }

    /// <summary>Subtítulo de la pista.</summary>
    [Category(CatDesc)]
    [DisplayName("Subtítulo")]
    [Description("Subtítulo o descripción secundaria de la pista.")]
    public string Subtítulo
    {
        get => _properties.Subtitle;
        set => _properties.Subtitle = value ?? string.Empty;
    }

    /// <summary>Comentarios de los metadatos.</summary>
    [Category(CatDesc)]
    [DisplayName("Comentarios")]
    [Description("Comentarios o notas adicionales integradas en los metadatos.")]
    public string Comentarios
    {
        get => _properties.Comment;
        set => _properties.Comment = value ?? string.Empty;
    }

    // ========================================== Multimedia ==========================================

    /// <summary>Intérpretes colaboradores o artistas principales.</summary>
    [Category(CatMulti)]
    [DisplayName("Intérpretes colaboradores")]
    [Description("Artistas principales o colaboradores (separados por punto y coma).")]
    public string IntérpretesColaboradores
    {
        get => _properties.Performers;
        set => _properties.Performers = value ?? string.Empty;
    }

    /// <summary>Intérprete del álbum.</summary>
    [Category(CatMulti)]
    [DisplayName("Intérprete del álbum")]
    [Description("Artista del álbum (separados por punto y coma).")]
    public string IntérpreteDelÁlbum
    {
        get => _properties.AlbumArtist;
        set => _properties.AlbumArtist = value ?? string.Empty;
    }

    /// <summary>Álbum musical.</summary>
    [Category(CatMulti)]
    [DisplayName("Álbum")]
    [Description("Nombre del álbum discográfico.")]
    public string Álbum
    {
        get => _properties.Album;
        set => _properties.Album = value ?? string.Empty;
    }

    /// <summary>Año de publicación.</summary>
    [Category(CatMulti)]
    [DisplayName("Año")]
    [Description("Año de publicación.")]
    public string Año
    {
        get => _properties.Year > 0 ? _properties.Year.ToString() : string.Empty;
        set
        {
            if (uint.TryParse(value, out uint year))
            {
                _properties.Year = year;
            }
            else if (string.IsNullOrWhiteSpace(value))
            {
                _properties.Year = 0;
            }
        }
    }

    /// <summary>Número de pista.</summary>
    [Category(CatMulti)]
    [DisplayName("Número de pista")]
    [Description("Número de la pista dentro del álbum.")]
    public string NúmeroDePista
    {
        get => _properties.Track > 0 ? _properties.Track.ToString() : string.Empty;
        set
        {
            if (uint.TryParse(value, out uint track))
            {
                _properties.Track = track;
            }
            else if (string.IsNullOrWhiteSpace(value))
            {
                _properties.Track = 0;
            }
        }
    }

    /// <summary>Género musical.</summary>
    [Category(CatMulti)]
    [DisplayName("Género")]
    [Description("Géneros musicales asignados (separados por punto y coma).")]
    public string Género
    {
        get => _properties.Genre;
        set => _properties.Genre = value ?? string.Empty;
    }

    /// <summary>Duración formateada en hh:mm:ss.</summary>
    [Category(CatMulti)]
    [DisplayName("Duración")]
    [Description("Duración total del archivo de audio.")]
    [ReadOnly(true)]
    public string Duración => _properties.Duration.ToString(@"hh\:mm\:ss");

    // ========================================== Audio ==========================================

    /// <summary>Velocidad de bits en kbps.</summary>
    [Category(CatAudio)]
    [DisplayName("Velocidad de bits")]
    [Description("Tasa de bits de la codificación de audio.")]
    [ReadOnly(true)]
    public string VelocidadDeBits => _properties.BitrateKbps > 0 ? $"{_properties.BitrateKbps}kbps" : "-";

    /// <summary>Canales de audio (mono, estéreo, etc.).</summary>
    [Category(CatAudio)]
    [DisplayName("Canales")]
    [Description("Número de canales de audio.")]
    [ReadOnly(true)]
    public string Canales => _properties.Channels switch
    {
        1 => "1 (mono)",
        2 => "2 (estéreo)",
        6 => "6 (5.1)",
        8 => "8 (7.1)",
        _ => _properties.Channels > 0 ? $"{_properties.Channels} canales" : "-"
    };

    /// <summary>Frecuencia de muestreo en kHz.</summary>
    [Category(CatAudio)]
    [DisplayName("Velocidad de muestra de sonido")]
    [Description("Frecuencia de muestreo de audio.")]
    [ReadOnly(true)]
    public string FrecuenciaDeMuestreo => _properties.SampleRateHz > 0
        ? $"{(_properties.SampleRateHz / 1000.0):0.000} kHz"
        : "-";

    // ========================================== Origen ==========================================

    /// <summary>Información de copyright.</summary>
    [Category(CatOrigen)]
    [DisplayName("Copyright")]
    [Description("Aviso de derechos de autor o copyright registrado.")]
    public string Copyright
    {
        get => _properties.Copyright;
        set => _properties.Copyright = value ?? string.Empty;
    }

    // ========================================== Contenido ==========================================

    /// <summary>Compositores de la obra.</summary>
    [Category(CatCont)]
    [DisplayName("Compositores")]
    [Description("Compositores de la obra (separados por punto y coma).")]
    public string Compositores
    {
        get => _properties.Composers;
        set => _properties.Composers = value ?? string.Empty;
    }
}
