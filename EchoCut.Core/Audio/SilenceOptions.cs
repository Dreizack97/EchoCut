using System.ComponentModel;

namespace EchoCut.Audio;

/// <summary>
/// Parámetros configurables del algoritmo de detección de silencio.
/// Los valores por defecto están calibrados para música comercial masterizada.
/// </summary>
/// <remarks>
/// Los atributos <see cref="CategoryAttribute"/>, <see cref="DisplayNameAttribute"/> y
/// <see cref="DescriptionAttribute"/> son los que la ventana de parámetros avanzados usa para
/// construirse sola. Añadir aquí una propiedad pública la expone en la interfaz sin tocar el
/// formulario; el precio es que las explicaciones deben caber en una línea legible.
/// </remarks>
public sealed class SilenceOptions
{
    private const string UmbralCategory = "1 · Umbral";
    private const string FramingCategory = "2 · Medición";
    private const string DecisionCategory = "3 · Decisión";
    private const string FadeCategory = "4 · Fundido";
    private const string CutCategory = "5 · Corte";
    private const string PreviewCategory = "6 · Previsualización";
    private const string LeadingCategory = "7 · Inicio";

    // ------------------------------------------------------------------------------- Umbral

    /// <summary>Umbral base en dBFS por debajo del cual una trama se considera silencio.</summary>
    /// <value>Nivel en dBFS, antes de la adaptación al ruido de fondo. Por defecto, -50.0.</value>
    [Category(UmbralCategory)]
    [DisplayName("Umbral base (dBFS)")]
    [Description("Nivel por debajo del cual una trama se considera silencio, antes de adaptarlo al ruido de la pista.")]
    public double ThresholdDbfs { get; set; } = -50.0;

    /// <summary>
    /// Separación entre el umbral de entrada al silencio y el de salida (disparador de Schmitt).
    /// Las tramas en esta banda son decaimiento ambiguo (reverberación, fade) y se preservan.
    /// </summary>
    /// <value>Separación en dB entre el umbral alto y el bajo. Por defecto, 6.0.</value>
    [Category(UmbralCategory)]
    [DisplayName("Histéresis (dB)")]
    [Description("Separación entre el umbral que confirma música y el que declara silencio. La banda intermedia se conserva siempre: es decaimiento ambiguo.")]
    public double HysteresisDb { get; set; } = 6.0;

    /// <summary>Margen sobre el piso de ruido medido. Evita falsos negativos en grabaciones con hiss.</summary>
    /// <value>Margen en dB sobre el piso de ruido estimado. Por defecto, 6.0.</value>
    [Category(UmbralCategory)]
    [DisplayName("Margen sobre el ruido (dB)")]
    [Description("Cuánto se levanta el umbral por encima del ruido de fondo medido. Sin él, una grabación con hiss audible nunca cruzaría el umbral fijo.")]
    public double NoiseFloorMarginDb { get; set; } = 6.0;

    /// <summary>Techo absoluto del umbral efectivo. Impide que el piso adaptativo se coma música real.</summary>
    /// <value>Nivel en dBFS que el umbral efectivo nunca puede superar. Por defecto, -35.0.</value>
    [Category(UmbralCategory)]
    [DisplayName("Umbral máximo (dBFS)")]
    [Description("Techo absoluto del umbral efectivo. Impide que la adaptación al ruido lo suba tanto que empiece a comerse música.")]
    public double MaxThresholdDbfs { get; set; } = -35.0;

    /// <summary>
    /// El umbral efectivo nunca supera (nivel de programa − este valor). Protege másters muy
    /// silenciosos, en los que un umbral fijo queda demasiado cerca de la música.
    /// </summary>
    /// <value>Margen en dB por debajo del nivel de programa. Por defecto, 20.0.</value>
    /// <remarks>
    /// La referencia es el pasaje más sonoro medido en RMS sobre <see cref="LevelProbeSeconds"/>, no
    /// el pico instantáneo. Un pico lo fija una sola muestra —un clic aislado basta para moverlo— y
    /// además decae junto con la ventana: en una pista que termina en un fundido largo, el pico de
    /// los últimos 30 s queda muy por debajo del de la canción y hunde el umbral hasta no detectar
    /// nada. Como el nivel de programa está por debajo del pico en el factor de cresta (10-14 dB en
    /// música masterizada), el valor por defecto es 20 y no 30.
    /// </remarks>
    [Category(UmbralCategory)]
    [DisplayName("Margen bajo el nivel de programa (dB)")]
    [Description("El umbral nunca se acerca al pasaje más sonoro más de esto. Protege másters muy silenciosos, donde un umbral fijo queda demasiado cerca de la música.")]
    public double ProgramRelativeFloorDb { get; set; } = 20.0;

