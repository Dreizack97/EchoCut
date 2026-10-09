namespace EchoCut.Audio;

/// <summary>
/// Ediciones que cambian las muestras de una pista: sus fundidos y los fragmentos borrados.
/// </summary>
/// <param name="Fades">Fundidos, o <c>null</c> si no hay ninguno.</param>
/// <param name="Deletions">Fragmentos borrados; <see cref="DeletedRegions.Empty"/> si no hay ninguno.</param>
/// <remarks>
/// Se agrupan porque comparten consecuencia: cualquiera de ellas impide la copia de flujo y obliga a
/// recodificar, y las dos se aplican en el mismo recorrido del PCM, tanto al escribir como al escuchar.
/// </remarks>
public sealed record AudioEdits(TrackFades? Fades, DeletedRegions Deletions)
{
    /// <value><c>true</c> si hay algún fundido o algún borrado.</value>
    public bool HasAny => Fades is not null || !Deletions.IsEmpty;

    /// <summary>Crea las ediciones a partir de sus partes, o nada si están vacías.</summary>
    /// <param name="fades">Fundidos, o <c>null</c>.</param>
    /// <param name="deletions">Fragmentos borrados, o <c>null</c>.</param>
    /// <returns>Las ediciones, o <c>null</c> si no hay ninguna.</returns>
    public static AudioEdits? From(TrackFades? fades, DeletedRegions? deletions)
    {
        AudioEdits edits = new(fades is { HasAny: true } ? fades : null, deletions ?? DeletedRegions.Empty);
        return edits.HasAny ? edits : null;
    }

    /// <summary>Se queda con lo que de verdad afecta a la copia.</summary>
    /// <param name="range">Tramo que conserva la copia.</param>
    /// <returns>Las ediciones que llegan a la copia, o <c>null</c> si ninguna llega.</returns>
    /// <remarks>
    /// Lo que cae entero en lo que el recorte ya elimina no cuenta: si no queda nada, la copia sigue
    /// saliendo por copia de flujo, sin pérdida.
    /// </remarks>
    public AudioEdits? Within(TrimRange range) => From(Fades?.Within(range), Deletions.Within(range));

    /// <summary>Segundos que durará la copia de un tramo una vez quitado lo borrado.</summary>
    /// <param name="range">Tramo que conserva la copia.</param>
    /// <returns>La duración del tramo menos lo borrado dentro de él.</returns>
    public double KeptSeconds(TrimRange range) => range.DurationSeconds - Deletions.Within(range).TotalSeconds;

    /// <summary>Comprueba que todas las ediciones sean válidas.</summary>
    /// <exception cref="ArgumentNullException">Se lanza si <see cref="Deletions"/> es <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se lanza si algún fundido no es válido.</exception>
    public void ThrowIfInvalid()
    {
        ArgumentNullException.ThrowIfNull(Deletions);
        Fades?.ThrowIfInvalid();
    }
}
