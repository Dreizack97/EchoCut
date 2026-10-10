namespace EchoCut.Audio;

/// <summary>Calidad con la que se codifica un MP3, como los preajustes habituales de LAME.</summary>
/// <remarks>
/// Los de tasa variable reparten los bits donde la música los necesita: a igual calidad percibida
/// ocupan menos que los de tasa constante, y por eso V0 es el valor por defecto. Los de tasa
/// constante existen porque algunos reproductores antiguos y algunos servicios los exigen.
/// </remarks>
public enum Mp3Quality
{
    /// <summary>Tasa variable V0 (unos 245 kbps): transparente para casi cualquier oído.</summary>
    VbrV0,

    /// <summary>Tasa variable V2 (unos 190 kbps): muy buena calidad con menos tamaño.</summary>
    VbrV2,

    /// <summary>Tasa constante de 320 kbps: la máxima del formato.</summary>
    Cbr320,

    /// <summary>Tasa constante de 192 kbps: compatible con todo y de tamaño contenido.</summary>
    Cbr192,
}

/// <summary>Traducción de cada <see cref="Mp3Quality"/> a opciones de FFmpeg y a texto.</summary>
public static class Mp3QualityExtensions
{
    /// <summary>Opciones del codificador <c>libmp3lame</c> para una calidad.</summary>
    /// <param name="quality">Calidad elegida.</param>
    /// <returns>
    /// <c>-q:a</c> para las de tasa variable, que es como FFmpeg expone el <c>-V</c> de LAME, y
    /// <c>-b:a</c> para las de tasa constante.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">Se lanza si la calidad no es un valor conocido.</exception>
    public static string[] EncoderArguments(this Mp3Quality quality) => quality switch
    {
        Mp3Quality.VbrV0 => ["-q:a", "0"],
        Mp3Quality.VbrV2 => ["-q:a", "2"],
        Mp3Quality.Cbr320 => ["-b:a", "320k"],
        Mp3Quality.Cbr192 => ["-b:a", "192k"],
        _ => throw new ArgumentOutOfRangeException(nameof(quality), quality, "Calidad de MP3 desconocida."),
    };

    /// <summary>Nombre de una calidad tal como se presenta al usuario.</summary>
    /// <param name="quality">Calidad a nombrar.</param>
    /// <returns>Nombre corto con la tasa aproximada.</returns>
    public static string DisplayName(this Mp3Quality quality) => quality switch
    {
        Mp3Quality.VbrV0 => "VBR V0 (~245 kbps)",
        Mp3Quality.VbrV2 => "VBR V2 (~190 kbps)",
        Mp3Quality.Cbr320 => "CBR 320 kbps",
        Mp3Quality.Cbr192 => "CBR 192 kbps",
        _ => quality.ToString(),
    };
}