    /// <summary>
    /// Suelo absoluto del umbral efectivo. Por debajo de este nivel no hay contenido audible en
    /// ningún sistema de reproducción (el piso de ruido de 16 bits ronda los −96 dBFS), y sin este
    /// tope el ajuste relativo al nivel de programa hundiría el umbral en pistas casi mudas hasta el
    /// punto de que ni el silencio digital se reconocería como silencio.
    /// </summary>
    /// <value>Nivel en dBFS que el umbral efectivo nunca puede rebasar por abajo. Por defecto, -90.0.</value>
    [Category(UmbralCategory)]
    [DisplayName("Umbral mínimo (dBFS)")]
    [Description("Suelo absoluto del umbral. Sin él, una pista casi muda hundiría el umbral hasta no reconocer ni el silencio digital.")]
    public double MinThresholdDbfs { get; set; } = -90.0;

    // ----------------------------------------------------------------------------- Medición

    /// <summary>Duración de cada trama de análisis, en milisegundos.</summary>
    /// <value>Duración de trama en milisegundos. Por defecto, 20.0.</value>
    [Category(FramingCategory)]
    [DisplayName("Trama de análisis (ms)")]
    [Description("Duración de cada trama sobre la que se mide energía. Marca la resolución temporal de todo el análisis.")]
    public double FrameMilliseconds { get; set; } = 20.0;

    /// <summary>
    /// Corte del paso alto que se aplica antes de medir el nivel. Cero lo desactiva.
    /// </summary>
    /// <value>Frecuencia de corte en Hz, o <c>0</c> para desactivar el filtro. Por defecto, <see cref="Biquad.RlbHighPassHz"/>.</value>
    /// <remarks>
    /// Una medida de energía en banda completa cuenta lo que nadie oye. Un offset de continua o un
    /// retumbe subsónico de una digitalización de vinilo mantienen la cola muy por encima del
    /// umbral y bloquean la detección aunque el silencio sea perfectamente audible como tal. El
    /// valor por defecto es el del paso alto RLB de la norma EBU R128, que es exactamente el filtro
    /// que Audacity usa para el mismo fin al medir sonoridad; a 440 Hz atenúa 0,07 dB, así que no
    /// altera la medida de nada que sí se oiga.
    /// </remarks>
    [Category(FramingCategory)]
    [DisplayName("Paso alto previo (Hz)")]
    [Description("Corte del filtro que descarta continua y retumbe antes de medir. Sin él, un subgrave inaudible mantiene la cola por encima del umbral. Cero lo desactiva.")]
    public double HighPassHz { get; set; } = Biquad.RlbHighPassHz;

    /// <summary>
    /// Tramo sobre el que se buscan el pasaje más silencioso y el más sonoro de la ventana, que son
    /// respectivamente el piso de ruido y el nivel de programa.
    /// </summary>
    /// <value>Duración del tramo de sondeo, en segundos. Por defecto, 0.5.</value>
    /// <remarks>
    /// Sustituye a un percentil sobre toda la ventana. Un percentil mide la <em>composición</em> de
    /// lo analizado, no el ruido: en una cola con 20 s de silencio el percentil 5 cae de lleno en el
    /// silencio, pero con solo 1 s cae dentro de la música y el piso medido se dispara. Buscar el
    /// medio segundo más silencioso da la misma respuesta en ambos casos, que es la propiedad que
    /// hace falta cuando la ventana de sondeo se amplía sobre la marcha.
    /// </remarks>
    [Category(FramingCategory)]
    [DisplayName("Sondeo de nivel (s)")]
    [Description("Duración del pasaje más silencioso y del más sonoro que se buscan para estimar el ruido de fondo y el nivel de programa.")]
    public double LevelProbeSeconds { get; set; } = 0.5;

    /// <summary>
    /// Ventana de confirmación de contenido musical. Se exige que al menos la mitad de sus tramas
    /// superen el umbral alto para romper una racha de silencio; así un clic aislado no la rompe.
    /// </summary>
    /// <value>Duración de la ventana de confirmación, en milisegundos. Por defecto, 150.0.</value>
    [Category(FramingCategory)]
    [DisplayName("Confirmación de contenido (ms)")]
    [Description("Ventana en la que al menos la mitad de las tramas deben superar el umbral alto para dar por terminada una racha de silencio. Evita que un clic aislado la rompa.")]
    public double MinContentMilliseconds { get; set; } = 150.0;

