using EchoCut.Audio;
using NAudio.Wave;
using System.Globalization;
using System.Runtime.InteropServices;

namespace EchoCut.Playback;

/// <summary>
/// Reproduce un tramo corto de una pista para poder juzgar de oído el punto de corte, sabiendo en
/// todo momento qué instante está sonando.
/// </summary>
/// <remarks>
/// <para>
/// FFmpeg decodifica el tramo a PCM estéreo de 16 bits directamente en memoria y NAudio lo envía
/// a la tarjeta con <see cref="WaveOut"/>. A diferencia de reproducir un WAV temporal, el
/// dispositivo informa de cuántos bytes ha sonado ya, que es lo que permite dibujar un cursor
/// sincronizado con lo que se oye, y avisa cuando termina en vez de obligar a adivinarlo con un
/// temporizador.
/// </para>
/// <para>
/// El tramo cabe holgadamente en memoria: la previsualización dura como mucho 30 s, unos 5 MB.
/// </para>
/// <para>
/// <see cref="WaveOut"/> notifica el final desde un hilo propio; el reproductor lo devuelve al
/// contexto de sincronización en el que se creó, de modo que creado en la interfaz,
/// <see cref="PlaybackCompleted"/> llega listo para tocar controles.
/// </para>
/// </remarks>
public sealed class AudioPreviewPlayer : IDisposable
{
    /// <summary>Formato de reproducción. No es el de análisis: aquí sí se escucha, y en estéreo.</summary>
    private static readonly WaveFormat PlaybackFormat = new(44100, 16, 2);

    /// <summary>Duración de cada búfer del dispositivo: la recomendada por NAudio.</summary>
    private const int BufferMilliseconds = 100;

    private readonly string _ffmpegPath;
    private readonly SynchronizationContext? _context;

    private WaveOut? _output;
    private PreviewWindow _window;
    private int _generation;
    private bool _disposed;

    /// <summary>Crea el reproductor contra el ejecutable de FFmpeg indicado.</summary>
    /// <param name="ffmpegPath">Ruta absoluta a <c>ffmpeg.exe</c>, ya resuelta.</param>
    /// <remarks>Debe crearse en el hilo en el que se quiera recibir <see cref="PlaybackCompleted"/>.</remarks>
    public AudioPreviewPlayer(string ffmpegPath)
    {
        _ffmpegPath = ffmpegPath;
        _context = SynchronizationContext.Current;
    }

    /// <summary>
    /// Se produce cuando el tramo termina de sonar por sí solo. No se produce al detenerlo con
    /// <see cref="Stop"/> ni al sustituirlo por otro: en esos casos quien lo pidió ya lo sabe.
    /// </summary>
    public event EventHandler? PlaybackCompleted;

    /// <value><c>true</c> mientras suena un tramo.</value>
    public bool IsPlaying => _output?.PlaybackState == PlaybackState.Playing;

    /// <value>
    /// Instante del archivo que está sonando, en segundos, o <c>null</c> si no suena nada. Lo da el
    /// propio dispositivo, así que incluye la latencia de salida: es lo que de verdad se oye.
    /// </value>
    public double? PositionSeconds
    {
        get
        {
            if (_output is not { } output)
            {
                return null;
            }

            double played = output.GetPosition() / (double)PlaybackFormat.AverageBytesPerSecond;
            return _window.StartSeconds + Math.Min(played, _window.DurationSeconds);
        }
    }

