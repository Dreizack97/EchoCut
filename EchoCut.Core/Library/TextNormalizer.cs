using System.Globalization;
using System.Text;

namespace EchoCut.Library;

/// <summary>
/// Proporciona utilidades para la normalización de texto en metadatos y nombres de pistas de audio.
/// </summary>
public static class TextNormalizer
{
    /// <summary>
    /// Elimina acentos diacríticos (á, é, í, ó, ú, ü, etc.) de una cadena de texto, preservando la letra «ñ» y «Ñ».
    /// </summary>
    /// <param name="text">Cadena de texto a procesar.</param>
    /// <returns>Cadena de texto sin acentos diacríticos.</returns>
    public static string RemoveAccents(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return text ?? string.Empty;
        }

        string normalized = text.Normalize(NormalizationForm.FormD);
        StringBuilder builder = new(normalized.Length);

        for (int i = 0; i < normalized.Length; i++)
        {
            char c = normalized[i];
            UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(c);

            if (category == UnicodeCategory.NonSpacingMark)
            {
                // Conservar la virgulilla diacrítica de la ñ / Ñ (U+0303)
                if (c == '\u0303' && builder.Length > 0 && (builder[^1] == 'n' || builder[^1] == 'N'))
                {
                    builder.Append(c);
                }

                continue;
            }

            builder.Append(c);
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    /// <summary>
    /// Normaliza una cadena de texto eliminando acentos diacríticos (preservando «ñ» y «Ñ»)
    /// y convirtiendo cada palabra a formato TitleCase.
    /// </summary>
    /// <param name="text">Cadena de texto a normalizar.</param>
    /// <param name="culture">Cultura de texto opcional; si no se indica, usa <see cref="CultureInfo.CurrentCulture"/>.</param>
    /// <returns>Cadena normalizada en TitleCase sin acentos.</returns>
    public static string NormalizeTitleCase(string? text, CultureInfo? culture = null)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return text ?? string.Empty;
        }

        string withoutAccents = RemoveAccents(text.Trim());
        CultureInfo targetCulture = culture ?? CultureInfo.CurrentCulture;

        return targetCulture.TextInfo.ToTitleCase(withoutAccents.ToLower(targetCulture));
    }
}
