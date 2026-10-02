using EchoCut.Audio;

namespace EchoCut.Processing;

/// <summary>El silencio medido en uno de los bordes de una pista y lo que se decidió hacer con él.</summary>
/// <param name="SilenceSeconds">Silencio detectado entre el borde del archivo y la música, redondeado a centésimas.</param>
/// <param name="RemovedSeconds">Segundos que se eliminarían en este borde, medidos desde el propio borde.</param>
/// <param name="ShouldTrim">Si procede recortar este borde.</param>
/// <param name="FadeDetected">Si la música entra o sale con un fundido regular junto al silencio.</param>
/// <param name="ReachesSilenceFloor">Si el silencio se hunde o se estabiliza lo bastante como para ser silencio real.</param>
/// <remarks>
/// Guarda las medidas que no dependen de la tolerancia para poder rehacer la decisión al moverla
/// sin volver a decodificar. El valor por defecto describe un borde sin silencio que no se recorta,
/// que es lo que corresponde al principio cuando su análisis está desactivado.
/// </remarks>
public readonly record struct EdgeTrim(
    double SilenceSeconds,
    double RemovedSeconds,
    bool ShouldTrim,
    bool FadeDetected,
    bool ReachesSilenceFloor)
{
    /// <summary>Toma la decisión del detector para un borde.</summary>
    /// <param name="result">Resultado del detector sobre ese borde.</param>
    /// <returns>El borde con las medidas y la decisión del detector, incluido el afinado del corte.</returns>
    public static EdgeTrim From(SilenceResult result) => new(
        SilenceSeconds: Math.Round(result.SilenceSeconds, 2),
        RemovedSeconds: result.SavedSeconds,
        ShouldTrim: result.ShouldTrim,
        FadeDetected: result.FadeDetected,
        ReachesSilenceFloor: result.ReachesSilenceFloor);

    /// <summary>Rehace la decisión con otros parámetros a partir de las medidas ya tomadas.</summary>
    /// <param name="minimumSilenceSeconds">Silencio mínimo exigido en este borde.</param>
    /// <param name="options">Parámetros del algoritmo, de los que se toman la tolerancia, la guarda de fundido y el ahorro mínimo.</param>
    /// <returns>El mismo borde con la cantidad a eliminar y la decisión recalculadas.</returns>
    /// <remarks>
    /// Aplica las mismas reglas que <see cref="SilenceDetector"/> salvo el afinado del corte a la
    /// frontera más silenciosa, que necesitaría la curva de nivel: el corte queda exactamente a la
    /// tolerancia de la música, como mucho a <see cref="SilenceOptions.CutSearchSeconds"/> de donde
    /// lo habría dejado un análisis nuevo.
    /// </remarks>
    public EdgeTrim Reconsider(double minimumSilenceSeconds, SilenceOptions options)
    {
        double keptSeconds = options.ToleranceSeconds + (FadeDetected ? options.FadeGuardSeconds : 0.0);
        double removedSeconds = Math.Max(0.0, SilenceSeconds - keptSeconds);

        return this with
        {
            RemovedSeconds = removedSeconds,
            ShouldTrim = SilenceSeconds >= minimumSilenceSeconds
                         && removedSeconds >= options.MinSavingsSeconds
                         && ReachesSilenceFloor,
        };
    }
}
