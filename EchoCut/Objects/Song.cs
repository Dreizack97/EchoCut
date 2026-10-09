using EchoCut.Audio;
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

    /// <summary>El tramo a conservar se ajustó a mano y recorta algo, o la pista lleva fundidos o borrados.</summary>
    public const string StatusAdjusted = "Ajustado";

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

    /// <summary>
    /// Diferencia por debajo de la cual un extremo del ajuste manual se considera el borde del
    /// archivo: un milisegundo está muy por debajo del paquete más corto que puede cortar la copia
    /// de flujo.
    /// </summary>
    private const double EdgeEpsilonSeconds = 0.001;

    private TrackAnalysis? _analysis;
    private TrimRange? _manualRange;
    private AudioEdits? _edits;
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

    /// <summary>Silencio detectado al principio de la pista, en segundos.</summary>
    /// <value>
    /// Silencio inicial en segundos, o <c>null</c> si aún no se ha analizado. Vale cero si el
    /// análisis del principio estaba desactivado.
    /// </value>
    [DisplayName("Silencio inicial (s)")]
    public double? LeadingSilence => _analysis?.Leading.SilenceSeconds;

    /// <summary>Silencio detectado al final de la pista, en segundos.</summary>
    /// <value>Silencio final en segundos, o <c>null</c> si aún no se ha analizado.</value>
    [DisplayName("Silencio final (s)")]
    public double? Silence => _analysis?.Trailing.SilenceSeconds;

    /// <summary>Segundos que se eliminarían al recortar.</summary>
    /// <value>
    /// Segundos que se ahorrarían al recortar según el ajuste manual o, si no lo hay, según el
    /// análisis, sumando lo borrado a mano dentro de lo que se conserva; <c>null</c> si no hay
    /// tramo que conservar.
    /// </value>
    [DisplayName("Recorte (s)")]
    public double? Crop => _manualRange is null && _edits is null
        ? _analysis?.CropSeconds
        : TrimRange is { } range
            ? Math.Round(DurationSeconds - (_edits?.KeptSeconds(range) ?? range.DurationSeconds), 2)
            : null;

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
    public TrackInfo Track { get; private set; }

    /// <summary>Actualiza la pista representada tras modificar sus metadatos o su nombre de archivo.</summary>
    /// <param name="track">Pista con los datos actualizados.</param>
    public void UpdateTrack(TrackInfo track)
    {
        Track = track;
        OnPropertyChanged(nameof(Track));
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(Artist));
        OnPropertyChanged(nameof(Album));
        OnPropertyChanged(nameof(Duration));
        OnPropertyChanged(nameof(Extension));
        OnPropertyChanged(nameof(Bitrate));
        OnPropertyChanged(nameof(Size));
        OnPropertyChanged(nameof(FilePath));
        OnPropertyChanged(nameof(DurationSeconds));
    }

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
            OnPropertyChanged(nameof(LeadingSilence));
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

    /// <summary>Instante en el que terminaría la copia recortada, en segundos.</summary>
    /// <value>
    /// Segundo del archivo en el que se produciría el corte final —la duración completa si el final
    /// no se recorta—, o <c>null</c> si no se ha analizado.
    /// </value>
    [Browsable(false)]
    public double? CutSeconds => TrimRange?.EndSeconds;

    /// <summary>Tramo que conservaría la copia recortada.</summary>
    /// <value>
    /// El ajuste manual si lo hay; si no, el calculado por el análisis; <c>null</c> si no hay
    /// ninguno. Es el único punto del que el recorte y la previsualización toman sus extremos.
    /// </value>
    /// <remarks>
    /// Una pista con ediciones pero sin análisis ni ajuste se conserva completa: un fundido o un
    /// borrado bastan para que haya una copia que escribir.
    /// </remarks>
    [Browsable(false)]
    public TrimRange? TrimRange =>
        _manualRange ?? _analysis?.Range ?? (_edits is not null ? new TrimRange(0.0, DurationSeconds) : null);

    /// <summary>Tramo ajustado a mano desde el espectrograma.</summary>
    /// <value>
    /// El tramo elegido por el usuario, o <c>null</c> si manda el análisis. Dura lo que la sesión:
    /// sobrevive a un nuevo análisis y a los cambios de tolerancia, porque es una decisión tomada a
    /// la vista de la señal que ningún parámetro debe deshacer.
    /// </value>
    [Browsable(false)]
    public TrimRange? ManualRange => _manualRange;

    /// <summary>Fundidos y borrados que se aplicarán a la copia, elegidos en el editor de forma de onda.</summary>
    /// <value>
    /// Las ediciones de la pista, o <c>null</c> si no lleva ninguna. Como el ajuste manual, duran lo
    /// que la sesión y sobreviven a un nuevo análisis.
    /// </value>
    [Browsable(false)]
    public AudioEdits? Edits => _edits;

    /// <summary>Si procede escribir una copia recortada.</summary>
    /// <value>
    /// Siempre que algún fundido o borrado llegue a la copia; si no, con ajuste manual, si este
    /// descarta algo del principio o del final, y sin él, la decisión del análisis.
    /// </value>
    [Browsable(false)]
    public bool ShouldTrim => HasEffectiveEdits || (_manualRange is { } manual
        ? manual.StartSeconds > EdgeEpsilonSeconds || manual.EndSeconds < DurationSeconds - EdgeEpsilonSeconds
        : _analysis?.ShouldTrim ?? false);

    /// <value><c>true</c> si algún fundido o borrado afecta al tramo que conserva la copia.</value>
    private bool HasEffectiveEdits => _edits is not null && TrimRange is { } range && _edits.Within(range) is not null;

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
    /// <remarks>Si hay un ajuste manual, se conserva y sigue mandando sobre el análisis nuevo.</remarks>
    public void Complete(TrackAnalysis analysis)
    {
        Analysis = analysis;
        ErrorMessage = string.Empty;
        Estatus = DecisionStatus();
    }

    /// <summary>Fija o retira el ajuste manual del tramo a conservar.</summary>
    /// <param name="range">Tramo elegido, o <c>null</c> para volver a la decisión del análisis.</param>
    /// <exception cref="ArgumentOutOfRangeException">Se lanza si el tramo no es válido.</exception>
    public void AdjustManually(TrimRange? range)
    {
        range?.ThrowIfInvalid();

        _manualRange = range;
        OnPropertyChanged(nameof(ManualRange));
        OnPropertyChanged(nameof(Crop));
        Estatus = DecisionStatus();
    }

    /// <summary>Fija o retira los fundidos y borrados de la copia.</summary>
    /// <param name="edits">Ediciones elegidas, o <c>null</c> (o vacías) para quitarlas.</param>
    /// <exception cref="ArgumentOutOfRangeException">Se lanza si algún fundido no es válido.</exception>
    public void ApplyEdits(AudioEdits? edits)
    {
        edits?.ThrowIfInvalid();

        _edits = edits is { HasAny: true } ? edits : null;
        OnPropertyChanged(nameof(Edits));

        // Un análisis hecho sobre el resultado editado deja de valer si las ediciones cambian: sus
        // silencios eran los de otra copia. Uno hecho sobre el original sigue valiendo tal cual.
        if (_analysis is { } analysis && !analysis.Describes(_edits))
        {
            Analysis = null;
        }

        OnPropertyChanged(nameof(Crop));
        Estatus = DecisionStatus();
    }

    private string DecisionStatus() => (_manualRange, _analysis) switch
    {
        _ when HasEffectiveEdits => StatusAdjusted,
        (null, null) => StatusPending,
        _ when !ShouldTrim => StatusNoSilence,
        (null, _) => StatusAnalyzed,
        _ => StatusAdjusted,
    };

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
