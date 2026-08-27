using System.Buffers;

namespace EchoCut.Audio;

/// <summary>
/// Posición de cada trama en el tiempo. La rejilla se alinea con el principio del búfer, así que la
/// última trama puede ser más corta o más larga que el resto; todo lo que decide el algoritmo se
/// mide hacia atrás desde la última muestra, de modo que la posición donde aterrizó el salto de
/// FFmpeg nunca influye en el resultado.
/// </summary>
/// <param name="count">Número total de tramas de la curva de nivel.</param>
/// <param name="frameSeconds">Duración de una trama completa, en segundos.</param>
/// <param name="lastFrameSeconds">Duración de la última trama, que puede diferir del resto.</param>
internal readonly struct FrameTimeline(int count, double frameSeconds, double lastFrameSeconds)
{
    /// <value>Número total de tramas de la curva de nivel.</value>
    public int Count { get; } = count;

    /// <value>Duración de una trama completa, en segundos.</value>
    public double FrameSeconds { get; } = frameSeconds;

    /// <value>Duración de la última trama, en segundos.</value>
    public double LastFrameSeconds { get; } = lastFrameSeconds;

    /// <summary>Segundos entre el comienzo de la trama indicada y el final del búfer.</summary>
    /// <param name="frameIndex">Índice de la trama, en orden cronológico.</param>
    /// <returns>
    /// Distancia en segundos hasta el final del búfer, o <c>0</c> si <paramref name="frameIndex"/>
    /// cae fuera de rango.
    /// </returns>
    public double TimeFromEnd(int frameIndex) =>
        frameIndex >= Count ? 0.0 : LastFrameSeconds + ((Count - 1 - frameIndex) * FrameSeconds);

    /// <summary>
    /// Eje temporal creciente para los ajustes por mínimos cuadrados. El origen es arbitrario —una
    /// pendiente no depende de dónde se ponga el cero—, lo que importa es que avance con el índice.
    /// </summary>
    /// <param name="frameIndex">Índice de la trama, en orden cronológico.</param>
    /// <returns>Instante de la trama en el eje temporal creciente usado para los ajustes lineales.</returns>
    public double Time(int frameIndex) => -TimeFromEnd(frameIndex);

    /// <value>Duración total cubierta por la curva de nivel, en segundos.</value>
    public double TotalSeconds => TimeFromEnd(0);
}

/// <summary>
/// Núcleo DSP del análisis de silencio. Es deliberadamente puro: recibe la curva de nivel en memoria
/// y devuelve un resultado, sin tocar archivos, procesos ni interfaz. Esa pureza es lo que
/// permite validarlo con señales sintéticas generadas en memoria.
/// </summary>
public static class SilenceDetector
{
    /// <summary>
    /// Analiza la cola de una pista a partir del PCM completo. Es la entrada cómoda para las
    /// pruebas; el programa usa <see cref="AnalyzeFrames"/> alimentando un <see cref="LevelFramer"/>
    /// desde el decodificador, sin llegar a tener nunca todas las muestras en memoria.
    /// </summary>
    /// <param name="samples">PCM mono en punto flotante. Puede ser solo el final del archivo.</param>
    /// <param name="sampleRate">Frecuencia de muestreo de <paramref name="samples"/>.</param>
    /// <param name="totalDurationSeconds">Duración completa del archivo.</param>
    /// <param name="options">Parámetros del algoritmo de detección.</param>
    /// <returns>Resultado del análisis, incluida la decisión de recorte y las métricas que la justifican.</returns>
    /// <exception cref="ArgumentNullException">Se lanza si <paramref name="options"/> es <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se lanza si <paramref name="sampleRate"/> es cero o negativo.</exception>
    public static SilenceResult Analyze(
        ReadOnlySpan<float> samples,
        int sampleRate,
        double totalDurationSeconds,
        SilenceOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sampleRate);

        int expectedFrames = (int)(samples.Length / Math.Max(1.0, sampleRate * options.FrameMilliseconds / 1000.0)) + 1;

        using LevelFramer framer = new(sampleRate, options.FrameMilliseconds, expectedFrames, options.HighPassHz);
        framer.Write(samples);
        framer.Complete();

