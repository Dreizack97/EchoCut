namespace EchoCut.Audio;

/// <summary>
/// Fragmentos que el usuario borró de una pista, como el «Borrar» de Audacity: se quitan sus
/// muestras y lo anterior se une con lo posterior.
/// </summary>
/// <remarks>
/// <para>
/// Es inmutable y está siempre normalizado —ordenado, sin solapes y con los tramos contiguos
/// fusionados—, de modo que borrar dos veces lo mismo o en distinto orden da el mismo resultado.
/// </para>
/// <para>
/// Los tramos se guardan en tiempo del original, no en el de la copia: así el recorte, los
/// fundidos y los borrados comparten una sola línea de tiempo y ninguno desplaza a los demás. La
/// traducción a la línea de tiempo de la copia se hace solo al escribirla o escucharla.
/// </para>
/// </remarks>
public sealed class DeletedRegions
{
    private readonly TimeRegion[] _regions;

    private DeletedRegions(TimeRegion[] regions) => _regions = regions;

    /// <value>Ningún fragmento borrado.</value>
    public static DeletedRegions Empty { get; } = new([]);

    /// <value>Los fragmentos borrados, ordenados y sin solapes.</value>
    public IReadOnlyList<TimeRegion> Regions => _regions;

    /// <value><c>true</c> si no hay nada borrado.</value>
    public bool IsEmpty => _regions.Length == 0;

    /// <value>Segundos borrados en total.</value>
    public double TotalSeconds => _regions.Sum(region => region.DurationSeconds);

    /// <summary>Borra además un tramo.</summary>
    /// <param name="region">Tramo a borrar.</param>
    /// <returns>Un conjunto nuevo con el tramo fusionado con los que toque o solape.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se lanza si el tramo no es válido.</exception>
    public DeletedRegions Add(TimeRegion region)
    {
        region.ThrowIfInvalid();

        List<TimeRegion> result = [];
        TimeRegion merged = region;
        foreach (TimeRegion existing in _regions)
        {
            if (existing.EndSeconds < merged.StartSeconds || existing.StartSeconds > merged.EndSeconds)
            {
                result.Add(existing);
            }
            else
            {
                merged = new TimeRegion(
                    Math.Min(existing.StartSeconds, merged.StartSeconds),
                    Math.Max(existing.EndSeconds, merged.EndSeconds));
            }
        }

        result.Add(merged);
        return new DeletedRegions([.. result.OrderBy(r => r.StartSeconds)]);
    }

    /// <summary>Restaura el audio borrado que cae dentro de un tramo.</summary>
    /// <param name="region">Tramo a restaurar.</param>
    /// <returns>Un conjunto nuevo sin esa parte; los fragmentos que la rebasan conservan lo que queda fuera.</returns>
    public DeletedRegions Remove(TimeRegion region)
    {
        List<TimeRegion> result = [];
        foreach (TimeRegion existing in _regions)
        {
            if (!existing.Overlaps(region))
            {
                result.Add(existing);
                continue;
            }

            if (existing.StartSeconds < region.StartSeconds)
            {
                result.Add(existing with { EndSeconds = region.StartSeconds });
            }

            if (existing.EndSeconds > region.EndSeconds)
            {
                result.Add(existing with { StartSeconds = region.EndSeconds });
            }
        }

        return result.Count == _regions.Length && result.SequenceEqual(_regions) ? this : new DeletedRegions([.. result]);
    }

    /// <summary>Recorta los fragmentos a lo que conserva la copia.</summary>
    /// <param name="range">Tramo que conserva la copia.</param>
    /// <returns>Solo la parte de cada fragmento que cae dentro de <paramref name="range"/>.</returns>
    /// <remarks>
    /// Borrar dentro de lo que el recorte ya elimina no cambia la copia; descartarlo permite saber si
    /// queda algún borrado efectivo y, si no, seguir recortando sin recodificar.
    /// </remarks>
    public DeletedRegions Within(TrimRange range)
    {
        List<TimeRegion> result = [];
        foreach (TimeRegion existing in _regions)
        {
            double start = Math.Max(existing.StartSeconds, range.StartSeconds);
            double end = Math.Min(existing.EndSeconds, range.EndSeconds);
            if (end > start)
            {
                result.Add(new TimeRegion(start, end));
            }
        }

        return result.Count == 0 ? Empty : new DeletedRegions([.. result]);
    }

    /// <summary>Fragmento borrado que contiene un instante.</summary>
    /// <param name="seconds">Instante, en segundos del original.</param>
    /// <returns>El fragmento, o <c>null</c> si el instante no está borrado.</returns>
    public TimeRegion? RegionAt(double seconds)
    {
        foreach (TimeRegion existing in _regions)
        {
            if (existing.Contains(seconds))
            {
                return existing;
            }
        }

        return null;
    }

