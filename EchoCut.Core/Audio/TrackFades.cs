namespace EchoCut.Audio;

/// <summary>Los fundidos de una pista: como mucho uno de aparición y uno de desaparición.</summary>
/// <param name="FadeIn">Aparición, o <c>null</c> si no hay.</param>
/// <param name="FadeOut">Desaparición, o <c>null</c> si no hay.</param>
/// <remarks>
/// Es inmutable porque viaja con la petición de recorte a los hilos del lote, igual que
/// <see cref="TrimRange"/>.
/// </remarks>
public sealed record TrackFades(Fade? FadeIn, Fade? FadeOut)
{
    /// <value><c>true</c> si hay al menos un fundido.</value>
    public bool HasAny => FadeIn is not null || FadeOut is not null;

    /// <summary>Ganancia combinada en un instante, para dibujarla.</summary>
    /// <param name="seconds">Instante, en segundos desde el principio del archivo original.</param>
    /// <returns>Producto de las ganancias de ambos fundidos; 1 fuera de los dos.</returns>
    /// <remarks>
    /// Si los tramos se solapan, las ganancias se multiplican: es lo que se obtendría en Audacity
    /// aplicando un efecto detrás del otro.
    /// </remarks>
    public double GainAt(double seconds) =>
        (FadeIn?.GainAt(seconds) ?? 1.0) * (FadeOut?.GainAt(seconds) ?? 1.0);

    /// <summary>Comprueba que cada fundido sea válido y esté en su casilla.</summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Se lanza si algún fundido no es válido o si su sentido no corresponde a la propiedad que lo
    /// contiene.
    /// </exception>
    public void ThrowIfInvalid()
    {
        if (FadeIn is { } fadeIn)
        {
            fadeIn.ThrowIfInvalid();
            if (fadeIn.Direction != FadeDirection.In)
            {
                throw new ArgumentOutOfRangeException(nameof(FadeIn), fadeIn.Direction, "La aparición debe tener sentido de entrada.");
            }
        }

        if (FadeOut is { } fadeOut)
        {
            fadeOut.ThrowIfInvalid();
            if (fadeOut.Direction != FadeDirection.Out)
            {
                throw new ArgumentOutOfRangeException(nameof(FadeOut), fadeOut.Direction, "La desaparición debe tener sentido de salida.");
            }
        }
    }

    /// <summary>Descarta los fundidos que caen enteros fuera de lo que conserva la copia.</summary>
    /// <param name="range">Tramo que conserva la copia.</param>
    /// <returns>
    /// Los fundidos que afectan a la copia, o <c>null</c> si no queda ninguno. Un fundido que el
    /// recorte corta a medias se conserva entero: su curva sigue midiéndose sobre el tramo que eligió
    /// el usuario, así que la copia arranca o termina a la ganancia que le toca en ese punto.
    /// </returns>
    /// <remarks>
    /// Importa porque un fundido obliga a recodificar: si ninguno llega a la copia, se puede seguir
    /// recortando por copia de flujo, sin pérdida.
    /// </remarks>
    public TrackFades? Within(TrimRange range)
    {
        Fade? fadeIn = FadeIn is { } i && i.Overlaps(range) ? i : null;
        Fade? fadeOut = FadeOut is { } o && o.Overlaps(range) ? o : null;

        return fadeIn is null && fadeOut is null ? null : new TrackFades(fadeIn, fadeOut);
    }
}