        return AnalyzeFrames(
            framer.Levels,
            framer.FrameSeconds,
            framer.LastFrameSeconds,
            framer.PeakDbfs,
            totalDurationSeconds,
            options);
    }

    /// <summary>
    /// Analiza la curva de nivel por trama producida por <see cref="LevelFramer"/>.
    /// </summary>
    /// <param name="frameDb">Nivel RMS de cada trama, en dBFS, en orden cronológico.</param>
    /// <param name="frameSeconds">Duración de una trama completa.</param>
    /// <param name="lastFrameSeconds">Duración de la última trama, que puede diferir del resto.</param>
    /// <param name="peakDbfs">Pico absoluto medido sobre las muestras, solo para diagnóstico.</param>
    /// <param name="totalDurationSeconds">
    /// Duración completa del archivo. El silencio se mide hacia atrás desde la última trama, así que
    /// basta con este dato para situar el corte dentro del archivo entero.
    /// </param>
    /// <param name="options">Parámetros del algoritmo de detección.</param>
    /// <returns>Resultado del análisis, incluida la decisión de recorte y las métricas que la justifican.</returns>
    /// <exception cref="ArgumentNullException">Se lanza si <paramref name="options"/> es <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se lanza si <paramref name="frameSeconds"/> es cero o negativo.</exception>
    public static SilenceResult AnalyzeFrames(
        ReadOnlySpan<double> frameDb,
        double frameSeconds,
        double lastFrameSeconds,
        double peakDbfs,
        double totalDurationSeconds,
        SilenceOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(frameSeconds);

        int frameCount = frameDb.Length;
        if (frameCount == 0)
        {
            return NoSilence(totalDurationSeconds, options, SilenceOptions.SilenceFloorDbfs, 0, 0);
        }

        FrameTimeline timeline = new(frameCount, frameSeconds, lastFrameSeconds);

        (double noiseFloorDbfs, double programLevelDbfs) = LevelStatistics(frameDb, frameSeconds, options);
        double threshold = EffectiveThreshold(options, noiseFloorDbfs, programLevelDbfs);
        double musicThreshold = threshold + options.HysteresisDb;

        int lastContentFrame = FindLastContentFrame(frameDb, musicThreshold, options, frameSeconds);

        if (lastContentFrame < 0)
        {
            // Ninguna trama del búfer contiene música: la ventana de sondeo se quedó corta.
            return new SilenceResult
            {
                TrailingSilenceSeconds = timeline.TotalSeconds,
                CutSeconds = totalDurationSeconds,
                SavedSeconds = 0,
                ShouldTrim = false,
                EntireBufferSilent = true,
                EffectiveThresholdDbfs = threshold,
                NoiseFloorDbfs = noiseFloorDbfs,
                ProgramLevelDbfs = programLevelDbfs,
                PeakDbfs = peakDbfs,
                AnalyzedSeconds = timeline.TotalSeconds,
                FrameCount = frameCount,
            };
        }

        // Histéresis: el contenido se confirma con el umbral alto, pero el silencio no empieza
        // hasta que la energía cae por debajo del umbral bajo. Las tramas intermedias son
        // decaimiento (reverberación o final de fundido) y se conservan siempre.
        int silenceStart = lastContentFrame + 1;
        while (silenceStart < frameCount && frameDb[silenceStart] >= threshold)
        {
            silenceStart++;
        }

        double trailingSilenceSeconds = timeline.TimeFromEnd(silenceStart);
        (double slope, double fitQuality) = TailSlope(frameDb, silenceStart, threshold, timeline, options);

        // Un fundido de salida no es solo "la energía baja": es una bajada sostenida y regular.
        // Exigir también un ajuste lineal bueno descarta el decaimiento irregular con el que
        // termina cualquier canción, que de otro modo se clasificaría como fundido casi siempre.
        bool fadeDetected = slope < options.FadeSlopeDbPerSecond
                            && fitQuality >= options.FadeMinimumFitQuality;

        // El silencio residual toca fondo en el piso del medio; una cola de reverberación sigue
        // cayendo y el archivo acaba a media caída. Comparar el nivel del tramo final contra el
        // umbral separa ambos casos, cosa que la duración del silencio por sí sola no consigue.
        double tailFloorDbfs = TailFloor(frameDb, frameSeconds, options);
        double silenceSlope = RegionSlope(frameDb, silenceStart, frameCount - silenceStart, timeline);

        // Basta con una de las dos evidencias. Un silencio digital se hunde decenas de dB bajo el
        // umbral; una cola de hiss no se hunde nada —el umbral se calculó sobre ella— pero es
        // plana. Solo se rechaza lo que ni se hunde ni se estabiliza: reverberación en curso.
        bool reachesSilenceFloor = tailFloorDbfs <= threshold - options.MinimumSilenceDepthDb
                                   || silenceSlope >= options.MaxSilenceDecayDbPerSecond;

        double keptSeconds = options.ToleranceSeconds + (fadeDetected ? options.FadeGuardSeconds : 0.0);
        double cutTimeFromEnd = RefineCut(
            frameDb,
            timeline,
            Math.Max(0.0, trailingSilenceSeconds - keptSeconds),
            options);

        double cutSeconds = Math.Clamp(totalDurationSeconds - cutTimeFromEnd, 0.0, totalDurationSeconds);
        double savedSeconds = totalDurationSeconds - cutSeconds;

        return new SilenceResult
        {
            TrailingSilenceSeconds = trailingSilenceSeconds,
            CutSeconds = cutSeconds,
            SavedSeconds = savedSeconds,
            ShouldTrim = trailingSilenceSeconds >= options.MinSilenceSeconds
                         && savedSeconds >= options.MinSavingsSeconds
                         && reachesSilenceFloor,
            EntireBufferSilent = false,
            FadeDetected = fadeDetected,
            EffectiveThresholdDbfs = threshold,
            NoiseFloorDbfs = noiseFloorDbfs,
            ProgramLevelDbfs = programLevelDbfs,
            TailFloorDbfs = tailFloorDbfs,
            ReachesSilenceFloor = reachesSilenceFloor,
            SilenceSlopeDbPerSecond = silenceSlope,
            PeakDbfs = peakDbfs,
            TailSlopeDbPerSecond = slope,
            TailFitQuality = fitQuality,
            AnalyzedSeconds = timeline.TotalSeconds,
            FrameCount = frameCount,
        };
    }

    /// <summary>
    /// Combina el umbral fijo con el piso de ruido medido. El fallo típico de un umbral fijo no es
    /// recortar de más en pistas limpias, sino no detectar nada en grabaciones con hiss audible:
    /// con ruido de fondo a −45 dBFS, un umbral de −50 dBFS jamás llega a cruzarse.
    /// </summary>
    /// <param name="options">Parámetros del algoritmo de detección.</param>
    /// <param name="noiseFloorDbfs">Piso de ruido estimado, en dBFS.</param>
    /// <param name="programLevelDbfs">Nivel de programa estimado, en dBFS.</param>
    /// <returns>Umbral efectivo, en dBFS, tras aplicar el piso adaptativo y los tres topes de seguridad.</returns>
    internal static double EffectiveThreshold(
        SilenceOptions options,
        double noiseFloorDbfs,
        double programLevelDbfs)
    {
        double threshold = Math.Max(options.ThresholdDbfs, noiseFloorDbfs + options.NoiseFloorMarginDb);
        threshold = Math.Min(threshold, options.MaxThresholdDbfs);
        threshold = Math.Min(threshold, programLevelDbfs - options.ProgramRelativeFloorDb);
        return Math.Max(threshold, options.MinThresholdDbfs);
    }

    /// <summary>
    /// Recorre la curva de nivel con una media deslizante y devuelve su mínimo —el pasaje más
    /// silencioso, que es el piso de ruido— y su máximo —el pasaje más sonoro, que es el nivel de
    /// programa—. Una sola pasada da las dos referencias que necesita el umbral, sin ordenar ni
    /// copiar nada.
    /// </summary>
    /// <param name="frameDb">Nivel RMS de cada trama, en dBFS, en orden cronológico.</param>
    /// <param name="frameSeconds">Duración de una trama completa, en segundos.</param>
    /// <param name="options">Parámetros del algoritmo de detección.</param>
    /// <returns>Piso de ruido y nivel de programa estimados, ambos en dBFS.</returns>
    private static (double NoiseFloorDbfs, double ProgramLevelDbfs) LevelStatistics(
        ReadOnlySpan<double> frameDb,
        double frameSeconds,
        SilenceOptions options)
    {
        int window = Math.Clamp(
            (int)Math.Round(options.LevelProbeSeconds / frameSeconds),
            1,
            frameDb.Length);

        double sum = 0;
        for (int i = 0; i < window; i++)
        {
            sum += frameDb[i];
        }

        double minimum = sum / window;
        double maximum = minimum;

        for (int i = window; i < frameDb.Length; i++)
        {
            sum += frameDb[i] - frameDb[i - window];
            double mean = sum / window;

            if (mean < minimum)
            {
                minimum = mean;
            }

            if (mean > maximum)
            {
                maximum = mean;
            }
        }

        return (minimum, maximum);
    }

    /// <summary>
    /// Localiza la última trama con contenido musical recorriendo el búfer hacia atrás.
    /// Una racha de silencio solo se rompe cuando al menos la mitad de las tramas de la ventana
    /// de confirmación superan el umbral alto, de modo que un clic o un pop aislado no la aborta.
    /// </summary>
    /// <remarks>
    /// El recuento se lleva de forma incremental: al retroceder una posición, la ventana pierde una
    /// trama por la derecha y gana otra por la izquierda. Recontarla entera en cada paso multiplica
    /// el trabajo por su tamaño sin cambiar el resultado.
    /// </remarks>
    /// <param name="frameDb">Nivel RMS de cada trama, en dBFS, en orden cronológico.</param>
    /// <param name="musicThreshold">Umbral alto del disparador de Schmitt, en dBFS.</param>
    /// <param name="options">Parámetros del algoritmo de detección.</param>
    /// <param name="frameSeconds">Duración de una trama completa, en segundos.</param>
    /// <returns>
    /// Índice de la última trama con contenido musical confirmado, o <c>-1</c> si ninguna trama del
    /// búfer lo tiene.
    /// </returns>
    private static int FindLastContentFrame(
        ReadOnlySpan<double> frameDb,
        double musicThreshold,
        SilenceOptions options,
        double frameSeconds)
    {
        int windowFrames = Math.Max(
            1,
            (int)Math.Round(options.MinContentMilliseconds / 1000.0 / frameSeconds));

        int count = frameDb.Length;
        int hits = 0;
        for (int j = Math.Max(0, count - windowFrames); j < count; j++)
        {
            if (frameDb[j] > musicThreshold)
            {
                hits++;
            }
        }

        for (int i = count - 1; i >= 0; i--)
        {
            int length = i - Math.Max(0, i - windowFrames + 1) + 1;
            if (hits >= (length + 1) / 2)
            {
                return i;
            }

            if (frameDb[i] > musicThreshold)
            {
                hits--;
            }

            int incoming = i - windowFrames;
            if (incoming >= 0 && frameDb[incoming] > musicThreshold)
            {
                hits++;
            }
        }

        return -1;
    }

    /// <summary>
    /// Ajusta por mínimos cuadrados la pendiente de energía (dB/s) del contenido previo al corte.
    /// Un decaimiento monótono pronunciado delata un fundido de salida o una cola de reverberación.
    /// </summary>
    /// <remarks>
    /// Solo entran en el ajuste las tramas que superan el umbral. Si se incluyeran las tramas ya
    /// silenciosas, el escalón que separa la música del silencio daría una pendiente enorme y
    /// cualquier final abrupto se clasificaría como fundido.
    /// </remarks>
    /// <param name="frameDb">Nivel RMS de cada trama, en dBFS, en orden cronológico.</param>
    /// <param name="silenceStart">Índice de la primera trama ya clasificada como silencio.</param>
    /// <param name="threshold">Umbral efectivo, en dBFS, que separa contenido de silencio.</param>
    /// <param name="timeline">Escala temporal de la curva de nivel.</param>
    /// <param name="options">Parámetros del algoritmo de detección.</param>
    /// <returns>
    /// Pendiente en dB/s y bondad del ajuste (R², entre 0 y 1). Ambos valen <c>0</c> si no hay tramas
    /// suficientes o el ajuste es numéricamente degenerado.
    /// </returns>
    private static (double Slope, double RSquared) TailSlope(
        ReadOnlySpan<double> frameDb,
        int silenceStart,
        double threshold,
        FrameTimeline timeline,
        SilenceOptions options)
    {
        int windowFrames = Math.Max(2, (int)Math.Round(options.FadeAnalysisSeconds / timeline.FrameSeconds));

        int count = 0;
        double sumX = 0, sumY = 0, sumXy = 0, sumXx = 0, sumYy = 0;
        for (int i = silenceStart - 1; i >= 0 && count < windowFrames; i--)
        {
            if (frameDb[i] < threshold)
            {
                continue;
            }

            double x = timeline.Time(i);
            double y = frameDb[i];
            sumX += x;
            sumY += y;
            sumXy += x * y;
            sumXx += x * x;
            sumYy += y * y;
            count++;
        }

        if (count < 2)
        {
            return (0.0, 0.0);
        }

        double denominator = (count * sumXx) - (sumX * sumX);
        if (Math.Abs(denominator) < 1e-12)
        {
            return (0.0, 0.0);
        }

        double covariance = (count * sumXy) - (sumX * sumY);
        double slope = covariance / denominator;

        double spreadY = (count * sumYy) - (sumY * sumY);
        double rSquared = spreadY <= 1e-12
            ? 0.0
            : Math.Clamp(covariance * covariance / (denominator * spreadY), 0.0, 1.0);

        return (slope, rSquared);
    }

    /// <summary>Pendiente por mínimos cuadrados (dB/s) de un rango de tramas.</summary>
    /// <param name="frameDb">Nivel RMS de cada trama, en dBFS, en orden cronológico.</param>
    /// <param name="start">Índice de la primera trama del rango.</param>
    /// <param name="count">Número de tramas del rango, a partir de <paramref name="start"/>.</param>
    /// <param name="timeline">Escala temporal de la curva de nivel.</param>
    /// <returns>Pendiente en dB/s del rango, o <c>0</c> si tiene menos de dos tramas.</returns>
    private static double RegionSlope(
        ReadOnlySpan<double> frameDb,
        int start,
        int count,
        FrameTimeline timeline)
    {
        if (count < 2)
        {
            return 0.0;
        }

        double sumX = 0, sumY = 0, sumXy = 0, sumXx = 0;
        for (int i = 0; i < count; i++)
        {
            double x = timeline.Time(start + i);
            double y = frameDb[start + i];
            sumX += x;
            sumY += y;
            sumXy += x * y;
            sumXx += x * x;
        }

        double denominator = (count * sumXx) - (sumX * sumX);
        return Math.Abs(denominator) < 1e-12 ? 0.0 : ((count * sumXy) - (sumX * sumY)) / denominator;
    }

    /// <summary>
    /// Retrasa el corte hasta la frontera de tramas más silenciosa que quepa dentro del margen de
    /// búsqueda, para que la última muestra conservada tenga el menor nivel posible y el truncado
    /// se note lo menos posible.
    /// </summary>
    /// <remarks>
    /// Recoge la idea del <c>ToDo</c> de <c>TruncSilenceBase.h</c> de Audacity sobre usar cruces por
    /// cero para mejorar el empalme. Aquí no se puede hacer un fundido —el recorte es una copia de
    /// flujo sin recodificar— pero sí elegir dónde cortar, que es la mitad del problema.
    /// Los empates conservan el candidato más temprano: si nada mejora, el corte no se mueve y no se
    /// regala ahorro.
    /// </remarks>
    /// <param name="frameDb">Nivel RMS de cada trama, en dBFS, en orden cronológico.</param>
    /// <param name="timeline">Escala temporal de la curva de nivel.</param>
    /// <param name="cutTimeFromEnd">Distancia del corte al final del archivo, en segundos.</param>
    /// <param name="options">Parámetros del algoritmo de detección.</param>
    /// <returns>
    /// Distancia al final del archivo, en segundos, del corte ya afinado. Nunca es menor que
    /// <paramref name="cutTimeFromEnd"/> menos <see cref="SilenceOptions.CutSearchSeconds"/>.
    /// </returns>
    private static double RefineCut(
        ReadOnlySpan<double> frameDb,
        FrameTimeline timeline,
        double cutTimeFromEnd,
        SilenceOptions options)
    {
        if (options.CutSearchSeconds <= 0 || cutTimeFromEnd <= 0 || timeline.Count < 2)
        {
            return cutTimeFromEnd;
        }

        // Primera frontera que no cae antes del corte. Las fronteras se recorren en orden
        // cronológico, así que su distancia al final decrece según avanza el índice. Se arranca una
        // antes y se filtra en el bucle, porque el redondeo del cociente puede dejar fuera
        // justamente la frontera que coincide con el corte.
        int first = timeline.Count - 2
                    - (int)Math.Floor((cutTimeFromEnd - timeline.LastFrameSeconds) / timeline.FrameSeconds);
        first = Math.Clamp(first, 1, timeline.Count - 1);

        // El instante objetivo sale de restar la tolerancia a un tiempo que ya era múltiplo de la
        // trama, y esa resta lo deja a unos 10⁻¹⁶ s por debajo de una frontera. Sin este margen, la
        // frontera exacta se descarta por "anterior al corte" y el ajuste salta a la siguiente,
        // desplazando el corte una trama entera por un error de redondeo.
        double epsilon = timeline.FrameSeconds * 1e-6;
        double limit = Math.Max(0.0, cutTimeFromEnd - options.CutSearchSeconds);
        double best = cutTimeFromEnd;
        double bestLevel = double.PositiveInfinity;

        for (int frame = first; frame < timeline.Count; frame++)
        {
            double time = timeline.TimeFromEnd(frame);
            if (time > cutTimeFromEnd + epsilon)
            {
                continue;
            }

            if (time < limit)
            {
                break;
            }

            // La trama anterior a la frontera es la última que sobrevive al recorte: su nivel es el
            // que determina el salto en la forma de onda.
            double level = frameDb[frame - 1];
            if (level < bestLevel)
            {
                bestLevel = level;
                best = time;
            }
        }

        return best;
    }

    /// <summary>
    /// Nivel representativo del tramo final del archivo. Se usa la mediana y no la media para que
    /// un clic o un pop en los últimos milisegundos no eleve artificialmente el fondo medido.
    /// </summary>
    /// <param name="frameDb">Nivel RMS de cada trama, en dBFS, en orden cronológico.</param>
    /// <param name="frameSeconds">Duración de una trama completa, en segundos.</param>
    /// <param name="options">Parámetros del algoritmo de detección.</param>
    /// <returns>Nivel mediano del tramo final analizado, en dBFS.</returns>
    private static double TailFloor(ReadOnlySpan<double> frameDb, double frameSeconds, SilenceOptions options)
    {
        int probeFrames = Math.Clamp(
            (int)Math.Round(options.SilenceDepthProbeSeconds / frameSeconds),
            1,
            frameDb.Length);

        double[] rented = ArrayPool<double>.Shared.Rent(probeFrames);
        try
        {
            Span<double> tail = rented.AsSpan(0, probeFrames);
            frameDb[^probeFrames..].CopyTo(tail);
            tail.Sort();
            return tail[probeFrames / 2];
        }
        finally
        {
            ArrayPool<double>.Shared.Return(rented);
        }
    }

    /// <summary>Convierte una potencia media (el cuadrado de una señal RMS) a dBFS.</summary>
    /// <param name="meanSquare">Media de los cuadrados de las muestras de la trama.</param>
    /// <returns>Nivel en dBFS, acotado por abajo en <see cref="SilenceOptions.SilenceFloorDbfs"/>.</returns>
    internal static double PowerToDbfs(double meanSquare) =>
        meanSquare <= 0 ? SilenceOptions.SilenceFloorDbfs
                        : Math.Max(SilenceOptions.SilenceFloorDbfs, 10.0 * Math.Log10(meanSquare));

    /// <summary>Convierte una amplitud de pico a dBFS.</summary>
    /// <param name="amplitude">Amplitud de pico, en el rango habitual de PCM normalizado.</param>
    /// <returns>Nivel en dBFS, acotado por abajo en <see cref="SilenceOptions.SilenceFloorDbfs"/>.</returns>
    internal static double AmplitudeToDbfs(double amplitude) =>
        amplitude <= 0 ? SilenceOptions.SilenceFloorDbfs
                       : Math.Max(SilenceOptions.SilenceFloorDbfs, 20.0 * Math.Log10(amplitude));

    /// <summary>Construye el resultado por defecto para un búfer sin tramas que analizar.</summary>
    /// <param name="totalDurationSeconds">Duración completa del archivo.</param>
    /// <param name="options">Parámetros del algoritmo de detección.</param>
    /// <param name="noiseFloorDbfs">Piso de ruido a reportar en el resultado.</param>
    /// <param name="analyzedSeconds">Segundos de audio efectivamente analizados.</param>
    /// <param name="frameCount">Número de tramas analizadas.</param>
    /// <returns>Resultado que declara que no hay silencio recortable.</returns>
    private static SilenceResult NoSilence(
        double totalDurationSeconds,
        SilenceOptions options,
        double noiseFloorDbfs,
        double analyzedSeconds,
        int frameCount) => new()
        {
            TrailingSilenceSeconds = 0,
            CutSeconds = totalDurationSeconds,
            SavedSeconds = 0,
            ShouldTrim = false,
            EffectiveThresholdDbfs = options.ThresholdDbfs,
            NoiseFloorDbfs = noiseFloorDbfs,
            ProgramLevelDbfs = SilenceOptions.SilenceFloorDbfs,
            PeakDbfs = SilenceOptions.SilenceFloorDbfs,
            AnalyzedSeconds = analyzedSeconds,
            FrameCount = frameCount,
        };
}
