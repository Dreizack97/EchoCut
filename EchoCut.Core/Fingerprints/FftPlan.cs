namespace EchoCut.Fingerprints;

/// <summary>Transformada rápida de Fourier compleja, radix 2, con las tablas precalculadas para un tamaño.</summary>
/// <remarks>
/// <para>
/// La huella acústica calcula decenas de miles de transformadas del mismo tamaño por canción, así
/// que la permutación de bits y los factores de giro se calculan una sola vez. La instancia no
/// guarda estado entre llamadas: varios hilos pueden compartirla.
/// </para>
/// <para>
/// Se implementa aquí, como el <c>Biquad</c>, para que el motor no dependa de bibliotecas de
/// terceros: es el algoritmo de Cooley–Tukey iterativo de los libros de texto.
/// </para>
/// </remarks>
internal sealed class FftPlan
{
    private readonly int[] _reversed;
    private readonly double[] _cos;
    private readonly double[] _sin;

    /// <summary>Prepara las tablas para un tamaño.</summary>
    /// <param name="size">Número de puntos; debe ser una potencia de dos.</param>
    /// <exception cref="ArgumentOutOfRangeException">Se lanza si el tamaño no es una potencia de dos mayor que 1.</exception>
    public FftPlan(int size)
    {
        if (size < 2 || (size & (size - 1)) != 0)
        {
            throw new ArgumentOutOfRangeException(nameof(size), size, "El tamaño de la FFT debe ser una potencia de dos.");
        }

        Size = size;
        _reversed = new int[size];
        int bits = (int)Math.Log2(size);
        for (int i = 0; i < size; i++)
        {
            int reversed = 0;
            for (int bit = 0; bit < bits; bit++)
            {
                reversed |= ((i >> bit) & 1) << (bits - 1 - bit);
            }

            _reversed[i] = reversed;
        }

        _cos = new double[size / 2];
        _sin = new double[size / 2];
        for (int k = 0; k < size / 2; k++)
        {
            double angle = 2.0 * Math.PI * k / size;
            _cos[k] = Math.Cos(angle);
            _sin[k] = Math.Sin(angle);
        }
    }

    /// <value>Número de puntos de la transformada.</value>
    public int Size { get; }

    /// <summary>Transforma en el sitio una señal compleja.</summary>
    /// <param name="real">Parte real; al volver, la parte real del espectro.</param>
    /// <param name="imaginary">Parte imaginaria; al volver, la del espectro.</param>
    public void Forward(Span<double> real, Span<double> imaginary)
    {
        int n = Size;
        for (int i = 0; i < n; i++)
        {
            int j = _reversed[i];
            if (j > i)
            {
                (real[i], real[j]) = (real[j], real[i]);
                (imaginary[i], imaginary[j]) = (imaginary[j], imaginary[i]);
            }
        }

        for (int length = 2; length <= n; length <<= 1)
        {
            int half = length / 2;
            int step = n / length;
            for (int start = 0; start < n; start += length)
            {
                for (int k = 0; k < half; k++)
                {
                    double wr = _cos[k * step];
                    double wi = -_sin[k * step];
                    int a = start + k;
                    int b = a + half;
                    double tr = (wr * real[b]) - (wi * imaginary[b]);
                    double ti = (wr * imaginary[b]) + (wi * real[b]);
                    real[b] = real[a] - tr;
                    imaginary[b] = imaginary[a] - ti;
                    real[a] += tr;
                    imaginary[a] += ti;
                }
            }
        }
    }
}
