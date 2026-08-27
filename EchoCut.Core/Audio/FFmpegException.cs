namespace EchoCut.Audio;

/// <summary>Error devuelto por un proceso de FFmpeg, con el stderr capturado como diagnóstico.</summary>
/// <remarks>
/// Es la excepción que atraviesa toda la capa <c>Audio/</c>: la lanzan tanto los fallos de proceso
/// (código de salida distinto de cero) como las condiciones de precondición que detecta el propio
/// código antes de invocar a FFmpeg, como no encontrar los ejecutables o que el destino de un
/// recorte coincida con el origen.
/// </remarks>
public sealed class FFmpegException : Exception
{
    /// <summary>Crea la excepción con el mensaje, el código de salida y el stderr capturado.</summary>
    /// <param name="message">Mensaje accionable, pensado para mostrarse tal cual al usuario.</param>
    /// <param name="exitCode">
    /// Código de salida del proceso, o <c>0</c> cuando la excepción no proviene de un proceso
    /// terminado (por ejemplo, una precondición que falló antes de lanzarlo).
    /// </param>
    /// <param name="standardError">
    /// Salida de error completa del proceso, o <c>null</c> si no hay ninguna que adjuntar.
    /// </param>
    public FFmpegException(string message, int exitCode = 0, string? standardError = null)
        : base(message)
    {
        ExitCode = exitCode;
        StandardError = standardError ?? string.Empty;
    }

    /// <summary>Código de salida del proceso de FFmpeg que originó el error.</summary>
    /// <value>Código de salida del proceso, o <c>0</c> si la excepción no proviene de un proceso.</value>
    public int ExitCode { get; }

    /// <summary>Contenido íntegro de la salida de error estándar del proceso.</summary>
    /// <value>Texto de <c>stderr</c> capturado, o cadena vacía si no había ninguno que registrar.</value>
    public string StandardError { get; }
}
