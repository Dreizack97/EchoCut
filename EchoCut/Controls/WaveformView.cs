using EchoCut.Audio;
using EchoCut.Objects;
using EchoCut.Waveforms;
using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.Runtime.InteropServices;

namespace EchoCut.Controls;

/// <summary>Marca de recorte de un <see cref="WaveformView"/>.</summary>
public enum TrimMarker
{
    /// <summary>Ninguna marca.</summary>
    None,

    /// <summary>Instante en el que empieza la copia recortada.</summary>
    Start,

    /// <summary>Instante en el que termina la copia recortada.</summary>
    End,
}

/// <summary>Tramo al que el usuario llevó un fundido arrastrando uno de sus bordes.</summary>
/// <param name="direction">Fundido ajustado.</param>
/// <param name="startSeconds">Comienzo del tramo, en segundos del archivo.</param>
/// <param name="endSeconds">Final del tramo, en segundos del archivo.</param>
public sealed class FadeAdjustedEventArgs(FadeDirection direction, double startSeconds, double endSeconds) : EventArgs
{
    /// <value>Fundido ajustado.</value>
    public FadeDirection Direction { get; } = direction;

    /// <value>Comienzo del tramo, en segundos del archivo.</value>
    public double StartSeconds { get; } = startSeconds;

    /// <value>Final del tramo, en segundos del archivo.</value>
    public double EndSeconds { get; } = endSeconds;
}

/// <summary>Instante en el que el usuario hizo clic sin llegar a arrastrar.</summary>
/// <param name="seconds">Instante, en segundos del archivo.</param>
public sealed class WaveformClickEventArgs(double seconds) : EventArgs
{
    /// <value>Instante del clic, en segundos del archivo.</value>
    public double Seconds { get; } = seconds;
}

/// <summary>
/// Muestra un tramo de una <see cref="Waveforms.Waveform"/> con las marcas de recorte superpuestas
/// y permite moverlas con el ratón o con el teclado.
/// </summary>
/// <remarks>
/// <para>
/// Lo que se eliminaría al recortar se pinta con <see cref="WaveformPalette.Removed"/>, como Audacity
/// pinta una selección: fondo gris y onda atenuada, sin ocultar la señal que se descarta.
/// </para>
/// <para>
/// La imagen se calcula solo cuando cambian los datos, el tramo visible, la escala o el tamaño;
/// mover una marca se limita a recomponer imágenes ya hechas, así que arrastrar es fluido.
/// </para>
/// <para>
/// Teclado: ← y → mueven la marca activa 10 ms (100 ms con Mayús, 1 s con Ctrl); Inicio y Fin eligen
/// la marca activa. El valor de las marcas se expone a los lectores de pantalla.
/// </para>
/// <para>
/// Selección: como en Audacity, arrastrar fuera de las marcas selecciona un tramo, y lo que se haga
/// con él —un fundido, borrarlo— lo decide quien contiene la vista. Un clic suelto no selecciona:
/// avisa con <see cref="WaveformClicked"/> y quien contiene la vista decide si sitúa el cursor de
/// reproducción, selecciona un fragmento borrado o muestra un fundido. Los bordes de la selección
/// y de los fundidos se pueden arrastrar, y los extremos se adhieren a las marcas de recorte y a
/// los bordes de lo borrado cercanos.
/// </para>
/// <para>
/// Zoom: la rueda desplaza la vista y Ctrl+rueda acerca o aleja alrededor del puntero, siempre
/// dentro de los límites fijados con <see cref="SetScrollLimits"/>, que son los del audio cargado.
/// </para>
/// <para>
/// Resultado: con <see cref="RenderEdits"/> la onda se pinta con los fundidos aplicados y, con
/// <see cref="CollapseDeletions"/>, en la línea de tiempo de la copia, sin lo borrado y con los
/// empalmes de <see cref="Splices"/> marcados. <see cref="ReadOnly"/> deja mirar sin editar, y
/// <see cref="MarkersOnly"/> deja mover solo las marcas de recorte, que es lo único que tiene sentido
/// ajustar sobre una línea de tiempo que ya no es la del original.
/// </para>
/// <para>
/// La envolvente de <see cref="Fades"/> se dibuja en ámbar con la misma escala vertical que la
/// onda; lo borrado, con la paleta de lo eliminado y un rayado que lo distingue del recorte.
/// </para>
/// </remarks>
[DefaultEvent(nameof(MarkersChanged))]
public sealed class WaveformView : Control
{
    /// <summary>Separación mínima entre marcas: un tramo vacío no es una copia que se pueda escribir.</summary>
    public const double MinimumGapSeconds = 0.1;

    /// <summary>Rojo del cursor de reproducción: contrasta 5.7:1 con el fondo blanco.</summary>
    private static readonly Color PlayheadColor = Color.FromArgb(0xC4, 0x2B, 0x1C);

    /// <summary>Marrón ámbar de la envolvente de fundido: contrasta 8.6:1 con el fondo blanco.</summary>
    private static readonly Color FadeLineColor = Color.FromArgb(0x8A, 0x3B, 0x00);

    /// <summary>Velo ámbar translúcido sobre el tramo con fundido; la onda se sigue viendo debajo.</summary>
    private static readonly Color FadeTintColor = Color.FromArgb(56, 0xE6, 0x9F, 0x00);

    /// <summary>Azul translúcido de la selección: tiñe sin ocultar la onda, como en Audacity.</summary>
    private static readonly Color SelectionTintColor = Color.FromArgb(64, 0x1F, 0x4E, 0x99);

    /// <summary>Azul de los bordes de la selección: contrasta 8.1:1 con el fondo blanco.</summary>
    private static readonly Color SelectionLineColor = Color.FromArgb(0x1F, 0x4E, 0x99);

    /// <summary>Rayado de lo borrado: la forma, y no solo el gris, lo distingue de lo que quita el recorte.</summary>
    private static readonly Color DeletedHatchColor = Color.FromArgb(150, 0x40, 0x40, 0x40);

    /// <summary>Morado de los empalmes del resultado: contrasta 7.4:1 con el fondo blanco y no se confunde con las marcas ni con el cursor.</summary>
    private static readonly Color SpliceColor = Color.FromArgb(0x6B, 0x2F, 0xA0);

    /// <summary>Gris oscuro del cursor de reproducción en reposo: contrasta 12.6:1 con el fondo blanco.</summary>
    private static readonly Color CursorColor = Color.FromArgb(0x33, 0x33, 0x33);

    /// <summary>
    /// Tramo visible más corto: unos cuantos bloques del resumen de la onda. Por debajo, la imagen
    /// no tiene más detalle que mostrar.
    /// </summary>
    public const double MinimumSpanSeconds = 0.05;

    /// <summary>Cuánto acerca o aleja cada paso de la rueda con Ctrl.</summary>
    private const double WheelZoomFactor = 1.25;

    /// <summary>Fracción de la vista que desplaza cada paso de la rueda.</summary>
    private const double WheelScrollFraction = 0.1;

    /// <summary>Recorrido mínimo, en píxeles lógicos, para que un clic se tome por selección y no por un clic suelto.</summary>
    private const int SelectionThreshold = 3;

