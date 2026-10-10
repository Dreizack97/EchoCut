using System.Globalization;
using System.Text.Json;

namespace EchoCut.Audio;

/// <summary>Formato del primer flujo de audio de un archivo, tal como lo describe ffprobe.</summary>
/// <param name="CodecName">Nombre del códec según FFmpeg, por ejemplo <c>mp3</c>, <c>flac</c> o <c>pcm_s16le</c>.</param>
/// <param name="SampleRate">Frecuencia de muestreo, en Hz.</param>
/// <param name="Channels">Número de canales.</param>
/// <param name="BitRate">Tasa de bits del flujo o, si no la declara, la del contenedor; 0 si no se conoce.</param>
/// <param name="BitsPerSample">Resolución de las muestras originales; 0 si el códec no la declara (los de compresión con pérdida).</param>
/// <remarks>
/// Recodificar exige conocer el formato del original: el PCM que se entrega al codificador va sin
/// cabecera, y la copia debe salir con el mismo códec, frecuencia, canales y calidad.
/// </remarks>
public sealed record AudioStreamInfo(string CodecName, int SampleRate, int Channels, long BitRate, int BitsPerSample)
{
    /// <summary>Consulta a ffprobe el formato del primer flujo de audio.</summary>
    /// <param name="ffprobePath">Ruta absoluta a <c>ffprobe.exe</c>, ya resuelta.</param>
    /// <param name="filePath">Archivo a consultar.</param>
    /// <param name="cancellationToken">Token de cancelación para el proceso de ffprobe.</param>
    /// <returns>El formato del flujo.</returns>
    /// <remarks>
    /// Se pide JSON y no el formato de texto plano porque <c>bit_rate</c> aparece tanto en el flujo
    /// como en el contenedor, y solo así se distinguen sin depender del orden de las líneas.
    /// </remarks>
    /// <exception cref="FFmpegException">
    /// Se lanza si ffprobe termina con error, si el archivo no tiene audio o si la respuesta no
    /// incluye frecuencia y canales utilizables.
    /// </exception>
    public static async Task<AudioStreamInfo> ProbeAsync(string ffprobePath, string filePath, CancellationToken cancellationToken)
    {
        string[] arguments =
        [
            "-v", "error",
            "-select_streams", "a:0",
            "-show_entries", "stream=codec_name,sample_rate,channels,bit_rate,bits_per_raw_sample,bits_per_sample:format=bit_rate",
            "-of", "json",
            filePath,
        ];

        FFmpegResult result = await FFmpegRunner.RunAsync(ffprobePath, arguments, cancellationToken).ConfigureAwait(false);
        if (result.ExitCode != 0)
        {
            throw new FFmpegException(
                $"ffprobe no pudo leer el archivo: {FFmpegRunner.FirstLine(result.StandardError)}",
                result.ExitCode,
                result.StandardError);
        }

        return Parse(result.StandardOutput);
    }

    /// <summary>Interpreta la respuesta JSON de ffprobe.</summary>
    /// <param name="json">Salida de ffprobe con <c>-of json</c>.</param>
    /// <returns>El formato del flujo.</returns>
    /// <remarks>
    /// ffprobe entrega las cifras como cadenas y escribe <c>N/A</c> o las omite cuando no las conoce;
    /// cualquiera de esos casos se trata como 0.
    /// </remarks>
    /// <exception cref="FFmpegException">Se lanza si no hay flujo de audio o le faltan frecuencia o canales.</exception>
    public static AudioStreamInfo Parse(string json)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement root = document.RootElement;

            if (!root.TryGetProperty("streams", out JsonElement streams)
                || streams.ValueKind != JsonValueKind.Array
                || streams.GetArrayLength() == 0)
            {
                throw new FFmpegException("El archivo no contiene ningún flujo de audio.");
            }

            JsonElement stream = streams[0];
            int sampleRate = (int)Number(stream, "sample_rate");
            int channels = (int)Number(stream, "channels");
            if (sampleRate <= 0 || channels <= 0)
            {
                throw new FFmpegException("ffprobe no informó la frecuencia o los canales del audio.");
            }

            long bitRate = Number(stream, "bit_rate");
            if (bitRate <= 0 && root.TryGetProperty("format", out JsonElement format))
            {
                bitRate = Number(format, "bit_rate");
            }

            long bits = Number(stream, "bits_per_raw_sample");
            if (bits <= 0)
            {
                bits = Number(stream, "bits_per_sample");
            }

            string codec = stream.TryGetProperty("codec_name", out JsonElement name) ? name.GetString() ?? string.Empty : string.Empty;
            return new AudioStreamInfo(codec, sampleRate, channels, bitRate, (int)bits);
        }
        catch (JsonException exception)
        {
            throw new FFmpegException($"ffprobe devolvió una respuesta no válida: {exception.Message}");
        }
    }

    private static long Number(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out JsonElement value))
        {
            return 0;
        }

        return value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetInt64(out long number) => number,
            JsonValueKind.String when long.TryParse(value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out long parsed) => parsed,
            _ => 0,
        };
    }
}
