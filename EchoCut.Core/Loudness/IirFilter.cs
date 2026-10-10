namespace EchoCut.Loudness;

/// <summary>
/// Filtro IIR en forma directa I con historia circular, para las dos etapas de ponderación de
/// ReplayGain.
/// </summary>
/// <remarks>
/// La historia se guarda duplicada (cada valor se escribe en <c>i</c> y en <c>i + orden</c>) para
/// que la ventana de las últimas muestras sea siempre contigua: el bucle interno no necesita
/// módulos ni ramas, y el filtro de orden 10 corre por cada muestra de cada canal de la pista.
/// </remarks>
internal sealed class IirFilter
{
    private readonly int _order;
    private readonly double[] _b;
    private readonly double[] _a;
    private readonly double[] _inputs;
    private readonly double[] _outputs;
    private readonly double _bias;
    private int _head;

    /// <summary>Crea el filtro a partir de los coeficientes intercalados de <c>gain_analysis.c</c>.</summary>
    /// <param name="kernel">
    /// Coeficientes en el orden de MP3Gain: <c>b0, a1, b1, a2, b2, …, aN, bN</c>.
    /// </param>
    /// <param name="bias">
    /// Constante que se suma a cada salida. El Yule-Walker de MP3Gain suma 1e-10 para que la
    /// realimentación no caiga en números desnormalizados durante el silencio, que en x86 son
    /// órdenes de magnitud más lentos.
    /// </param>
    public IirFilter(ReadOnlySpan<double> kernel, double bias = 0)
    {
        _order = (kernel.Length - 1) / 2;
        _b = new double[_order + 1];
        _a = new double[_order + 1];
        _b[0] = kernel[0];
        for (int k = 1; k <= _order; k++)
        {
            _a[k] = kernel[(2 * k) - 1];
            _b[k] = kernel[2 * k];
        }

        _inputs = new double[2 * _order];
        _outputs = new double[2 * _order];
        _bias = bias;
    }

    /// <summary>Filtra una muestra y avanza la historia.</summary>
    /// <param name="input">Muestra de entrada.</param>
    /// <returns>La muestra filtrada.</returns>
    public double Process(double input)
    {
        // x[n-k] e y[n-k] viven en la posición _head + k - 1.
        double accumulator = _bias + (_b[0] * input);
        int head = _head;
        for (int k = 1; k <= _order; k++)
        {
            accumulator += (_b[k] * _inputs[head + k - 1]) - (_a[k] * _outputs[head + k - 1]);
        }

        head = head == 0 ? _order - 1 : head - 1;
        _inputs[head] = _inputs[head + _order] = input;
        _outputs[head] = _outputs[head + _order] = accumulator;
        _head = head;

        return accumulator;
    }
}
