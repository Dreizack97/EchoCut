using System.Globalization;

namespace EchoCut.Audio;

/// <summary>
/// Elige con qué codificador de FFmpeg se vuelve a escribir una pista en su mismo formato.
/// </summary>
/// <remarks>
/// <para>
/// Un fundido cambia las muestras, así que la copia que lo lleva no puede salir por copia de flujo.
/// Se recodifica al mismo códec para no cambiar extensión ni reproductores compatibles: en los
/// formatos sin pérdida no se pierde nada; en los de compresión con pérdida se paga una generación,
/// a la misma tasa de bits que el original para no inflar ni empobrecer el archivo.
/// </para>
/// <para>
/// El codificador se nombra explícitamente: si se dejara a FFmpeg elegir por el nombre del códec,
/// para Vorbis y Opus tomaría sus codificadores nativos experimentales, que rechaza sin
/// <c>-strict</c> y suenan peor que las bibliotecas de referencia.
/// </para>
/// </remarks>
public static class AudioEncoding
{
    /// <summary>Codificadores con pérdida, por códec de origen, con la tasa de bits de la copia como parámetro.</summary>
    private static readonly Dictionary<string, string> LossyEncoders = new(StringComparer.OrdinalIgnoreCase)
    {
        ["mp3"] = "libmp3lame",
        ["mp2"] = "mp2",
        ["aac"] = "aac",
        ["vorbis"] = "libvorbis",
        ["opus"] = "libopus",
        ["wmav1"] = "wmav1",
        ["wmav2"] = "wmav2",
    };

    /// <summary>Tasa de bits de reserva para un formato con pérdida que no declara la suya.</summary>
    private const long FallbackBitRate = 256_000;

    /// <summary>Argumentos de salida que codifican el audio en el mismo formato que el original.</summary>
    /// <param name="source">Formato del original.</param>
    /// <param name="extension">Extensión del archivo, con punto, para las opciones propias del contenedor.</param>
    /// <returns>Opciones de códec y, si hacen falta, de contenedor, listas para ir antes del destino.</returns>
    /// <exception cref="FFmpegException">Se lanza si FFmpeg no tiene codificador para el códec del original.</exception>
    public static IReadOnlyList<string> ArgumentsFor(AudioStreamInfo source, string extension)
    {
        ArgumentNullException.ThrowIfNull(source);

        List<string> arguments = ["-c:a", .. EncoderFor(source)];

        // Mismas etiquetas ID3 que escribe el recorte por copia, para que ambas salidas se lean igual.
        if (string.Equals(extension, ".mp3", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, ".mp2", StringComparison.OrdinalIgnoreCase))
        {
            arguments.AddRange(["-id3v2_version", "3"]);
        }

        return arguments;
    }

    private static List<string> EncoderFor(AudioStreamInfo source)
    {
        string codec = source.CodecName;

        if (LossyEncoders.TryGetValue(codec, out string? lossy))
        {
            long bitRate = source.BitRate > 0 ? source.BitRate : FallbackBitRate;
            return [lossy, "-b:a", bitRate.ToString(CultureInfo.InvariantCulture)];
        }

        // El PCM se pide con su mismo nombre: el códec ya fija resolución y orden de bytes.
        if (codec.StartsWith("pcm_", StringComparison.OrdinalIgnoreCase))
        {
            return [codec];
        }

        // FLAC, ALAC y WavPack reciben flotantes y FFmpeg elegiría la mayor resolución que admiten;
        // se fija la del original para que un archivo de 16 bits no salga de 24 ocupando más.
        bool deep = source.BitsPerSample > 16;
        return codec.ToLowerInvariant() switch
        {
            "flac" => ["flac", "-sample_fmt", deep ? "s32" : "s16"],
            "alac" => ["alac", "-sample_fmt", deep ? "s32p" : "s16p"],
            "wavpack" => ["wavpack", "-sample_fmt", deep ? "s32p" : "s16p"],
            _ => throw new FFmpegException(
                $"No se puede aplicar un fundido a audio «{codec}»: FFmpeg no sabe volver a codificarlo en ese formato."),
        };
    }
}
