using System.Globalization;

namespace EchoCut.Loudness;

/// <summary>
/// Lee y escribe los campos con los que MP3Gain anota sus cambios, para que EchoCut y MP3Gain
/// puedan deshacer los cambios del otro.
/// </summary>
/// <remarks>
/// <para>
/// <c>MP3GAIN_UNDO</c> vive en la etiqueta APE con el formato <c>+003,+003,N</c>: los pasos que
/// deshacen el cambio en el canal izquierdo y en el derecho, y si se aplicó con desbordamiento
/// (<c>W</c>) o acotado (<c>N</c>). <c>MP3GAIN_MINMAX</c> guarda el rango de <c>global_gain</c>.
/// </para>
/// <para>
/// Los valores ReplayGain se escriben en todas las etiquetas que los admiten. Se corrigen tras cada
/// cambio porque un reproductor que los aplica sobre un audio ya modificado sumaría la ganancia
/// dos veces.
/// </para>
/// </remarks>
internal static class Mp3GainTags
{
    private const string UndoKey = "MP3GAIN_UNDO";
    private const string MinMaxKey = "MP3GAIN_MINMAX";

    /// <summary>Pasos anotados para deshacer los cambios previos.</summary>
    /// <param name="file">Archivo abierto con TagLibSharp.</param>
    /// <returns>Los pasos que deshacen el cambio; 0 si no hay ninguno anotado.</returns>
    /// <exception cref="InvalidDataException">
    /// Se lanza si el cambio anotado es distinto por canal o se aplicó con desbordamiento: EchoCut
    /// siempre cambia los dos canales a la vez y acotando, así que no podría deshacerlo con exactitud.
    /// </exception>
    public static int ReadUndo(TagLib.File file)
    {
        if (file.GetTag(TagLib.TagTypes.Ape, create: false) is not TagLib.Ape.Tag ape
            || ape.GetItem(UndoKey)?.ToString() is not { Length: > 0 } value)
        {
            return 0;
        }

        string[] parts = value.Split(',', StringSplitOptions.TrimEntries);
        if (parts.Length < 2
            || !int.TryParse(parts[0], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int left)
            || !int.TryParse(parts[1], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int right))
        {
            throw new InvalidDataException($"La etiqueta {UndoKey} («{value}») no es legible.");
        }

        if (left != right || (parts.Length > 2 && parts[2].Equals("W", StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidDataException($"Otro programa cambió el volumen de cada canal por separado o con desbordamiento ({UndoKey} «{value}»), y no se puede restaurar con exactitud.");
        }

        return left;
    }

    /// <summary>Acumula pasos en <c>MP3GAIN_UNDO</c>; si el total vuelve a 0, quita el campo.</summary>
    /// <param name="file">Archivo abierto con TagLibSharp.</param>
    /// <param name="undoSteps">Pasos que deshacen el cambio recién aplicado.</param>
    public static void AddUndo(TagLib.File file, int undoSteps)
    {
        int total = ReadUndo(file) + undoSteps;
        TagLib.Ape.Tag ape = Ape(file);

        if (total == 0)
        {
            ape.RemoveItem(UndoKey);
            return;
        }

        string steps = total.ToString("+000;-000", CultureInfo.InvariantCulture);
        ape.SetValue(UndoKey, $"{steps},{steps},N");
    }

    /// <summary>Anota el rango de <c>global_gain</c> tras el cambio.</summary>
    /// <param name="file">Archivo abierto con TagLibSharp.</param>
    /// <param name="scan">Resumen de las tramas ya modificadas.</param>
    public static void SetMinMax(TagLib.File file, Mp3GainScan scan) =>
        Ape(file).SetValue(MinMaxKey, string.Create(CultureInfo.InvariantCulture, $"{scan.MinGain:000},{scan.MaxGain:000}"));

    /// <summary>Escribe la ganancia y el pico de pista que quedan tras aplicar el cambio.</summary>
    /// <param name="file">Archivo abierto con TagLibSharp.</param>
    /// <param name="measured">Medida previa al cambio.</param>
    /// <param name="steps">Pasos aplicados.</param>
    /// <remarks>
    /// La ganancia de pista sigue refiriéndose a 89 dB aunque el objetivo elegido sea otro, como en
    /// MP3Gain: es lo que esperan los reproductores. La de álbum, si la hay, solo se desplaza.
    /// </remarks>
    public static void SetTrackGain(TagLib.File file, ReplayGainResult measured, int steps)
    {
        TagLib.Tag tag = file.Tag;
        tag.ReplayGainTrackGain = Math.Round(measured.GainDb - (steps * Mp3GainEditor.StepDecibels), 2);
        tag.ReplayGainTrackPeak = Math.Round(measured.Peak * AmplitudeOf(steps), 6);

        if (!double.IsNaN(tag.ReplayGainAlbumGain))
        {
            tag.ReplayGainAlbumGain = Math.Round(tag.ReplayGainAlbumGain - (steps * Mp3GainEditor.StepDecibels), 2);
        }

        if (!double.IsNaN(tag.ReplayGainAlbumPeak))
        {
            tag.ReplayGainAlbumPeak = Math.Round(tag.ReplayGainAlbumPeak * AmplitudeOf(steps), 6);
        }
    }

    /// <summary>Desplaza los valores ReplayGain que ya existan tras cambiar el audio.</summary>
    /// <param name="file">Archivo abierto con TagLibSharp.</param>
    /// <param name="steps">Pasos aplicados.</param>
    public static void ShiftReplayGain(TagLib.File file, int steps)
    {
        TagLib.Tag tag = file.Tag;
        double gain = steps * Mp3GainEditor.StepDecibels;
        double amplitude = AmplitudeOf(steps);

        if (!double.IsNaN(tag.ReplayGainTrackGain))
        {
            tag.ReplayGainTrackGain = Math.Round(tag.ReplayGainTrackGain - gain, 2);
        }

        if (!double.IsNaN(tag.ReplayGainTrackPeak))
        {
            tag.ReplayGainTrackPeak = Math.Round(tag.ReplayGainTrackPeak * amplitude, 6);
        }

        if (!double.IsNaN(tag.ReplayGainAlbumGain))
        {
            tag.ReplayGainAlbumGain = Math.Round(tag.ReplayGainAlbumGain - gain, 2);
        }

        if (!double.IsNaN(tag.ReplayGainAlbumPeak))
        {
            tag.ReplayGainAlbumPeak = Math.Round(tag.ReplayGainAlbumPeak * amplitude, 6);
        }
    }

    /// <summary>Factor de amplitud de un número de pasos: 2^(pasos/4).</summary>
    private static double AmplitudeOf(int steps) => Math.Pow(2.0, steps / 4.0);

    private static TagLib.Ape.Tag Ape(TagLib.File file) =>
        (TagLib.Ape.Tag)file.GetTag(TagLib.TagTypes.Ape, create: true);
}