    private const double FineStepSeconds = 0.01;
    private const double CoarseStepSeconds = 0.1;
    private const double LargeStepSeconds = 1.0;

    private static readonly double[] RulerSteps = [0.01, 0.02, 0.05, 0.1, 0.2, 0.5, 1, 2, 5, 10, 15, 30, 60, 120, 300, 600, 900, 1800];
    /// <summary>Referencias lineales, en orden de prioridad: extremos y centro antes que los intermedios.</summary>
    private static readonly double[] LinearTicks = [1.0, -1.0, 0.0, 0.5, -0.5];

    /// <summary>Referencias en dB, en orden de prioridad: el fondo de escala antes que los niveles bajos.</summary>
    private static readonly double[] DecibelTicks = [0, -12, -24, -36, -48];

    private Waveform? _waveform;
    private AmplitudeScale _amplitudeScale = AmplitudeScale.Linear;
    private double _durationSeconds = 1.0;
    private double _viewStartSeconds;
    private double _viewEndSeconds = 1.0;
    private double _scrollMinSeconds;
    private double _scrollMaxSeconds = 1.0;
    private double? _cursorSeconds;
    private AudioEdits? _renderEdits;
    private bool _collapseDeletions;
    private IReadOnlyList<double> _splices = [];
    private bool _readOnly;
    private bool _markersOnly;
    private double _startMarkerSeconds;
    private double _endMarkerSeconds = 1.0;
    private bool _showStartMarker = true;
    private bool _showEndMarker = true;
    private bool _showTimeRuler = true;
    private string _statusText = string.Empty;
    private double? _playheadSeconds;
    private TrimMarker _activeMarker;
    private TrimMarker _dragging;
    private TrackFades? _fades;
    private TimeRegion? _selection;
    private DeletedRegions _deletions = DeletedRegions.Empty;
    private DragTarget _dragTarget;
    private double _dragAnchor;
    private int _dragOriginX;
    private bool _dragStarted;

    private PinnedBitmap? _normalImage;
    private PinnedBitmap? _removedImage;
    private bool _imagesStale = true;

