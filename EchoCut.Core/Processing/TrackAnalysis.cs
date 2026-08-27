using EchoCut.Audio;

namespace EchoCut.Processing;

/// <summary>
/// El resultado del análisis de una pista, ya redondeado a las cifras que tienen sentido enseñar.
/// </summary>
/// <remarks>
/// Existe para que el redondeo ocurra una sola vez. La rejilla y el CSV muestran las mismas
/// medidas, y cuando cada uno redondeaba por su cuenta bastaba con tocar un sitio para que la
/// exportación dejara de coincidir con lo que el usuario tenía delante.
/// </remarks>
/// <param name="DurationSeconds">Duración real del archivo medida durante el análisis.</param>
/// <param name="SilenceSeconds">Silencio final detectado, en segundos.</param>
/// <param name="CropSeconds">Segundos que se eliminarían al recortar.</param>
/// <param name="CutSeconds">Instante de corte, en segundos desde el principio del archivo.</param>
/// <param name="ShouldTrim">Si procede recortar la pista.</param>
/// <param name="FadeDetected">Si se detectó un fundido de salida antes del silencio.</param>
/// <param name="ReachesSilenceFloor">Si la cola se hunde lo suficiente como para ser silencio real.</param>
/// <param name="ThresholdDbfs">Umbral efectivo aplicado.</param>
/// <param name="NoiseFloorDbfs">Piso de ruido estimado.</param>
/// <param name="ProgramLevelDbfs">Nivel del pasaje más sonoro analizado.</param>
/// <param name="TailFloorDbfs">Nivel mediano del tramo final.</param>
/// <param name="PeakDbfs">Pico de muestra de la porción analizada.</param>
/// <param name="AnalysisMilliseconds">Tiempo de reloj que tardó el análisis.</param>
/// <param name="DecodedSeconds">Segundos de audio efectivamente decodificados.</param>
public sealed record TrackAnalysis(
    double DurationSeconds,
    double SilenceSeconds,
    double CropSeconds,
    double CutSeconds,
    bool ShouldTrim,
    bool FadeDetected,
    bool ReachesSilenceFloor,
    double ThresholdDbfs,
    double NoiseFloorDbfs,
    double ProgramLevelDbfs,
    double TailFloorDbfs,
    double PeakDbfs,
    double AnalysisMilliseconds,
    double DecodedSeconds)
{
    /// <summary>Segundos de audio procesados por segundo de reloj.</summary>
    /// <value>Cociente entre lo decodificado y lo que costó, o <c>null</c> si el análisis no midió tiempo.</value>
    public double? RealTimeFactor =>
        AnalysisMilliseconds > 0 ? DecodedSeconds / (AnalysisMilliseconds / 1000.0) : null;

    /// <summary>Proyecta el informe crudo del analizador a sus cifras presentables.</summary>
    /// <param name="report">Informe devuelto por <see cref="SilenceAnalyzer"/>.</param>
    /// <returns>El mismo resultado, redondeado.</returns>
    public static TrackAnalysis From(AnalysisReport report)
    {
        SilenceResult result = report.Result;

        return new TrackAnalysis(
            DurationSeconds: report.DurationSeconds,
            SilenceSeconds: Math.Round(result.TrailingSilenceSeconds, 2),
            CropSeconds: Math.Round(result.SavedSeconds, 2),
            CutSeconds: result.CutSeconds,
            ShouldTrim: result.ShouldTrim,
            FadeDetected: result.FadeDetected,
            ReachesSilenceFloor: result.ReachesSilenceFloor,
            ThresholdDbfs: Math.Round(result.EffectiveThresholdDbfs, 1),
            NoiseFloorDbfs: Math.Round(result.NoiseFloorDbfs, 1),
            ProgramLevelDbfs: Math.Round(result.ProgramLevelDbfs, 1),
            TailFloorDbfs: Math.Round(result.TailFloorDbfs, 1),
            PeakDbfs: Math.Round(result.PeakDbfs, 1),
            AnalysisMilliseconds: report.ElapsedMilliseconds,
            DecodedSeconds: Math.Round(report.DecodedSeconds, 2));
    }
}
