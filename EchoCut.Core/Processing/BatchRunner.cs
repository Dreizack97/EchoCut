using EchoCut.Library;

namespace EchoCut.Processing;

/// <summary>En qué punto está una pista dentro del lote.</summary>
public enum TrackState
{
    /// <summary>El trabajo sobre la pista acaba de empezar.</summary>
    Running,

    /// <summary>El trabajo terminó y hay resultado.</summary>
    Completed,

    /// <summary>El trabajo terminó con un error propio de esta pista; el lote continúa.</summary>
    Failed,

    /// <summary>El trabajo se interrumpió porque se canceló el lote.</summary>
    Cancelled,
}

/// <summary>Aviso de que una pista del lote cambió de estado.</summary>
/// <typeparam name="TResult">Tipo del resultado que produce el trabajo sobre cada pista.</typeparam>
/// <param name="Track">Pista afectada.</param>
/// <param name="State">Punto en el que está.</param>
/// <param name="Result">Resultado, solo cuando <paramref name="State"/> es <see cref="TrackState.Completed"/>.</param>
/// <param name="Error">Error, solo cuando <paramref name="State"/> es <see cref="TrackState.Failed"/>.</param>
/// <param name="CompletedCount">Pistas terminadas hasta el momento, con o sin éxito.</param>
public sealed record TrackProgress<TResult>(
    TrackInfo Track,
    TrackState State,
    TResult? Result,
    Exception? Error,
    int CompletedCount)
    where TResult : class;

/// <summary>Cómo terminó un lote completo.</summary>
/// <param name="Total">Pistas que se intentaron procesar.</param>
/// <param name="Succeeded">Pistas que terminaron con resultado.</param>
/// <param name="Failed">Pistas que terminaron con un error propio.</param>
public sealed record BatchSummary(int Total, int Succeeded, int Failed);

/// <summary>
/// El esqueleto común de los dos lotes: avisar del inicio, ejecutar, clasificar el desenlace y
/// llevar la cuenta. Analizar y recortar solo se diferencian en el trabajo que hacen por pista.
/// </summary>
internal static class BatchRunner
{
    /// <summary>Recorre el lote en paralelo notificando el estado de cada pista.</summary>
    /// <typeparam name="TItem">Tipo de cada elemento del lote.</typeparam>
    /// <typeparam name="TResult">Tipo del resultado por pista.</typeparam>
    /// <param name="items">Elementos a procesar.</param>
    /// <param name="maxDegreeOfParallelism">Elementos simultáneos; se acota en <see cref="BatchProcessor"/>.</param>
    /// <param name="trackOf">Cómo obtener la pista a la que se refiere cada elemento.</param>
    /// <param name="work">Trabajo a realizar sobre cada elemento.</param>
    /// <param name="progress">
    /// Canal de avisos. Quien lo cree en el hilo de interfaz recibirá los avisos ya marshalados,
    /// que es justo lo que necesita para tocar controles sin comprobaciones de hilo.
    /// </param>
    /// <param name="cancellationToken">Token de cancelación del lote.</param>
    /// <returns>El recuento final del lote.</returns>
    /// <exception cref="OperationCanceledException">Se lanza si se cancela el lote.</exception>
    public static async Task<BatchSummary> RunAsync<TItem, TResult>(
        IReadOnlyList<TItem> items,
        int maxDegreeOfParallelism,
        Func<TItem, TrackInfo> trackOf,
        Func<TItem, CancellationToken, Task<TResult>> work,
        IProgress<TrackProgress<TResult>>? progress,
        CancellationToken cancellationToken)
        where TResult : class
    {
        int completed = 0;
        int failed = 0;

        await BatchProcessor.ForEachAsync(
            items,
            maxDegreeOfParallelism,
            async (item, token) =>
            {
                TrackInfo track = trackOf(item);
                Report(progress, track, TrackState.Running, null, null, Volatile.Read(ref completed));

                try
                {
                    TResult result = await work(item, token).ConfigureAwait(false);
                    Report(progress, track, TrackState.Completed, result, null, Interlocked.Increment(ref completed));
                }
                catch (OperationCanceledException)
                {
                    // La cancelación no se cuenta como terminada: la barra debe quedarse donde
                    // estaba, no saltar al final fingiendo que el lote se completó.
                    Report(progress, track, TrackState.Cancelled, null, null, Volatile.Read(ref completed));
                    throw;
                }
                catch (Exception exception)
                {
                    // Un archivo ilegible no aborta el lote: se anota y se sigue con el resto.
                    Interlocked.Increment(ref failed);
                    Report(progress, track, TrackState.Failed, null, exception, Interlocked.Increment(ref completed));
                }
            },
            cancellationToken).ConfigureAwait(false);

        return new BatchSummary(items.Count, completed - failed, failed);
    }

    private static void Report<TResult>(
        IProgress<TrackProgress<TResult>>? progress,
        TrackInfo track,
        TrackState state,
        TResult? result,
        Exception? error,
        int completedCount)
        where TResult : class =>
        progress?.Report(new TrackProgress<TResult>(track, state, result, error, completedCount));
}
