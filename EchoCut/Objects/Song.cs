using EchoCut.Export;
using EchoCut.Library;
using EchoCut.Processing;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EchoCut.Objects;

/// <summary>
/// Fila enlazada a <c>dataGrid</c>: la vista de una <see cref="TrackInfo"/> y de su análisis.
/// </summary>
/// <remarks>
/// <para>
/// Las columnas de la rejilla se generan implícitamente a partir de las propiedades públicas
/// visibles y sus <see cref="DisplayNameAttribute"/>, en este orden; las marcadas con
/// <c>[Browsable(false)]</c> se transportan pero no se muestran.
/// </para>
/// <para>
/// Salvo <see cref="Estatus"/>, todo lo que se ve son proyecciones de <see cref="Track"/> y
/// <see cref="Analysis"/>: la fila no guarda copias sueltas de las medidas. Es lo que impide que
/// la rejilla y el CSV acaben enseñando cifras distintas de la misma pista.
/// </para>
/// </remarks>
public sealed class Song : INotifyPropertyChanged
{
    /// <summary>La pista todavía no se ha analizado.</summary>
    public const string StatusPending = "Pendiente";

    /// <summary>El análisis está en curso.</summary>
    public const string StatusAnalyzing = "Analizando…";

    /// <summary>El análisis terminó y hay silencio final recortable.</summary>
    public const string StatusAnalyzed = "Analizado";

    /// <summary>El análisis terminó y no hay silencio final que recortar.</summary>
    public const string StatusNoSilence = "Sin silencio";

    /// <summary>El recorte está en curso.</summary>
    public const string StatusTrimming = "Recortando…";

    /// <summary>Ya se escribió la copia recortada.</summary>
    public const string StatusTrimmed = "Recortado";

    /// <summary>El análisis o el recorte se cancelaron antes de terminar.</summary>
    public const string StatusCancelled = "Cancelado";

    /// <summary>El análisis o el recorte terminaron con un error.</summary>
    public const string StatusError = "Error";

    private static readonly TrackInfo Unknown =
        new(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
            TimeSpan.Zero, string.Empty, 0, 0);

    private TrackAnalysis? _analysis;
    private string _estatus = StatusPending;
    private string _errorMessage = string.Empty;

    /// <summary>Crea una fila sin pista asociada. Solo para el enlace de datos y las pruebas.</summary>
    public Song() : this(Unknown)
    {
    }

    /// <summary>Crea la fila que representa a una pista.</summary>
    /// <param name="track">Pista que representa la fila.</param>
    public Song(TrackInfo track) => Track = track;

    /// <summary>Nombre del archivo, sin extensión.</summary>
    /// <value>Nombre del archivo.</value>
    [DisplayName("Nombre")]
    public string Name => Track.Name;

    /// <summary>Título tomado de los metadatos del archivo.</summary>
    /// <value>Título, o cadena vacía si el archivo no lo tiene.</value>
    [DisplayName("Título")]
    public string Title => Track.Title;

    /// <summary>Artista tomado de los metadatos del archivo.</summary>
    /// <value>Artista, o cadena vacía si el archivo no lo tiene.</value>
    [DisplayName("Artista")]
    public string Artist => Track.Artist;

    /// <summary>Álbum tomado de los metadatos del archivo.</summary>
    /// <value>Álbum, o cadena vacía si el archivo no lo tiene.</value>
    [DisplayName("Álbum")]
    public string Album => Track.Album;

    /// <summary>Duración formateada para mostrar en la rejilla.</summary>
    /// <value>Duración en formato <c>mm:ss</c>.</value>
    [DisplayName("Duración")]
    public string Duration => TrackFormat.Duration(Track.Duration);

    /// <summary>Extensión del archivo, con el punto incluido.</summary>
    /// <value>Extensión del archivo, por ejemplo <c>.mp3</c>.</value>
    [DisplayName("Ext.")]
    public string Extension => Track.Extension;

    /// <summary>Bitrate formateado para mostrar en la rejilla.</summary>
    /// <value>Bitrate con su unidad, por ejemplo <c>320 Kbps</c>.</value>
    [DisplayName("Bitrate")]
    public string Bitrate => TrackFormat.Bitrate(Track.BitrateKbps);

    /// <summary>Tamaño formateado para mostrar en la rejilla.</summary>
    /// <value>Tamaño con su unidad, por ejemplo <c>4,20 MB</c>.</value>
    [DisplayName("Tamaño")]
    public string Size => TrackFormat.Size(Track.SizeBytes);

    /// <summary>Silencio detectado al final de la pista, en segundos.</summary>
    /// <value>Silencio final en segundos, o <c>null</c> si aún no se ha analizado.</value>
    [DisplayName("Silencio (s)")]
    public double? Silence => _analysis?.SilenceSeconds;

