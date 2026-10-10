namespace EchoCut.Audio;

/// <summary>
/// Ganancia de cada curva de fundido, portada de los preajustes de «Adjustable fade» de Audacity
/// (<c>share/nyquist-plug-ins/adjustable-fade.ny</c>).
/// </summary>
/// <remarks>
/// <para>
/// Todas las curvas de Audacity son simétricas: la desaparición es la aparición recorrida al revés,
/// <c>gOut(x) = gIn(1 − x)</c>. Por eso basta con describir la aparición; quien aplica el fundido
/// invierte el progreso.
/// </para>
/// <para>
/// Las fórmulas son las que resultan de evaluar el código Nyquist con el preajuste correspondiente,
/// sin la maquinaria de ajuste de punto medio que solo usa el modo personalizado.
/// </para>
/// </remarks>
public static class FadeShape
{
    /// <summary>
    /// Nivel del que parte la curva exponencial: −60 dB, el <c>(log-exp-curve -60 0)</c> de Audacity.
    /// Una exponencial nunca llega a cero, así que se reescala para que el primer punto sea silencio.
    /// </summary>
    private static readonly double ExponentialBase = Math.Pow(10, -60.0 / 20.0);

    /// <summary>
    /// Base de la curva logarítmica: +15.311 dB, el <c>(log-exp-curve 15.311 0)</c> de Audacity. Es
    /// el valor que deja la mitad del fundido exactamente en −3 dB (potencia mitad), simétrico de
    /// la exponencial respecto de la rampa lineal.
    /// </summary>
    private static readonly double LogarithmicBase = Math.Pow(10, 15.311 / 20.0);

    /// <summary>Ganancia de una aparición tras recorrer la fracción indicada del tramo.</summary>
    /// <param name="curve">Forma del fundido.</param>
    /// <param name="progress">Fracción recorrida: 0 al comienzo, 1 al final. Se acota a ese intervalo.</param>
    /// <returns>Ganancia de amplitud entre 0 (silencio) y 1 (nivel original).</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se lanza si <paramref name="curve"/> no es un valor conocido.</exception>
    public static double FadeInGain(FadeCurve curve, double progress)
    {
        double x = Math.Clamp(progress, 0.0, 1.0);

        return curve switch
        {
            // (linear 0 1) → pwlv 0 1 1.
            FadeCurve.Linear => x,

            // (pwev b 1 1) escalado a [0, 1]: b^(1 − x) recorre de b a 1 en progresión geométrica.
            FadeCurve.Exponential => GeometricRamp(ExponentialBase, x),
            FadeCurve.Logarithmic => GeometricRamp(LogarithmicBase, x),

            // (simple-curve 0 1 0.5): la rampa lineal elevada a 0.5.
            FadeCurve.Rounded => Math.Sqrt(x),

            // (cosine-curve 0 1): un cuarto de periodo de seno a lo largo del tramo.
            FadeCurve.Cosine => Math.Sin(x * Math.PI / 2.0),

            // (raised-cos 0 1 0.0): medio periodo de coseno elevado, 0.5·(1 − cos πx).
            FadeCurve.SCurve => 0.5 * (1.0 - Math.Cos(x * Math.PI)),

            _ => throw new ArgumentOutOfRangeException(nameof(curve), curve, "Curva de fundido desconocida."),
        };
    }

    /// <summary>
    /// Rampa geométrica de <paramref name="b"/> a 1 reescalada a [0, 1]:
    /// <c>(b^(1 − x) − b) / (1 − b)</c>, la normalización de <c>log-exp-curve</c>.
    /// </summary>
    /// <remarks>
    /// Con <c>b &lt; 1</c> la curva arranca despacio (exponencial); con <c>b &gt; 1</c> arranca
    /// deprisa (logarítmica). En ambos casos empieza en 0 y termina en 1; se acota porque el
    /// redondeo de <see cref="Math.Pow"/> puede dejar en los extremos un −0.0000001 que, multiplicado
    /// por la muestra, le invertiría la fase.
    /// </remarks>
    private static double GeometricRamp(double b, double x) =>
        Math.Clamp((Math.Pow(b, 1.0 - x) - b) / (1.0 - b), 0.0, 1.0);
}