    // ----------------------------------------------------------------------------- Decisión

    /// <summary>Silencio mínimo para considerar que la cola es recortable.</summary>
    /// <value>Duración mínima en segundos. Por defecto, 2.0.</value>
    [Category(DecisionCategory)]
    [DisplayName("Silencio mínimo (s)")]
    [Description("Silencio final que debe haber para que la cola se considere recortable.")]
    public double MinSilenceSeconds { get; set; } = 2.0;

    /// <summary>
    /// Cuánto debe hundirse el final del archivo por debajo del umbral para aceptarlo como
    /// silencio de verdad. Un silencio residual toca fondo en el piso del medio; una cola de
    /// reverberación sigue decayendo y el archivo termina a media caída, aún claramente audible.
    /// Sin esta comprobación, esa reverberación se recorta y el final suena cortado en seco.
    /// </summary>
    /// <value>Profundidad exigida en dB por debajo del umbral. Por defecto, 12.0.</value>
    [Category(DecisionCategory)]
    [DisplayName("Profundidad exigida (dB)")]
    [Description("Cuánto debe hundirse el final por debajo del umbral para aceptarlo como silencio. Distingue el silencio real de una reverberación que todavía cae.")]
    public double MinimumSilenceDepthDb { get; set; } = 12.0;

    /// <summary>Tramo final sobre el que se mide ese fondo.</summary>
    /// <value>Duración del tramo de sondeo, en segundos. Por defecto, 0.5.</value>
    [Category(DecisionCategory)]
    [DisplayName("Sondeo de profundidad (s)")]
    [Description("Tramo final del archivo sobre el que se mide esa profundidad.")]
    public double SilenceDepthProbeSeconds { get; set; } = 0.5;

    /// <summary>
    /// Pendiente máxima (dB/s) que puede tener la propia zona de silencio para seguir
    /// considerándose estable. Un piso de ruido real es plano; una reverberación todavía cae.
    /// Sirve para aceptar colas de hiss, que por definición nunca se hunden bajo un umbral
    /// calculado a partir de ellas mismas y que la prueba de profundidad rechazaría sola.
    /// </summary>
    /// <value>Pendiente máxima admitida, en dB/s. Por defecto, -2.0.</value>
    [Category(DecisionCategory)]
    [DisplayName("Caída máxima del silencio (dB/s)")]
    [Description("Pendiente que puede tener la zona de silencio y seguir considerándose estable. Es la vía por la que se aceptan las colas de hiss, que nunca se hunden.")]
    public double MaxSilenceDecayDbPerSecond { get; set; } = -2.0;

    /// <summary>Ganancia mínima para que merezca la pena reescribir el archivo.</summary>
    /// <value>Ahorro mínimo exigido, en segundos. Por defecto, 0.5.</value>
    [Category(DecisionCategory)]
    [DisplayName("Ahorro mínimo (s)")]
    [Description("Segundos que hay que ganar para que merezca la pena escribir un archivo nuevo.")]
    public double MinSavingsSeconds { get; set; } = 0.5;

    // ------------------------------------------------------------------------------ Fundido

    /// <summary>Tramo previo al corte sobre el que se mide la pendiente de energía.</summary>
    /// <value>Duración del tramo de análisis, en segundos. Por defecto, 2.0.</value>
    [Category(FadeCategory)]
    [DisplayName("Tramo de análisis (s)")]
    [Description("Tramo previo al corte sobre el que se mide la pendiente de energía.")]
    public double FadeAnalysisSeconds { get; set; } = 2.0;

    /// <summary>Pendiente (dB/s) por debajo de la cual se considera que hay un fundido de salida.</summary>
    /// <value>Pendiente umbral, en dB/s. Por defecto, -6.0.</value>
    [Category(FadeCategory)]
    [DisplayName("Pendiente de fundido (dB/s)")]
    [Description("Pendiente por debajo de la cual el decaimiento se considera un fundido de salida.")]
    public double FadeSlopeDbPerSecond { get; set; } = -6.0;

    /// <summary>
    /// R² mínimo del ajuste de esa pendiente. Sin este requisito, el decaimiento irregular del
    /// final de una canción cualquiera basta para disparar la detección de fundido.
    /// </summary>
    /// <value>Bondad de ajuste mínima exigida, entre 0 y 1. Por defecto, 0.5.</value>
    [Category(FadeCategory)]
    [DisplayName("Regularidad exigida (R²)")]
    [Description("Cuánto debe ajustar esa pendiente a una recta, de 0 a 1. Sin este requisito, el final irregular de cualquier canción se clasificaría como fundido.")]
    public double FadeMinimumFitQuality { get; set; } = 0.5;

