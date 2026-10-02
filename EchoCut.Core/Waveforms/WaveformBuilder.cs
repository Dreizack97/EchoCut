using EchoCut.Audio;
using System.Buffers;

namespace EchoCut.Waveforms;

/// <summary>
/// Resume la forma de onda a medida que el decodificador entrega las muestras, sin retener el
/// audio: solo acumula el bloque en curso.
/// </summary>
/// <remarks>
/// Recibe PCM mono; <see cref="AudioDecoder"/> ya mezcla los canales con <c>-ac 1</c>, que es la
/// figura única que interesa para localizar silencios.
/// </remarks>
public sealed class WaveformBuilder : ISampleSink, IDisposable
{
    private const int InitialBlocks = 4096;

    private readonly int _sampleRate;

    private float[] _blocks;
    private int _blockCount;
    private long _sampleCount;

    private float _min = float.MaxValue;
    private float _max = float.MinValue;
    private double _sumOfSquares;
    private int _samplesInBlock;

    private bool _completed;
    private bool _delivered;
    private bool _disposed;

    /// <summary>Prepara el resumen para audio a la frecuencia indicada.</summary>
    /// <param name="sampleRate">Frecuencia de muestreo de las muestras que se escribirán, en Hz.</param>
    /// <exception cref="ArgumentOutOfRangeException">Se lanza si <paramref name="sampleRate"/> no es positiva.</exception>
    public WaveformBuilder(int sampleRate)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sampleRate);

        _sampleRate = sampleRate;
        _blocks = ArrayPool<float>.Shared.Rent(InitialBlocks * Waveform.ValuesPerBlock);
    }

    /// <value>Segundos de audio escritos hasta ahora.</value>
    public double WrittenSeconds => _sampleCount / (double)_sampleRate;

    /// <inheritdoc/>
    public void Write(ReadOnlySpan<float> samples)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_completed)
        {
            throw new InvalidOperationException("No se pueden escribir muestras tras completar la forma de onda.");
        }

        _sampleCount += samples.Length;

        foreach (float sample in samples)
        {
            _min = Math.Min(_min, sample);
            _max = Math.Max(_max, sample);
            _sumOfSquares += sample * (double)sample;

            if (++_samplesInBlock == Waveform.SamplesPerBlock)
            {
                EmitBlock();
            }
        }
    }

    /// <summary>
    /// Cierra el último bloque aunque esté incompleto, para que el final del audio —justo lo que se
    /// inspecciona al recortar— no se quede fuera.
    /// </summary>
    public void Complete()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_completed)
        {
            return;
        }

        if (_samplesInBlock > 0)
        {
            EmitBlock();
        }

        _completed = true;
    }

    /// <summary>Entrega la forma de onda y le cede la memoria del resumen.</summary>
    /// <param name="startSeconds">Instante del archivo en el que empezaba la primera muestra escrita.</param>
    /// <returns>La forma de onda; quien la recibe debe liberarla.</returns>
    /// <exception cref="InvalidOperationException">
    /// Se lanza si no se llamó antes a <see cref="Complete"/> o si la forma de onda ya se entregó.
    /// </exception>
    public Waveform Build(double startSeconds)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!_completed || _delivered)
        {
            throw new InvalidOperationException("La forma de onda debe completarse y solo puede entregarse una vez.");
        }

        _delivered = true;
        return new Waveform(_blocks, _blockCount, _sampleRate, startSeconds, _sampleCount);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (!_delivered)
        {
            ArrayPool<float>.Shared.Return(_blocks);
        }
    }

    private void EmitBlock()
    {
        int offset = _blockCount * Waveform.ValuesPerBlock;
        if (offset + Waveform.ValuesPerBlock > _blocks.Length)
        {
            float[] larger = ArrayPool<float>.Shared.Rent(_blocks.Length * 2);
            _blocks.AsSpan(0, offset).CopyTo(larger);
            ArrayPool<float>.Shared.Return(_blocks);
            _blocks = larger;
        }

        _blocks[offset] = _min;
        _blocks[offset + 1] = _max;
        _blocks[offset + 2] = (float)(_sumOfSquares / _samplesInBlock);
        _blockCount++;

        _min = float.MaxValue;
        _max = float.MinValue;
        _sumOfSquares = 0;
        _samplesInBlock = 0;
    }
}
