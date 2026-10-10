namespace EchoCut.Library;

/// <summary>Extensiones que EchoCut considera audio analizable.</summary>
/// <remarks>
/// La lista es la que TagLibSharp sabe leer y FFmpeg sabe decodificar a la vez: un formato que
/// una de las dos no maneje se contaría como archivo encontrado y luego fallaría al analizarlo.
/// </remarks>
public static class AudioFileTypes
{
    private static readonly HashSet<string> Supported = new(StringComparer.OrdinalIgnoreCase)
    {
        ".aa", ".aax", ".aac", ".aiff", ".ape", ".dsf", ".flac", ".m4a", ".m4b", ".m4p", ".mp2", ".mp3",
        ".mpc", ".mpp", ".ogg", ".oga", ".wav", ".wma", ".wv", ".webm",
    };

    /// <summary>Extensiones admitidas, en minúsculas y con el punto incluido.</summary>
    /// <value>Colección de extensiones reconocidas.</value>
    public static IReadOnlyCollection<string> Extensions => Supported;

    /// <summary>Indica si la ruta tiene una extensión de audio admitida.</summary>
    /// <param name="filePath">Ruta del archivo a comprobar.</param>
    /// <returns><c>true</c> si la extensión está en <see cref="Extensions"/>, ignorando mayúsculas.</returns>
    public static bool IsSupported(string filePath) => Supported.Contains(Path.GetExtension(filePath));
}