    /// <summary>Margen extra que se conserva cuando se detecta un fundido de salida.</summary>
    /// <value>Margen adicional en segundos. Por defecto, 0.5.</value>
    [Category(FadeCategory)]
    [DisplayName("Guarda de fundido (s)")]
    [Description("Margen extra que se conserva cuando se detecta un fundido, además de la tolerancia normal.")]
    public double FadeGuardSeconds { get; set; } = 0.5;

    // -------------------------------------------------------------------------------- Corte

    /// <summary>Silencio que se conserva tras el corte (expuesto en la ventana principal).</summary>
    /// <value>Tolerancia en segundos. Por defecto, 0.3.</value>
    [Category(CutCategory)]
    [DisplayName("Tolerancia (s)")]
    [Description("Silencio que se conserva tras el corte. También se ajusta desde la ventana principal.")]
    public double ToleranceSeconds { get; set; } = 0.3;

    /// <summary>
    /// Margen dentro del cual el corte puede retrasarse hasta la frontera de tramas más silenciosa.
    /// Cero desactiva el ajuste.
    /// </summary>
    /// <value>Margen de búsqueda en segundos, o <c>0</c> para desactivarlo. Por defecto, 0.2.</value>
    /// <remarks>
    /// Truncar en seco deja un salto en la forma de onda que se oye como un clic, y es tanto más
    /// audible cuanto más nivel tenga la última muestra conservada. El corte solo se desplaza hacia
    /// adelante, nunca hacia atrás, así que el ajuste jamás se come audio que la tolerancia había
    /// decidido conservar; a cambio renuncia como mucho a este margen de ahorro.
    /// Nota: con <c>-c copy</c> FFmpeg trunca en frontera de trama del códec —unos 26 ms en MP3—,
    /// de modo que la resolución real del ajuste la limita el contenedor. Atenúa el clic, no lo
    /// elimina; eliminarlo exigiría recodificar, que es justo lo que el diseño evita.
    /// </remarks>
    [Category(CutCategory)]
    [DisplayName("Búsqueda del corte (s)")]
    [Description("Margen dentro del cual el corte se retrasa hasta el punto más silencioso, para que el truncado se note menos. Cero lo desactiva.")]
    public double CutSearchSeconds { get; set; } = 0.2;

    // ------------------------------------------------------------------------- Previsualización

    /// <summary>
    /// Tiempo en segundos de la música previa al punto de corte que se reproduce para previsualizar el final recortado.
    /// </summary>
    /// <value>Duración del tramo en segundos. Por defecto, 3.0.</value>
    [Category(PreviewCategory)]
    [DisplayName("Tiempo de previsualización (s)")]
    [Description("Duración de la música previa al corte que se reproduce al pulsar el botón de previsualización para juzgar cómo quedará el final.")]
    public double PreviewSeconds { get; set; } = AudioPreview.ContextSeconds;

    // --------------------------------------------------------------------------------- Inicio

    /// <summary>Si el análisis también busca y recorta el silencio al principio de la pista.</summary>
    /// <value><c>true</c> para analizar y recortar el inicio. Por defecto, <c>true</c>.</value>
    /// <remarks>
    /// Desactivarlo ahorra la decodificación del inicio de cada pista, útil en bibliotecas cuyos
    /// silencios iniciales son intencionados, como los álbumes en directo o sin pausas.
    /// </remarks>
    [Category(LeadingCategory)]
    [DisplayName("Recortar silencio inicial")]
    [Description("Busca también el silencio al principio de la pista y lo recorta conservando la tolerancia antes de que empiece la música.")]
    public bool TrimLeadingSilence { get; set; } = true;

    /// <summary>Silencio inicial mínimo para considerar que el inicio es recortable.</summary>
    /// <value>Duración mínima en segundos. Por defecto, 1.0.</value>
    /// <remarks>
    /// Es menor que <see cref="MinSilenceSeconds"/> porque una entrada muerta es más corta que una
    /// cola: los másters comerciales suelen arrancar con unas décimas de silencio deliberadas, que
    /// este mínimo respeta, mientras que un ripeo o una grabación arrastran segundos enteros.
    /// </remarks>
    [Category(LeadingCategory)]
    [DisplayName("Silencio inicial mínimo (s)")]
    [Description("Silencio que debe haber al principio para que el inicio se considere recortable. El resto de criterios (umbral, profundidad, fundido, ahorro mínimo) son los mismos que para el final.")]
    public double MinLeadingSilenceSeconds { get; set; } = 1.0;

