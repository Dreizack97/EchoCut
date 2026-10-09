namespace EchoCut;

/// <summary>Convierte una combinación de teclas en el texto con el que se anuncia.</summary>
/// <remarks>
/// <see cref="KeysConverter"/> devuelve los nombres de .NET («Delete», «Return», «Oemcomma»), que no
/// coinciden con lo rotulado en un teclado en español; aquí se usan los nombres de la tecla física.
/// </remarks>
public static class ShortcutText
{
    /// <summary>Texto de una combinación, por ejemplo «Ctrl+Shift+O» o «Supr».</summary>
    /// <param name="keys">Tecla con sus modificadores.</param>
    /// <returns>Los modificadores y la tecla unidos con «+».</returns>
    public static string Of(Keys keys)
    {
        List<string> parts = [];

        if (keys.HasFlag(Keys.Control))
        {
            parts.Add("Ctrl");
        }

        if (keys.HasFlag(Keys.Shift))
        {
            parts.Add("Shift");
        }

        if (keys.HasFlag(Keys.Alt))
        {
            parts.Add("Alt");
        }

        parts.Add((keys & Keys.KeyCode) switch
        {
            Keys.Enter => "Entrar",
            Keys.Escape => "Esc",
            Keys.Delete => "Supr",
            Keys.Home => "Inicio",
            Keys.End => "Fin",
            Keys.Left => "←",
            Keys.Right => "→",
            Keys.Oemcomma => ",",
            >= Keys.D0 and <= Keys.D9 and var digit => ((int)(digit - Keys.D0)).ToString(System.Globalization.CultureInfo.InvariantCulture),
            var other => other.ToString(),
        });

        return string.Join("+", parts);
    }
}
