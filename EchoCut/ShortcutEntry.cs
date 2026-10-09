namespace EchoCut;

/// <summary>Atajo tal como se presenta en la ventana «Atajos de teclado».</summary>
/// <param name="Group">Contexto en el que funciona, por ejemplo «Rejilla».</param>
/// <param name="Action">Qué hace.</param>
/// <param name="Keys">Teclas o gesto que lo disparan, ya en texto.</param>
public sealed record ShortcutEntry(string Group, string Action, string Keys);
