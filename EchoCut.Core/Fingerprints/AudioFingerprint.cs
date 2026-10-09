namespace EchoCut.Fingerprints;

/// <summary>
/// Huella acústica de una pista: una palabra de 32 bits por trama que resume cómo cambia su
/// espectro, al estilo del algoritmo de Haitsma y Kalker.
/// </summary>
/// <remarks>
/// <para>
/// No guarda audio sino unos pocos bytes por segundo —unos 170 B/s—, así que la de una biblioteca
/// entera cabe en memoria y se puede comparar sin volver a decodificar nada.
/// </para>
/// <para>
/// Cada trama lleva además si sonaba algo. El silencio produce bits al azar —el signo de diferencias
/// entre energías ínfimas—, y contarlos al comparar penalizaría a dos copias de la misma canción solo
/// por tener silencios de distinta duración.
/// </para>
/// </remarks>
public sealed class AudioFingerprint
{
    private readonly uint[] _frames;
    private readonly bool[] _audible;

    /// <summary>Crea la huella a partir de sus tramas.</summary>
    /// <param name="frames">Palabra de 32 bits de cada trama, en orden.</param>
    /// <param name="audible">Si cada trama sonaba; misma longitud que <paramref name="frames"/>.</param>
    /// <param name="frameSeconds">Separación entre tramas, en segundos.</param>
    /// <exception cref="ArgumentException">Se lanza si las longitudes no coinciden.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se lanza si la separación no es positiva.</exception>
    public AudioFingerprint(uint[] frames, bool[] audible, double frameSeconds)
    {
        ArgumentNullException.ThrowIfNull(frames);
        ArgumentNullException.ThrowIfNull(audible);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(frameSeconds);
        if (frames.Length != audible.Length)
        {
            throw new ArgumentException("Cada trama debe indicar si sonaba.", nameof(audible));
        }

        _frames = frames;
        _audible = audible;
        FrameSeconds = frameSeconds;
        AudibleCount = audible.Count(static value => value);
    }

    /// <value>Número de tramas.</value>
    public int Count => _frames.Length;

    /// <value>Tramas en las que sonaba algo.</value>
    public int AudibleCount { get; }

    /// <value>Separación entre tramas, en segundos.</value>
    public double FrameSeconds { get; }

    /// <value>Duración que abarca la huella, en segundos.</value>
    public double DurationSeconds => Count * FrameSeconds;

    /// <value>Las palabras de todas las tramas.</value>
    public ReadOnlySpan<uint> Frames => _frames;

    /// <summary>Si en una trama sonaba algo.</summary>
    /// <param name="index">Índice de la trama.</param>
    /// <returns><c>true</c> si superaba el umbral de silencio.</returns>
    public bool IsAudible(int index) => _audible[index];
}
