namespace EchoCut.Audio;

/// <summary>
/// Resuelve las rutas de <c>ffmpeg.exe</c> y <c>ffprobe.exe</c>. Prioriza una ruta guardada por el
/// usuario y, si no hay, recorre el PATH del sistema. No distribuye binarios ni los descarga.
/// </summary>
/// <remarks>
/// El estado (<see cref="FFmpegPath"/> y <see cref="FFprobePath"/>) solo se asigna cuando ambos
/// ejecutables existen a la vez: no hay un estado intermedio en el que uno de los dos esté resuelto
/// y el otro no, lo que simplifica todo el código que consulta <see cref="IsResolved"/>.
/// </remarks>
public sealed class FFmpegLocator
{
    private const string FFmpegExecutable = "ffmpeg.exe";
    private const string FFprobeExecutable = "ffprobe.exe";

    /// <summary>Ruta absoluta a <c>ffmpeg.exe</c>, una vez resuelta.</summary>
    /// <value>Ruta del ejecutable, o <c>null</c> si aún no se ha localizado un par válido.</value>
    public string? FFmpegPath { get; private set; }

    /// <summary>Ruta absoluta a <c>ffprobe.exe</c>, una vez resuelta.</summary>
    /// <value>Ruta del ejecutable, o <c>null</c> si aún no se ha localizado un par válido.</value>
    public string? FFprobePath { get; private set; }

    /// <summary>Si ya se localizó un par válido de ejecutables.</summary>
    /// <value><c>true</c> cuando tanto <see cref="FFmpegPath"/> como <see cref="FFprobePath"/> tienen valor.</value>
    public bool IsResolved => FFmpegPath is not null && FFprobePath is not null;

    /// <summary>
    /// Intenta resolver los binarios probando primero la carpeta preferida y, si falla, el PATH.
    /// </summary>
    /// <param name="preferredDirectory">
    /// Carpeta que el usuario eligió en una sesión anterior, o <c>null</c> si no hay ninguna
    /// recordada. Se prueba antes que el PATH porque representa una elección explícita del usuario.
    /// </param>
    /// <returns><c>true</c> si se resolvió un par válido de ejecutables; <c>false</c> en caso contrario.</returns>
    public bool TryResolve(string? preferredDirectory)
    {
        if (TryUseDirectory(preferredDirectory))
        {
            return true;
        }

        string? fromPath = FindInPath(FFmpegExecutable);
        return fromPath is not null && TryUseDirectory(Path.GetDirectoryName(fromPath));
    }

    /// <summary>Acepta la carpeta que contiene ambos ejecutables, o la ruta directa a ffmpeg.exe.</summary>
    /// <param name="directoryOrExecutable">
    /// Carpeta candidata, o ruta completa a <c>ffmpeg.exe</c> dentro de ella; en ese segundo caso se
    /// usa su carpeta contenedora. Un valor vacío o solo espacios se rechaza sin más comprobaciones.
    /// </param>
    /// <returns>
    /// <c>true</c> si la carpeta resultante contiene tanto <c>ffmpeg.exe</c> como <c>ffprobe.exe</c>,
    /// en cuyo caso <see cref="FFmpegPath"/> y <see cref="FFprobePath"/> quedan asignados;
    /// <c>false</c> en caso contrario, sin modificar el estado previo.
    /// </returns>
    public bool TryUseDirectory(string? directoryOrExecutable)
    {
        if (string.IsNullOrWhiteSpace(directoryOrExecutable))
        {
            return false;
        }

        string directory = File.Exists(directoryOrExecutable)
            ? Path.GetDirectoryName(directoryOrExecutable) ?? string.Empty
            : directoryOrExecutable;

        string ffmpeg = Path.Combine(directory, FFmpegExecutable);
        string ffprobe = Path.Combine(directory, FFprobeExecutable);

        if (!File.Exists(ffmpeg) || !File.Exists(ffprobe))
        {
            return false;
        }

        FFmpegPath = ffmpeg;
        FFprobePath = ffprobe;
        return true;
    }

    /// <summary>Exige que los ejecutables ya estén resueltos y devuelve sus rutas.</summary>
    /// <returns>Tupla con las rutas de <c>ffmpeg.exe</c> y <c>ffprobe.exe</c>.</returns>
    /// <exception cref="FFmpegException">
    /// Se lanza si <see cref="IsResolved"/> es <c>false</c>, con un mensaje pensado para mostrarse
    /// directamente al usuario e indicarle qué hacer.
    /// </exception>
    public (string FFmpeg, string FFprobe) Require()
    {
        if (FFmpegPath is null || FFprobePath is null)
        {
            throw new FFmpegException(
                "No se encontró FFmpeg. Indica la carpeta que contiene ffmpeg.exe y ffprobe.exe.");
        }

        return (FFmpegPath, FFprobePath);
    }

    /// <summary>Busca <paramref name="executable"/> en las carpetas listadas en la variable PATH.</summary>
    /// <param name="executable">Nombre del ejecutable a buscar, con extensión.</param>
    /// <returns>Ruta completa del primer ejecutable encontrado, o <c>null</c> si no aparece en el PATH.</returns>
    private static string? FindInPath(string executable)
    {
        string? path = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }

        foreach (string directory in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            string candidate;
            try
            {
                candidate = Path.Combine(directory.Trim('"'), executable);
            }
            catch (ArgumentException)
            {
                // Una entrada del PATH con caracteres inválidos no debe abortar la búsqueda.
                continue;
            }

            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }
}