    /// <summary>Decodifica el tramo indicado y empieza a reproducirlo, sustituyendo al que sonara.</summary>
    /// <param name="filePath">Ruta del archivo de audio a previsualizar.</param>
    /// <param name="window">Tramo del archivo a reproducir.</param>
    /// <param name="fades">
    /// Fundidos que llevará la copia, para que el tramo suene como sonará ella; <c>null</c> si no
    /// lleva ninguno.
    /// </param>
    /// <param name="cancellationToken">
    /// Token de cancelación para la decodificación con FFmpeg. Una vez iniciada la reproducción, no
    /// afecta al sonido ya en curso: para eso está <see cref="Stop"/>.
    /// </param>
    /// <returns>Una tarea que termina cuando el tramo empieza a sonar.</returns>
    /// <remarks>
    /// Si se pide otro tramo mientras este se decodifica, gana el último: el anterior se descarta sin
    /// llegar a sonar, para que dos clics seguidos nunca dejen dos reproducciones superpuestas.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">Se lanza si el reproductor ya se liberó con <see cref="Dispose"/>.</exception>
    /// <exception cref="FFmpegException">
    /// Se lanza si <paramref name="window"/> tiene duración no positiva, o si FFmpeg falla al
    /// decodificar el tramo.
    /// </exception>
    public async Task PlayAsync(string filePath, PreviewWindow window, TrackFades? fades, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (window.DurationSeconds <= 0)
        {
            throw new FFmpegException("La pista es demasiado corta para previsualizarla.");
        }

        Stop();
        int generation = ++_generation;

        string[] arguments =
        [
            "-v", "error",
            "-ss", Seconds(window.StartSeconds),
            "-i", filePath,
            "-t", Seconds(window.DurationSeconds),
            "-vn",
            "-ac", PlaybackFormat.Channels.ToString(CultureInfo.InvariantCulture),
            "-ar", PlaybackFormat.SampleRate.ToString(CultureInfo.InvariantCulture),
            "-f", "s16le",
            "-",
        ];

        byte[] pcm = await FFmpegRunner.ReadOutputAsync(_ffmpegPath, arguments, cancellationToken).ConfigureAwait(true);

        cancellationToken.ThrowIfCancellationRequested();
        if (_disposed || generation != _generation)
        {
            return;
        }

        if (fades is not null)
        {
            ApplyFades(pcm, fades, window);
        }

        WaveOut output = new() { BufferMilliseconds = BufferMilliseconds };
        output.PlaybackStopped += (_, _) => OnStopped(output);
        output.Init(new RawSourceWaveStream(pcm, 0, pcm.Length, PlaybackFormat));

        _output = output;
        _window = window;
        output.Play();
    }

    /// <summary>Atenúa el PCM decodificado con la misma envolvente que se aplicará a la copia.</summary>
    /// <remarks>
    /// Se trabaja sobre las muestras de 16 bits ya decodificadas: la ventana mide segundos y atenuar
    /// aquí ahorra pedir a FFmpeg flotantes solo para convertirlos después.
    /// </remarks>
    private static void ApplyFades(byte[] pcm, TrackFades fades, PreviewWindow window)
    {
        FadeEnvelope envelope = new(fades, PlaybackFormat.SampleRate, window.StartSeconds);
        Span<short> samples = MemoryMarshal.Cast<byte, short>(pcm.AsSpan());
        int channels = PlaybackFormat.Channels;

        for (int frame = 0; frame < samples.Length / channels; frame++)
        {
            double gain = envelope.GainAt(frame);
            if (gain >= 1.0)
            {
                continue;
            }

            Span<short> frameSamples = samples.Slice(frame * channels, channels);
            for (int channel = 0; channel < channels; channel++)
            {
                frameSamples[channel] = (short)Math.Round(frameSamples[channel] * gain);
            }
        }
    }

    /// <summary>Detiene la reproducción en curso, si la hay, sin producir <see cref="PlaybackCompleted"/>.</summary>
    public void Stop()
    {
        _generation++;

        // Se suelta antes de parar: así el aviso de parada que llega después ya no la reconoce
        // como la reproducción en curso y no se confunde con un final natural.
        WaveOut? output = _output;
        _output = null;

        if (output is not null)
        {
            output.Stop();
            output.Dispose();
        }
    }

    /// <summary>Detiene la reproducción y libera el dispositivo.</summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Stop();
    }

    private static string Seconds(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    /// <summary>Atiende el aviso de parada, que llega desde el hilo del dispositivo.</summary>
    private void OnStopped(WaveOut output)
    {
        void Complete()
        {
            if (!ReferenceEquals(_output, output))
            {
                return;
            }

            _output = null;
            output.Dispose();
            PlaybackCompleted?.Invoke(this, EventArgs.Empty);
        }

        if (_context is null)
        {
            Complete();
        }
        else
        {
            _context.Post(_ => Complete(), null);
        }
    }
}
