using EchoCut.Audio;
using EchoCut.Fingerprints;
using EchoCut.Library;

namespace EchoCut.Processing;

/// <summary>Una pista dentro de un grupo de duplicados.</summary>
/// <param name="Track">Pista.</param>
/// <param name="Similarity">
/// Parecido con la que mejor se le parece del grupo, de 0 a 1; la propuesta para conservar lleva el
/// de su pareja más cercana.
/// </param>
/// <param name="IsBest">Si es la que se propone conservar, por ser la de mejor calidad.</param>
public sealed record DuplicateMember(TrackInfo Track, double Similarity, bool IsBest);

/// <summary>Pistas que contienen la misma grabación.</summary>
/// <param name="Members">Pistas del grupo, de mejor a peor calidad: la primera es la que se propone conservar.</param>
public sealed record DuplicateGroup(IReadOnlyList<DuplicateMember> Members);

/// <summary>
/// Busca canciones duplicadas por su audio, no por su nombre: la misma grabación en otro formato, a
/// otra tasa de bits o volumen, o con silencios distintos.
/// </summary>
/// <remarks>
/// <para>
/// Primero calcula en paralelo la huella acústica de cada pista —decodificar a 5512 Hz y en mono
/// cuesta poco—, después las compara con <see cref="DuplicateDetector"/> y agrupa las coincidencias:
/// si A se parece a B y B a C, las tres son copias de lo mismo aunque A y C no se hayan emparejado.
/// </para>
/// <para>
/// No borra nada: propone qué conservar y deja la decisión al usuario.
/// </para>
/// </remarks>
public sealed class DuplicateFinder
{
    /// <summary>Extensiones de formatos sin pérdida, preferidos a cualquier copia comprimida con pérdida.</summary>
    private static readonly HashSet<string> LosslessExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".flac", ".wav", ".aiff", ".ape", ".wv", ".dsf",
    };

    private readonly FFmpegLocator _locator;

    /// <summary>Crea el servicio contra un localizador de FFmpeg ya compartido con el resto de la aplicación.</summary>
    /// <param name="locator">Localizador de los ejecutables de FFmpeg.</param>
    public DuplicateFinder(FFmpegLocator locator) => _locator = locator;

    /// <summary>Busca los grupos de pistas que contienen la misma grabación.</summary>
    /// <param name="tracks">Pistas entre las que buscar.</param>
    /// <param name="maxDegreeOfParallelism">Pistas cuya huella se calcula a la vez.</param>
    /// <param name="progress">
    /// Avisos por pista mientras se calculan las huellas, o <c>null</c>. La comparación posterior es
    /// rápida y no avisa.
    /// </param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Los grupos, de más a menos copias; vacío si no hay duplicados.</returns>
    /// <remarks>Una pista que no se puede decodificar se notifica como fallida y queda fuera de la búsqueda.</remarks>
    /// <exception cref="FFmpegException">Se lanza si FFmpeg no está resuelto.</exception>
    /// <exception cref="OperationCanceledException">Se lanza si se cancela.</exception>
    public async Task<IReadOnlyList<DuplicateGroup>> FindAsync(
        IReadOnlyList<TrackInfo> tracks,
        int maxDegreeOfParallelism,
        IProgress<TrackProgress<AudioFingerprint>>? progress,
        CancellationToken cancellationToken)
    {
        (string ffmpeg, string ffprobe) = _locator.Require();
        AudioDecoder decoder = new(ffmpeg, ffprobe, FingerprintBuilder.SampleRate);
        AudioFingerprint?[] fingerprints = new AudioFingerprint?[tracks.Count];

        await BatchRunner.RunAsync(
            [.. Enumerable.Range(0, tracks.Count)],
            maxDegreeOfParallelism,
            index => tracks[index],
            async (index, token) =>
            {
                using FingerprintBuilder builder = new();
                await decoder.DecodeAllIntoAsync(tracks[index].FilePath, builder, token).ConfigureAwait(false);
                AudioFingerprint fingerprint = builder.Build();
                fingerprints[index] = fingerprint;
                return fingerprint;
            },
            progress,
            cancellationToken).ConfigureAwait(false);

        int[] analyzed = [.. Enumerable.Range(0, tracks.Count).Where(index => fingerprints[index] is not null)];
        IReadOnlyList<FingerprintMatch> matches = await Task.Run(
            () => DuplicateDetector.FindMatches([.. analyzed.Select(index => fingerprints[index]!)], cancellationToken),
            cancellationToken).ConfigureAwait(false);

        return Group([.. analyzed.Select(index => tracks[index])], matches);
    }

    /// <summary>Junta las parejas en grupos y ordena cada grupo de mejor a peor calidad.</summary>
    /// <param name="tracks">Pistas analizadas, en el orden de las huellas.</param>
    /// <param name="matches">Parejas encontradas, con índices en <paramref name="tracks"/>.</param>
    /// <returns>Los grupos, de más a menos copias.</returns>
    public static IReadOnlyList<DuplicateGroup> Group(IReadOnlyList<TrackInfo> tracks, IReadOnlyList<FingerprintMatch> matches)
    {
        int[] parent = [.. Enumerable.Range(0, tracks.Count)];
        double[] similarity = new double[tracks.Count];

        foreach (FingerprintMatch match in matches)
        {
            parent[Find(parent, match.First)] = Find(parent, match.Second);
            similarity[match.First] = Math.Max(similarity[match.First], match.Similarity);
            similarity[match.Second] = Math.Max(similarity[match.Second], match.Similarity);
        }

        return
        [
            .. Enumerable.Range(0, tracks.Count)
                .Where(index => similarity[index] > 0.0)
                .GroupBy(index => Find(parent, index))
                .Where(group => group.Count() > 1)
                .Select(group =>
                {
                    List<int> ranked = [.. group.OrderByDescending(index => QualityScore(tracks[index]))];
                    return new DuplicateGroup([.. ranked.Select((index, position) => new DuplicateMember(tracks[index], similarity[index], position == 0))]);
                })
                .OrderByDescending(group => group.Members.Count)
                .ThenBy(group => group.Members[0].Track.Name, StringComparer.CurrentCultureIgnoreCase),
        ];
    }

    /// <summary>
    /// Cuánto conviene conservar una copia: primero sin pérdida, después más tasa de bits, después más
    /// duración —una copia a la que no le falta nada— y, por último, más tamaño.
    /// </summary>
    private static (bool Lossless, int Bitrate, double Duration, long Size) QualityScore(TrackInfo track) =>
        (LosslessExtensions.Contains(track.Extension), track.BitrateKbps, Math.Round(track.DurationSeconds, 1), track.SizeBytes);

    private static int Find(int[] parent, int index)
    {
        while (parent[index] != index)
        {
            parent[index] = parent[parent[index]];
            index = parent[index];
        }

        return index;
    }
}
