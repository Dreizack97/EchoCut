using EchoCut.Audio;
using EchoCut.Processing;

namespace EchoCut;

/// <summary>Lo que el editor de forma de onda toma de la ventana principal para trabajar.</summary>
/// <param name="Waveforms">Servicio que carga las formas de onda.</param>
/// <param name="Analysis">Servicio que analiza el silencio, también sobre el resultado editado.</param>
/// <param name="FFmpegPath">Ruta de FFmpeg para escuchar.</param>
/// <param name="PreviewSeconds">Segundos que se escuchan de cada borde.</param>
/// <param name="Options">
/// Parámetros vigentes del análisis, con la tolerancia que haya en la ventana principal en el
/// momento de pedirlos: el usuario puede cambiarla con el editor abierto.
/// </param>
public sealed record WaveformEditorServices(
    WaveformService Waveforms,
    AnalysisService Analysis,
    string FFmpegPath,
    double PreviewSeconds,
    Func<SilenceOptions> Options);
