using System.Diagnostics;

namespace EchoCut.Shell;

/// <summary>
/// Integración con las aplicaciones del escritorio: mostrar un archivo en el Explorador y abrirlo
/// en Audacity para inspeccionarlo a mano.
/// </summary>
/// <remarks>
/// Vive en el proyecto de interfaz y no en el núcleo porque es integración con el escritorio de
/// Windows, no parte del motor: EchoCut sigue funcionando igual sin ninguna de las dos.
/// </remarks>
public static class ExternalApps
{
    private static readonly string[] AudacityCandidates =
    [
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Audacity", "audacity.exe"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Audacity", "audacity.exe"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Audacity", "audacity.exe"),
    ];

    /// <summary>Abre el Explorador con el archivo indicado ya seleccionado.</summary>
    /// <param name="filePath">Ruta del archivo a mostrar.</param>
    public static void RevealInExplorer(string filePath) => Launch("explorer.exe", $"/select,\"{filePath}\"");

    /// <summary>Busca Audacity en las rutas de instalación habituales.</summary>
    /// <returns>Ruta del ejecutable, o <c>null</c> si no está en ninguna de ellas.</returns>
    public static string? FindAudacity() => AudacityCandidates.FirstOrDefault(File.Exists);

    /// <summary>Abre un archivo en Audacity.</summary>
    /// <param name="audacityPath">Ruta del ejecutable de Audacity, obtenida con <see cref="FindAudacity"/>.</param>
    /// <param name="filePath">Ruta del archivo a abrir.</param>
    public static void OpenInAudacity(string audacityPath, string filePath) =>
        OpenInAudacity(audacityPath, [filePath]);

    /// <summary>Abre uno o varios archivos en Audacity.</summary>
    /// <param name="audacityPath">Ruta del ejecutable de Audacity, obtenida con <see cref="FindAudacity"/>.</param>
    /// <param name="filePaths">Colección de rutas de los archivos a abrir.</param>
    public static void OpenInAudacity(string audacityPath, IEnumerable<string> filePaths)
    {
        string arguments = string.Join(" ", filePaths.Select(path => $"\"{path}\""));
        if (!string.IsNullOrWhiteSpace(arguments))
        {
            Launch(audacityPath, arguments);
        }
    }

    private static void Launch(string fileName, string arguments) =>
        Process.Start(new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = true,
        });
}
