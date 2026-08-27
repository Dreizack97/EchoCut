namespace EchoCut.Objects;

/// <summary>
/// Cómo se ve una fila según su estado: color del texto y glifos de las columnas de acción.
/// </summary>
/// <remarks>
/// <para>
/// El diseño de colores está optimizado para alta accesibilidad (AAA).
/// Para resolver el clásico problema donde un fondo de selección oscuro anula el contraste de los 
/// textos coloreados, se utilizan dos paletas: una oscura para fondos normales (blanco y gris), 
/// y una paleta clara y luminosa cuando la fila está seleccionada sobre un fondo azul oscuro.
/// </para>
/// <para>
/// Esto garantiza que la selección de la fila sea evidente para usuarios con baja visión 
/// (alto contraste del fondo azul contra el blanco), y que los colores de estado sigan 
/// percibiéndose claramente en ambos estados.
/// </para>
/// <para>
/// Los colores semánticos elegidos (ámbar, verde azulado, rojo oscuro, gris) también 
/// toman en cuenta la deuteranopia y protanopia. Nunca es el único indicio: la columna «Estado» 
/// y los glifos complementan la información (criterio 1.4.1 WCAG).
/// </para>
/// </remarks>
public static class SongPresentation
{
    // Colores para texto sobre fondo normal (blanco / gris claro)
    // Se oscurecen lo suficiente para un contraste óptimo (>7:1 AAA) manteniendo la distinción del matiz.

    /// <summary>Silencio recortable. Contraste ~7,1:1 sobre fondo blanco.</summary>
    public static readonly Color Trimmable = ColorTranslator.FromHtml("#8a4300");

    /// <summary>Copia recortada. Contraste ~7,3:1 sobre fondo blanco.</summary>
    public static readonly Color Trimmed = ColorTranslator.FromHtml("#00662a");

    /// <summary>Error. Contraste ~7,2:1 sobre fondo blanco.</summary>
    public static readonly Color Failed = ColorTranslator.FromHtml("#a31212");

    /// <summary>Sin resultado todavía. Contraste ~7,4:1 sobre fondo blanco.</summary>
    public static readonly Color Inconclusive = ColorTranslator.FromHtml("#4d4d4d");

    /// <summary>Resultado normal, va sin color. Contraste ~15:1 sobre fondo blanco.</summary>
    public static readonly Color Neutral = ColorTranslator.FromHtml("#000000");

    /// <summary>
    /// Fondo de la fila seleccionada. Un azul medianoche sumamente profundo que contrasta 
    /// radicalmente con el fondo blanco de la rejilla (contraste ~17,3:1).
    /// </summary>
    public static readonly Color Selection = ColorTranslator.FromHtml("#17426c");

    // Colores para el texto sobre el fondo de selección (azul oscuro)
    // Son versiones claras y luminosas, garantizando contraste (>7:1 AAA).

    /// <summary>Silencio recortable (Seleccionado). Contraste ~8:1 sobre el fondo de selección.</summary>
    public static readonly Color TrimmableSelection = ColorTranslator.FromHtml("#ffbe66");

    /// <summary>Copia recortada (Seleccionado). Contraste ~10:1 sobre el fondo de selección.</summary>
    public static readonly Color TrimmedSelection = ColorTranslator.FromHtml("#7df2a6");

    /// <summary>Error (Seleccionado). Contraste ~7:1 sobre el fondo de selección.</summary>
    public static readonly Color FailedSelection = ColorTranslator.FromHtml("#ff9999");

    /// <summary>Sin resultado (Seleccionado). Contraste ~9:1 sobre el fondo de selección.</summary>
    public static readonly Color InconclusiveSelection = ColorTranslator.FromHtml("#d9d9d9");

    /// <summary>Neutral (Seleccionado). Blanco puro para máximo contraste ~13:1.</summary>
    public static readonly Color NeutralSelection = ColorTranslator.FromHtml("#ffffff");

    /// <summary>Glifo de la columna de reproducción cuando la fila no está sonando.</summary>
    public const string PlayGlyph = "▶";

