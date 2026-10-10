using EchoCut.Audio;

namespace EchoCut.Loudness;

/// <summary>Sonoridad medida de una pista según ReplayGain 1.0.</summary>
/// <param name="GainDb">
/// Ganancia que llevaría la pista a <see cref="ReplayGainAnalyzer.ReferenceLevelDb"/>, en dB:
/// positiva si suena más baja que la referencia y negativa si suena más alta.
/// </param>
/// <param name="Peak">
/// Pico absoluto de las muestras decodificadas, en escala completa (1.0 = 0 dBFS). Un MP3
/// decodificado en coma flotante puede pasar de 1.0: la saturación ya venía codificada.
/// </param>
public sealed record ReplayGainResult(double GainDb, double Peak)
{
    /// <summary>Nivel percibido de la pista en la escala de ReplayGain.</summary>
    /// <value>Decibelios SPL equivalentes; 89 dB es la referencia.</value>
    public double LevelDb => ReplayGainAnalyzer.ReferenceLevelDb - GainDb;

    /// <summary>La medida que tendría la pista tras cambiar su <c>global_gain</c>.</summary>
    /// <param name="steps">Pasos de 1.5 dB aplicados.</param>
    /// <returns>La medida desplazada, sin volver a decodificar.</returns>
    /// <remarks>
    /// Es exacta, no una estimación: el cambio escala todas las muestras por el mismo factor
    /// 2^(pasos/4), así que el percentil se desplaza en dB y el pico se multiplica por el factor.
    /// </remarks>
    public ReplayGainResult AfterSteps(int steps) =>
        new(GainDb - (steps * Mp3GainEditor.StepDecibels), Peak * Math.Pow(2.0, steps / 4.0));
}

/// <summary>
/// Mide la sonoridad de una pista con el algoritmo ReplayGain 1.0 de MP3Gain
/// (<c>gain_analysis.c</c>), portado para consumir el PCM que entrega <see cref="AudioDecoder"/>.
/// </summary>
/// <remarks>
/// <para>
/// Cada canal pasa por el Yule-Walker y el Butterworth de <see cref="ReplayGainFilters"/>. Se toma
/// el RMS de ventanas de 50 ms promediando los dos canales y cada valor, en dB, se cuenta en un
/// histograma de 0.01 dB. El nivel de la pista es el percentil 95 de ese histograma: el que solo
/// supera el 5 % más fuerte. Así un final en silencio o una introducción tranquila no rebajan la
/// medida, y unos pocos golpes muy fuertes tampoco la disparan.
/// </para>
/// <para>
/// La ganancia es <c>64.82 − percentil</c>: la constante calibra el resultado para que un ruido
/// rosa a −20 dBFS reproducido a 83 dB SPL quede a 89 dB, la referencia de ReplayGain. La
/// calibración supone muestras en la escala de 16 bits, de ahí que la entrada se multiplique por
/// 32767 como hace MP3Gain con la salida de su decodificador.
/// </para>
/// <para>
/// Espera PCM intercalado del número de canales indicado; un bloque puede partir una pareja
/// izquierda-derecha y la muestra suelta se guarda para el siguiente. Las ventanas que no llegan a
/// completarse al final se descartan, igual que en el original.
/// </para>
/// </remarks>
public sealed class ReplayGainAnalyzer : ISampleSink
{
    /// <summary>Nivel de referencia de ReplayGain, en dB SPL.</summary>
    public const double ReferenceLevelDb = 89.0;

    private const double PinkReference = 64.82;
    private const int StepsPerDb = 100;
    private const int MaxDb = 120;
    private const double RmsPercentile = 0.95;
    private const int WindowMilliseconds = 50;
    private const double SampleScale = 32767.0;
    private const double DenormalBias = 1e-10;

    private readonly int _channels;
    private readonly IirFilter _yuleLeft;
    private readonly IirFilter _yuleRight;
    private readonly IirFilter _butterLeft;
    private readonly IirFilter _butterRight;
    private readonly int _windowLength;
    private readonly uint[] _histogram = new uint[StepsPerDb * MaxDb];

    private double _sumLeft;
    private double _sumRight;
    private int _windowFill;
    private float _pendingLeft;
    private bool _hasPendingLeft;
    private float _peak;

