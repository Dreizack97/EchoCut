using EchoCut.Audio;

namespace EchoCut.Objects;

/// <summary>Nombres de las curvas y sentidos de fundido tal como se muestran en la interfaz.</summary>
/// <remarks>
/// Viven en la interfaz y no en <see cref="FadeCurve"/> porque el motor no sabe de idiomas ni de
/// presentación; aquí los comparten la lista desplegable, las etiquetas de la forma de onda y el
/// texto que leen los lectores de pantalla.
/// </remarks>
public static class FadeText
{
    /// <summary>Curvas en el orden en que se ofrecen: de la más neutra a la más marcada.</summary>
    public static IReadOnlyList<FadeCurve> Curves { get; } =
        [FadeCurve.Linear, FadeCurve.SCurve, FadeCurve.Cosine, FadeCurve.Rounded, FadeCurve.Logarithmic, FadeCurve.Exponential];

    /// <summary>Nombre de una curva.</summary>
    /// <param name="curve">Curva a nombrar.</param>
    /// <returns>Nombre en español, como en el «Adjustable fade» de Audacity.</returns>
    public static string Name(FadeCurve curve) => curve switch
    {
        FadeCurve.Linear => "Lineal",
        FadeCurve.Exponential => "Exponencial",
        FadeCurve.Logarithmic => "Logarítmica",
        FadeCurve.Rounded => "Redondeada",
        FadeCurve.Cosine => "Coseno",
        FadeCurve.SCurve => "Curva S",
        _ => curve.ToString(),
    };

    /// <summary>Nombre de un sentido de fundido.</summary>
    /// <param name="direction">Sentido a nombrar.</param>
    /// <returns>«Aparición» o «Desaparición».</returns>
    public static string Name(FadeDirection direction) =>
        direction == FadeDirection.In ? "Aparición" : "Desaparición";
}
