namespace EchoCut;

/// <summary>Pista tal como la necesita el diálogo para calcular y validar su nombre nuevo.</summary>
/// <param name="Directory">Carpeta del archivo.</param>
/// <param name="Name">Nombre actual, sin extensión.</param>
/// <param name="Extension">Extensión con su punto, por ejemplo «.mp3».</param>
public sealed record RenameItem(string Directory, string Name, string Extension);
