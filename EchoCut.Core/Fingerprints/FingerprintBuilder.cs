using EchoCut.Audio;
using System.Buffers;

namespace EchoCut.Fingerprints;

/// <summary>
/// Calcula la huella acústica de una pista según la decodifica FFmpeg, sin retener el audio.
/// </summary>
/// <remarks>
/// <para>
/// Sigue el algoritmo de Haitsma y Kalker (Philips, 2002): cada trama se divide en 33 bandas de
/// frecuencia logarítmicas entre 300 y 2000 Hz, donde está lo más estable de una grabación frente a
/// la compresión y la ecualización, y cada uno de sus 32 bits es el signo de cómo cambia la
/// diferencia de energía entre dos bandas vecinas de una trama a la siguiente:
/// <c>F(n,m) = [E(n,m) − E(n,m+1)] − [E(n−1,m) − E(n−1,m+1)] &gt; 0</c>.
/// </para>
/// <para>
/// Por construcción no depende del volumen —escalar todas las energías no cambia ningún signo— y
/// apenas del códec o de la tasa de bits: dos copias de la misma grabación difieren en pocos bits,
/// y dos grabaciones distintas, en la mitad.
/// </para>
/// <para>
/// Las tramas son de 1024 muestras a 5512 Hz (186 ms), con salto de 128 (23 ms): la resolución
/// alcanza para separar las bandas graves, que miden unos 18 Hz, y el solape hace que dos copias
/// desfasadas medio salto sigan dando casi las mismas palabras.
/// </para>
/// </remarks>
public sealed class FingerprintBuilder : ISampleSink, IDisposable
{
    /// <summary>Frecuencia a la que se pide el audio: de sobra para la banda de 300 a 2000 Hz.</summary>
    public const int SampleRate = 5512;

    /// <summary>Muestras por trama.</summary>
    public const int FrameSize = 1024;

    /// <summary>Muestras entre el comienzo de una trama y el de la siguiente.</summary>
    public const int HopSize = 128;

    private const int BandCount = 33;
    private const double LowestHz = 300.0;
    private const double HighestHz = 2000.0;

    /// <summary>
    /// Nivel por debajo del cual una trama se toma por silencio. Muy por debajo de la música más
    /// tenue y por encima del ruido de fondo de una grabación limpia.
    /// </summary>
    private const double SilenceDbfs = -55.0;

    private static readonly FftPlan Plan = new(FrameSize);
    private static readonly double[] Window = CreateHannWindow();
    private static readonly int[] BandEdges = CreateBandEdges();

    private readonly float[] _ring = ArrayPool<float>.Shared.Rent(FrameSize);
    private readonly double[] _real = ArrayPool<double>.Shared.Rent(FrameSize);
    private readonly double[] _imaginary = ArrayPool<double>.Shared.Rent(FrameSize);
    private double[] _bands = ArrayPool<double>.Shared.Rent(BandCount);
    private double[] _previousBands = ArrayPool<double>.Shared.Rent(BandCount);
    private readonly List<uint> _frames = [];
    private readonly List<bool> _audible = [];

    private int _write;
    private long _samples;
    private int _sinceFrame;
    private bool _hasPrevious;
    private bool _previousAudible;
    private bool _disposed;

    /// <inheritdoc/>
    /// <exception cref="ObjectDisposedException">Se lanza si la instancia ya se liberó.</exception>
    public void Write(ReadOnlySpan<float> samples)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        foreach (float sample in samples)
        {
            _ring[_write] = sample;
            _write = (_write + 1) % FrameSize;
            _samples++;
            _sinceFrame++;

            if (_samples >= FrameSize && _sinceFrame >= HopSize)
            {
                ComputeFrame();
                _sinceFrame = 0;
            }
        }
    }

    /// <summary>Entrega la huella de todo lo escrito.</summary>
    /// <returns>La huella; vacía si la pista dura menos de una trama.</returns>
    /// <exception cref="ObjectDisposedException">Se lanza si la instancia ya se liberó.</exception>
    public AudioFingerprint Build()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return new AudioFingerprint([.. _frames], [.. _audible], HopSize / (double)SampleRate);
    }

    /// <summary>Devuelve los búferes al grupo compartido.</summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        ArrayPool<float>.Shared.Return(_ring);
        ArrayPool<double>.Shared.Return(_real);
        ArrayPool<double>.Shared.Return(_imaginary);
        ArrayPool<double>.Shared.Return(_bands);
        ArrayPool<double>.Shared.Return(_previousBands);
    }

    /// <summary>Analiza la trama que forman las últimas <see cref="FrameSize"/> muestras.</summary>
    private void ComputeFrame()
    {
        double energy = 0.0;
        for (int i = 0; i < FrameSize; i++)
        {
            // _write apunta a la muestra más antigua del anillo.
            float sample = _ring[(_write + i) % FrameSize];
            energy += sample * sample;
            _real[i] = sample * Window[i];
            _imaginary[i] = 0.0;
        }

        Plan.Forward(_real.AsSpan(0, FrameSize), _imaginary.AsSpan(0, FrameSize));

        for (int band = 0; band < BandCount; band++)
        {
            double sum = 0.0;
            for (int bin = BandEdges[band]; bin < BandEdges[band + 1]; bin++)
            {
                sum += (_real[bin] * _real[bin]) + (_imaginary[bin] * _imaginary[bin]);
            }

            _bands[band] = sum;
        }

        bool audible = 10.0 * Math.Log10(Math.Max(energy / FrameSize, 1e-20)) > SilenceDbfs;

        if (_hasPrevious)
        {
            uint word = 0;
            for (int bit = 0; bit < BandCount - 1; bit++)
            {
                double now = _bands[bit] - _bands[bit + 1];
                double before = _previousBands[bit] - _previousBands[bit + 1];
                if (now - before > 0.0)
                {
                    word |= 1u << bit;
                }
            }

            _frames.Add(word);
            _audible.Add(audible && _previousAudible);
        }

        (_bands, _previousBands) = (_previousBands, _bands);
        _previousAudible = audible;
        _hasPrevious = true;
    }

    private static double[] CreateHannWindow()
    {
        double[] window = new double[FrameSize];
        for (int i = 0; i < FrameSize; i++)
        {
            window[i] = 0.5 * (1.0 - Math.Cos(2.0 * Math.PI * i / (FrameSize - 1)));
        }

        return window;
    }

    /// <summary>Primer bin de cada banda, y el de después de la última: bordes repartidos en escala logarítmica.</summary>
    private static int[] CreateBandEdges()
    {
        int[] edges = new int[BandCount + 1];
        for (int band = 0; band <= BandCount; band++)
        {
            double hz = LowestHz * Math.Pow(HighestHz / LowestHz, band / (double)BandCount);
            edges[band] = (int)Math.Round(hz * FrameSize / SampleRate);
        }

        return edges;
    }
}
