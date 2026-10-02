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
/// </remarks>
[DefaultEvent(nameof(MarkersChanged))]
public sealed class WaveformView : Control
{
    /// <summary>Separación mínima entre marcas: un tramo vacío no es una copia que se pueda escribir.</summary>
    public const double MinimumGapSeconds = 0.1;

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
    private TrimMarker _activeMarker;
    private TrimMarker _dragging;

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

        if (_showStartMarker)
        {
            DrawMarker(g, image, TrimMarker.Start, _startMarkerSeconds);
        }

        if (_showEndMarker)
        {
            DrawMarker(g, image, TrimMarker.End, _endMarkerSeconds);
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

        base.OnMouseDown(e);
    }

    /// <inheritdoc/>
    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (_dragging != TrimMarker.None)
        {
            MoveMarker(_dragging, SecondsAt(e.X));
        }
        else
        {
            Cursor = HitTest(e.X) == TrimMarker.None ? Cursors.Default : Cursors.SizeWE;
        }

        base.OnMouseMove(e);
    }

    /// <inheritdoc/>
    protected override void OnMouseUp(MouseEventArgs e)
    {
        _dragging = TrimMarker.None;
        Capture = false;
        base.OnMouseUp(e);
    }

    /// <inheritdoc/>
    protected override void OnMouseCaptureChanged(EventArgs e)
    {
        _dragging = TrimMarker.None;
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

        return string.Join(", ", parts);
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
