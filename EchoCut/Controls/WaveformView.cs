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

/// <summary>Tramo que el usuario seleccionó para un fundido.</summary>
/// <param name="direction">Fundido al que corresponde la selección.</param>
/// <param name="startSeconds">Comienzo del tramo, en segundos del archivo.</param>
/// <param name="endSeconds">Final del tramo, en segundos del archivo.</param>
public sealed class FadeSelectionEventArgs(FadeDirection direction, double startSeconds, double endSeconds) : EventArgs
{
    /// <value>Fundido al que corresponde la selección.</value>
    public FadeDirection Direction { get; } = direction;

    /// <value>Comienzo del tramo, en segundos del archivo.</value>
    public double StartSeconds { get; } = startSeconds;

    /// <value>Final del tramo, en segundos del archivo.</value>
    public double EndSeconds { get; } = endSeconds;
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
/// Fundidos: con <see cref="SelectionTarget"/> asignado, arrastrar fuera de las marcas selecciona el
/// tramo de ese fundido, como una selección de Audacity, y arrastrar uno de sus bordes lo ajusta.
/// Los extremos se adhieren a las marcas de recorte cercanas. La envolvente de <see cref="Fades"/>
/// se dibuja sobre la onda en ámbar, con la misma escala vertical que ella.
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
    private FadeDirection? _selectionTarget;
    private double? _selectionAnchor;
    private int _selectionOriginX;
    private bool _selectionStarted;

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

    /// <summary>Se produce mientras el usuario selecciona, o ajusta, el tramo de un fundido con el ratón.</summary>
    /// <remarks>
    /// Se produce en cada movimiento, no solo al soltar, para que quien escucha actualice en vivo las
    /// demás vistas y los campos numéricos. La vista no cambia <see cref="Fades"/> por su cuenta: es
    /// quien escucha quien decide si el tramo vale.
    /// </remarks>
    [Category("Forma de onda")]
    [Description("Se produce mientras el usuario selecciona el tramo de un fundido.")]
    public event EventHandler<FadeSelectionEventArgs>? FadeSelected;

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

    /// <summary>Fundido que se selecciona al arrastrar sobre la onda.</summary>
    /// <value>El sentido del fundido, o <c>null</c> si en esta vista no se seleccionan fundidos.</value>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public FadeDirection? SelectionTarget
    {
        get => _selectionTarget;
        set => _selectionTarget = value;
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
        _viewStartSeconds = startSeconds;
        _viewEndSeconds = endSeconds;
        _imagesStale = true;
        Invalidate();
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
        }

        DrawFades(g, image);

        if (_showStartMarker)
        {
            DrawMarker(g, image, TrimMarker.Start, _startMarkerSeconds);
        }

        if (_showEndMarker)
        {
            DrawMarker(g, image, TrimMarker.End, _endMarkerSeconds);
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

        if (e.Button == MouseButtons.Left && HitTest(e.X) is var marker and not TrimMarker.None)
        {
            _dragging = marker;
            ActiveMarker = marker;
            Capture = true;
        }
        else if (e.Button == MouseButtons.Left && _selectionTarget is not null && _waveform is not null)
        {
            // Sobre un borde del fundido se arrastra ese borde: el ancla es el borde contrario. En
            // cualquier otro punto empieza una selección nueva anclada donde se pulsó.
            FadeEdge edge = FadeEdgeAt(e.X);
            _selectionAnchor = edge switch
            {
                FadeEdge.Start => TargetFade!.Value.EndSeconds,
                FadeEdge.End => TargetFade!.Value.StartSeconds,
                _ => Snap(SecondsAt(e.X)),
            };
            _selectionStarted = edge != FadeEdge.None;
            _selectionOriginX = e.X;
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
        else if (_selectionAnchor is { } anchor)
        {
            _selectionStarted |= Math.Abs(e.X - _selectionOriginX) >= LogicalToDeviceUnits(SelectionThreshold);
            if (_selectionStarted)
            {
                double current = Snap(SecondsAt(e.X));
                double start = Math.Clamp(Math.Min(anchor, current), 0.0, _durationSeconds);
                double end = Math.Clamp(Math.Max(anchor, current), 0.0, _durationSeconds);
                FadeSelected?.Invoke(this, new FadeSelectionEventArgs(_selectionTarget!.Value, start, end));
            }
        }
        else
        {
            Cursor = HitTest(e.X) != TrimMarker.None || FadeEdgeAt(e.X) != FadeEdge.None
                ? Cursors.SizeWE
                : _selectionTarget is not null && _waveform is not null ? Cursors.IBeam : Cursors.Default;
        }

        base.OnMouseMove(e);
    }

    /// <inheritdoc/>
    protected override void OnMouseUp(MouseEventArgs e)
    {
        _dragging = TrimMarker.None;
        _selectionAnchor = null;
        Capture = false;
        base.OnMouseUp(e);
    }

    /// <inheritdoc/>
    protected override void OnMouseCaptureChanged(EventArgs e)
    {
        _dragging = TrimMarker.None;
        _selectionAnchor = null;
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

    /// <summary>Fundido que se edita en esta vista, si existe.</summary>
    private Fade? TargetFade => _selectionTarget switch
    {
        FadeDirection.In => _fades?.FadeIn,
        FadeDirection.Out => _fades?.FadeOut,
        _ => null,
    };

    /// <summary>Borde del fundido editable que queda bajo el puntero.</summary>
    private FadeEdge FadeEdgeAt(int x)
    {
        if (TargetFade is not { } fade)
        {
            return FadeEdge.None;
        }

        int tolerance = LogicalToDeviceUnits(6);
        float toStart = Math.Abs(XFor(fade.StartSeconds) - x);
        float toEnd = Math.Abs(XFor(fade.EndSeconds) - x);

        if (Math.Min(toStart, toEnd) > tolerance)
        {
            return FadeEdge.None;
        }

        return toStart <= toEnd ? FadeEdge.Start : FadeEdge.End;
    }

    /// <summary>
    /// Adhiere un instante a la marca de recorte más cercana si cae a pocos píxeles de ella: un
    /// fundido que debe acabar justo en el corte final no tendría que depender del pulso.
    /// </summary>
    private double Snap(double seconds)
    {
        int tolerance = LogicalToDeviceUnits(6);
        foreach (double marker in (ReadOnlySpan<double>)[_startMarkerSeconds, _endMarkerSeconds])
        {
            if (Math.Abs(XFor(marker) - XFor(seconds)) <= tolerance)
            {
                return marker;
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
            palette);
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

        return string.Join(", ", parts);
    }

    /// <summary>Borde de un fundido.</summary>
    private enum FadeEdge
    {
        None,
        Start,
        End,
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
