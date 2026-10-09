namespace EchoCut.Audio;

/// <summary>Sentido de un fundido.</summary>
public enum FadeDirection
{
    /// <summary>Aparición: la ganancia sube de silencio a nivel completo.</summary>
    In,

    /// <summary>Desaparición: la ganancia baja de nivel completo a silencio.</summary>
    Out,
}

/// <summary>
/// Forma de la ganancia a lo largo de un fundido. Son los preajustes de «Adjustable fade» de
/// Audacity (<c>share/nyquist-plug-ins/adjustable-fade.ny</c>); la fórmula de cada uno está en
/// <see cref="FadeShape"/>.
/// </summary>
public enum FadeCurve
{
    /// <summary>Rampa lineal de amplitud: el «Fade In/Out» integrado de Audacity.</summary>
    Linear,

    /// <summary>Exponencial desde −60 dB: avanza lento al principio; a mitad de camino va por −30 dB.</summary>
    Exponential,

    /// <summary>Logarítmica: sube deprisa al principio; a mitad de camino ya está en −3 dB.</summary>
    Logarithmic,

    /// <summary>Redondeada: raíz cuadrada de la rampa lineal, entre la lineal y la logarítmica.</summary>
    Rounded,

    /// <summary>Cuarto de seno: potencia constante, la habitual en fundidos cruzados.</summary>
    Cosine,

    /// <summary>Curva S (coseno elevado): arranque y llegada suaves, sin quiebros en los extremos.</summary>
    SCurve,
}

/// <summary>Un fundido sobre el tramo que señaló el usuario.</summary>
/// <param name="Direction">Si el audio aparece o desaparece.</param>
/// <param name="StartSeconds">Comienzo del tramo, en segundos desde el principio del archivo original.</param>
/// <param name="EndSeconds">Final del tramo, en segundos desde el principio del archivo original.</param>
/// <param name="Curve">Forma de la ganancia dentro del tramo.</param>
/// <remarks>
/// Como en Audacity, el efecto solo toca la selección: lo anterior a una aparición y lo posterior a
/// una desaparición conservan su nivel. Se expresa en tiempo absoluto del original, igual que
/// <see cref="TrimRange"/>, para que mover las marcas de recorte no desplace el fundido.
/// </remarks>
public readonly record struct Fade(FadeDirection Direction, double StartSeconds, double EndSeconds, FadeCurve Curve)
{
    /// <summary>
    /// Duración mínima de un fundido. Audacity exige más de dos muestras; diez milisegundos son
    /// cientos de muestras y quedan muy por debajo de lo que un oído percibe como fundido.
    /// </summary>
    public const double MinimumSeconds = 0.01;

    /// <value>Duración del tramo, en segundos.</value>
    public double DurationSeconds => EndSeconds - StartSeconds;

    /// <summary>Ganancia del fundido en un instante, para dibujarla.</summary>
    /// <param name="seconds">Instante, en segundos desde el principio del archivo original.</param>
    /// <returns>
    /// Ganancia entre 0 y 1; fuera del tramo vale 1, porque el fundido no toca lo que no se seleccionó.
    /// </returns>
    /// <remarks>
    /// Es la versión continua de la envolvente que se aplica al audio, muestra
    /// a muestra.
    /// </remarks>
    public double GainAt(double seconds)
    {
        if (seconds < StartSeconds || seconds > EndSeconds || DurationSeconds <= 0)
        {
            return 1.0;
        }

        double progress = (seconds - StartSeconds) / DurationSeconds;
        return FadeShape.FadeInGain(Curve, Direction == FadeDirection.In ? progress : 1.0 - progress);
    }

    /// <summary>Comprueba que el fundido se pueda aplicar.</summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Se lanza si algún extremo no es finito, si el comienzo es negativo, si dura menos de
    /// <see cref="MinimumSeconds"/> o si la dirección o la curva no son valores conocidos.
    /// </exception>
    public void ThrowIfInvalid()
    {
        if (!Enum.IsDefined(Direction))
        {
            throw new ArgumentOutOfRangeException(nameof(Direction), Direction, "Sentido de fundido desconocido.");
        }

        if (!Enum.IsDefined(Curve))
        {
            throw new ArgumentOutOfRangeException(nameof(Curve), Curve, "Curva de fundido desconocida.");
        }

        if (!double.IsFinite(StartSeconds) || StartSeconds < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(StartSeconds), StartSeconds, "El comienzo del fundido debe ser un número finito y no negativo.");
        }

        if (!double.IsFinite(EndSeconds) || EndSeconds - StartSeconds < MinimumSeconds - 1e-9)
        {
            throw new ArgumentOutOfRangeException(
                nameof(EndSeconds), EndSeconds, $"El fundido debe durar al menos {MinimumSeconds:0.00} s.");
        }
    }

    /// <summary>Si el fundido afecta a alguna parte del tramo que conserva la copia.</summary>
    /// <param name="range">Tramo que conserva la copia.</param>
    /// <returns><c>true</c> si ambos tramos se solapan.</returns>
    public bool Overlaps(TrimRange range) => StartSeconds < range.EndSeconds && EndSeconds > range.StartSeconds;
}