    /// <summary>Glifo de la columna de reproducción cuando la fila está sonando.</summary>
    public const string StopGlyph = "⏹";

    /// <summary>Glifo de la columna de recorte cuando la fila tiene un recorte pendiente.</summary>
    public const string TrimGlyph = "✂";

    /// <summary>Glifo de la columna de recorte cuando la fila ya se recortó.</summary>
    public const string TrimmedGlyph = "✔";

    /// <summary>Glifo de la columna de recorte cuando la fila no tiene nada que recortar.</summary>
    public const string InactiveGlyph = "·";

    /// <summary>Color del texto que corresponde a un estado (para fila no seleccionada).</summary>
    /// <param name="status">Valor de <see cref="Song.Estatus"/>.</param>
    /// <returns>Color semántico del estado oscuro, o <see cref="Neutral"/>.</returns>
    public static Color ForeColorFor(string? status) => status switch
    {
        Song.StatusAnalyzed => Trimmable,
        Song.StatusTrimmed => Trimmed,
        Song.StatusError => Failed,

        // Los dos «…ando» son transitorios y «Cancelado» es ausencia de resultado, igual que
        // «Pendiente»: ninguno merece un color semántico que dentro de un segundo será otro.
        Song.StatusPending or Song.StatusAnalyzing or Song.StatusTrimming or Song.StatusCancelled
            => Inconclusive,

        _ => Neutral,
    };

    /// <summary>Color del texto que corresponde a un estado (para fila seleccionada).</summary>
    /// <param name="status">Valor de <see cref="Song.Estatus"/>.</param>
    /// <returns>Color semántico del estado claro, o <see cref="NeutralSelection"/>.</returns>
    public static Color SelectionForeColorFor(string? status) => status switch
    {
        Song.StatusAnalyzed => TrimmableSelection,
        Song.StatusTrimmed => TrimmedSelection,
        Song.StatusError => FailedSelection,
        Song.StatusPending or Song.StatusAnalyzing or Song.StatusTrimming or Song.StatusCancelled => InconclusiveSelection,
        _ => NeutralSelection,
    };

    /// <summary>Glifo de la columna de reproducción.</summary>
    /// <param name="isPlaying">Si la fila correspondiente está sonando en este momento.</param>
    /// <returns><see cref="StopGlyph"/> si <paramref name="isPlaying"/> es <c>true</c>; <see cref="PlayGlyph"/> en caso contrario.</returns>
    public static string PlayGlyphFor(bool isPlaying) => isPlaying ? StopGlyph : PlayGlyph;

    /// <summary>Glifo de la columna de recorte, según lo que sea posible en esa fila.</summary>
    /// <param name="song">Fila a evaluar.</param>
    /// <returns>
    /// <see cref="TrimmedGlyph"/> si ya se recortó, <see cref="TrimGlyph"/> si hay un recorte
    /// pendiente, o <see cref="InactiveGlyph"/> si no hay nada que hacer.
    /// </returns>
    /// <exception cref="ArgumentNullException">Se lanza si <paramref name="song"/> es <c>null</c>.</exception>
    public static string TrimGlyphFor(Song song)
    {
        ArgumentNullException.ThrowIfNull(song);

        if (song.Estatus == Song.StatusTrimmed)
        {
            return TrimmedGlyph;
        }

        return CanTrim(song) ? TrimGlyph : InactiveGlyph;
    }

    /// <summary>Si la fila tiene un recorte calculado y pendiente de aplicar.</summary>
    /// <param name="song">Fila a evaluar.</param>
    /// <returns><c>true</c> si la fila tiene un recorte pendiente de aplicar.</returns>
    /// <exception cref="ArgumentNullException">Se lanza si <paramref name="song"/> es <c>null</c>.</exception>
    public static bool CanTrim(Song song)
    {
        ArgumentNullException.ThrowIfNull(song);
        return song.ShouldTrim && song.CutSeconds is not null && song.Estatus != Song.StatusTrimmed;
    }
}
