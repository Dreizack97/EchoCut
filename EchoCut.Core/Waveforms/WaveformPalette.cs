namespace EchoCut.Waveforms;

/// <summary>Colores con que se pinta una forma de onda, en ARGB de 32 bits.</summary>
/// <param name="Background">Fondo de la vista.</param>
/// <param name="Peak">Contorno de los picos, del mínimo al máximo de cada píxel.</param>
/// <param name="Rms">Banda interior del nivel eficaz.</param>
/// <param name="CenterLine">Línea de amplitud cero.</param>
public readonly record struct WaveformPalette(uint Background, uint Peak, uint Rms, uint CenterLine)
{
    /// <summary>
    /// Los colores de Audacity: picos (50, 50, 200) y RMS (100, 100, 220) sobre blanco. El azul de
    /// los picos contrasta 8.7:1 con el fondo, por encima del AAA que sigue el resto de la interfaz.
    /// </summary>
    public static WaveformPalette Normal { get; } = new(0xFFFFFFFF, 0xFF3232C8, 0xFF6464DC, 0xFF9A9A9A);

    /// <summary>
    /// La zona que eliminaría el recorte: fondo gris y onda gris atenuada. Distinguirla por luminosidad
    /// y no solo por matiz la hace legible también con daltonismo, y la onda sigue a la vista para
    /// comprobar que lo que se descarta es silencio. Los picos grises contrastan 3.4:1 con su fondo,
    /// por encima del 3:1 que WCAG exige a los elementos gráficos.
    /// </summary>
    public static WaveformPalette Removed { get; } = new(0xFFD4D4D4, 0xFF6E6E6E, 0xFF939393, 0xFF9A9A9A);
}
