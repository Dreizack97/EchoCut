using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;

namespace EchoCut.Shell;

/// <summary>
/// Garantiza una sola ventana de EchoCut por sesión y le hace llegar las rutas con las que se
/// vuelva a lanzar la aplicación.
/// </summary>
/// <remarks>
/// <para>
/// El Explorador lanza un proceso por cada archivo seleccionado al usar «Abrir con EchoCut»: sin
/// esto, seleccionar cincuenta canciones abriría cincuenta ventanas. El primer proceso se queda con
/// un mutex y escucha en una tubería con nombre; los siguientes le envían sus rutas y terminan.
/// </para>
/// <para>
/// Mutex y tubería son locales a la sesión y la tubería solo admite al mismo usuario
/// (<see cref="PipeOptions.CurrentUserOnly"/>): otra cuenta de la máquina no puede inyectar rutas.
/// </para>
/// </remarks>
public sealed class SingleInstance : IDisposable
{
    /// <summary>Identificador común al mutex y a la tubería.</summary>
    private const string Id = "EchoCut.SingleInstance.7c1f0e2a";

    /// <summary>Lo que espera un proceso secundario a que el principal atienda la tubería.</summary>
    /// <remarks>
    /// El principal puede estar todavía arrancando cuando llegan los primeros secundarios, que el
    /// Explorador lanza casi a la vez; unos segundos bastan para que empiece a escuchar.
    /// </remarks>
    private const int ConnectTimeoutMilliseconds = 5000;

    /// <summary>Valor de <c>AllowSetForegroundWindow</c> que autoriza a cualquier proceso.</summary>
    private const int AsfwAny = -1;

    private readonly Mutex _mutex;
    private readonly CancellationTokenSource _listening = new();

    /// <summary>Intenta convertirse en la instancia principal.</summary>
    public SingleInstance()
    {
        _mutex = new Mutex(initiallyOwned: true, $@"Local\{Id}", out bool createdNew);
        IsPrimary = createdNew;
    }

    /// <summary>Si este proceso es la instancia principal, la que muestra la ventana.</summary>
    /// <value><c>true</c> si no había otra instancia en la sesión.</value>
    public bool IsPrimary { get; }

    /// <summary>Nombre de la tubería, distinto por usuario para no cruzar sesiones de escritorio remoto.</summary>
    private static string PipeName => $"{Id}.{Environment.UserName}";

    /// <summary>Envía las rutas a la instancia principal.</summary>
    /// <param name="paths">Rutas recibidas por línea de comandos; puede estar vacía.</param>
    /// <returns><c>true</c> si la instancia principal las recibió.</returns>
    /// <remarks>
    /// Las rutas se envían absolutas porque la instancia principal no comparte el directorio de
    /// trabajo de este proceso. Antes de enviarlas se cede el derecho a pasar al primer plano: el
    /// Explorador lo concede a este proceso, no a la ventana que ya estaba abierta.
    /// </remarks>
    public static bool TrySend(IReadOnlyList<string> paths)
    {
        AllowSetForegroundWindow(AsfwAny);

        try
        {
            using NamedPipeClientStream client = new(".", PipeName, PipeDirection.Out, PipeOptions.CurrentUserOnly);
            client.Connect(ConnectTimeoutMilliseconds);

            using StreamWriter writer = new(client, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            foreach (string path in paths)
            {
                writer.WriteLine(Path.GetFullPath(path));
            }

            return true;
        }
        catch (Exception exception) when (exception is TimeoutException or IOException or UnauthorizedAccessException or ArgumentException)
        {
            return false;
        }
    }

    /// <summary>
    /// Empieza a atender, en segundo plano, a las instancias secundarias.
    /// </summary>
    /// <param name="onPaths">
    /// Recibe las rutas de cada instancia secundaria, quizá ninguna si solo se volvió a abrir la
    /// aplicación. Se invoca desde un hilo de trabajo.
    /// </param>
    public void Listen(Action<IReadOnlyList<string>> onPaths)
    {
        CancellationToken token = _listening.Token;

        _ = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await using NamedPipeServerStream server = new(
                        PipeName,
                        PipeDirection.In,
                        1,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);

                    await server.WaitForConnectionAsync(token).ConfigureAwait(false);

                    using StreamReader reader = new(server, Encoding.UTF8);
                    List<string> paths = [];
                    while (await reader.ReadLineAsync(token).ConfigureAwait(false) is { } line)
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            paths.Add(line);
                        }
                    }

                    onPaths(paths);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (IOException)
                {
                    // Un secundario que se cierra a medio envío no debe dejar de atender al resto.
                }
            }
        }, token);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _listening.Cancel();
        _listening.Dispose();

        if (IsPrimary)
        {
            _mutex.ReleaseMutex();
        }

        _mutex.Dispose();
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AllowSetForegroundWindow(int processId);
}
