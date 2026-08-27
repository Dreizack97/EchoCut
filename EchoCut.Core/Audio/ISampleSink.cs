namespace EchoCut.Audio;

/// <summary>
/// Destino de las muestras que salen del decodificador. Existe para que el PCM no tenga que
/// materializarse entero en memoria: quien decodifica empuja los trozos según llegan y quien
/// analiza los consume al vuelo.
/// </summary>
/// <remarks>
/// Es deliberadamente síncrona. Una firma asíncrona obligaría a copiar cada trozo a un
/// <see cref="Memory{T}"/> —un <see cref="Span{T}"/> no puede cruzar un <c>await</c>— y a asignar
/// una tarea por lectura, que es justo el coste que este diseño elimina.
/// </remarks>
public interface ISampleSink
{
    /// <summary>Consume un bloque de muestras. El búfer se reutiliza al volver, no se puede retener.</summary>
    /// <param name="samples">
    /// Muestras mono en punto flotante, en orden cronológico. La memoria que respalda el <c>span</c>
    /// no es válida más allá de esta llamada.
    /// </param>
    void Write(ReadOnlySpan<float> samples);
}
