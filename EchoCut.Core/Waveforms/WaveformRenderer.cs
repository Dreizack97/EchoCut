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
        WaveformPalette palette)
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

            if (!waveform.TrySummarize(t0, t0 + secondsPerPixel, out WaveformPeak peak))
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
