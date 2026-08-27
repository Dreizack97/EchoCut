using System.Diagnostics;
using System.Text;

namespace EchoCut.Audio;

/// <summary>Salidas capturadas de un proceso de FFmpeg ya terminado.</summary>
/// <param name="ExitCode">Código de salida del proceso.</param>
/// <param name="StandardOutput">Contenido íntegro de la salida estándar.</param>
/// <param name="StandardError">Contenido íntegro de la salida de error.</param>
public sealed record FFmpegResult(int ExitCode, string StandardOutput, string StandardError);

/// <summary>
/// Lanzamiento de procesos de FFmpeg. Los argumentos se pasan por <see cref="ProcessStartInfo.ArgumentList"/>,
/// de modo que las rutas con espacios o comillas no necesitan escaparse a mano.
/// </summary>
public static class FFmpegRunner
{
    /// <summary>Arranca el proceso sin esperar. El llamador es responsable de liberarlo.</summary>
    /// <param name="executable">Ruta al ejecutable a lanzar.</param>
    /// <param name="arguments">
    /// Argumentos de la línea de comandos, uno por elemento; no se aplica ningún escapado adicional.
    /// </param>
    /// <param name="redirectStandardOutput">
    /// Si se debe redirigir la salida estándar del proceso para poder leerla desde el llamador.
    /// </param>
    /// <returns>El proceso recién iniciado, ya en ejecución.</returns>
    /// <exception cref="FFmpegException">Se lanza si el sistema operativo no llega a crear el proceso.</exception>
    public static Process Start(string executable, IReadOnlyList<string> arguments, bool redirectStandardOutput)
    {
        ProcessStartInfo info = new()
        {
            FileName = executable,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = redirectStandardOutput,
            RedirectStandardError = true,
        };

        foreach (string argument in arguments)
        {
            info.ArgumentList.Add(argument);
        }

        return Process.Start(info)
               ?? throw new FFmpegException($"No se pudo iniciar el proceso '{executable}'.");
    }

    /// <summary>Ejecuta el proceso hasta el final y devuelve sus salidas.</summary>
    /// <param name="executable">Ruta al ejecutable a lanzar.</param>
    /// <param name="arguments">Argumentos de la línea de comandos.</param>
    /// <param name="cancellationToken">
    /// Token de cancelación. Si se activa mientras el proceso sigue vivo, se mata el árbol completo
    /// antes de propagar la cancelación.
    /// </param>
    /// <returns>Código de salida y contenido de ambas tuberías, ya leídas por completo.</returns>
    public static async Task<FFmpegResult> RunAsync(
        string executable,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken)
    {
        using Process process = Start(executable, arguments, redirectStandardOutput: true);

        // Ambas tuberías se drenan en paralelo: leer una y luego la otra se bloquea en cuanto
        // el búfer del sistema de la que no se está leyendo se llena.
        Task<string> stdout = process.StandardOutput.ReadToEndAsync(cancellationToken);
        Task<string> stderr = process.StandardError.ReadToEndAsync(cancellationToken);

        try
        {
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            return new FFmpegResult(process.ExitCode, await stdout.ConfigureAwait(false), await stderr.ConfigureAwait(false));
        }
        catch (OperationCanceledException)
        {
            KillQuietly(process);
            throw;
        }
    }

    /// <summary>Ejecuta el proceso y lanza <see cref="FFmpegException"/> si termina con error.</summary>
    /// <param name="executable">Ruta al ejecutable a lanzar.</param>
    /// <param name="arguments">Argumentos de la línea de comandos.</param>
    /// <param name="cancellationToken">Token de cancelación, propagado a <see cref="RunAsync"/>.</param>
    /// <exception cref="FFmpegException">
    /// Se lanza si el proceso termina con un código de salida distinto de cero, con la primera línea
    /// útil de <c>stderr</c> incluida en el mensaje.
    /// </exception>
    public static async Task RunCheckedAsync(
        string executable,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken)
    {
        FFmpegResult result = await RunAsync(executable, arguments, cancellationToken).ConfigureAwait(false);
        if (result.ExitCode != 0)
        {
            throw new FFmpegException(
                $"FFmpeg terminó con código {result.ExitCode}: {FirstLine(result.StandardError)}",
                result.ExitCode,
                result.StandardError);
        }
    }

    /// <summary>Drena stderr en segundo plano para que el proceso no se bloquee al llenarse la tubería.</summary>
    /// <param name="process">Proceso cuya salida de error se va a leer hasta el final.</param>
    /// <returns>
    /// Tarea que se completa con el contenido íntegro de <c>stderr</c>, o con una cadena vacía si la
    /// tubería se rompe porque el proceso se mató durante una cancelación.
    /// </returns>
    public static Task<string> DrainStandardErrorAsync(Process process) =>
        Task.Run(async () =>
        {
            try
            {
                return await process.StandardError.ReadToEndAsync().ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Si el proceso se mata durante una cancelación, la tubería se rompe: no hay
                // diagnóstico que recoger y tampoco es un fallo que deba propagarse.
                return string.Empty;
            }
        });

    /// <summary>Mata el proceso, si sigue vivo, sin propagar ningún error del propio intento.</summary>
    /// <param name="process">Proceso a terminar, junto con todo su árbol de hijos.</param>
    public static void KillQuietly(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (Exception)
        {
            // El proceso pudo terminar entre la comprobación y la llamada.
        }
    }

    /// <summary>Extrae la primera línea no vacía de un bloque de texto, típicamente de <c>stderr</c>.</summary>
    /// <param name="text">Texto de origen, potencialmente multilínea o vacío.</param>
    /// <returns>La primera línea con contenido, recortada de espacios; <c>"sin detalle"</c> si no hay ninguna.</returns>
    public static string FirstLine(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "sin detalle";
        }

        StringBuilder builder = new();
        foreach (string line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            string trimmed = line.Trim();
            if (trimmed.Length > 0)
            {
                builder.Append(trimmed);
                break;
            }
        }

        return builder.Length == 0 ? "sin detalle" : builder.ToString();
    }
}
