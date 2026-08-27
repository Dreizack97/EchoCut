namespace EchoCut.Audio;

/// <summary>
/// Filtro biquad en forma directa I. Portado de <c>au3/libraries/au3-math/Biquad.h</c> de Audacity,
/// conservando su decisión de calcular siempre en doble precisión: en simple, un paso alto con la
/// frecuencia de corte muy por debajo de la de muestreo acumula error de redondeo hasta volverse
/// inestable, porque los polos quedan pegadísimos a la circunferencia unidad.
/// </summary>
public sealed class Biquad
{
    /// <summary>
    /// Frecuencia de corte del paso alto RLB de la norma EBU R128, tal cual aparece en
    /// <c>EBUR128::CalcWeightingFilter</c> (<c>au3/libraries/au3-math/EBUR128.cpp</c>).
    /// </summary>
    public const double RlbHighPassHz = 38.13547087602444;

    private const double RlbQ = 0.5003270373238773;

    private readonly double _b0;
    private readonly double _b1;
    private readonly double _b2;
    private readonly double _a1;
    private readonly double _a2;

    private double _previousIn;
    private double _previousPreviousIn;
    private double _previousOut;
    private double _previousPreviousOut;

    private Biquad(double b0, double b1, double b2, double a1, double a2)
    {
        _b0 = b0;
        _b1 = b1;
        _b2 = b2;
        _a1 = a1;
        _a2 = a2;
    }

    /// <summary>
    /// Paso alto de segundo orden por transformada bilineal, con la adaptación a la frecuencia de
    /// muestreo de la etapa RLB de <c>EBUR128::CalcWeightingFilter</c>.
    /// </summary>
    /// <param name="sampleRate">Frecuencia de muestreo de la señal a filtrar, en Hz.</param>
    /// <param name="cutoffHz">Frecuencia de corte del filtro, en Hz.</param>
    /// <param name="q">
    /// Factor de calidad del filtro. Por defecto, el de la etapa RLB de la norma EBU R128
    /// (<see cref="RlbQ"/>); solo hace falta indicar otro para reproducir un filtro distinto.
    /// </param>
    /// <returns>Instancia de <see cref="Biquad"/> lista para procesar muestras con <see cref="ProcessOne"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Se lanza si <paramref name="sampleRate"/>, <paramref name="cutoffHz"/> o <paramref name="q"/>
    /// son cero o negativos.
    /// </exception>
    /// <remarks>
    /// Audacity deja el numerador sin normalizar porque a la medida de sonoridad le da igual una
    /// ganancia constante. Aquí sí importa: EchoCut compara contra umbrales en dBFS absolutos, así
    /// que el numerador se divide por <c>a0</c> para que la banda pasante quede a ganancia unidad y
    /// los niveles medidos sigan siendo comparables con los de una señal sin filtrar.
    /// </remarks>
    public static Biquad HighPass(double sampleRate, double cutoffHz, double q = RlbQ)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sampleRate);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cutoffHz);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(q);

        double k = Math.Tan(Math.PI * cutoffHz / sampleRate);
        double a0 = 1.0 + (k / q) + (k * k);

        return new Biquad(
            b0: 1.0 / a0,
            b1: -2.0 / a0,
            b2: 1.0 / a0,
            a1: 2.0 * ((k * k) - 1.0) / a0,
            a2: (1.0 - (k / q) + (k * k)) / a0);
    }

    /// <summary>Filtra una muestra y avanza el estado.</summary>
    /// <param name="input">Muestra de entrada, en el instante actual.</param>
    /// <returns>Muestra filtrada, correspondiente al mismo instante que <paramref name="input"/>.</returns>
    public double ProcessOne(double input)
    {
        double output = (input * _b0)
                        + (_previousIn * _b1)
                        + (_previousPreviousIn * _b2)
                        - (_previousOut * _a1)
                        - (_previousPreviousOut * _a2);

        _previousPreviousIn = _previousIn;
        _previousIn = input;
        _previousPreviousOut = _previousOut;
        _previousOut = output;

        return output;
    }

    /// <summary>
    /// Coloca el estado en el régimen permanente que corresponde a una entrada constante de
    /// <paramref name="firstSample"/>, de modo que la salida arranca ya en cero.
    /// </summary>
    /// <param name="firstSample">Primera muestra de la señal a filtrar.</param>
    /// <remarks>
    /// Sin esto, un archivo con offset de continua entrega un escalón al arrancar el filtro y las
    /// primeras decenas de milisegundos salen con un pico que no está en la señal. Como esas tramas
    /// alimentan la estimación del piso de ruido, el transitorio del propio filtro falsearía justo
    /// la medida que el filtro venía a limpiar.
    /// </remarks>
    public void Prime(double firstSample)
    {
        _previousIn = firstSample;
        _previousPreviousIn = firstSample;
        _previousOut = 0;
        _previousPreviousOut = 0;
    }

    /// <summary>Vacía la memoria del filtro, como si nunca hubiera procesado ninguna muestra.</summary>
    public void Reset()
    {
        _previousIn = 0;
        _previousPreviousIn = 0;
        _previousOut = 0;
        _previousPreviousOut = 0;
    }
}
