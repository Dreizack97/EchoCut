namespace EchoCut.Library;

/// <summary>
/// Una pista tal y como está en disco, antes de analizarla: ruta y metadatos, sin nada de la
/// interfaz ni del resultado del algoritmo.
/// </summary>
/// <remarks>
/// Es un registro inmutable a propósito. Es el objeto que los servicios del lote reciben y
/// devuelven en sus notificaciones de progreso, y esos servicios corren en paralelo: si la pista
/// fuera mutable, cada notificación tendría que preocuparse por quién más la está tocando.
/// </remarks>
/// <param name="FilePath">Ruta absoluta del archivo original.</param>
/// <param name="Name">Nombre del archivo, sin extensión.</param>
/// <param name="Title">Título de los metadatos, o cadena vacía si el archivo no lo tiene.</param>
/// <param name="Artist">Artista de los metadatos, o cadena vacía si el archivo no lo tiene.</param>
/// <param name="Album">Álbum de los metadatos, o cadena vacía si el archivo no lo tiene.</param>
/// <param name="Duration">Duración declarada por los metadatos.</param>
/// <param name="Extension">Extensión del archivo, con el punto incluido.</param>
/// <param name="BitrateKbps">Bitrate declarado por los metadatos, en kbps.</param>
/// <param name="SizeBytes">Tamaño del archivo en disco, en bytes.</param>
public sealed record TrackInfo(
    string FilePath,
    string Name,
    string Title,
    string Artist,
    string Album,
    TimeSpan Duration,
    string Extension,
    int BitrateKbps,
    long SizeBytes)
{
    /// <summary>Duración declarada por los metadatos, en segundos.</summary>
    /// <value>Segundos de <see cref="Duration"/>.</value>
    public double DurationSeconds => Duration.TotalSeconds;

    /// <summary>Carpeta que contiene el archivo.</summary>
    /// <value>Carpeta contenedora, o cadena vacía si la ruta no tiene ninguna.</value>
    public string Directory => Path.GetDirectoryName(FilePath) ?? string.Empty;
}
