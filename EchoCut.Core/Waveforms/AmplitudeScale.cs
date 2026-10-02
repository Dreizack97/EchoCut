namespace EchoCut.Waveforms;

/// <summary>Cómo se reparte la altura de la vista entre las amplitudes.</summary>
public enum AmplitudeScale
{
    /// <summary>Altura proporcional a la amplitud, de −1 a 1. Es la vista habitual de Audacity.</summary>
    Linear,

    /// <summary>
    /// Altura proporcional al nivel en dB dentro de <see cref="AmplitudeScaleExtensions.DecibelRange"/>.
    /// Agranda lo que en lineal parece una línea plana: colas de fundido, hiss y silencios casi inaudibles.
    /// </summary>
    Decibels,
}

/// <summary>Conversión de amplitudes a alturas según la escala.</summary>
public static class AmplitudeScaleExtensions
{
    /// <summary>
    /// Nivel más bajo representable en la escala en dB; lo que queda por debajo se dibuja en el
    /// centro. Es el valor por defecto de la vista «Forma de onda (dB)» de Audacity.
    /// </summary>
    public const double DecibelRange = 60.0;

    /// <summary>Altura relativa de una amplitud: <c>1</c> en el borde superior, <c>−1</c> en el inferior y <c>0</c> en el centro.</summary>
    /// <param name="scale">Escala de la vista.</param>
    /// <param name="amplitude">Amplitud de la muestra, normalmente entre −1 y 1.</param>
    /// <returns>Altura relativa acotada a <c>[−1, 1]</c>, con el signo de la amplitud.</returns>
    /// <remarks>
    /// En dB se conserva el signo para que la figura siga siendo simétrica como en lineal:
    /// <c>signo(a) · max(0, 1 + 20·log10|a| / rango)</c>, la misma fórmula de Audacity.
    /// </remarks>
    public static double ToHeight(this AmplitudeScale scale, double amplitude)
    {
        if (scale == AmplitudeScale.Linear)
        {
            return Math.Clamp(amplitude, -1.0, 1.0);
        }

        double magnitude = Math.Abs(amplitude);
        if (magnitude <= 0)
        {
            return 0.0;
        }

        double height = Math.Clamp(1.0 + (20.0 * Math.Log10(magnitude) / DecibelRange), 0.0, 1.0);
        return Math.CopySign(height, amplitude);
    }
}
