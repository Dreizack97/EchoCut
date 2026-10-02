namespace EchoCut.Audio;

/// <summary>Tramo del archivo original que conserva la copia recortada.</summary>
/// <param name="StartSeconds">
/// Instante en el que empieza la copia, en segundos desde el principio del archivo. Cero conserva
/// el inicio intacto.
/// </param>
/// <param name="EndSeconds">Instante en el que termina la copia, en segundos desde el principio del archivo.</param>
/// <remarks>
/// Se expresa en tiempo absoluto del original —no como inicio más duración— porque así lo producen
/// el análisis y el ajuste manual: dos marcas sobre la misma línea de tiempo que se mueven por
/// separado sin que mover una desplace a la otra.
/// </remarks>
public readonly record struct TrimRange(double StartSeconds, double EndSeconds)
{
    /// <value>Duración de la copia, en segundos.</value>
    public double DurationSeconds => EndSeconds - StartSeconds;

    /// <value>
    /// <c>true</c> si la copia descarta parte del inicio. Recortar el inicio con copia de flujo
    /// cambia el primer paquete que ve el decodificador, así que solo se pide cuando hace falta.
    /// </value>
    public bool TrimsStart => StartSeconds > 0;

    /// <summary>Tramo que conserva el inicio y corta solo el final.</summary>
    /// <param name="endSeconds">Instante de corte final, en segundos desde el principio del archivo.</param>
    /// <returns>Tramo de <c>0</c> a <paramref name="endSeconds"/>.</returns>
    public static TrimRange EndingAt(double endSeconds) => new(0.0, endSeconds);

    /// <summary>Comprueba que el tramo se pueda pasar a FFmpeg.</summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Se lanza si algún extremo no es finito, si el inicio es negativo o si el final no queda
    /// estrictamente después del inicio. Un tramo vacío produciría un archivo sin audio.
    /// </exception>
    public void ThrowIfInvalid()
    {
        if (!double.IsFinite(StartSeconds) || StartSeconds < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(StartSeconds), StartSeconds, "El inicio del recorte debe ser un número finito y no negativo.");
        }

        if (!double.IsFinite(EndSeconds) || EndSeconds <= StartSeconds)
        {
            throw new ArgumentOutOfRangeException(
                nameof(EndSeconds), EndSeconds, "El final del recorte debe ser un número finito posterior al inicio.");
        }
    }
}
