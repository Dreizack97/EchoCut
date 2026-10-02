using System.Buffers;

namespace EchoCut.Waveforms;

/// <summary>Envolvente de la señal en un intervalo de tiempo.</summary>
/// <param name="Min">Muestra más negativa del intervalo.</param>
/// <param name="Max">Muestra más positiva del intervalo.</param>
/// <param name="Rms">Valor eficaz del intervalo: el nivel promedio que se percibe, no el de los picos.</param>
public readonly record struct WaveformPeak(float Min, float Max, float Rms);

/// <summary>
/// Forma de onda resumida de una pista o de un tramo: mínimo, máximo y energía de cada bloque de
/// <see cref="SamplesPerBlock"/> muestras.
/// </summary>
/// <remarks>
/// <para>
/// Es el mismo resumen con el que Audacity dibuja sus pistas: con él se pinta cualquier nivel de
/// zoom por encima del tamaño de bloque sin volver a decodificar, y ocupa 256 veces menos que el
/// PCM —unos 3.7 MB para una mezcla de 30 minutos a 44.1 kHz—.
/// </para>
/// <para>
/// Se guarda la media de los cuadrados y no el RMS de cada bloque porque es lo único que se puede
/// promediar entre bloques: la media de varios RMS no es el RMS del conjunto.
/// </para>
/// <para>La memoria sale de <see cref="ArrayPool{T}"/>; hay que liberarla con <see cref="Dispose"/>.</para>
/// </remarks>
public sealed class Waveform : IDisposable
{
    /// <summary>
    /// Muestras por bloque: 5.8 ms a 44.1 kHz, por debajo de lo que ocupa un píxel incluso en la
    /// vista de detalle de un borde.
    /// </summary>
    public const int SamplesPerBlock = 256;

    /// <summary>Valores guardados por bloque: mínimo, máximo y media de los cuadrados.</summary>
    internal const int ValuesPerBlock = 3;

    private float[]? _blocks;

    internal Waveform(float[] blocks, int blockCount, int sampleRate, double startSeconds, long sampleCount)
    {
        _blocks = blocks;
        BlockCount = blockCount;
        SampleRate = sampleRate;
        StartSeconds = startSeconds;
        DecodedSeconds = sampleCount / (double)sampleRate;
    }

    /// <value>Número de bloques del resumen.</value>
    public int BlockCount { get; }

    /// <value>Frecuencia de muestreo del audio resumido, en Hz.</value>
    public int SampleRate { get; }

    /// <value>Instante del archivo en el que empieza la primera muestra, en segundos.</value>
    public double StartSeconds { get; }

    /// <value>Segundos de audio que entregó el decodificador.</value>
    public double DecodedSeconds { get; }

    /// <value>Instante del archivo en el que termina la última muestra, en segundos.</value>
    public double EndSeconds => StartSeconds + DecodedSeconds;

    /// <value>Duración de un bloque, en segundos.</value>
    public double BlockSeconds => SamplesPerBlock / (double)SampleRate;

    /// <summary>Envolvente del intervalo indicado, agregando los bloques que lo tocan.</summary>
    /// <param name="fromSeconds">Inicio del intervalo, en segundos del archivo.</param>
    /// <param name="toSeconds">Final del intervalo, en segundos del archivo.</param>
    /// <param name="peak">Envolvente del intervalo, si hay datos en él.</param>
    /// <returns><c>false</c> si el intervalo cae entero fuera del tramo resumido.</returns>
    /// <remarks>
    /// Un intervalo más corto que un bloque toma el bloque que lo contiene: con zoom, varios
    /// píxeles repiten bloque en lugar de quedar vacíos.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">Se lanza si la forma de onda ya se liberó.</exception>
    public bool TrySummarize(double fromSeconds, double toSeconds, out WaveformPeak peak)
    {
        ObjectDisposedException.ThrowIf(_blocks is null, this);

        peak = default;
        if (BlockCount == 0 || toSeconds <= StartSeconds || fromSeconds >= EndSeconds)
        {
            return false;
        }

        int first = Math.Clamp((int)Math.Floor((fromSeconds - StartSeconds) / BlockSeconds), 0, BlockCount - 1);
        int last = Math.Clamp((int)Math.Ceiling((toSeconds - StartSeconds) / BlockSeconds) - 1, first, BlockCount - 1);

        ReadOnlySpan<float> blocks = _blocks.AsSpan(first * ValuesPerBlock, (last - first + 1) * ValuesPerBlock);
        float min = float.MaxValue;
        float max = float.MinValue;
        double sumOfMeanSquares = 0;

        for (int i = 0; i < blocks.Length; i += ValuesPerBlock)
        {
            min = Math.Min(min, blocks[i]);
            max = Math.Max(max, blocks[i + 1]);
            sumOfMeanSquares += blocks[i + 2];
        }

        peak = new WaveformPeak(min, max, (float)Math.Sqrt(sumOfMeanSquares / (last - first + 1)));
        return true;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        float[]? blocks = Interlocked.Exchange(ref _blocks, null);
        if (blocks is not null)
        {
            ArrayPool<float>.Shared.Return(blocks);
        }
    }
}
