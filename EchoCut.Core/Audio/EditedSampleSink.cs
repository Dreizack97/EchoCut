using System.Buffers;

namespace EchoCut.Audio;

/// <summary>
/// Destino intermedio que aplica a las muestras mono las ediciones de una pista —fundidos y
/// borrados— antes de pasarlas al destino final.
/// </summary>
/// <remarks>
/// <para>
/// Permite analizar o dibujar el resultado editado con las mismas piezas que el original: lo que
/// decodifica FFmpeg pasa por <see cref="PcmEditor"/>, el mismo código que escribe la copia, y el
/// destino final ve exactamente lo que quedará.
/// </para>
/// <para>
/// El origen de las ediciones es el principio del archivo: hay que decodificarlo desde la primera
/// muestra, porque un borrado desplaza todo lo que viene después.
/// </para>
/// </remarks>
/// <param name="inner">Destino que recibe el resultado editado.</param>
/// <param name="edits">Ediciones a aplicar, en tiempo del original.</param>
/// <param name="sampleRate">Frecuencia de las muestras que llegan, en Hz.</param>
public sealed class EditedSampleSink(ISampleSink inner, AudioEdits edits, int sampleRate) : ISampleSink
{
    private readonly PcmEditor _editor = new(edits, sampleRate, 0.0, channels: 1);
    private long _frame;

    /// <inheritdoc/>
    /// <remarks>
    /// El bloque que llega es de solo lectura y el editor trabaja en el sitio, así que se copia a un
    /// búfer del grupo compartido; se devuelve antes de salir, porque el destino no puede retenerlo.
    /// </remarks>
    public void Write(ReadOnlySpan<float> samples)
    {
        if (samples.IsEmpty)
        {
            return;
        }

        float[] buffer = ArrayPool<float>.Shared.Rent(samples.Length);
        try
        {
            Span<float> block = buffer.AsSpan(0, samples.Length);
            samples.CopyTo(block);

            int kept = _editor.Process(block, _frame);
            _frame += samples.Length;

            if (kept > 0)
            {
                inner.Write(block[..kept]);
            }
        }
        finally
        {
            ArrayPool<float>.Shared.Return(buffer);
        }
    }
}
