namespace EchoCut.Audio;

/// <summary>Tramo del archivo que se va a reproducir, en segundos desde el principio.</summary>
/// <param name="StartSeconds">Instante de inicio de la ventana, en segundos desde el principio del archivo.</param>
/// <param name="DurationSeconds">Duración de la ventana, en segundos.</param>
public readonly record struct PreviewWindow(double StartSeconds, double DurationSeconds)
{
    /// <value>Instante en el que termina la ventana, en segundos desde el principio del archivo.</value>
    public double EndSeconds => StartSeconds + DurationSeconds;
}

/// <summary>
/// Decide qué tramo de una pista tiene sentido escuchar para juzgar su recorte.
/// </summary>
/// <remarks>
/// Es una función pura y vive aparte del reproductor precisamente para poder comprobarla sin tarjeta
/// de sonido ni FFmpeg: toda la aritmética delicada —el acotado a la duración real, el caso de la
/// pista más corta que la propia ventana— está aquí.
/// </remarks>
public static class AudioPreview
{
    /// <summary>
    /// Música que se incluye antes del punto de corte. Es el contexto mínimo para juzgar si el corte
    /// suena abrupto: con menos no se llega a reconocer la frase musical que queda interrumpida.
    /// </summary>
    public const double ContextSeconds = 3.0;

    /// <summary>
    /// Duración de la previsualización de una pista todavía sin analizar, donde no hay punto de
    /// corte que enseñar y lo único que cabe ofrecer es su final.
    /// </summary>
    public const double UnanalyzedSeconds = 5.0;

    /// <summary>
    /// Calcula el tramo a reproducir.
    /// </summary>
    /// <param name="totalDurationSeconds">Duración completa del archivo.</param>
    /// <param name="cutSeconds">Instante de corte calculado, o <c>null</c> si no se ha analizado.</param>
    /// <param name="silenceSeconds">Silencio final detectado, o <c>null</c> si no se ha analizado.</param>
    /// <param name="toleranceSeconds">Silencio que el recorte conserva.</param>
    /// <param name="contextSeconds">Duración de la música previa al corte a incluir en la previsualización.</param>
    /// <remarks>
    /// En una pista analizada la ventana <em>termina en el punto de corte</em>, de modo que lo que
    /// suena es exactamente el final que tendrá la copia recortada. Reproducir desde el principio
    /// obligaría a esperar minutos para llegar a lo único que este programa decide, y reproducir el
    /// silencio que se elimina no diría nada sobre si el corte queda bien.
    /// </remarks>
    /// <returns>
    /// Ventana a reproducir, acotada a <c>[0, totalDurationSeconds]</c>. Es vacía (duración cero) si
    /// <paramref name="totalDurationSeconds"/> no es positiva.
    /// </returns>
    public static PreviewWindow WindowFor(
        double totalDurationSeconds,
        double? cutSeconds,
        double? silenceSeconds,
        double toleranceSeconds,
        double contextSeconds = ContextSeconds)
    {
        if (totalDurationSeconds <= 0)
        {
            return new PreviewWindow(0, 0);
        }

        double context = Math.Max(0.1, contextSeconds);

        if (cutSeconds is not { } cut || cut <= 0)
        {
            double duration = Math.Max(context, UnanalyzedSeconds);
            double start = Math.Max(0.0, totalDurationSeconds - duration);
            return new PreviewWindow(start, totalDurationSeconds - start);
        }

        cut = Math.Min(cut, totalDurationSeconds);

        // El silencio conservado va incluido en la ventana: forma parte del final resultante y es lo
        // que permite oír que el corte no llega pisando la música.
        double kept = Math.Max(0.0, Math.Min(toleranceSeconds, silenceSeconds ?? toleranceSeconds));
        double desired = context + kept;

        double startSeconds = Math.Max(0.0, cut - desired);
        return new PreviewWindow(startSeconds, cut - startSeconds);
    }
}