    /// <summary>Nivel asignado a una trama con energía cero.</summary>
    public const double SilenceFloorDbfs = -120.0;

    /// <summary>Crea una copia superficial independiente de esta instancia.</summary>
    /// <returns>
    /// Nueva instancia con los mismos valores; como todas las propiedades son tipos de valor, es
    /// equivalente a una copia profunda.
    /// </returns>
    public SilenceOptions Clone() => (SilenceOptions)MemberwiseClone();

    /// <summary>
    /// Acota cada parámetro a un rango en el que el algoritmo tiene sentido.
    /// </summary>
    /// <remarks>
    /// Los parámetros llegan de un JSON editable a mano y de una rejilla de propiedades sin
    /// validación por celda, así que un valor imposible no es hipotético. Acotarlos en un único
    /// sitio evita que un ajuste absurdo se propague hasta un fallo lejano y difícil de leer, como
    /// una trama de cero muestras o una frecuencia de corte por encima de la de Nyquist.
    /// </remarks>
    public void Normalize()
    {
        ThresholdDbfs = Math.Clamp(ThresholdDbfs, -120.0, 0.0);
        HysteresisDb = Math.Clamp(HysteresisDb, 0.0, 30.0);
        NoiseFloorMarginDb = Math.Clamp(NoiseFloorMarginDb, 0.0, 30.0);
        MaxThresholdDbfs = Math.Clamp(MaxThresholdDbfs, -120.0, 0.0);
        ProgramRelativeFloorDb = Math.Clamp(ProgramRelativeFloorDb, 0.0, 60.0);
        MinThresholdDbfs = Math.Clamp(MinThresholdDbfs, -120.0, 0.0);

        FrameMilliseconds = Math.Clamp(FrameMilliseconds, 5.0, 100.0);
        HighPassHz = HighPassHz <= 0 ? 0.0 : Math.Clamp(HighPassHz, 1.0, 500.0);
        LevelProbeSeconds = Math.Clamp(LevelProbeSeconds, 0.05, 10.0);
        MinContentMilliseconds = Math.Clamp(MinContentMilliseconds, 10.0, 2000.0);

        MinSilenceSeconds = Math.Clamp(MinSilenceSeconds, 0.1, 120.0);
        MinimumSilenceDepthDb = Math.Clamp(MinimumSilenceDepthDb, 0.0, 60.0);
        SilenceDepthProbeSeconds = Math.Clamp(SilenceDepthProbeSeconds, 0.05, 10.0);
        MaxSilenceDecayDbPerSecond = Math.Clamp(MaxSilenceDecayDbPerSecond, -60.0, 0.0);
        MinSavingsSeconds = Math.Clamp(MinSavingsSeconds, 0.0, 60.0);

        FadeAnalysisSeconds = Math.Clamp(FadeAnalysisSeconds, 0.1, 30.0);
        FadeSlopeDbPerSecond = Math.Clamp(FadeSlopeDbPerSecond, -60.0, 0.0);
        FadeMinimumFitQuality = Math.Clamp(FadeMinimumFitQuality, 0.0, 1.0);
        FadeGuardSeconds = Math.Clamp(FadeGuardSeconds, 0.0, 10.0);

        ToleranceSeconds = Math.Clamp(ToleranceSeconds, 0.0, 5.0);
        CutSearchSeconds = Math.Clamp(CutSearchSeconds, 0.0, 5.0);
        PreviewSeconds = Math.Clamp(PreviewSeconds, 0.5, 30.0);

        MinLeadingSilenceSeconds = Math.Clamp(MinLeadingSilenceSeconds, 0.1, 120.0);
    }

    /// <summary>
    /// Comprueba las reglas que relacionan unos parámetros con otros y que ningún acotado por
    /// separado puede garantizar. Devuelve el motivo en español, o <c>null</c> si todo encaja.
    /// </summary>
    /// <returns>
    /// Mensaje en español que describe la primera incoherencia encontrada, o <c>null</c> si todos
    /// los parámetros son mutuamente compatibles.
    /// </returns>
    public string? Validate()
    {
        if (MaxThresholdDbfs < ThresholdDbfs)
        {
            return "El umbral máximo no puede quedar por debajo del umbral base: el piso adaptativo "
                   + "no tendría margen para subir y la detección se comportaría como con umbral fijo.";
        }

        if (MinThresholdDbfs > ThresholdDbfs)
        {
            return "El umbral mínimo no puede quedar por encima del umbral base: el suelo absoluto "
                   + "anularía el valor que se pretende usar como punto de partida.";
        }

        return null;
    }
}
