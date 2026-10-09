namespace EchoCut;

/// <summary>Qué transformación aplica <see cref="RenameSongsDialog"/> a los nombres.</summary>
public enum RenameMode
{
    /// <summary>Antepone un número consecutivo, por ejemplo «0001 - ».</summary>
    Sequence,

    /// <summary>Quita una cantidad de caracteres del principio.</summary>
    RemoveLeading,
}
