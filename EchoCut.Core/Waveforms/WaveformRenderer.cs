using EchoCut.Audio;

namespace EchoCut.Waveforms;

/// <summary>Pinta un tramo de una <see cref="Waveform"/> en un búfer de píxeles ARGB de 32 bits.</summary>
/// <remarks>
/// Escribe en memoria y no en un <c>Bitmap</c> para que el motor siga libre de <c>System.Drawing</c>:
/// la interfaz solo envuelve el búfer en una imagen. Como Audacity, cada columna de píxeles rellena
/// del mínimo al máximo de su intervalo y, encima, la banda del nivel eficaz; tomar los extremos y no
/// una muestra suelta es lo que impide que reducir la vista borre un clic dentro del silencio.
/// </remarks>
public static class WaveformRenderer
{
    /// <summary>Pinta el tramo indicado ocupando todo el búfer.</summary>
    /// <param name="waveform">Forma de onda de origen.</param>
    /// <param name="pixels">Destino, fila a fila de arriba abajo; al menos <paramref name="stride"/> × <paramref name="height"/> píxeles.</param>
    /// <param name="width">Anchura de la imagen, en píxeles.</param>
    /// <param name="height">Altura de la imagen, en píxeles.</param>
    /// <param name="stride">Píxeles entre el comienzo de una fila y el de la siguiente.</param>
    /// <param name="startSeconds">Instante del archivo en el borde izquierdo.</param>
    /// <param name="endSeconds">Instante del archivo en el borde derecho.</param>
    /// <param name="scale">Escala vertical.</param>
    /// <param name="palette">Colores.</param>
    /// <param name="edits">
    /// Ediciones con las que dibujar la onda, o <c>null</c> para dibujar el original tal cual. Los
    /// fundidos atenúan cada columna con la ganancia de su instante.
    /// </param>
    /// <param name="collapseDeletions">
    /// Si el tramo se expresa en la línea de tiempo del resultado: lo borrado desaparece y cada
    /// columna junta los tramos del original que suenan en ella, como la copia.
    /// </param>
    /// <remarks>
    /// El resultado se dibuja a partir del resumen del original, sin decodificar otra vez: cambiar un
    /// fundido o borrar algo solo cuesta repintar.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Se lanza si las dimensiones no son positivas, si <paramref name="stride"/> es menor que la
    /// anchura o si el tramo de tiempo está vacío.
    /// </exception>
    /// <exception cref="ArgumentException">Se lanza si el búfer es menor que la imagen.</exception>
    public static void Render(
        Waveform waveform,
        Span<uint> pixels,
        int width,
        int height,
        int stride,
        double startSeconds,
        double endSeconds,
        AmplitudeScale scale,
        WaveformPalette palette,
        AudioEdits? edits = null,
        bool collapseDeletions = false)
    {
        ArgumentNullException.ThrowIfNull(waveform);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        ArgumentOutOfRangeException.ThrowIfLessThan(stride, width);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(endSeconds, startSeconds);

        if (pixels.Length < (long)stride * height)
        {
            throw new ArgumentException("El búfer de píxeles es menor que la imagen.", nameof(pixels));
        }

        int center = RowFor(0.0, height);
        double secondsPerPixel = (endSeconds - startSeconds) / width;

        for (int x = 0; x < width; x++)
        {
            double t0 = startSeconds + (x * secondsPerPixel);
            FillColumn(pixels, x, 0, height - 1, stride, palette.Background);
            SetPixel(pixels, x, center, stride, palette.CenterLine);

            if (!TrySummarize(waveform, t0, t0 + secondsPerPixel, edits, collapseDeletions, out WaveformPeak peak))
            {
                continue;
            }

            int top = RowFor(scale.ToHeight(peak.Max), height);
            int bottom = RowFor(scale.ToHeight(peak.Min), height);
            FillColumn(pixels, x, Math.Min(top, bottom), Math.Max(top, bottom), stride, palette.Peak);

            // La banda RMS es simétrica y nunca sobresale de los picos: en una columna asimétrica
            // (un transitorio, un desplazamiento de continua) se recorta a lo que ocupan.
            int rmsTop = Math.Max(top, RowFor(scale.ToHeight(peak.Rms), height));
            int rmsBottom = Math.Min(bottom, RowFor(-scale.ToHeight(peak.Rms), height));
            if (rmsBottom >= rmsTop)
            {
                FillColumn(pixels, x, rmsTop, rmsBottom, stride, palette.Rms);
            }
        }
    }

    /// <summary>Fila que corresponde a una altura relativa, con <c>1</c> en la fila superior.</summary>
    /// <summary>Envolvente de una columna, con las ediciones aplicadas.</summary>
    /// <remarks>
    /// Cada tramo del original se atenúa con la ganancia de su punto medio: dentro de una columna la
    /// ganancia apenas cambia, y así un fundido se ve tal como sonará. Al juntar tramos, los extremos
    /// se toman de todos y el nivel eficaz se pondera por la duración de cada uno.
    /// </remarks>
    private static bool TrySummarize(
        Waveform waveform,
        double fromSeconds,
        double toSeconds,
        AudioEdits? edits,
        bool collapseDeletions,
        out WaveformPeak peak)
    {
        if (edits is null)
        {
            return waveform.TrySummarize(fromSeconds, toSeconds, out peak);
        }

        IEnumerable<TimeRegion> spans = collapseDeletions
            ? edits.Deletions.SourceSpans(fromSeconds, toSeconds)
            : [new TimeRegion(fromSeconds, toSeconds)];

        float min = float.MaxValue;
        float max = float.MinValue;
        double energy = 0.0;
        double seconds = 0.0;

        foreach (TimeRegion span in spans)
        {
            if (!waveform.TrySummarize(span.StartSeconds, span.EndSeconds, out WaveformPeak part))
            {
                continue;
            }

            float gain = (float)(edits.Fades?.GainAt((span.StartSeconds + span.EndSeconds) / 2.0) ?? 1.0);
            min = Math.Min(min, part.Min * gain);
            max = Math.Max(max, part.Max * gain);
            energy += part.Rms * gain * part.Rms * gain * span.DurationSeconds;
            seconds += span.DurationSeconds;
        }

        if (seconds <= 0.0)
        {
            peak = default;
            return false;
        }

        peak = new WaveformPeak(min, max, (float)Math.Sqrt(energy / seconds));
        return true;
    }

    private static int RowFor(double height, int rows) =>
        Math.Clamp((int)Math.Round((1.0 - height) * (rows - 1) / 2.0), 0, rows - 1);

    private static void FillColumn(Span<uint> pixels, int x, int fromRow, int toRow, int stride, uint color)
    {
        for (int y = fromRow; y <= toRow; y++)
        {
            pixels[(y * stride) + x] = color;
        }
    }

    private static void SetPixel(Span<uint> pixels, int x, int y, int stride, uint color) =>
        pixels[(y * stride) + x] = color;
}
