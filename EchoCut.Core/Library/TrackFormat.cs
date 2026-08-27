using System.Globalization;

namespace EchoCut.Library;

/// <summary>
/// Convierte las magnitudes de una pista en el texto con el que se presentan.
/// </summary>
/// <remarks>
/// Vive en el núcleo, y no en la interfaz, porque hay dos consumidores —la rejilla y el CSV— y
/// tenerlo en un solo sitio es lo que garantiza que la exportación diga exactamente lo mismo que
/// se ve en pantalla. Es formateo de datos con la cultura del sistema, no dibujo: no arrastra
/// ninguna dependencia de escritorio.
/// </remarks>
public static class TrackFormat
{
    /// <summary>Formatea una duración para mostrarla.</summary>
    /// <param name="duration">Duración a formatear.</param>
    /// <returns>Duración en formato <c>mm:ss</c>.</returns>
    public static string Duration(TimeSpan duration) => duration.ToString(@"mm\:ss");

    /// <summary>Formatea un bitrate para mostrarlo.</summary>
    /// <param name="kbps">Bitrate en kbps.</param>
    /// <returns>Bitrate con su unidad, por ejemplo <c>320 Kbps</c>.</returns>
    public static string Bitrate(int kbps) => string.Join(' ', kbps, "Kbps");

    /// <summary>Formatea un tamaño de archivo para mostrarlo.</summary>
    /// <param name="bytes">Tamaño en bytes.</param>
    /// <returns>Tamaño en megabytes con dos decimales y su unidad, por ejemplo <c>4,20 MB</c>.</returns>
    public static string Size(long bytes) =>
        string.Join(' ', (bytes / (1024.0 * 1024.0)).ToString("N2"), "MB");

    /// <summary>Formatea un número para el CSV, dejando vacías las medidas que no existen.</summary>
    /// <param name="value">Valor a formatear, o <c>null</c> si no hay medida.</param>
    /// <returns>Número con hasta tres decimales en la cultura actual, o cadena vacía si es <c>null</c>.</returns>
    public static string Number(double? value) =>
        value is null ? string.Empty : value.Value.ToString("0.###", CultureInfo.CurrentCulture);
}
