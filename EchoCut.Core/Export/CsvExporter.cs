using EchoCut.Library;
using EchoCut.Processing;
using System.Globalization;
using System.Text;

namespace EchoCut.Export;

/// <summary>Una fila del informe: la pista, lo que dio el análisis y cómo quedó.</summary>
/// <param name="Track">Metadatos de la pista.</param>
/// <param name="Analysis">Resultado del análisis, o <c>null</c> si no llegó a analizarse.</param>
/// <param name="Status">Estado final, tal como se muestra en la columna «Estado».</param>
/// <param name="ErrorMessage">Detalle del error, o cadena vacía si no hubo ninguno.</param>
public sealed record TrackRecord(
    TrackInfo Track,
    TrackAnalysis? Analysis,
    string Status,
    string ErrorMessage);

/// <summary>Vuelca los resultados a CSV.</summary>
/// <remarks>
/// El separador es el de la lista del sistema y el archivo se escribe en UTF-8 con BOM, que es lo
/// que hace que Excel lo abra directamente en la configuración regional del usuario, sin pasar por
/// el asistente de importación.
/// </remarks>
public static class CsvExporter
{
    /// <summary>Codificación con la que debe escribirse el CSV para que Excel lo abra sin asistente.</summary>
    /// <value>UTF-8 con marca de orden de bytes.</value>
    public static Encoding Encoding { get; } = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);

    private static readonly string[] Headers =
    [
        "Nombre", "Título", "Artista", "Álbum", "Duración (s)", "Ext.", "Bitrate", "Tamaño",
        "Silencio (s)", "Recorte (s)", "Corte (s)", "Recortable", "Fundido", "Toca fondo",
        "Umbral (dBFS)", "Piso de ruido (dBFS)", "Nivel de programa (dBFS)",
        "Fondo de cola (dBFS)", "Pico (dBFS)",
        "Análisis (ms)", "Decodificado (s)", "Factor tiempo real", "Estado", "Error",
    ];

    /// <summary>Construye el contenido completo del CSV, cabecera incluida.</summary>
    /// <param name="records">Filas a exportar, en el orden en que deben aparecer.</param>
    /// <returns>El texto del CSV.</returns>
    public static string Build(IEnumerable<TrackRecord> records)
    {
        string separator = CultureInfo.CurrentCulture.TextInfo.ListSeparator;
        StringBuilder builder = new();

        builder.AppendLine(string.Join(separator, Headers));

        foreach (TrackRecord record in records)
        {
            TrackInfo track = record.Track;
            TrackAnalysis? analysis = record.Analysis;

            builder.AppendLine(string.Join(separator,
                Escape(track.Name, separator),
                Escape(track.Title, separator),
                Escape(track.Artist, separator),
                Escape(track.Album, separator),
                TrackFormat.Number(analysis?.DurationSeconds ?? track.DurationSeconds),
                Escape(track.Extension, separator),
                Escape(TrackFormat.Bitrate(track.BitrateKbps), separator),
                Escape(TrackFormat.Size(track.SizeBytes), separator),
                TrackFormat.Number(analysis?.SilenceSeconds),
                TrackFormat.Number(analysis?.CropSeconds),
                TrackFormat.Number(analysis?.CutSeconds),
                Flag(analysis?.ShouldTrim),
                Flag(analysis?.FadeDetected),
                Flag(analysis?.ReachesSilenceFloor),
                TrackFormat.Number(analysis?.ThresholdDbfs),
                TrackFormat.Number(analysis?.NoiseFloorDbfs),
                TrackFormat.Number(analysis?.ProgramLevelDbfs),
                TrackFormat.Number(analysis?.TailFloorDbfs),
                TrackFormat.Number(analysis?.PeakDbfs),
                TrackFormat.Number(analysis?.AnalysisMilliseconds),
                TrackFormat.Number(analysis?.DecodedSeconds),
                TrackFormat.Number(analysis?.RealTimeFactor),
                Escape(record.Status, separator),
                Escape(record.ErrorMessage, separator)));
        }

        return builder.ToString();
    }

    private static string Flag(bool? value) => value is true ? "Sí" : "No";

    /// <summary>
    /// Entrecomilla un campo solo cuando hace falta.
    /// </summary>
    /// <remarks>
    /// Entrecomillarlo todo sería válido, pero un CSV donde cada celda va entre comillas es mucho
    /// menos legible cuando alguien lo abre en un editor de texto en vez de en una hoja de cálculo.
    /// </remarks>
    private static string Escape(string value, string separator)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        if (!value.Contains(separator, StringComparison.Ordinal)
            && value.IndexOfAny(['"', '\n', '\r']) < 0)
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }
}
