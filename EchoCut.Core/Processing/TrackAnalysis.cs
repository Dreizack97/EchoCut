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
/// <param name="Leading">Silencio del principio y decisión sobre él.</param>
/// <param name="Trailing">Silencio del final y decisión sobre él.</param>
/// <param name="ThresholdDbfs">Umbral efectivo aplicado al final.</param>
/// <param name="NoiseFloorDbfs">Piso de ruido estimado en el final.</param>
/// <param name="ProgramLevelDbfs">Nivel del pasaje más sonoro del final analizado.</param>
/// <param name="TailFloorDbfs">Nivel mediano del tramo final.</param>
/// <param name="PeakDbfs">Pico de muestra del final analizado.</param>
/// <param name="AnalysisMilliseconds">Tiempo de reloj que tardó el análisis.</param>
/// <param name="DecodedSeconds">Segundos de audio efectivamente decodificados.</param>
public sealed record TrackAnalysis(
    double DurationSeconds,
    EdgeTrim Leading,
    EdgeTrim Trailing,
    double ThresholdDbfs,
    double NoiseFloorDbfs,
    double ProgramLevelDbfs,
    double TailFloorDbfs,
    double PeakDbfs,
    double AnalysisMilliseconds,
    double DecodedSeconds)
{
    /// <summary>Instante en el que empieza la copia recortada.</summary>
    /// <value>Segundos desde el principio del archivo; <c>0</c> si el principio no se recorta.</value>
    public double StartSeconds => Leading.ShouldTrim ? Leading.RemovedSeconds : 0.0;

    /// <summary>Instante en el que termina la copia recortada.</summary>
    /// <value>Segundos desde el principio del archivo; la duración completa si el final no se recorta.</value>
    public double EndSeconds => Trailing.ShouldTrim
        ? Math.Clamp(DurationSeconds - Trailing.RemovedSeconds, StartSeconds, DurationSeconds)
        : DurationSeconds;

    /// <summary>Tramo del original que conserva la copia recortada.</summary>
    /// <value>De <see cref="StartSeconds"/> a <see cref="EndSeconds"/>.</value>
    public TrimRange Range => new(StartSeconds, EndSeconds);

    /// <summary>Si procede escribir una copia recortada.</summary>
    /// <value><c>true</c> si al menos uno de los dos bordes se recorta.</value>
    public bool ShouldTrim => Leading.ShouldTrim || Trailing.ShouldTrim;

    /// <summary>Segundos que se eliminarían al recortar, sumando ambos bordes.</summary>
    /// <value>Duración eliminada, redondeada a centésimas.</value>
    public double CropSeconds => Math.Round(DurationSeconds - Range.DurationSeconds, 2);

    /// <summary>Segundos de audio procesados por segundo de reloj.</summary>
    /// <value>Cociente entre lo decodificado y lo que costó, o <c>null</c> si el análisis no midió tiempo.</value>
    public double? RealTimeFactor =>
        AnalysisMilliseconds > 0 ? DecodedSeconds / (AnalysisMilliseconds / 1000.0) : null;

    /// <summary>Rehace la decisión de ambos bordes con otros parámetros, sin volver a decodificar.</summary>
    /// <param name="options">Parámetros vigentes, con la tolerancia elegida en la ventana principal.</param>
    /// <returns>El mismo análisis con los cortes y las decisiones recalculados.</returns>
    /// <remarks>
    /// Si el análisis del principio está desactivado, su borde deja de recortarse aunque se hubiera
    /// medido; si se activa sobre un análisis hecho sin él, no hay medida y no se recorta hasta
    /// volver a analizar.
    /// </remarks>
    public TrackAnalysis WithOptions(SilenceOptions options) => this with
    {
        Leading = options.TrimLeadingSilence
            ? Leading.Reconsider(options.MinLeadingSilenceSeconds, options)
            : Leading with { ShouldTrim = false },
        Trailing = Trailing.Reconsider(options.MinSilenceSeconds, options),
    };

    /// <summary>Proyecta el informe crudo del analizador a sus cifras presentables.</summary>
    /// <param name="report">Informe devuelto por <see cref="SilenceAnalyzer"/>.</param>
    /// <returns>El mismo resultado, redondeado.</returns>
    public static TrackAnalysis From(AnalysisReport report)
    {
        SilenceResult trailing = report.Trailing;

        return new TrackAnalysis(
            DurationSeconds: report.DurationSeconds,
            Leading: report.Leading is { } leading ? EdgeTrim.From(leading) : default,
            Trailing: EdgeTrim.From(trailing),
            ThresholdDbfs: Math.Round(trailing.EffectiveThresholdDbfs, 1),
            NoiseFloorDbfs: Math.Round(trailing.NoiseFloorDbfs, 1),
            ProgramLevelDbfs: Math.Round(trailing.ProgramLevelDbfs, 1),
            TailFloorDbfs: Math.Round(trailing.TailFloorDbfs, 1),
            PeakDbfs: Math.Round(trailing.PeakDbfs, 1),
            AnalysisMilliseconds: report.ElapsedMilliseconds,
            DecodedSeconds: Math.Round(report.DecodedSeconds, 2));
    }
}