    /// <summary>Crea la vista vacía, con doble búfer y capaz de recibir el foco del teclado.</summary>
    public WaveformView()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.UserPaint
            | ControlStyles.ResizeRedraw
            | ControlStyles.Selectable,
            true);
    }

    /// <summary>Se produce cuando el usuario mueve una marca, con el ratón o con el teclado.</summary>
    /// <remarks>No se produce cuando las marcas se fijan desde código con <see cref="SetMarkers"/>.</remarks>
    [Category("Forma de onda")]
    [Description("Se produce cuando el usuario mueve una marca de recorte.")]
    public event EventHandler? MarkersChanged;

    /// <summary>Se produce mientras el usuario selecciona un tramo con el ratón, o cuando la quita.</summary>
    /// <remarks>
    /// Se produce en cada movimiento, no solo al soltar, para que quien escucha lleve la selección en
    /// vivo a las demás vistas. No se produce al fijarla desde código con <see cref="Selection"/>.
    /// </remarks>
    [Category("Forma de onda")]
    [Description("Se produce mientras el usuario selecciona un tramo.")]
    public event EventHandler? SelectionChanged;

    /// <summary>Se produce mientras el usuario arrastra un borde de un fundido.</summary>
    /// <remarks>
    /// La vista no cambia <see cref="Fades"/> por su cuenta: es quien escucha quien decide si el
    /// tramo vale y lo devuelve a todas las vistas.
    /// </remarks>
    [Category("Forma de onda")]
    [Description("Se produce mientras el usuario arrastra un borde de un fundido.")]
    public event EventHandler<FadeAdjustedEventArgs>? FadeAdjusted;

    /// <summary>Se produce con un clic que no llegó a arrastrar fuera de las marcas.</summary>
    [Category("Forma de onda")]
    [Description("Se produce con un clic sin arrastre sobre la forma de onda.")]
    public event EventHandler<WaveformClickEventArgs>? WaveformClicked;

    /// <summary>Se produce cuando cambia el tramo visible, por zoom o desplazamiento.</summary>
    [Category("Forma de onda")]
    [Description("Se produce cuando cambia el tramo visible.")]
    public event EventHandler? ViewChanged;

    /// <summary>Ediciones con las que se pinta la onda.</summary>
    /// <value>
    /// Los fundidos y borrados que se aplican al pintar, o <c>null</c> para pintar el original tal
    /// cual. Distinto de <see cref="Fades"/>, que solo dibuja la envolvente encima.
    /// </value>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public AudioEdits? RenderEdits
    {
        get => _renderEdits;
        set
        {
            _renderEdits = value;
            _imagesStale = true;
            Invalidate();
        }
    }

    /// <summary>Si la vista muestra la línea de tiempo del resultado, sin lo borrado.</summary>
    /// <value><c>true</c> para juntar lo que queda a ambos lados de cada borrado, como la copia.</value>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool CollapseDeletions
    {
        get => _collapseDeletions;
        set
        {
            _collapseDeletions = value;
            _imagesStale = true;
            Invalidate();
        }
    }

    /// <summary>Instantes de la vista en los que se unió lo anterior y lo posterior a un borrado.</summary>
    /// <value>Empalmes a marcar, en segundos de la línea de tiempo que muestra la vista.</value>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public IReadOnlyList<double> Splices
    {
        get => _splices;
        set
        {
            _splices = value ?? [];
            Invalidate();
        }
    }

    /// <summary>Si la vista solo se mira: ni marcas, ni selección, ni fundidos se pueden tocar.</summary>
    /// <value><c>true</c> para desactivar la edición; la rueda sigue desplazando y acercando.</value>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool ReadOnly
    {
        get => _readOnly;
        set
        {
            _readOnly = value;
            EndDrag();
            Invalidate();
        }
    }

    /// <summary>Si en la vista solo se pueden mover las marcas de recorte.</summary>
    /// <value>
    /// <c>true</c> para dejar mover las marcas con el ratón y el teclado, y marcar con un clic desde
    /// dónde escuchar, sin seleccionar ni ajustar fundidos.
    /// </value>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool MarkersOnly
    {
        get => _markersOnly;
        set
        {
            _markersOnly = value;
            EndDrag();
            Invalidate();
        }
    }

    /// <summary>Punto desde el que sonará la reproducción si no hay selección.</summary>
    /// <value>Segundos del archivo, o <c>null</c> si no se fijó.</value>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public double? CursorSeconds
    {
        get => _cursorSeconds;
        set
        {
            _cursorSeconds = value;
            Invalidate();
        }
    }

    /// <summary>Fundidos que se dibujan sobre la onda.</summary>
    /// <value>Los fundidos de la pista, o <c>null</c> si no hay ninguno.</value>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TrackFades? Fades
    {
        get => _fades;
        set
        {
            _fades = value;
            Invalidate();
            AccessibilityNotifyClients(AccessibleEvents.ValueChange, -1);
        }
    }

    /// <summary>Tramo seleccionado.</summary>
    /// <value>La selección, o <c>null</c> si no hay ninguna.</value>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TimeRegion? Selection
    {
        get => _selection;
        set
        {
            _selection = value;
            Invalidate();
            AccessibilityNotifyClients(AccessibleEvents.ValueChange, -1);
        }
    }

    /// <summary>Fragmentos borrados que se dibujan sobre la onda.</summary>
    /// <value>Lo borrado; <see cref="DeletedRegions.Empty"/> si no hay nada.</value>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public DeletedRegions Deletions
    {
        get => _deletions;
        set
        {
            _deletions = value ?? DeletedRegions.Empty;
            Invalidate();
            AccessibilityNotifyClients(AccessibleEvents.ValueChange, -1);
        }
    }

    /// <summary>Escala vertical de la forma de onda.</summary>
    /// <value>Lineal o en dB. Por defecto, <see cref="AmplitudeScale.Linear"/>.</value>
    [Category("Forma de onda")]
    [Description("Escala vertical: lineal de −1 a 1, o en dB para ver colas de fundido y hiss.")]
    [DefaultValue(AmplitudeScale.Linear)]
    public AmplitudeScale AmplitudeScale
    {
        get => _amplitudeScale;
        set
        {
            _amplitudeScale = value;
            _imagesStale = true;
            Invalidate();
        }
    }

    /// <summary>Si se muestra y se puede mover la marca de inicio.</summary>
    /// <value><c>true</c> para mostrarla. Por defecto, <c>true</c>.</value>
    [Category("Forma de onda")]
    [Description("Muestra la marca de inicio de la copia y permite moverla.")]
    [DefaultValue(true)]
    public bool ShowStartMarker
    {
        get => _showStartMarker;
        set
        {
            _showStartMarker = value;
            Invalidate();
        }
    }

    /// <summary>Si se muestra y se puede mover la marca de final.</summary>
    /// <value><c>true</c> para mostrarla. Por defecto, <c>true</c>.</value>
    [Category("Forma de onda")]
    [Description("Muestra la marca de final de la copia y permite moverla.")]
    [DefaultValue(true)]
    public bool ShowEndMarker
    {
        get => _showEndMarker;
        set
        {
            _showEndMarker = value;
            Invalidate();
        }
    }

    /// <summary>Si se dibuja la regla de tiempo bajo la forma de onda.</summary>
    /// <value><c>true</c> para dibujarla. Por defecto, <c>true</c>.</value>
    [Category("Forma de onda")]
    [Description("Dibuja la regla de tiempo bajo la forma de onda.")]
    [DefaultValue(true)]
    public bool ShowTimeRuler
    {
        get => _showTimeRuler;
        set
        {
            _showTimeRuler = value;
            _imagesStale = true;
            Invalidate();
        }
    }

    /// <summary>Forma de onda que se muestra. La vista no la libera: pertenece a quien la asigna.</summary>
    /// <value>La forma de onda, o <c>null</c> para mostrar <see cref="StatusText"/>.</value>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Waveform? Waveform
    {
        get => _waveform;
        set
        {
            _waveform = value;
            _imagesStale = true;
            Invalidate();
        }
    }

    /// <summary>Texto que se muestra mientras no hay forma de onda, como el progreso o un error.</summary>
    /// <value>Texto centrado en la vista vacía.</value>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string StatusText
    {
        get => _statusText;
        set
        {
            _statusText = value ?? string.Empty;
            Invalidate();
        }
    }

    /// <summary>Instante que está sonando, para dibujar el cursor de reproducción.</summary>
    /// <value>Segundos del archivo, o <c>null</c> si no suena nada.</value>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public double? PlayheadSeconds
    {
        get => _playheadSeconds;
        set
        {
            if (_playheadSeconds != value)
            {
                _playheadSeconds = value;
                Invalidate();
            }
        }
    }

    /// <summary>Marca que mueve el teclado.</summary>
    /// <value>La marca activa, o <see cref="TrimMarker.None"/> si aún no se eligió.</value>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TrimMarker ActiveMarker
    {
        get => _activeMarker;
        set
        {
            _activeMarker = IsShown(value) ? value : TrimMarker.None;
            Invalidate();
        }
    }

    /// <value>Duración de la pista, que limita la marca de final.</value>
    public double DurationSeconds => _durationSeconds;

    /// <value>Instante en el borde izquierdo de la vista.</value>
    public double ViewStartSeconds => _viewStartSeconds;

    /// <value>Instante en el borde derecho de la vista.</value>
    public double ViewEndSeconds => _viewEndSeconds;

    /// <value>Instante en el que empezaría la copia recortada.</value>
    public double StartMarkerSeconds => _startMarkerSeconds;

    /// <value>Instante en el que terminaría la copia recortada.</value>
    public double EndMarkerSeconds => _endMarkerSeconds;

    private int RulerHeight => _showTimeRuler ? Font.Height + LogicalToDeviceUnits(6) : 0;

    private Rectangle ImageBounds => new(0, 0, Math.Max(1, ClientSize.Width), Math.Max(1, ClientSize.Height - RulerHeight));

    /// <summary>Fija el tramo de tiempo visible.</summary>
    /// <param name="durationSeconds">Duración de la pista, que limita la marca de final.</param>
    /// <param name="startSeconds">Instante en el borde izquierdo.</param>
    /// <param name="endSeconds">Instante en el borde derecho; debe ser posterior al izquierdo.</param>
    /// <exception cref="ArgumentOutOfRangeException">Se lanza si el tramo está vacío o la duración no es positiva.</exception>
    public void SetView(double durationSeconds, double startSeconds, double endSeconds)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(durationSeconds);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(endSeconds, startSeconds);

        _durationSeconds = durationSeconds;
        _scrollMinSeconds = 0.0;
        _scrollMaxSeconds = durationSeconds;
        _viewStartSeconds = startSeconds;
        _viewEndSeconds = endSeconds;
        _imagesStale = true;
        Invalidate();
    }

    /// <summary>Limita hasta dónde se puede desplazar o alejar la vista.</summary>
    /// <param name="minSeconds">Instante más temprano que se puede mostrar.</param>
    /// <param name="maxSeconds">Instante más tardío que se puede mostrar.</param>
    /// <remarks>
    /// La vista del final solo tiene cargada la cola de la pista: más allá no hay onda que pintar,
    /// así que no se deja llegar.
    /// </remarks>
    public void SetScrollLimits(double minSeconds, double maxSeconds)
    {
        _scrollMinSeconds = Math.Max(0.0, minSeconds);
        _scrollMaxSeconds = Math.Max(_scrollMinSeconds + MinimumSpanSeconds, maxSeconds);
        ShowRange(_viewStartSeconds, _viewEndSeconds);
    }

    /// <summary>Muestra un tramo, ajustado a los límites y al zoom máximo, y avisa si cambió.</summary>
    /// <param name="startSeconds">Instante deseado en el borde izquierdo.</param>
    /// <param name="endSeconds">Instante deseado en el borde derecho.</param>
    public void ShowRange(double startSeconds, double endSeconds)
    {
        double limit = _scrollMaxSeconds - _scrollMinSeconds;
        double span = Math.Clamp(endSeconds - startSeconds, MinimumSpanSeconds, limit);
        double start = Math.Clamp(startSeconds, _scrollMinSeconds, _scrollMaxSeconds - span);

        if (start == _viewStartSeconds && start + span == _viewEndSeconds)
        {
            return;
        }

        _viewStartSeconds = start;
        _viewEndSeconds = start + span;
        _imagesStale = true;
        Invalidate();
        ViewChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Acerca o aleja la vista manteniendo un instante en el mismo sitio de la pantalla.</summary>
    /// <param name="factor">Mayor que 1 acerca; menor que 1 aleja.</param>
    /// <param name="anchorSeconds">Instante que no se mueve; el centro de la vista si es <c>null</c>.</param>
    public void ZoomBy(double factor, double? anchorSeconds = null)
    {
        double anchor = anchorSeconds ?? (_viewStartSeconds + _viewEndSeconds) / 2.0;
        double span = _viewEndSeconds - _viewStartSeconds;
        double fraction = (anchor - _viewStartSeconds) / span;
        double newSpan = span / factor;
        ShowRange(anchor - (fraction * newSpan), anchor + ((1.0 - fraction) * newSpan));
    }
    /// <summary>Fija las marcas desde código, sin producir <see cref="MarkersChanged"/>.</summary>
    /// <param name="startSeconds">Instante de inicio de la copia.</param>
    /// <param name="endSeconds">Instante de final de la copia.</param>
    public void SetMarkers(double startSeconds, double endSeconds)
    {
        _startMarkerSeconds = startSeconds;
        _endMarkerSeconds = endSeconds;
        Invalidate();
        AccessibilityNotifyClients(AccessibleEvents.ValueChange, -1);
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ReleaseImages();
        }

        base.Dispose(disposing);
    }

    /// <inheritdoc/>
    protected override AccessibleObject CreateAccessibilityInstance() => new ViewAccessibleObject(this);

    /// <inheritdoc/>
    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        Rectangle image = ImageBounds;

        if (_waveform is null)
        {
            using SolidBrush background = new(Color.FromArgb(unchecked((int)WaveformPalette.Normal.Background)));
            g.FillRectangle(background, image);
            TextRenderer.DrawText(g, _statusText, Font, image, ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak);
        }
        else
        {
            EnsureImages(image.Size);
            g.DrawImageUnscaled(_normalImage!.Bitmap, 0, 0);
            DrawRemoved(g, image, _viewStartSeconds, _startMarkerSeconds);
            DrawRemoved(g, image, _endMarkerSeconds, _viewEndSeconds);
            DrawDeletions(g, image);
        }

        DrawFades(g, image);
        DrawSplices(g, image);
        DrawSelection(g, image);

        if (_showStartMarker)
        {
            DrawMarker(g, image, TrimMarker.Start, _startMarkerSeconds);
        }

        if (_showEndMarker)
        {
            DrawMarker(g, image, TrimMarker.End, _endMarkerSeconds);
        }

        if (_cursorSeconds is { } cursor && _playheadSeconds is null)
        {
            DrawCursor(g, image, cursor);
        }

        if (_playheadSeconds is { } playhead)
        {
            DrawPlayhead(g, image, playhead);
        }

        // Las referencias de amplitud van encima de las marcas para que una marca pegada al borde
        // no tape la cifra.
        if (_waveform is not null)
        {
            DrawAmplitudeLabels(g, image);
        }

        if (_showTimeRuler)
        {
            DrawRuler(g, new Rectangle(0, image.Bottom, ClientSize.Width, RulerHeight));
        }

        if (Focused && ShowFocusCues)
        {
            ControlPaint.DrawFocusRectangle(g, new Rectangle(1, 1, image.Width - 2, image.Height - 2));
        }
    }

    /// <inheritdoc/>
    protected override void OnGotFocus(EventArgs e)
    {
        if (_activeMarker == TrimMarker.None)
        {
            _activeMarker = _showStartMarker ? TrimMarker.Start : _showEndMarker ? TrimMarker.End : TrimMarker.None;
        }

        Invalidate();
        base.OnGotFocus(e);
    }

    /// <inheritdoc/>
    protected override void OnLostFocus(EventArgs e)
    {
        Invalidate();
        base.OnLostFocus(e);
    }

    /// <inheritdoc/>
    protected override bool IsInputKey(Keys keyData) =>
        (keyData & Keys.KeyCode) is Keys.Left or Keys.Right or Keys.Home or Keys.End || base.IsInputKey(keyData);

    /// <inheritdoc/>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (_readOnly)
        {
            base.OnKeyDown(e);
            return;
        }

        switch (e.KeyCode)
        {
            case Keys.Home when _showStartMarker:
                ActiveMarker = TrimMarker.Start;
                e.Handled = true;
                break;

            case Keys.End when _showEndMarker:
                ActiveMarker = TrimMarker.End;
                e.Handled = true;
                break;

            case Keys.Left or Keys.Right when _activeMarker != TrimMarker.None:
                double step = e.Control ? LargeStepSeconds : e.Shift ? CoarseStepSeconds : FineStepSeconds;
                double direction = e.KeyCode == Keys.Left ? -1.0 : 1.0;
                MoveMarker(_activeMarker, MarkerSeconds(_activeMarker) + (direction * step));
                e.Handled = true;
                break;
        }

        base.OnKeyDown(e);
    }

    /// <inheritdoc/>
    protected override void OnMouseDown(MouseEventArgs e)
    {
        Focus();

        if (_readOnly)
        {
            base.OnMouseDown(e);
            return;
        }

        if (e.Button == MouseButtons.Left && HitTest(e.X) is var marker and not TrimMarker.None)
        {
            _dragging = marker;
            ActiveMarker = marker;
            Capture = true;
        }
        else if (e.Button == MouseButtons.Left && _waveform is not null && _markersOnly)
        {
            // Fuera de las marcas solo cabe un clic, que marca desde dónde escuchar.
            _dragTarget = DragTarget.Click;
            _dragStarted = false;
            _dragOriginX = e.X;
            Capture = true;
        }
        else if (e.Button == MouseButtons.Left && _waveform is not null)
        {
            // Sobre un borde de la selección o de un fundido se arrastra ese borde: el ancla es el
            // borde contrario. En cualquier otro punto empieza una selección nueva donde se pulsó.
            _dragTarget = EdgeAt(e.X, out double anchor);
            _dragStarted = _dragTarget != DragTarget.None;
            if (_dragTarget == DragTarget.None)
            {
                _dragTarget = DragTarget.Selection;
                anchor = Snap(SecondsAt(e.X));
            }

            _dragAnchor = anchor;
            _dragOriginX = e.X;
            Capture = true;
        }

        base.OnMouseDown(e);
    }

    /// <inheritdoc/>
    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (_dragging != TrimMarker.None)
        {
            MoveMarker(_dragging, SecondsAt(e.X));
        }
        else if (_dragTarget != DragTarget.None)
        {
            _dragStarted |= Math.Abs(e.X - _dragOriginX) >= LogicalToDeviceUnits(SelectionThreshold);
            if (_dragStarted)
            {
                double current = Snap(SecondsAt(e.X));
                double start = Math.Clamp(Math.Min(_dragAnchor, current), 0.0, _durationSeconds);
                double end = Math.Clamp(Math.Max(_dragAnchor, current), 0.0, _durationSeconds);
                ApplyDrag(start, end);
            }
        }
        else
        {
            Cursor = _readOnly || _waveform is null
                ? Cursors.Default
                : HitTest(e.X) != TrimMarker.None ? Cursors.SizeWE
                : _markersOnly ? Cursors.Default
                : EdgeAt(e.X, out _) != DragTarget.None ? Cursors.SizeWE : Cursors.IBeam;
        }

        base.OnMouseMove(e);
    }

    /// <inheritdoc/>
    protected override void OnMouseUp(MouseEventArgs e)
    {
        // Un clic que no llegó a arrastrar no es una selección: lo interpreta quien contiene la
        // vista, que sabe si toca situar el cursor, seleccionar lo borrado o mostrar un fundido.
        if ((_dragTarget is DragTarget.Selection or DragTarget.Click) && !_dragStarted)
        {
            WaveformClicked?.Invoke(this, new WaveformClickEventArgs(Math.Clamp(SecondsAt(e.X), 0.0, _durationSeconds)));
        }

        EndDrag();
        Capture = false;
        base.OnMouseUp(e);
    }

    /// <inheritdoc/>
    protected override void OnMouseWheel(MouseEventArgs e)
    {
        double notches = e.Delta / (double)SystemInformation.MouseWheelScrollDelta;
        if ((ModifierKeys & Keys.Control) != 0)
        {
            ZoomBy(Math.Pow(WheelZoomFactor, notches), SecondsAt(e.X));
        }
        else
        {
            double shift = -notches * WheelScrollFraction * (_viewEndSeconds - _viewStartSeconds);
            ShowRange(_viewStartSeconds + shift, _viewEndSeconds + shift);
        }

        if (e is HandledMouseEventArgs handled)
        {
            handled.Handled = true;
        }

        base.OnMouseWheel(e);
    }

    /// <inheritdoc/>
    protected override void OnMouseCaptureChanged(EventArgs e)
    {
        EndDrag();
        base.OnMouseCaptureChanged(e);
    }

    /// <summary>Formatea un instante como minutos y segundos con los decimales indicados.</summary>
    /// <remarks>Redondea una sola vez para que 59.9996 s no se muestre como «00:59» seguido de «1.000».</remarks>
    private static string FormatTime(double seconds, int decimals)
    {
        double rounded = Math.Round(Math.Max(0.0, seconds), decimals);
        int minutes = (int)(rounded / 60);
        double rest = rounded - (minutes * 60);
        string format = decimals == 0 ? "00" : "00." + new string('0', decimals);
        return $"{minutes:00}:{rest.ToString(format, CultureInfo.CurrentCulture)}";
    }

    private bool IsShown(TrimMarker marker) => marker switch
    {
        TrimMarker.Start => _showStartMarker,
        TrimMarker.End => _showEndMarker,
        _ => true,
    };

    private double MarkerSeconds(TrimMarker marker) =>
        marker == TrimMarker.Start ? _startMarkerSeconds : _endMarkerSeconds;

    private float XFor(double seconds) =>
        (float)((seconds - _viewStartSeconds) / (_viewEndSeconds - _viewStartSeconds) * ImageBounds.Width);

    private double SecondsAt(int x) =>
        _viewStartSeconds + (x * (_viewEndSeconds - _viewStartSeconds) / ImageBounds.Width);

    /// <summary>Marca bajo el puntero, dando preferencia a la activa si se solapan.</summary>
    private TrimMarker HitTest(int x)
    {
        int tolerance = LogicalToDeviceUnits(6);
        TrimMarker best = TrimMarker.None;
        float bestDistance = float.MaxValue;

        foreach (TrimMarker marker in (ReadOnlySpan<TrimMarker>)[_activeMarker, TrimMarker.Start, TrimMarker.End])
        {
            if (marker == TrimMarker.None || !IsShown(marker))
            {
                continue;
            }

            float distance = Math.Abs(XFor(MarkerSeconds(marker)) - x);
            if (distance <= tolerance && distance < bestDistance)
            {
                best = marker;
                bestDistance = distance;
            }
        }

        return best;
    }

    private void EndDrag()
    {
        _dragging = TrimMarker.None;
        _dragTarget = DragTarget.None;
        _dragStarted = false;
    }

    /// <summary>Lleva el tramo arrastrado a lo que se está arrastrando y avisa.</summary>
    private void ApplyDrag(double start, double end)
    {
        switch (_dragTarget)
        {
            case DragTarget.Selection:
                _selection = end > start ? new TimeRegion(start, end) : null;
                Invalidate();
                SelectionChanged?.Invoke(this, EventArgs.Empty);
                break;

            case DragTarget.FadeIn:
                FadeAdjusted?.Invoke(this, new FadeAdjustedEventArgs(FadeDirection.In, start, end));
                break;

            case DragTarget.FadeOut:
                FadeAdjusted?.Invoke(this, new FadeAdjustedEventArgs(FadeDirection.Out, start, end));
                break;
        }
    }

    /// <summary>Borde arrastrable bajo el puntero: primero los de la selección, después los de los fundidos.</summary>
    /// <param name="x">Posición del puntero.</param>
    /// <param name="anchor">Borde contrario al encontrado, que queda fijo mientras se arrastra.</param>
    /// <returns>Qué se arrastraría, o <see cref="DragTarget.None"/> si no hay ningún borde cerca.</returns>
    private DragTarget EdgeAt(int x, out double anchor)
    {
        anchor = 0.0;
        int tolerance = LogicalToDeviceUnits(6);

        (DragTarget Target, double Start, double End)?[] candidates =
        [
            _selection is { } selection ? (DragTarget.Selection, selection.StartSeconds, selection.EndSeconds) : null,
            _fades?.FadeIn is { } fadeIn ? (DragTarget.FadeIn, fadeIn.StartSeconds, fadeIn.EndSeconds) : null,
            _fades?.FadeOut is { } fadeOut ? (DragTarget.FadeOut, fadeOut.StartSeconds, fadeOut.EndSeconds) : null,
        ];

        foreach ((DragTarget target, double start, double end) in candidates.OfType<(DragTarget, double, double)>())
        {
            float toStart = Math.Abs(XFor(start) - x);
            float toEnd = Math.Abs(XFor(end) - x);
            if (Math.Min(toStart, toEnd) <= tolerance)
            {
                anchor = toStart <= toEnd ? end : start;
                return target;
            }
        }

        return DragTarget.None;
    }

    /// <summary>
    /// Adhiere un instante a la marca de recorte o al borde de lo borrado más cercano si cae a pocos
    /// píxeles: un fundido que debe acabar justo en el corte final no tendría que depender del pulso.
    /// </summary>
    private double Snap(double seconds)
    {
        int tolerance = LogicalToDeviceUnits(6);
        IEnumerable<double> targets = [_startMarkerSeconds, _endMarkerSeconds, .. _deletions.Regions.SelectMany(r => (double[])[r.StartSeconds, r.EndSeconds])];
        foreach (double target in targets)
        {
            if (Math.Abs(XFor(target) - XFor(seconds)) <= tolerance)
            {
                return target;
            }
        }

        return seconds;
    }

    /// <summary>Mueve una marca respetando el orden y la separación mínima, y avisa si cambió.</summary>
    private void MoveMarker(TrimMarker marker, double seconds)
    {
        double previous = MarkerSeconds(marker);

        if (marker == TrimMarker.Start)
        {
            _startMarkerSeconds = Math.Clamp(seconds, 0.0, Math.Max(0.0, _endMarkerSeconds - MinimumGapSeconds));
        }
        else
        {
            _endMarkerSeconds = Math.Clamp(seconds, Math.Min(_durationSeconds, _startMarkerSeconds + MinimumGapSeconds), _durationSeconds);
        }

        if (MarkerSeconds(marker) != previous)
        {
            Invalidate();
            AccessibilityNotifyClients(AccessibleEvents.ValueChange, -1);
            MarkersChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void EnsureImages(Size size)
    {
        if (!_imagesStale && _normalImage?.Size == size)
        {
            return;
        }

        ReleaseImages();
        _normalImage = RenderImage(size, WaveformPalette.Normal);
        _removedImage = RenderImage(size, WaveformPalette.Removed);
        _imagesStale = false;
    }

    private PinnedBitmap RenderImage(Size size, WaveformPalette palette)
    {
        PinnedBitmap image = new(size.Width, size.Height);
        WaveformRenderer.Render(
            _waveform!,
            image.Pixels,
            size.Width,
            size.Height,
            size.Width,
            _viewStartSeconds,
            _viewEndSeconds,
            _amplitudeScale,
            palette,
            _renderEdits,
            _collapseDeletions);
        return image;
    }

    private void ReleaseImages()
    {
        _normalImage?.Dispose();
        _removedImage?.Dispose();
        _normalImage = null;
        _removedImage = null;
    }

    /// <summary>Superpone la paleta de lo eliminado en el tramo que descartaría el recorte.</summary>
    private void DrawRemoved(Graphics g, Rectangle image, double fromSeconds, double toSeconds)
    {
        int x0 = Math.Clamp((int)Math.Floor(XFor(fromSeconds)), 0, image.Width);
        int x1 = Math.Clamp((int)Math.Ceiling(XFor(toSeconds)), 0, image.Width);
        if (x1 <= x0)
        {
            return;
        }

        Rectangle part = new(x0, 0, x1 - x0, image.Height);
        g.DrawImage(_removedImage!.Bitmap, part, part, GraphicsUnit.Pixel);
    }

    /// <summary>
    /// Marca cada empalme con una línea morada discontinua y un rombo arriba: ahí se juntan lo
    /// anterior y lo posterior a un borrado, que es donde podría oírse un salto.
    /// </summary>
    private void DrawSplices(Graphics g, Rectangle image)
    {
        if (_splices.Count == 0)
        {
            return;
        }

        float half = LogicalToDeviceUnits(5);
        using Pen line = new(SpliceColor, LogicalToDeviceUnits(1)) { DashStyle = DashStyle.Dash };
        using SolidBrush mark = new(SpliceColor);

        g.SmoothingMode = SmoothingMode.AntiAlias;
        foreach (double seconds in _splices)
        {
            float x = XFor(seconds);
            if (x < 0 || x > image.Width)
            {
                continue;
            }

            g.DrawLine(line, x, 0, x, image.Bottom);
            g.FillPolygon(mark, [new PointF(x, 0), new PointF(x + half, half), new PointF(x, half * 2), new PointF(x - half, half)]);
        }

        g.SmoothingMode = SmoothingMode.None;
    }

    /// <summary>Dibuja lo borrado con la paleta de lo eliminado, rayado y con su etiqueta.</summary>
    private void DrawDeletions(Graphics g, Rectangle image)
    {
        foreach (TimeRegion region in _deletions.Regions)
        {
            DrawRemoved(g, image, region.StartSeconds, region.EndSeconds);

            int x0 = Math.Clamp((int)Math.Floor(XFor(region.StartSeconds)), 0, image.Width);
            int x1 = Math.Clamp((int)Math.Ceiling(XFor(region.EndSeconds)), 0, image.Width);
            if (x1 <= x0)
            {
                continue;
            }

            using (HatchBrush hatch = new(HatchStyle.WideUpwardDiagonal, DeletedHatchColor, Color.Transparent))
            {
                g.FillRectangle(hatch, x0, 0, x1 - x0, image.Height);
            }

            DrawCenteredLabel(g, "Borrado", x0, x1, image.Height / 2, Color.White);
        }
    }

    /// <summary>Dibuja la selección: un velo azul, sus bordes y su duración.</summary>
    private void DrawSelection(Graphics g, Rectangle image)
    {
        if (_selection is not { } selection)
        {
            return;
        }

        int x0 = Math.Clamp((int)Math.Floor(XFor(selection.StartSeconds)), 0, image.Width);
        int x1 = Math.Clamp((int)Math.Ceiling(XFor(selection.EndSeconds)), 0, image.Width);
        if (x1 <= x0)
        {
            return;
        }

        using (SolidBrush tint = new(SelectionTintColor))
        {
            g.FillRectangle(tint, x0, 0, x1 - x0, image.Height);
        }

        using (Pen edge = new(SelectionLineColor, LogicalToDeviceUnits(2)))
        {
            g.DrawLine(edge, x0, 0, x0, image.Bottom);
            g.DrawLine(edge, x1, 0, x1, image.Bottom);
        }

        int top = LogicalToDeviceUnits(6) + Font.Height + LogicalToDeviceUnits(3);
        DrawCenteredLabel(g, $"Selección {selection.DurationSeconds.ToString("0.000", CultureInfo.CurrentCulture)} s", x0, x1, top, Color.White);
    }

    /// <summary>Etiqueta centrada en un tramo; si no cabe, se omite para no tapar la onda vecina.</summary>
    private void DrawCenteredLabel(Graphics g, string label, int x0, int x1, int top, Color background)
    {
        Size text = TextRenderer.MeasureText(g, label, Font);
        if (text.Width + LogicalToDeviceUnits(6) > x1 - x0)
        {
            return;
        }

        Rectangle box = new(x0 + ((x1 - x0 - text.Width) / 2), top, text.Width, text.Height);
        using (SolidBrush fill = new(background))
        {
            g.FillRectangle(fill, box);
        }

        g.DrawRectangle(Pens.Black, box);
        TextRenderer.DrawText(g, label, Font, box, Color.Black, TextFormatFlags.NoPadding);
    }

    /// <summary>
    /// Dibuja cada fundido: un velo ámbar sobre su tramo, la envolvente de ganancia arriba y abajo
    /// —como las envolventes de Audacity— y una etiqueta con su sentido y su curva.
    /// </summary>
    /// <remarks>
    /// La envolvente es la ganancia combinada de ambos fundidos pasada por la escala vertical de la
    /// vista: en dB se ve la caída real, y donde dos fundidos se solapan se ve su producto, que es lo
    /// que sonará.
    /// </remarks>
    private void DrawFades(Graphics g, Rectangle image)
    {
        if (_fades is not { } fades)
        {
            return;
        }

        foreach (Fade? candidate in (ReadOnlySpan<Fade?>)[fades.FadeIn, fades.FadeOut])
        {
            if (candidate is not { } fade)
            {
                continue;
            }

            int x0 = Math.Clamp((int)Math.Floor(XFor(fade.StartSeconds)), 0, image.Width);
            int x1 = Math.Clamp((int)Math.Ceiling(XFor(fade.EndSeconds)), 0, image.Width);
            if (x1 <= x0)
            {
                continue;
            }

            using (SolidBrush tint = new(FadeTintColor))
            {
                g.FillRectangle(tint, x0, 0, x1 - x0, image.Height);
            }

            DrawEnvelope(g, image, fades, fade, x0, x1);
            DrawFadeEdges(g, image, fade);
            DrawFadeLabel(g, image, fade, x0, x1);
        }
    }

    /// <summary>Envolvente de un fundido, muestreada una vez por píxel.</summary>
    /// <remarks>
    /// El instante de cada píxel se acota al tramo del fundido: los píxeles de los bordes, redondeados
    /// hacia fuera, caerían justo fuera de él, donde la ganancia vuelve a 1, y dibujarían un pico
    /// vertical que no existe en el audio.
    /// </remarks>
    private void DrawEnvelope(Graphics g, Rectangle image, TrackFades fades, Fade fade, int x0, int x1)
    {
        PointF[] top = new PointF[x1 - x0 + 1];
        PointF[] bottom = new PointF[top.Length];
        float middle = (image.Height - 1) / 2f;

        for (int i = 0; i < top.Length; i++)
        {
            int x = x0 + i;
            double seconds = Math.Clamp(SecondsAt(x), fade.StartSeconds, fade.EndSeconds);
            float height = (float)_amplitudeScale.ToHeight(fades.GainAt(seconds));
            top[i] = new PointF(x, middle - (height * middle));
            bottom[i] = new PointF(x, middle + (height * middle));
        }

        g.SmoothingMode = SmoothingMode.AntiAlias;
        using (Pen outline = new(Color.White, LogicalToDeviceUnits(4)))
        {
            g.DrawLines(outline, top);
            g.DrawLines(outline, bottom);
        }

        using (Pen line = new(FadeLineColor, LogicalToDeviceUnits(2)))
        {
            g.DrawLines(line, top);
            g.DrawLines(line, bottom);
        }

        g.SmoothingMode = SmoothingMode.None;
    }

    /// <summary>Bordes del tramo en trazo discontinuo, para distinguirlos de las marcas de recorte, que son continuas.</summary>
    private void DrawFadeEdges(Graphics g, Rectangle image, Fade fade)
    {
        using Pen edge = new(FadeLineColor, LogicalToDeviceUnits(1)) { DashStyle = DashStyle.Dash };
        foreach (double seconds in (ReadOnlySpan<double>)[fade.StartSeconds, fade.EndSeconds])
        {
            float x = XFor(seconds);
            if (x >= 0 && x <= image.Width)
            {
                g.DrawLine(edge, x, 0, x, image.Bottom);
            }
        }
    }

    /// <summary>Etiqueta al pie del tramo; si no cabe en él, se omite para no tapar la onda vecina.</summary>
    private void DrawFadeLabel(Graphics g, Rectangle image, Fade fade, int x0, int x1)
    {
        string label = $"{FadeText.Name(fade.Direction)} · {FadeText.Name(fade.Curve)}";
        Size text = TextRenderer.MeasureText(g, label, Font);
        int padding = LogicalToDeviceUnits(3);
        if (text.Width + (2 * padding) > x1 - x0)
        {
            return;
        }

        int left = fade.Direction == FadeDirection.In ? x0 + padding : x1 - text.Width - padding;

        // Como las etiquetas de las marcas, empieza donde terminan las referencias de amplitud:
        // encima de ellas quedaría tapada.
        int gutter = _waveform is null ? 0 : AmplitudeLabelsWidth(g) + padding;
        if (left < gutter)
        {
            if (gutter + text.Width + padding > x1)
            {
                return;
            }

            left = gutter;
        }

        Rectangle box = new(left, image.Bottom - text.Height - padding, text.Width, text.Height);

        g.FillRectangle(Brushes.White, box);
        g.DrawRectangle(Pens.Black, box);
        TextRenderer.DrawText(g, label, Font, box, Color.Black, TextFormatFlags.NoPadding);
    }

    /// <summary>
    /// Dibuja una marca como una línea negra con borde blanco y una etiqueta con su instante: así se
    /// distingue tanto sobre el fondo blanco como sobre la onda azul o la zona gris.
    /// </summary>
    private void DrawMarker(Graphics g, Rectangle image, TrimMarker marker, double seconds)
    {
        float x = XFor(seconds);
        if (x < -1 || x > image.Width + 1)
        {
            return;
        }

        bool active = marker == _activeMarker && (Focused || _dragging == marker);
        float width = LogicalToDeviceUnits(active ? 3 : 1);

        g.SmoothingMode = SmoothingMode.None;
        using (Pen outline = new(Color.White, width + LogicalToDeviceUnits(2)))
        {
            g.DrawLine(outline, x, 0, x, image.Bottom);
        }

        using (Pen line = new(Color.Black, width))
        {
            g.DrawLine(line, x, 0, x, image.Bottom);
        }

        string label = $"{(marker == TrimMarker.Start ? "Inicio" : "Final")} {FormatTime(seconds, 3)}";
        Size text = TextRenderer.MeasureText(g, label, Font);
        int padding = LogicalToDeviceUnits(3);
        int left = marker == TrimMarker.Start ? (int)x + padding : (int)x - text.Width - padding;

        // La columna izquierda es de las referencias de amplitud: una etiqueta de marca encima
        // quedaría tapada y se leería mal, así que empieza donde terminan ellas.
        int gutter = _waveform is null ? 0 : AmplitudeLabelsWidth(g) + padding;
        left = Math.Clamp(left, Math.Min(gutter, Math.Max(0, image.Width - text.Width - 1)), Math.Max(0, image.Width - text.Width - 1));
        Rectangle box = new(left, padding, text.Width, text.Height);

        g.FillRectangle(active ? Brushes.Yellow : Brushes.White, box);
        g.DrawRectangle(Pens.Black, box);
        TextRenderer.DrawText(g, label, Font, box, Color.Black, TextFormatFlags.NoPadding);
    }

    /// <summary>
    /// Dibuja el punto desde el que sonará la reproducción: una línea gris oscura discontinua. El
    /// trazo, y no solo el color, lo distingue del cursor que avanza mientras suena, que es rojo,
    /// continuo y con triángulo.
    /// </summary>
    private void DrawCursor(Graphics g, Rectangle image, double seconds)
    {
        float x = XFor(seconds);
        if (x < 0 || x > image.Width)
        {
            return;
        }

        using Pen line = new(CursorColor, LogicalToDeviceUnits(1)) { DashStyle = DashStyle.Dot };
        g.DrawLine(line, x, 0, x, image.Bottom);
    }

    /// <summary>
    /// Dibuja el cursor de reproducción: una línea roja con borde blanco rematada por un triángulo.
    /// La forma, y no solo el color, lo distingue de las marcas de recorte, que son negras y sin
    /// triángulo.
    /// </summary>
    private void DrawPlayhead(Graphics g, Rectangle image, double seconds)
    {
        float x = XFor(seconds);
        if (x < 0 || x > image.Width)
        {
            return;
        }

        float width = LogicalToDeviceUnits(2);
        using (Pen outline = new(Color.White, width + LogicalToDeviceUnits(2)))
        {
            g.DrawLine(outline, x, 0, x, image.Bottom);
        }

        using Pen line = new(PlayheadColor, width);
        g.DrawLine(line, x, 0, x, image.Bottom);

        float half = LogicalToDeviceUnits(6);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using SolidBrush head = new(PlayheadColor);
        g.FillPolygon(head, [new PointF(x - half, 0), new PointF(x + half, 0), new PointF(x, half * 1.5f)]);
        g.SmoothingMode = SmoothingMode.None;
    }

    /// <summary>
    /// Referencias de amplitud a la izquierda, como en Audacity: valores lineales o niveles en dB
    /// reflejados en ambas mitades, con −∞ en el centro.
    /// </summary>
    /// <remarks>
    /// Se omite la que se solaparía con otra ya dibujada: en una vista baja no caben todas, y unas
    /// cifras amontonadas no orientan. Se recorren por prioridad para conservar las más útiles.
    /// </remarks>
    private void DrawAmplitudeLabels(Graphics g, Rectangle image)
    {
        int padding = LogicalToDeviceUnits(2);
        using SolidBrush shade = new(Color.FromArgb(200, Color.White));
        List<(int Top, int Bottom)> placed = [];

        foreach ((double height, string text) in AmplitudeLabels())
        {
            int y = (int)Math.Round((1.0 - height) * (image.Height - 1) / 2.0);
            Size size = TextRenderer.MeasureText(g, text, Font);
            int top = Math.Clamp(y - (size.Height / 2), 0, Math.Max(0, image.Height - size.Height));
            int bottom = top + size.Height;

            if (placed.Exists(other => top < other.Bottom && bottom > other.Top))
            {
                continue;
            }

            placed.Add((top, bottom));
            g.FillRectangle(shade, padding, top, size.Width, size.Height);
            TextRenderer.DrawText(g, text, Font, new Point(padding, top), Color.Black);
        }
    }

    /// <summary>Referencias de la escala vigente, de la más a la menos útil para orientarse.</summary>
    private IEnumerable<(double Height, string Text)> AmplitudeLabels()
    {
        if (_amplitudeScale == AmplitudeScale.Linear)
        {
            foreach (double value in LinearTicks)
            {
                yield return (value, value.ToString("0.0", CultureInfo.CurrentCulture));
            }

            yield break;
        }

        yield return (1.0, $"{DecibelTicks[0]:0} dB");
        yield return (-1.0, $"{DecibelTicks[0]:0} dB");
        yield return (0.0, "−∞");

        foreach (double db in DecibelTicks.Skip(1))
        {
            double height = _amplitudeScale.ToHeight(Math.Pow(10, db / 20));
            yield return (height, $"{db:0} dB");
            yield return (-height, $"{db:0} dB");
        }
    }

    /// <summary>Anchura que ocupan las referencias de amplitud, con su margen, en la escala vigente.</summary>
    private int AmplitudeLabelsWidth(Graphics g)
    {
        string widest = _amplitudeScale == AmplitudeScale.Linear
            ? (-1.0).ToString("0.0", CultureInfo.CurrentCulture)
            : $"{DecibelTicks[^1]:0} dB";
        return TextRenderer.MeasureText(g, widest, Font).Width + LogicalToDeviceUnits(2);
    }

    private void DrawRuler(Graphics g, Rectangle ruler)
    {
        using SolidBrush background = new(BackColor);
        g.FillRectangle(background, ruler);

        double span = _viewEndSeconds - _viewStartSeconds;
        int minimumSpacing = TextRenderer.MeasureText("00:00.00", Font).Width + LogicalToDeviceUnits(12);
        double step = RulerSteps.FirstOrDefault(s => s / span * ruler.Width >= minimumSpacing, RulerSteps[^1]);
        int tick = LogicalToDeviceUnits(4);

        using Pen pen = new(ForeColor);
        for (double t = Math.Ceiling(_viewStartSeconds / step) * step; t <= _viewEndSeconds; t += step)
        {
            int x = (int)Math.Round(XFor(t));
            g.DrawLine(pen, x, ruler.Top, x, ruler.Top + tick);

            string label = FormatTime(t, step >= 1 ? 0 : step >= 0.1 ? 1 : 2);
            if (x + LogicalToDeviceUnits(2) + TextRenderer.MeasureText(g, label, Font).Width > ruler.Right)
            {
                // Una etiqueta que no cabe entera se omite: cortada se leería como otra cifra.
                continue;
            }

            TextRenderer.DrawText(g, label, Font, new Point(x + LogicalToDeviceUnits(2), ruler.Top + tick - LogicalToDeviceUnits(2)), ForeColor);
        }
    }

    private string DescribeMarkers()
    {
        List<string> parts = [];
        if (_showStartMarker)
        {
            parts.Add($"inicio en {_startMarkerSeconds:0.000} s");
        }

        if (_showEndMarker)
        {
            parts.Add($"final en {_endMarkerSeconds:0.000} s");
        }

        foreach (Fade? candidate in (ReadOnlySpan<Fade?>)[_fades?.FadeIn, _fades?.FadeOut])
        {
            if (candidate is { } fade)
            {
                parts.Add($"{FadeText.Name(fade.Direction).ToLowerInvariant()} {FadeText.Name(fade.Curve).ToLowerInvariant()} de {fade.StartSeconds:0.000} a {fade.EndSeconds:0.000} s");
            }
        }

        foreach (TimeRegion region in _deletions.Regions)
        {
            parts.Add($"borrado de {region.StartSeconds:0.000} a {region.EndSeconds:0.000} s");
        }

        if (_selection is { } selection)
        {
            parts.Add($"selección de {selection.StartSeconds:0.000} a {selection.EndSeconds:0.000} s");
        }

        return string.Join(", ", parts);
    }

    /// <summary>Lo que se está arrastrando fuera de las marcas de recorte.</summary>
    private enum DragTarget
    {
        None,
        Click,
        Selection,
        FadeIn,
        FadeOut,
    }

    /// <summary>
    /// Imagen cuyos píxeles son un arreglo administrado fijado en memoria: el pintor escribe en él
    /// y GDI+ lo lee sin copiarlo, y sin necesidad de código no seguro.
    /// </summary>
    private sealed class PinnedBitmap : IDisposable
    {
        private GCHandle _handle;

        public PinnedBitmap(int width, int height)
        {
            Pixels = new uint[width * height];
            _handle = GCHandle.Alloc(Pixels, GCHandleType.Pinned);
            Bitmap = new Bitmap(width, height, width * sizeof(uint), PixelFormat.Format32bppPArgb, _handle.AddrOfPinnedObject());
            Size = new Size(width, height);
        }

        public uint[] Pixels { get; }

        public Bitmap Bitmap { get; }

        public Size Size { get; }

        public void Dispose()
        {
            Bitmap.Dispose();
            if (_handle.IsAllocated)
            {
                _handle.Free();
            }
        }
    }

    /// <summary>Expone a los lectores de pantalla la posición de las marcas como valor del control.</summary>
    private sealed class ViewAccessibleObject(WaveformView owner) : ControlAccessibleObject(owner)
    {
        public override string? Value => owner.DescribeMarkers();
    }
}
