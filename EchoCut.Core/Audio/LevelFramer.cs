using System.Buffers;

namespace EchoCut.Audio;

/// <summary>
/// Convierte un flujo de muestras en la curva de nivel por trama que consume
/// <see cref="SilenceDetector.AnalyzeFrames"/>. Es el único punto del programa que ve el PCM.
/// </summary>
/// <remarks>
/// <para>
/// Trabajar en streaming no es una optimización cosmética: materializar la cola entera son 2,6 MB
/// por archivo con la ventana de 30 s y hasta 21 MB con la ampliada, todos ellos por encima del
/// umbral del montón de objetos grandes y multiplicados por el grado de paralelismo del lote.
/// Reducido a la curva de nivel, lo que queda vivo son decenas de kilobytes alquilados de
/// <see cref="ArrayPool{T}"/>.
/// </para>
/// <para>
/// El bucle es escalar a propósito. El filtro es recursivo y sus muestras dependen unas de otras,
/// así que no se puede vectorizar; una vez pagado ese coste, hacer el cuadrado y el máximo con
/// <c>Vector&lt;float&gt;</c> en una pasada aparte obligaría a escribir y releer un búfer intermedio
/// y saldría más caro que fundirlo todo en un recorrido único. En cualquier caso el orden de
/// magnitud es de milisegundos frente a las decenas o centenas que tarda FFmpeg en decodificar.
/// </para>
/// </remarks>
public sealed class LevelFramer : ISampleSink, IDisposable
{
    private readonly int _sampleRate;
    private readonly int _frameSize;
    private readonly Biquad? _highPass;

    private double[] _levels;
    private int _count;

    private double _sumSquares;
    private int _filled;
    private double _lastFullFrameSumSquares;
    private double _peak;
    private bool _primed;
    private bool _completed;
    private double _lastFrameSeconds;

