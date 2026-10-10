using EchoCut.Library;
using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace EchoCut.Shell;

/// <summary>Estado de la opción «Abrir con EchoCut» en el menú contextual del Explorador.</summary>
public enum ExplorerIntegrationState
{
    /// <summary>No está registrada.</summary>
    NotRegistered,

    /// <summary>Registrada y apuntando a este ejecutable.</summary>
    Registered,

    /// <summary>Registrada, pero apunta a otro ejecutable: la aplicación se movió o hay otra copia.</summary>
    Outdated,
}

/// <summary>
/// Añade o quita «Abrir con EchoCut» del menú contextual del Explorador para archivos de audio y
/// carpetas.
/// </summary>
/// <remarks>
/// <para>
/// Se registra en <c>HKEY_CURRENT_USER\Software\Classes</c>, que no exige permisos de administrador
/// ni instalador: encaja con una distribución en zip. Los archivos usan
/// <c>SystemFileAssociations</c>, que añade la opción sin quitar a nadie la asociación de la
/// extensión: el doble clic sigue abriendo el reproductor de siempre.
/// </para>
/// <para>
/// En Windows 11 la opción aparece en «Mostrar más opciones»: el menú contextual moderno solo
/// admite verbos de aplicaciones empaquetadas (MSIX).
/// </para>
/// </remarks>
public static class ExplorerIntegration
{
    private const string VerbKey = "EchoCut";
    private const string MenuText = "Abrir con EchoCut";

    /// <summary>Evento de <c>SHChangeNotify</c> que pide al Explorador releer las asociaciones.</summary>
    private const int ShcneAssocChanged = 0x08000000;

    /// <summary>Ruta del ejecutable que se registra.</summary>
    /// <value>El ejecutable del proceso actual.</value>
    public static string ExecutablePath => Environment.ProcessPath ?? Application.ExecutablePath;

    /// <summary>Comprueba si la opción está registrada y apunta a este ejecutable.</summary>
    /// <returns>El estado del registro.</returns>
    /// <remarks>
    /// Basta con mirar la clave de carpetas: <see cref="Register"/> y <see cref="Unregister"/>
    /// escriben y borran todas a la vez.
    /// </remarks>
    public static ExplorerIntegrationState GetState()
    {
        using RegistryKey? command = Registry.CurrentUser.OpenSubKey($@"{DirectoryKey}\command");
        if (command?.GetValue(null) is not string registered)
        {
            return ExplorerIntegrationState.NotRegistered;
        }

        return registered.Equals(Command("%1"), StringComparison.OrdinalIgnoreCase)
            ? ExplorerIntegrationState.Registered
            : ExplorerIntegrationState.Outdated;
    }

    /// <summary>Registra la opción para las extensiones de audio admitidas y para las carpetas.</summary>
    /// <remarks>
    /// <c>MultiSelectModel=Player</c> mantiene la opción visible con más de quince elementos
    /// seleccionados, el límite por defecto del Explorador; las rutas de cada proceso lanzado se
    /// reúnen en una sola ventana mediante <see cref="SingleInstance"/>.
    /// </remarks>
    public static void Register()
    {
        foreach (string extension in AudioFileTypes.Extensions)
        {
            WriteVerb($@"Software\Classes\SystemFileAssociations\{extension}\shell\{VerbKey}", "%1");
        }

        WriteVerb(DirectoryKey, "%1");

        // En el fondo de una carpeta abierta no hay elemento seleccionado: %V es la carpeta misma.
        WriteVerb(BackgroundKey, "%V");

        NotifyExplorer();
    }

    /// <summary>Quita la opción de todas las claves en las que se registró.</summary>
    public static void Unregister()
    {
        foreach (string extension in AudioFileTypes.Extensions)
        {
            Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\SystemFileAssociations\{extension}\shell\{VerbKey}", throwOnMissingSubKey: false);
        }

        Registry.CurrentUser.DeleteSubKeyTree(DirectoryKey, throwOnMissingSubKey: false);
        Registry.CurrentUser.DeleteSubKeyTree(BackgroundKey, throwOnMissingSubKey: false);

        NotifyExplorer();
    }

    private static string DirectoryKey => $@"Software\Classes\Directory\shell\{VerbKey}";

    private static string BackgroundKey => $@"Software\Classes\Directory\Background\shell\{VerbKey}";

    private static string Command(string argument) => $"\"{ExecutablePath}\" \"{argument}\"";

    private static void WriteVerb(string keyPath, string argument)
    {
        using RegistryKey verb = Registry.CurrentUser.CreateSubKey(keyPath);
        verb.SetValue(null, MenuText);
        verb.SetValue("Icon", $"\"{ExecutablePath}\",0");
        verb.SetValue("MultiSelectModel", "Player");

        using RegistryKey command = verb.CreateSubKey("command");
        command.SetValue(null, Command(argument));
    }

    private static void NotifyExplorer() => SHChangeNotify(ShcneAssocChanged, 0, IntPtr.Zero, IntPtr.Zero);

    [DllImport("shell32.dll")]
    private static extern void SHChangeNotify(int eventId, uint flags, IntPtr item1, IntPtr item2);
}
