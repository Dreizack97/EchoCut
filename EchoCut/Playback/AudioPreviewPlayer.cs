using EchoCut.Audio;
using System.Globalization;
using System.Media;

namespace EchoCut.Playback;

/// <summary>
/// Reproduce un tramo corto de una pista para poder juzgar de oído el punto de corte.
/// </summary>
/// <remarks>
/// <para>
/// FFmpeg decodifica el tramo a WAV y <see cref="SoundPlayer"/> —que viene en el propio .NET— lo
/// reproduce, así que la previsualización no añade ninguna dependencia al proyecto. A cambio no hay
/// control de volumen ni de posición: solo sonar y parar, que es todo lo que una previsualización de
/// unos segundos necesita.
/// </para>
/// <para>
/// El WAV va a un archivo temporal y no a una tubería a propósito. Al escribir <c>-f wav</c> sobre
/// la salida estándar, FFmpeg no puede volver atrás a rellenar los tamaños de las cabeceras RIFF y
/// los deja marcados como indefinidos; <see cref="SoundPlayer"/> rechaza precisamente eso.
/// </para>
/// </remarks>
public sealed class AudioPreviewPlayer : IDisposable
{
    /// <summary>Frecuencia de reproducción. No es la de análisis: aquí sí se escucha.</summary>
    private const int PlaybackSampleRate = 44100;

    private readonly string _ffmpegPath;
    private readonly SoundPlayer _player = new();
    private readonly string _temporaryFile =
        Path.Combine(Path.GetTempPath(), $"echocut-preview-{Guid.NewGuid():N}.wav");

    private bool _disposed;

    /// <summary>Crea el reproductor contra el ejecutable de FFmpeg indicado.</summary>
    /// <param name="ffmpegPath">Ruta absoluta a <c>ffmpeg.exe</c>, ya resuelta.</param>
    public AudioPreviewPlayer(string ffmpegPath) => _ffmpegPath = ffmpegPath;

    /// <summary>Decodifica el tramo indicado y empieza a reproducirlo.</summary>
    /// <param name="filePath">Ruta del archivo de audio a previsualizar.</param>
    /// <param name="window">Tramo del archivo a reproducir.</param>
    /// <param name="cancellationToken">
    /// Token de cancelación para la decodificación con FFmpeg. Una vez iniciada la reproducción, no
    /// afecta al sonido ya en curso: para eso está <see cref="Stop"/>.
    /// </param>
    /// <returns>La duración real preparada, para saber cuándo dejará de sonar.</returns>
    /// <exception cref="ObjectDisposedException">Se lanza si el reproductor ya se liberó con <see cref="Dispose"/>.</exception>
    /// <exception cref="FFmpegException">
    /// Se lanza si <paramref name="window"/> tiene duración no positiva, o si FFmpeg falla al
    /// decodificar el tramo.
    /// </exception>
    public async Task<TimeSpan> PlayAsync(
        string filePath,
        PreviewWindow window,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (window.DurationSeconds <= 0)
        {
            throw new FFmpegException("La pista es demasiado corta para previsualizarla.");
        }

        Stop();

        string[] arguments =
        [
            "-v", "error",
            "-y",
            "-ss", window.StartSeconds.ToString("0.###", CultureInfo.InvariantCulture),
            "-i", filePath,
            "-t", window.DurationSeconds.ToString("0.###", CultureInfo.InvariantCulture),
            "-vn",
            "-ac", "2",
            "-ar", PlaybackSampleRate.ToString(CultureInfo.InvariantCulture),
            "-f", "wav",
            _temporaryFile,
        ];

        await FFmpegRunner.RunCheckedAsync(_ffmpegPath, arguments, cancellationToken).ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();

        _player.SoundLocation = _temporaryFile;
        _player.Load();
        _player.Play();

        return TimeSpan.FromSeconds(window.DurationSeconds);
    }

    /// <summary>Detiene la reproducción en curso, si la hay.</summary>
    public void Stop()
    {
        try
        {
            _player.Stop();
        }
        catch (Exception)
        {
            // Detener algo que no llegó a sonar no es un fallo del que haya que informar.
        }
    }

    /// <summary>Detiene la reproducción, libera el reproductor y borra el archivo temporal.</summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Stop();
        _player.Dispose();

        try
        {
            if (File.Exists(_temporaryFile))
            {
                File.Delete(_temporaryFile);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // El archivo temporal puede seguir bloqueado un instante tras detener la reproducción;
            // dejarlo atrás es preferible a impedir que la aplicación se cierre.
        }
    }
}
