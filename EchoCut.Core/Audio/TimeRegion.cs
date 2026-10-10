namespace EchoCut.Audio;

/// <summary>Un tramo de tiempo del archivo original, como una selección de Audacity.</summary>
/// <param name="StartSeconds">Comienzo del tramo, en segundos desde el principio del archivo original.</param>
/// <param name="EndSeconds">Final del tramo, en segundos desde el principio del archivo original.</param>
/// <remarks>
/// A diferencia de <see cref="TrimRange"/>, que es lo que se conserva, esto es un tramo cualquiera:
/// la selección del editor o un fragmento borrado. Comparte con él la referencia de tiempo absoluta
/// del original para que ninguna edición desplace a las demás.
/// </remarks>
public readonly record struct TimeRegion(double StartSeconds, double EndSeconds)
{
    /// <value>Duración del tramo, en segundos.</value>
    public double DurationSeconds => EndSeconds - StartSeconds;

    /// <summary>Si un instante cae dentro del tramo.</summary>
    /// <param name="seconds">Instante, en segundos del original.</param>
    /// <returns><c>true</c> si está entre el comienzo (incluido) y el final (excluido).</returns>
    public bool Contains(double seconds) => seconds >= StartSeconds && seconds < EndSeconds;

    /// <summary>Si dos tramos comparten algún instante.</summary>
    /// <param name="other">Tramo con el que comparar.</param>
    /// <returns><c>true</c> si se solapan; dos tramos que solo se tocan no se solapan.</returns>
    public bool Overlaps(TimeRegion other) => StartSeconds < other.EndSeconds && EndSeconds > other.StartSeconds;

    /// <summary>Comprueba que el tramo sea utilizable.</summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Se lanza si algún extremo no es finito, si el comienzo es negativo o si el final no queda
    /// estrictamente después del comienzo.
    /// </exception>
    public void ThrowIfInvalid()
    {
        if (!double.IsFinite(StartSeconds) || StartSeconds < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(StartSeconds), StartSeconds, "El comienzo del tramo debe ser un número finito y no negativo.");
        }

        if (!double.IsFinite(EndSeconds) || EndSeconds <= StartSeconds)
        {
            throw new ArgumentOutOfRangeException(
                nameof(EndSeconds), EndSeconds, "El final del tramo debe ser un número finito posterior al comienzo.");
        }
    }
}
