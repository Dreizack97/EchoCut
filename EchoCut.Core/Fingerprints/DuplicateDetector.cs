using System.Numerics;

namespace EchoCut.Fingerprints;

/// <summary>Dos huellas que pertenecen a la misma grabación.</summary>
/// <param name="First">Índice de la primera huella en la lista analizada.</param>
/// <param name="Second">Índice de la segunda huella; siempre mayor que <paramref name="First"/>.</param>
/// <param name="BitErrorRate">Proporción de bits distintos en las tramas que suenan en ambas, alineadas.</param>
/// <param name="OffsetSeconds">
/// Cuánto empieza antes la grabación en la primera que en la segunda: positivo si la primera lleva
/// más audio delante, como un silencio inicial más largo.
/// </param>
/// <param name="OverlapSeconds">Duración de lo que suena en ambas una vez alineadas.</param>
public sealed record FingerprintMatch(int First, int Second, double BitErrorRate, double OffsetSeconds, double OverlapSeconds)
{
    /// <summary>Parecido entre ambas, de 0 a 1.</summary>
    /// <value>
    /// 1 si coinciden todos los bits y 0 si coinciden como dos grabaciones sin relación, que difieren
    /// en la mitad: así el porcentaje se lee como cabría esperar y no parte de un 50 % engañoso.
    /// </value>
    public double Similarity => Math.Clamp(1.0 - (2.0 * BitErrorRate), 0.0, 1.0);
}

/// <summary>
/// Busca, entre muchas huellas, las que pertenecen a la misma grabación aunque estén en otro formato,
/// a otra tasa de bits o volumen, o con silencios de distinta duración.
/// </summary>
/// <remarks>
/// <para>
/// Comparar cada pista con todas las demás en todos los desfases posibles sería inabordable con una
/// biblioteca grande. Como en el sistema de Philips, se buscan primero candidatas por coincidencia
/// exacta de palabras de 32 bits en un índice invertido: entre copias de la misma grabación decenas
/// de palabras coinciden exactamente y en el mismo desfase, y entre grabaciones distintas casi
/// ninguna. Solo las candidatas se comparan bit a bit.
/// </para>
/// <para>
/// El índice guarda una de cada <see cref="IndexStride"/> tramas de cada huella y la búsqueda recorre
/// todas: basta con que una trama buscada caiga sobre una indexada en el desfase correcto, y así el
/// índice ocupa una cuarta parte.
/// </para>
/// <para>Es lógica pura, sin E/S: recibe las huellas ya calculadas.</para>
/// </remarks>
public static class DuplicateDetector
{
    /// <summary>
    /// Proporción de bits distintos por encima de la cual dos huellas no se consideran la misma
    /// grabación. Entre copias en distinto formato ronda 0.05–0.15, y entre grabaciones distintas,
    /// 0.5; el umbral deja margen para codificaciones agresivas sin acercarse al azar.
    /// </summary>
    public const double MaxBitErrorRate = 0.25;

    /// <summary>
    /// Parte de lo que suena en cada una de las dos —también en la más larga— que debe coincidir.
    /// Medirlo sobre la más larga evita que una mezcla de DJ o un popurrí que contiene una canción
    /// entera pase por copia suya; el silencio no cuenta, así que un silencio inicial o final más
    /// largo no impide reconocer una copia.
    /// </summary>
    public const double MinCoverage = 0.8;

    /// <summary>Duración mínima de lo que coincide: con menos, no hay base para afirmar que es la misma grabación.</summary>
    public const double MinOverlapSeconds = 10.0;

    /// <summary>Una de cada cuántas tramas de cada huella entra en el índice.</summary>
    private const int IndexStride = 4;

    /// <summary>Coincidencias exactas en el mismo desfase para que una pareja merezca compararse entera.</summary>
    private const int MinVotes = 2;

    /// <summary>
    /// Palabras que aparecen más veces que esto en el índice se ignoran al buscar: son las de pasajes
    /// estacionarios o casi silenciosos, que coinciden en cualquier pista y solo generan candidatas falsas.
    /// </summary>
    private const int MaxPostings = 256;

    /// <summary>Desfases mejor votados de cada pareja que se comparan enteros.</summary>
    private const int OffsetsPerPair = 3;

    /// <summary>Tramas a cada lado del desfase votado que se prueban, por si el mejor alineamiento cae al lado.</summary>
    private const int Refinement = 2;

