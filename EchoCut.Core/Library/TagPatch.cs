namespace EchoCut.Library;

/// <summary>
/// Etiquetas que se aplican por igual a un conjunto de pistas.
/// </summary>
/// <remarks>
/// Cada propiedad en blanco significa «no tocar», no «borrar»: así se puede fijar el álbum de un
/// lote sin perder los artistas y títulos que cada pista ya tuviera. Borrar etiquetas es otra
/// operación, <see cref="TrackEditor.StripMetadata"/>.
/// </remarks>
/// <param name="Artist">Intérprete; varios se separan con «;».</param>
/// <param name="Title">Título fijo; se ignora si <paramref name="TitleFromFileName"/> está activo.</param>
/// <param name="Album">Álbum.</param>
/// <param name="Genre">Género; varios se separan con «;».</param>
/// <param name="Comment">Comentario.</param>
/// <param name="TitleFromFileName">
/// Si cada pista toma como título su propio nombre de archivo. Un mismo título fijo para todo un
/// listado rara vez tiene sentido; derivarlo del archivo sí.
/// </param>
public sealed record TagPatch(
    string? Artist = null,
    string? Title = null,
    string? Album = null,
    string? Genre = null,
    string? Comment = null,
    bool TitleFromFileName = false)
{
    /// <summary>Si el parche no cambiaría nada en ninguna pista.</summary>
    /// <value><c>true</c> si todas las propiedades están en blanco y el título no se toma del archivo.</value>
    public bool IsEmpty =>
        !TitleFromFileName
        && string.IsNullOrWhiteSpace(Artist)
        && string.IsNullOrWhiteSpace(Title)
        && string.IsNullOrWhiteSpace(Album)
        && string.IsNullOrWhiteSpace(Genre)
        && string.IsNullOrWhiteSpace(Comment);
}