    /// <summary>Crea un analizador para una pista.</summary>
    /// <param name="sampleRate">
    /// Frecuencia del PCM; debe ser una de las admitidas (<see cref="IsSupported"/>).
    /// </param>
    /// <param name="channels">Canales intercalados en el PCM: 1 o 2.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Se lanza si la frecuencia no tiene coeficientes o si los canales no son 1 ni 2.
    /// </exception>
    public ReplayGainAnalyzer(int sampleRate, int channels = 2)
    {
        int index = ReplayGainFilters.IndexOf(sampleRate);
        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sampleRate), sampleRate, "ReplayGain no tiene filtros para esta frecuencia de muestreo.");
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(channels, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(channels, 2);

        _channels = channels;
        _yuleLeft = new IirFilter(ReplayGainFilters.Yule[index], DenormalBias);
        _yuleRight = new IirFilter(ReplayGainFilters.Yule[index], DenormalBias);
        _butterLeft = new IirFilter(ReplayGainFilters.Butter[index]);
        _butterRight = new IirFilter(ReplayGainFilters.Butter[index]);
        _windowLength = (int)Math.Ceiling(sampleRate * WindowMilliseconds / 1000.0);
    }

    /// <summary>Si hay filtros para una frecuencia de muestreo.</summary>
    /// <param name="sampleRate">Frecuencia, en Hz.</param>
    /// <returns><c>true</c> para las doce frecuencias estándar entre 8 y 96 kHz.</returns>
    public static bool IsSupported(int sampleRate) => ReplayGainFilters.IndexOf(sampleRate) >= 0;

    /// <summary>Frecuencia a la que conviene decodificar una pista para analizarla.</summary>
    /// <param name="nativeSampleRate">Frecuencia original de la pista.</param>
    /// <returns>
    /// La original si está admitida, para medir el pico sin el rizado de un remuestreo; si no,
    /// 44.1 kHz, cuya ponderación es la de referencia.
    /// </returns>
    public static int AnalysisRateFor(int nativeSampleRate) =>
        IsSupported(nativeSampleRate) ? nativeSampleRate : 44100;

    /// <inheritdoc/>
    public void Write(ReadOnlySpan<float> samples)
    {
        foreach (float sample in samples)
        {
            float magnitude = Math.Abs(sample);
            if (magnitude > _peak)
            {
                _peak = magnitude;
            }

            if (_channels == 1)
            {
                Accumulate(sample, sample);
            }
            else if (_hasPendingLeft)
            {
                Accumulate(_pendingLeft, sample);
                _hasPendingLeft = false;
            }
            else
            {
                _pendingLeft = sample;
                _hasPendingLeft = true;
            }
        }
    }

    /// <summary>Resultado de todo lo analizado.</summary>
    /// <returns>La ganancia hacia la referencia y el pico.</returns>
    /// <exception cref="InvalidDataException">
    /// Se lanza si no llegó audio suficiente para completar ni una ventana de 50 ms.
    /// </exception>
    public ReplayGainResult GetResult()
    {
        ulong total = 0;
        foreach (uint count in _histogram)
        {
            total += count;
        }

        if (total == 0)
        {
            throw new InvalidDataException("La pista es demasiado corta para medir su volumen.");
        }

        // Se recorre el histograma desde lo más fuerte hasta acumular el 5 % de las ventanas.
        long remaining = (long)Math.Ceiling(total * (1.0 - RmsPercentile));
        int index = _histogram.Length - 1;
        for (; index > 0; index--)
        {
            remaining -= _histogram[index];
            if (remaining <= 0)
            {
                break;
            }
        }

        return new ReplayGainResult(PinkReference - (index / (double)StepsPerDb), _peak);
    }

    private void Accumulate(float left, float right)
    {
        double filteredLeft = _butterLeft.Process(_yuleLeft.Process(left * SampleScale));
        double filteredRight = _channels == 1
            ? filteredLeft
            : _butterRight.Process(_yuleRight.Process(right * SampleScale));

        _sumLeft += filteredLeft * filteredLeft;
        _sumRight += filteredRight * filteredRight;

        if (++_windowFill < _windowLength)
        {
            return;
        }

        double meanSquare = ((_sumLeft + _sumRight) / _windowFill * 0.5) + 1e-37;
        int bin = (int)(StepsPerDb * 10.0 * Math.Log10(meanSquare));
        _histogram[Math.Clamp(bin, 0, _histogram.Length - 1)]++;

        _sumLeft = 0;
        _sumRight = 0;
        _windowFill = 0;
    }
}
