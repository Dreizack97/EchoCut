namespace EchoCut.Audio;

/// <summary>
/// Aplica los fundidos de una pista a PCM entrelazado, muestra a muestra, tal como lo hace el
/// <c>FadeEffectBase::ProcessBlock</c> de Audacity.
/// </summary>
/// <remarks>
/// <para>
/// Los fundidos se traducen una sola vez a cuentas de tramas sobre el flujo que se procesa; a partir
/// de ahí la posición de cada muestra es un entero y no se acumula error de redondeo en pistas largas.
/// </para>
/// <para>
/// Audacity calcula la aparición como <c>n / N</c> y la desaparición como <c>(N − 1 − n) / N</c>:
/// la primera muestra de una aparición es silencio y la última de una desaparición también. Aquí
/// se conserva esa indexación para cualquier curva.
/// </para>
/// <para>
/// No hace E/S ni guarda estado entre bloques: se puede usar igual en el renderizado de la copia
/// que en la previsualización.
/// </para>
/// </remarks>
public sealed class FadeEnvelope
{
    private readonly Segment[] _segments;

    /// <summary>Prepara los fundidos para un flujo concreto.</summary>
    /// <param name="fades">Fundidos a aplicar, en tiempo del archivo original.</param>
    /// <param name="sampleRate">Frecuencia del flujo, en Hz.</param>
    /// <param name="originSeconds">
    /// Instante del original al que corresponde la primera trama del flujo; por ejemplo, el comienzo
    /// de la copia recortada o de la ventana que se escucha.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">Se lanza si la frecuencia no es positiva o los fundidos no son válidos.</exception>
    public FadeEnvelope(TrackFades fades, int sampleRate, double originSeconds)
    {
        ArgumentNullException.ThrowIfNull(fades);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sampleRate);
        fades.ThrowIfInvalid();

        List<Segment> segments = [];
        foreach (Fade? candidate in (ReadOnlySpan<Fade?>)[fades.FadeIn, fades.FadeOut])
        {
            if (candidate is { } fade)
            {
                long first = (long)Math.Round((fade.StartSeconds - originSeconds) * sampleRate);
                long count = Math.Max(1, (long)Math.Round(fade.DurationSeconds * sampleRate));
                segments.Add(new Segment(first, count, fade.Curve, fade.Direction == FadeDirection.Out));
            }
        }

        _segments = [.. segments];
    }

    /// <summary>Ganancia que recibe una trama del flujo.</summary>
    /// <param name="frame">Índice de la trama, contado desde el origen del flujo.</param>
    /// <returns>Producto de las ganancias de los fundidos que la cubren; 1 si ninguno la cubre.</returns>
    public double GainAt(long frame)
    {
        double gain = 1.0;
        foreach (Segment segment in _segments)
        {
            if (segment.Contains(frame))
            {
                gain *= segment.GainAt(frame);
            }
        }

        return gain;
    }

    /// <summary>Atenúa en el sitio un bloque de PCM entrelazado.</summary>
    /// <param name="interleaved">Muestras entrelazadas; su longitud debe ser múltiplo de <paramref name="channels"/>.</param>
    /// <param name="channels">Canales por trama.</param>
    /// <param name="firstFrame">Índice, desde el origen del flujo, de la primera trama del bloque.</param>
    /// <remarks>
    /// Todos los canales de una trama reciben la misma ganancia, como en Audacity, para no alterar la
    /// imagen estéreo. Los bloques que no tocan ningún fundido —casi todos— salen sin recorrerse.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Se lanza si <paramref name="channels"/> no es positivo.</exception>
    public void Apply(Span<float> interleaved, int channels, long firstFrame)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(channels);

        long frames = interleaved.Length / channels;
        foreach (Segment segment in _segments)
        {
            long from = Math.Max(firstFrame, segment.FirstFrame);
            long to = Math.Min(firstFrame + frames, segment.FirstFrame + segment.FrameCount);

            for (long frame = from; frame < to; frame++)
            {
                float gain = (float)segment.GainAt(frame);
                Span<float> samples = interleaved.Slice((int)(frame - firstFrame) * channels, channels);
                for (int channel = 0; channel < samples.Length; channel++)
                {
                    samples[channel] *= gain;
                }
            }
        }
    }

    /// <summary>Un fundido expresado en tramas del flujo.</summary>
    private readonly record struct Segment(long FirstFrame, long FrameCount, FadeCurve Curve, bool Reverse)
    {
        public bool Contains(long frame) => frame >= FirstFrame && frame < FirstFrame + FrameCount;

        public double GainAt(long frame)
        {
            long n = frame - FirstFrame;
            double progress = (double)(Reverse ? FrameCount - 1 - n : n) / FrameCount;
            return FadeShape.FadeInGain(Curve, progress);
        }
    }
}
