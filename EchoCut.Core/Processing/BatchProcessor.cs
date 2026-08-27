namespace EchoCut.Processing;

/// <summary>
/// Recorrido paralelo del lote. Cada archivo genera su propio proceso de FFmpeg, así que el
/// trabajo escala bien con los núcleos disponibles; el tope evita saturar la E/S de disco.
/// </summary>
public static class BatchProcessor
{
    /// <summary>Procesa <paramref name="items"/> en paralelo, con un grado de paralelismo acotado.</summary>
    /// <typeparam name="T">Tipo de cada elemento del lote.</typeparam>
    /// <param name="items">Elementos a procesar. Si está vacía, la tarea se completa sin hacer nada.</param>
    /// <param name="maxDegreeOfParallelism">
    /// Número de elementos a procesar simultáneamente. Se acota a <c>[1, 64]</c> antes de usarse, así
    /// que un valor fuera de rango no lanza excepción: simplemente se recorta al límite más cercano.
    /// </param>
    /// <param name="body">
    /// Operación a ejecutar por cada elemento, con acceso al token de cancelación del lote.
    /// </param>
    /// <param name="cancellationToken">
    /// Token de cancelación del lote completo. Si se activa, deja de lanzarse trabajo nuevo y las
    /// tareas en curso reciben la cancelación a través del token que se les pasó.
    /// </param>
    public static async Task ForEachAsync<T>(
        IReadOnlyList<T> items,
        int maxDegreeOfParallelism,
        Func<T, CancellationToken, ValueTask> body,
        CancellationToken cancellationToken)
    {
        if (items.Count == 0)
        {
            return;
        }

        ParallelOptions options = new()
        {
            MaxDegreeOfParallelism = Math.Clamp(maxDegreeOfParallelism, 1, 64),
            CancellationToken = cancellationToken,
        };

        await Parallel.ForEachAsync(items, options, body).ConfigureAwait(false);
    }
}