    /// <summary>
    /// Instante del original que suena tras escuchar cierto tiempo de la copia a partir de un origen.
    /// </summary>
    /// <param name="originSeconds">Instante del original donde empezó a sonar.</param>
    /// <param name="elapsedSeconds">Tiempo ya sonado, en la línea de tiempo de la copia.</param>
    /// <returns>El instante del original, saltando lo borrado.</returns>
    /// <remarks>Sirve para situar el cursor de reproducción sobre la forma de onda del original.</remarks>
    public double SourceSecondsAt(double originSeconds, double elapsedSeconds)
    {
        double time = originSeconds;
        double remaining = Math.Max(0.0, elapsedSeconds);

        foreach (TimeRegion existing in _regions)
        {
            if (existing.EndSeconds <= time)
            {
                continue;
            }

            if (existing.StartSeconds > time)
            {
                double gap = existing.StartSeconds - time;
                // Al llegar justo al borde de un borrado se devuelve ese borde, no el final del
                // borrado: es el mismo instante de la copia y el cursor no debe saltar antes de tiempo.
                if (remaining <= gap)
                {
                    return time + remaining;
                }

                remaining -= gap;
            }

            time = existing.EndSeconds;
        }

        return time + remaining;
    }

    /// <summary>Instante de la copia que corresponde a un instante del original.</summary>
    /// <param name="sourceSeconds">Instante del original, en segundos.</param>
    /// <returns>
    /// El instante en la línea de tiempo del resultado, es decir, el del original menos todo lo
    /// borrado antes de él; dentro de un fragmento borrado, el punto del empalme.
    /// </returns>
    public double OutputSecondsAt(double sourceSeconds)
    {
        double removed = 0.0;
        foreach (TimeRegion existing in _regions)
        {
            if (existing.StartSeconds >= sourceSeconds)
            {
                break;
            }

            removed += Math.Min(existing.EndSeconds, sourceSeconds) - existing.StartSeconds;
        }

        return sourceSeconds - removed;
    }

    /// <summary>Tramos del original que suenan en un tramo del resultado.</summary>
    /// <param name="outputFromSeconds">Comienzo del tramo, en la línea de tiempo del resultado.</param>
    /// <param name="outputToSeconds">Final del tramo, en la línea de tiempo del resultado.</param>
    /// <returns>
    /// Los tramos del original, en orden, cuya unión es lo que suena en ese tramo del resultado: uno
    /// solo, salvo que el tramo cruce un empalme.
    /// </returns>
    /// <remarks>Sirve para dibujar la onda del resultado a partir del resumen del original, sin volver a decodificar.</remarks>
    public List<TimeRegion> SourceSpans(double outputFromSeconds, double outputToSeconds)
    {
        List<TimeRegion> spans = [];
        double remaining = outputToSeconds - outputFromSeconds;
        double time = SourceSecondsAt(0.0, outputFromSeconds);

        foreach (TimeRegion existing in _regions)
        {
            if (remaining <= 0.0)
            {
                return spans;
            }

            if (existing.EndSeconds <= time)
            {
                continue;
            }

            if (existing.StartSeconds <= time)
            {
                time = existing.EndSeconds;
                continue;
            }

            double gap = existing.StartSeconds - time;
            if (remaining <= gap)
            {
                break;
            }

            spans.Add(new TimeRegion(time, existing.StartSeconds));
            remaining -= gap;
            time = existing.EndSeconds;
        }

        if (remaining > 0.0)
        {
            spans.Add(new TimeRegion(time, time + remaining));
        }

        return spans;
    }

    /// <summary>
    /// Tramos de un bloque de PCM que se conservan, en tramas relativas al propio bloque.
    /// </summary>
    /// <param name="sampleRate">Frecuencia del flujo, en Hz.</param>
    /// <param name="originSeconds">Instante del original que corresponde a la trama 0 del flujo.</param>
    /// <param name="firstFrame">Índice, desde el origen, de la primera trama del bloque.</param>
    /// <param name="frameCount">Tramas del bloque.</param>
    /// <returns>Pares de desplazamiento y longitud, en tramas y en orden, de lo que no está borrado.</returns>
    /// <remarks>
    /// Los extremos se redondean a la muestra más cercana, como el <c>SnapToSample</c> con el que
    /// Audacity ajusta la selección antes de borrarla. La unión queda en seco, como en Audacity.
    /// </remarks>
    public List<(int Offset, int Count)> KeptRuns(int sampleRate, double originSeconds, long firstFrame, int frameCount)
    {
        List<(int Offset, int Count)> runs = [];
        long blockEnd = firstFrame + frameCount;
        long cursor = firstFrame;

        foreach (TimeRegion existing in _regions)
        {
            long deletedStart = Math.Max(cursor, (long)Math.Round((existing.StartSeconds - originSeconds) * sampleRate));
            long deletedEnd = Math.Min(blockEnd, (long)Math.Round((existing.EndSeconds - originSeconds) * sampleRate));
            if (deletedEnd <= cursor || deletedStart >= blockEnd)
            {
                continue;
            }

            if (deletedStart > cursor)
            {
                runs.Add(((int)(cursor - firstFrame), (int)(deletedStart - cursor)));
            }

            cursor = Math.Max(cursor, deletedEnd);
        }

        if (cursor < blockEnd)
        {
            runs.Add(((int)(cursor - firstFrame), (int)(blockEnd - cursor)));
        }

        return runs;
    }
}
