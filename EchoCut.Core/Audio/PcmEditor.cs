namespace EchoCut.Audio;

/// <summary>
/// Aplica a un flujo de PCM entrelazado las ediciones de una pista, bloque a bloque: atenúa los
/// fundidos y retira las tramas borradas.
/// </summary>
/// <remarks>
/// <para>
/// Es el único sitio donde se decide cómo suena la copia, y lo usan tanto el renderizado como la
/// escucha del editor: lo que se oye antes de guardar es exactamente lo que se escribe.
/// </para>
/// <para>
/// No guarda estado entre bloques más allá de lo que recibe en cada llamada, así que el llamador
/// lleva la cuenta de tramas leídas del flujo original.
/// </para>
/// </remarks>
public sealed class PcmEditor
{
    private readonly FadeEnvelope? _envelope;
    private readonly DeletedRegions _deletions;
    private readonly int _sampleRate;
    private readonly double _originSeconds;

    /// <summary>Prepara las ediciones para un flujo concreto.</summary>
    /// <param name="edits">Ediciones a aplicar, en tiempo del original.</param>
    /// <param name="sampleRate">Frecuencia del flujo, en Hz.</param>
    /// <param name="originSeconds">Instante del original que corresponde a la primera trama del flujo.</param>
    /// <param name="channels">Canales por trama.</param>
    /// <exception cref="ArgumentOutOfRangeException">Se lanza si la frecuencia o los canales no son positivos, o los fundidos no son válidos.</exception>
    public PcmEditor(AudioEdits edits, int sampleRate, double originSeconds, int channels)
    {
        ArgumentNullException.ThrowIfNull(edits);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sampleRate);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(channels);
        edits.ThrowIfInvalid();

        _envelope = edits.Fades is { } fades ? new FadeEnvelope(fades, sampleRate, originSeconds) : null;
        _deletions = edits.Deletions;
        _sampleRate = sampleRate;
        _originSeconds = originSeconds;
        Channels = channels;
    }

    /// <value>Canales por trama del flujo.</value>
    public int Channels { get; }

    /// <summary>Edita en el sitio un bloque y deja al principio las tramas que sobreviven.</summary>
    /// <param name="interleaved">Muestras entrelazadas; su longitud debe ser múltiplo de <see cref="Channels"/>.</param>
    /// <param name="firstFrame">Índice, desde el origen del flujo original, de la primera trama del bloque.</param>
    /// <returns>Tramas que se conservan, ya movidas al principio de <paramref name="interleaved"/>.</returns>
    /// <remarks>
    /// Primero se atenúa y después se borra: los fundidos se miden en tiempo del original, así que
    /// cada muestra que sobrevive conserva la ganancia que le tocaba en su posición original.
    /// </remarks>
    public int Process(Span<float> interleaved, long firstFrame)
    {
        int frames = interleaved.Length / Channels;
        _envelope?.Apply(interleaved, Channels, firstFrame);

        if (_deletions.IsEmpty)
        {
            return frames;
        }

        int written = 0;
        foreach ((int offset, int count) in _deletions.KeptRuns(_sampleRate, _originSeconds, firstFrame, frames))
        {
            if (offset != written)
            {
                interleaved.Slice(offset * Channels, count * Channels).CopyTo(interleaved[(written * Channels)..]);
            }

            written += count;
        }

        return written;
    }
}