    /// <summary>Crea el framer con el tamaño de trama y el filtrado indicados.</summary>
    /// <param name="sampleRate">Frecuencia de muestreo de las muestras que se van a escribir, en Hz.</param>
    /// <param name="frameMilliseconds">Duración de cada trama de análisis, en milisegundos.</param>
    /// <param name="expectedFrames">
    /// Tramas previstas. Solo dimensiona el búfer inicial: <c>-sseof</c> puede entregar más o menos
    /// audio del pedido, y el búfer crece solo si hace falta.
    /// </param>
    /// <param name="highPassHz">Corte del paso alto previo a la medida. Cero o menos lo desactiva.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Se lanza si <paramref name="sampleRate"/> o <paramref name="frameMilliseconds"/> son cero o
    /// negativos.
    /// </exception>
    public LevelFramer(int sampleRate, double frameMilliseconds, int expectedFrames, double highPassHz)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sampleRate);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(frameMilliseconds);

        _sampleRate = sampleRate;
        _frameSize = Math.Max(1, (int)Math.Round(sampleRate * frameMilliseconds / 1000.0));
        _highPass = highPassHz > 0 ? Biquad.HighPass(sampleRate, highPassHz) : null;
        _levels = ArrayPool<double>.Shared.Rent(Math.Clamp(expectedFrames, 64, 1 << 20));
        _lastFrameSeconds = _frameSize / (double)_sampleRate;
    }

    /// <summary>Duración de una trama completa, en segundos.</summary>
    /// <value>Cociente entre el tamaño de trama en muestras y la frecuencia de muestreo.</value>
    public double FrameSeconds => _frameSize / (double)_sampleRate;

    /// <summary>
    /// Duración de la última trama. La rejilla se alinea con el principio del búfer, así que el
    /// resto de la división cae al final; publicarlo permite a quien analiza seguir midiendo los
    /// tiempos hacia atrás desde la última muestra, que es lo que hace que el resultado no dependa
    /// de dónde aterrizara el salto de FFmpeg.
    /// </summary>
    public double LastFrameSeconds => _lastFrameSeconds;

    /// <summary>Pico absoluto de la señal <em>sin filtrar</em>: es una métrica de diagnóstico.</summary>
    /// <value>Pico expresado en dBFS, calculado sobre las muestras crudas antes del paso alto.</value>
    public double PeakDbfs => SilenceDetector.AmplitudeToDbfs(_peak);

    /// <summary>Curva de nivel acumulada hasta el momento, una entrada por trama completa.</summary>
    /// <value>Vista de solo lectura sobre el búfer interno; su contenido es válido hasta la próxima escritura.</value>
    public ReadOnlySpan<double> Levels => _levels.AsSpan(0, _count);

    /// <summary>Duración total de audio ya reducido a tramas.</summary>
    /// <value>Suma de la duración de todas las tramas completas más la de la última trama, si la hay.</value>
    public double AnalyzedSeconds =>
        _count == 0 ? 0.0 : ((_count - 1) * FrameSeconds) + _lastFrameSeconds;

    /// <inheritdoc/>
    /// <exception cref="ObjectDisposedException">Se lanza si el framer ya se liberó con <see cref="Dispose"/>.</exception>
    public void Write(ReadOnlySpan<float> samples)
    {
        ObjectDisposedException.ThrowIf(_levels.Length == 0, this);

        if (samples.IsEmpty)
        {
            return;
        }

        if (!_primed)
        {
            _primed = true;
            _highPass?.Prime(samples[0]);
        }

        Biquad? highPass = _highPass;
        int frameSize = _frameSize;
        double sum = _sumSquares;
        int filled = _filled;
        double peak = _peak;

        for (int i = 0; i < samples.Length; i++)
        {
            float raw = samples[i];
            double magnitude = Math.Abs(raw);
            if (magnitude > peak)
            {
                peak = magnitude;
            }

            double value = highPass is null ? raw : highPass.ProcessOne(raw);
            sum += value * value;

            if (++filled == frameSize)
            {
                Append(SilenceDetector.PowerToDbfs(sum / frameSize));
                _lastFullFrameSumSquares = sum;
                sum = 0.0;
                filled = 0;
            }
        }

        _sumSquares = sum;
        _filled = filled;
        _peak = peak;
    }

    /// <summary>Cierra la curva resolviendo el resto que no llenó una trama completa.</summary>
    public void Complete()
    {
        if (_completed)
        {
            return;
        }

        _completed = true;
        _lastFrameSeconds = _frameSize / (double)_sampleRate;

        if (_filled == 0)
        {
            return;
        }

        // Un resto de dos o tres muestras mide una energía que es puro ruido de medida, y además
        // dejaría la última trama —la más determinante, porque es la que toca el final del archivo—
        // como la menos fiable de todas. Por debajo de un cuarto de trama se funde con la anterior;
        // por encima se emite como trama corta con su duración real.
        if (_count > 0 && _filled < _frameSize / 4)
        {
            int length = _frameSize + _filled;
            _levels[_count - 1] = SilenceDetector.PowerToDbfs((_lastFullFrameSumSquares + _sumSquares) / length);
            _lastFrameSeconds = length / (double)_sampleRate;
        }
        else if (_filled >= _frameSize / 4)
        {
            Append(SilenceDetector.PowerToDbfs(_sumSquares / _filled));
            _lastFrameSeconds = _filled / (double)_sampleRate;
        }
    }

    /// <summary>Devuelve el búfer de niveles al <see cref="ArrayPool{T}"/> compartido.</summary>
    public void Dispose()
    {
        if (_levels.Length > 0)
        {
            ArrayPool<double>.Shared.Return(_levels);
            _levels = [];
        }
    }

    /// <summary>Añade un nivel a la curva, ampliando el búfer alquilado si no queda espacio.</summary>
    /// <param name="level">Nivel de la trama, en dBFS.</param>
    private void Append(double level)
    {
        if (_count == _levels.Length)
        {
            double[] bigger = ArrayPool<double>.Shared.Rent(_levels.Length * 2);
            _levels.AsSpan(0, _count).CopyTo(bigger);
            ArrayPool<double>.Shared.Return(_levels);
            _levels = bigger;
        }

        _levels[_count++] = level;
    }
}
