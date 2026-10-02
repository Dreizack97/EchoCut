namespace EchoCut.Audio;

/// <summary>
/// Resultado del análisis de uno de los bordes de una pista —su final o su principio—. Incluye
/// tanto la decisión de recorte como las métricas que la justifican, para poder auditar el
/// algoritmo desde el CSV.
/// </summary>
/// <remarks>
/// Es el mismo tipo para ambos bordes porque el principio se analiza como el final de la curva de
/// nivel invertida en el tiempo (ver <see cref="SilenceDetector.AnalyzeLeadingFrames"/>). Las
/// métricas de pendiente y fondo se refieren a esa curva vista desde el borde hacia la música.
/// </remarks>
public sealed record SilenceResult
{
    /// <summary>Silencio detectado en el borde analizado, en segundos.</summary>
    /// <value>Duración del silencio entre el borde del archivo y la música.</value>
    public double SilenceSeconds { get; init; }

    /// <summary>Instante de corte absoluto dentro del archivo, en segundos.</summary>
    /// <value>
    /// Segundo del archivo en el que se produciría el corte: el final de la copia si se analizó el
    /// final, o su comienzo si se analizó el principio.
    /// </value>
    public double CutSeconds { get; init; }

    /// <summary>Segundos que se eliminarían en este borde al recortar.</summary>
    /// <value>Duración entre el corte y el borde del archivo, en segundos.</value>
    public double SavedSeconds { get; init; }

    /// <summary>True si el silencio supera el mínimo y la ganancia justifica reescribir el archivo.</summary>
    /// <value><c>true</c> si procede recortar el archivo; <c>false</c> en caso contrario.</value>
    public bool ShouldTrim { get; init; }

    /// <summary>
    /// True si todas las tramas analizadas resultaron silenciosas: la ventana de sondeo se
    /// quedó corta y el llamador debe ampliarla antes de dar el resultado por bueno.
    /// </summary>
    /// <value><c>true</c> si la ventana analizada era enteramente silencio.</value>
    public bool EntireBufferSilent { get; init; }

    /// <summary>True si se detectó un fundido de salida y se aplicó el margen de guarda.</summary>
    /// <value><c>true</c> si el tramo previo al corte se clasificó como fundido.</value>
    public bool FadeDetected { get; init; }

    /// <summary>Umbral realmente aplicado tras combinar el valor fijo con el piso adaptativo.</summary>
    /// <value>Umbral efectivo, en dBFS.</value>
    public double EffectiveThresholdDbfs { get; init; }

    /// <summary>Piso de ruido estimado: nivel del pasaje más silencioso de la ventana analizada.</summary>
    /// <value>Piso de ruido, en dBFS.</value>
    public double NoiseFloorDbfs { get; init; }

    /// <summary>
    /// Nivel de programa: nivel del pasaje más sonoro de la ventana analizada. Es la referencia
    /// contra la que se acota el umbral para no comerse la música de un máster silencioso.
    /// </summary>
    /// <value>Nivel de programa, en dBFS.</value>
    public double ProgramLevelDbfs { get; init; }

    /// <summary>
    /// Nivel medio del tramo final del archivo. Si no queda muy por debajo del umbral, la cola
    /// todavía está decayendo y no es silencio residual sino reverberación.
    /// </summary>
    /// <value>Nivel mediano del tramo final analizado, en dBFS.</value>
    public double TailFloorDbfs { get; init; }

    /// <summary>True si el final del archivo toca fondo lo bastante como para ser silencio real.</summary>
    /// <value><c>true</c> si la cola se hunde lo suficiente o se mantiene estable.</value>
    public bool ReachesSilenceFloor { get; init; }

    /// <summary>Pendiente (dB/s) dentro de la propia zona de silencio. Cerca de cero = piso estable.</summary>
    /// <value>Pendiente de la zona de silencio, en dB/s.</value>
    public double SilenceSlopeDbPerSecond { get; init; }

    /// <summary>Pico de muestra de la porción analizada.</summary>
    /// <value>Nivel de pico, en dBFS.</value>
    public double PeakDbfs { get; init; }

    /// <summary>Pendiente de energía medida justo antes del corte, en dB/s.</summary>
    /// <value>Pendiente del tramo previo al corte, en dB/s.</value>
    public double TailSlopeDbPerSecond { get; init; }

    /// <summary>
    /// Bondad del ajuste lineal (R²) de esa pendiente, entre 0 y 1. Un fundido real decae de forma
    /// regular y ajusta bien; el final natural de una canción decae de forma irregular y ajusta mal.
    /// </summary>
    /// <value>Coeficiente de determinación del ajuste, entre 0 y 1.</value>
    public double TailFitQuality { get; init; }

    /// <summary>Segundos de audio efectivamente decodificados y analizados.</summary>
    /// <value>Duración analizada, en segundos.</value>
    public double AnalyzedSeconds { get; init; }

    /// <summary>Número de tramas evaluadas.</summary>
    /// <value>Cantidad de tramas de la curva de nivel analizada.</value>
    public int FrameCount { get; init; }
}
