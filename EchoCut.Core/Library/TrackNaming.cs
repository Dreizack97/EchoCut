using System.Globalization;

namespace EchoCut.Library;

/// <summary>
/// Reglas para componer el nombre nuevo de una pista antes de renombrarla.
/// </summary>
/// <remarks>
/// Son funciones puras sobre el nombre sin extensión, aparte de <see cref="TrackEditor"/>, para
/// que la vista previa del diálogo y el renombrado real calculen exactamente lo mismo: lo que el
/// usuario ve antes de aceptar es lo que acabará en disco.
/// </remarks>
public static class TrackNaming
{
    /// <summary>Separador por defecto entre el consecutivo y el nombre.</summary>
    public const string DefaultSeparator = " - ";

    /// <summary>Caracteres que se consideran separadores sobrantes al principio de un nombre.</summary>
    /// <remarks>
    /// Quitar un prefijo numérico como «100 - » suele dejar restos de su separador si no se
    /// cuentan bien los caracteres; estos son los que se usan para separar en los nombres de pista.
    /// </remarks>
    private static readonly char[] LeadingSeparators = [' ', '-', '_', '.', '–', '—'];

    /// <summary>Indica por qué un nombre de archivo, sin extensión, no es válido.</summary>
    /// <param name="name">Nombre propuesto; se juzga sin los espacios de los extremos.</param>
    /// <returns>La explicación del problema, o <c>null</c> si el nombre es válido.</returns>
    /// <remarks>
    /// Windows descarta en silencio los puntos finales de un nombre, así que «Título.» acabaría
    /// renombrado como «Título» y la ruta calculada ya no existiría.
    /// </remarks>
    public static string? GetProblem(string? name)
    {
        string cleanName = name?.Trim() ?? string.Empty;

        if (cleanName.Length == 0)
        {
            return "El nombre del archivo no puede estar vacío.";
        }

        if (cleanName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            return "El nombre del archivo contiene caracteres no válidos.";
        }

        return cleanName.EndsWith('.') ? "El nombre del archivo no puede terminar en punto." : null;
    }

    /// <summary>Antepone un número consecutivo con ceros a la izquierda.</summary>
    /// <param name="name">Nombre actual, sin extensión.</param>
    /// <param name="number">Número a anteponer; debe ser mayor o igual que cero.</param>
    /// <param name="digits">Dígitos mínimos del número, completados con ceros.</param>
    /// <param name="separator">Texto entre el número y el nombre.</param>
    /// <returns>El nombre con el consecutivo delante, por ejemplo «0001 - Artista - Título».</returns>
    /// <exception cref="ArgumentOutOfRangeException">Si <paramref name="number"/> es negativo o <paramref name="digits"/> no está entre 1 y 9.</exception>
    /// <remarks>
    /// Los ceros a la izquierda hacen que el orden alfabético del Explorador o de un reproductor
    /// coincida con el numérico: sin ellos, «10» quedaría antes que «2».
    /// </remarks>
    public static string WithSequence(string name, int number, int digits, string separator)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentOutOfRangeException.ThrowIfNegative(number);
        ArgumentOutOfRangeException.ThrowIfLessThan(digits, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(digits, 9);

        return number.ToString($"D{digits}", CultureInfo.InvariantCulture) + (separator ?? string.Empty) + name;
    }

    /// <summary>Quita una cantidad de caracteres del principio del nombre.</summary>
    /// <param name="name">Nombre actual, sin extensión.</param>
    /// <param name="count">Caracteres a quitar; si supera la longitud, el resultado queda vacío.</param>
    /// <param name="trimSeparators">Si además se quitan los espacios y separadores que queden al principio.</param>
    /// <returns>El nombre recortado; puede quedar vacío, y en ese caso no es un nombre válido.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Si <paramref name="count"/> es negativo.</exception>
    public static string WithoutLeading(string name, int count, bool trimSeparators)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        string result = count >= name.Length ? string.Empty : name[count..];
        return trimSeparators ? result.TrimStart(LeadingSeparators) : result;
    }
}