    /// <summary>Segundos que se eliminarían al recortar.</summary>
    /// <value>Segundos que se ahorrarían al recortar, o <c>null</c> si aún no se ha analizado.</value>
    [DisplayName("Recorte (s)")]
    public double? Crop => _analysis?.CropSeconds;

    /// <summary>Estado actual de la fila, tal como se muestra en la columna «Estado».</summary>
    /// <value>Uno de los valores <c>Status*</c> definidos en esta clase.</value>
    [DisplayName("Estado")]
    public string Estatus
    {
        get => _estatus;
        set => SetField(ref _estatus, value);
    }

    // ---- Datos de trabajo y métricas: viajan con la fila pero no son columnas de la rejilla. ----

    /// <summary>Pista que representa la fila.</summary>
    /// <value>Metadatos leídos al escanear la carpeta.</value>
    [Browsable(false)]
    public TrackInfo Track { get; }

    /// <summary>
    /// Resultado del análisis. Asignarlo repinta las columnas que dependen de él.
    /// </summary>
    /// <value>Análisis de la pista, o <c>null</c> si aún no se ha analizado.</value>
    [Browsable(false)]
    public TrackAnalysis? Analysis
    {
        get => _analysis;
        set
        {
            if (ReferenceEquals(_analysis, value))
            {
                return;
            }

            _analysis = value;

            // Silencio y Recorte se calculan a partir del análisis, así que el enlace no se entera
            // de que han cambiado si no se le avisa columna por columna.
            OnPropertyChanged(nameof(Analysis));
            OnPropertyChanged(nameof(Silence));
            OnPropertyChanged(nameof(Crop));
        }
    }

    /// <summary>Ruta completa del archivo original.</summary>
    /// <value>Ruta absoluta del archivo en disco.</value>
    [Browsable(false)]
    public string FilePath => Track.FilePath;

    /// <summary>Duración total del archivo, en segundos.</summary>
    /// <value>La medida por el análisis si ya se hizo; si no, la declarada por los metadatos.</value>
    [Browsable(false)]
    public double DurationSeconds => _analysis?.DurationSeconds ?? Track.DurationSeconds;

    /// <summary>Instante de corte calculado, en segundos.</summary>
    /// <value>Segundo del archivo en el que se produciría el corte, o <c>null</c> si no se ha analizado.</value>
    [Browsable(false)]
    public double? CutSeconds => _analysis?.CutSeconds;

    /// <summary>Si el análisis considera que la pista tiene cola recortable.</summary>
    /// <value><c>true</c> si procede recortar; <c>false</c> en caso contrario.</value>
    [Browsable(false)]
    public bool ShouldTrim => _analysis?.ShouldTrim ?? false;

    /// <summary>Detalle del último error, para el CSV y el tooltip de la fila.</summary>
    /// <value>Mensaje de error, o cadena vacía si no hay ninguno.</value>
    [Browsable(false)]
    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetField(ref _errorMessage, value);
    }

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Vuelca la fila a la representación que exporta el CSV.</summary>
    /// <returns>La fila del informe correspondiente a esta pista.</returns>
    public TrackRecord ToRecord() => new(Track, _analysis, _estatus, _errorMessage);

    /// <summary>Deja la fila lista para volver a analizarse y anota el error que la dejó así.</summary>
    /// <param name="exception">Error que interrumpió el análisis o el recorte.</param>
    public void Fail(Exception exception)
    {
        Analysis = null;
        ErrorMessage = exception.Message;
        Estatus = StatusError;
    }

    /// <summary>Anota el resultado del análisis y actualiza el estado en consecuencia.</summary>
    /// <param name="analysis">Resultado del análisis de esta pista.</param>
    public void Complete(TrackAnalysis analysis)
    {
        Analysis = analysis;
        ErrorMessage = string.Empty;
        Estatus = analysis.ShouldTrim ? StatusAnalyzed : StatusNoSilence;
    }

    /// <summary>Asigna un campo de respaldo y notifica el cambio solo si el valor es distinto.</summary>
    /// <typeparam name="T">Tipo del campo y de la propiedad asociada.</typeparam>
    /// <param name="field">Campo de respaldo a actualizar.</param>
    /// <param name="value">Nuevo valor a asignar.</param>
    /// <param name="propertyName">
    /// Nombre de la propiedad que notifica el cambio. Se rellena automáticamente por
    /// <see cref="CallerMemberNameAttribute"/> con el nombre del llamador; no hace falta indicarlo.
    /// </param>
    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        OnPropertyChanged(propertyName);
    }

    private void OnPropertyChanged(string? propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