    /// <summary>Busca las parejas de huellas que pertenecen a la misma grabación.</summary>
    /// <param name="fingerprints">Huellas a comparar entre sí; todas con la misma separación entre tramas.</param>
    /// <param name="cancellationToken">Token de cancelación, que se comprueba huella a huella.</param>
    /// <returns>Las parejas encontradas, cada una una sola vez y con el índice menor primero.</returns>
    /// <exception cref="OperationCanceledException">Se lanza si se cancela.</exception>
    public static IReadOnlyList<FingerprintMatch> FindMatches(IReadOnlyList<AudioFingerprint> fingerprints, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(fingerprints);
        Dictionary<uint, List<long>> index = BuildIndex(fingerprints);
        List<FingerprintMatch> matches = [];

        for (int first = 0; first < fingerprints.Count; first++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AudioFingerprint query = fingerprints[first];
            Dictionary<(int Second, int Offset), int> votes = [];

            ReadOnlySpan<uint> frames = query.Frames;
            for (int frame = 0; frame < frames.Length; frame++)
            {
                if (!query.IsAudible(frame) || !index.TryGetValue(frames[frame], out List<long>? postings) || postings.Count > MaxPostings)
                {
                    continue;
                }

                foreach (long posting in postings)
                {
                    int second = (int)(posting >> 32);
                    if (second <= first)
                    {
                        continue;
                    }

                    int offset = frame - (int)(posting & 0xFFFFFFFF);
                    votes[(second, offset)] = votes.GetValueOrDefault((second, offset)) + 1;
                }
            }

            foreach (IGrouping<int, KeyValuePair<(int Second, int Offset), int>> pair in votes
                .Where(vote => vote.Value >= MinVotes)
                .GroupBy(vote => vote.Key.Second))
            {
                AudioFingerprint other = fingerprints[pair.Key];
                FingerprintMatch? best = null;

                foreach (int offset in pair.OrderByDescending(vote => vote.Value).Take(OffsetsPerPair).Select(vote => vote.Key.Offset))
                {
                    for (int shift = offset - Refinement; shift <= offset + Refinement; shift++)
                    {
                        if (Compare(query, other, shift) is { } candidate && (best is null || candidate.BitErrorRate < best.BitErrorRate))
                        {
                            best = candidate with { First = first, Second = pair.Key };
                        }
                    }
                }

                if (best is not null && best.BitErrorRate <= MaxBitErrorRate)
                {
                    matches.Add(best);
                }
            }
        }

        return matches;
    }

    /// <summary>
    /// Compara dos huellas alineadas: la trama <c>i</c> de la primera con la <c>i − offset</c> de la segunda.
    /// </summary>
    /// <param name="first">Primera huella.</param>
    /// <param name="second">Segunda huella.</param>
    /// <param name="offset">Tramas que la grabación va adelantada en la primera respecto de la segunda.</param>
    /// <returns>
    /// La comparación sobre las tramas que suenan en ambas, o <c>null</c> si lo que coincide es
    /// demasiado poco para juzgar: menos de <see cref="MinOverlapSeconds"/> o de
    /// <see cref="MinCoverage"/> de lo que suena en la más larga.
    /// </returns>
    public static FingerprintMatch? Compare(AudioFingerprint first, AudioFingerprint second, int offset)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        int from = Math.Max(0, offset);
        int to = Math.Min(first.Count, second.Count + offset);
        ReadOnlySpan<uint> a = first.Frames;
        ReadOnlySpan<uint> b = second.Frames;

        long differing = 0;
        int compared = 0;
        for (int i = from; i < to; i++)
        {
            int j = i - offset;
            if (first.IsAudible(i) && second.IsAudible(j))
            {
                differing += BitOperations.PopCount(a[i] ^ b[j]);
                compared++;
            }
        }

        double overlapSeconds = compared * first.FrameSeconds;
        int longer = Math.Max(first.AudibleCount, second.AudibleCount);
        if (compared == 0 || overlapSeconds < MinOverlapSeconds || compared < MinCoverage * longer)
        {
            return null;
        }

        return new FingerprintMatch(0, 0, differing / (32.0 * compared), offset * first.FrameSeconds, overlapSeconds);
    }

    /// <summary>Indexa una de cada <see cref="IndexStride"/> tramas audibles de cada huella por su palabra.</summary>
    private static Dictionary<uint, List<long>> BuildIndex(IReadOnlyList<AudioFingerprint> fingerprints)
    {
        Dictionary<uint, List<long>> index = [];
        for (int print = 0; print < fingerprints.Count; print++)
        {
            AudioFingerprint fingerprint = fingerprints[print];
            ReadOnlySpan<uint> frames = fingerprint.Frames;
            for (int frame = 0; frame < frames.Length; frame += IndexStride)
            {
                if (!fingerprint.IsAudible(frame))
                {
                    continue;
                }

                if (!index.TryGetValue(frames[frame], out List<long>? postings))
                {
                    postings = [];
                    index[frames[frame]] = postings;
                }

                postings.Add(((long)print << 32) | (uint)frame);
            }
        }

        return index;
    }
}
