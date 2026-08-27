using EchoCut.Audio;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EchoCut.Options;

/// <summary>
/// Preferencias persistidas entre sesiones en <c>%APPDATA%\EchoCut\settings.json</c>.
/// Guardar aquí la carpeta de FFmpeg evita que el usuario tenga que localizarla en cada arranque.
/// </summary>
public sealed class AppSettings
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>Carpeta que contiene ffmpeg.exe y ffprobe.exe, si el usuario la eligió a mano.</summary>
    /// <value>Ruta de la carpeta elegida, o <c>null</c> si nunca se eligió ninguna.</value>
    public string? FFmpegDirectory { get; set; }

    /// <summary>Archivos procesados en paralelo.</summary>
    /// <value>Grado de paralelismo del lote, acotado a <c>[1, 64]</c> al cargar.</value>
    public int ThreadCount { get; set; } = DefaultThreadCount;

    /// <summary>Parámetros del algoritmo editables desde el diálogo "Avanzado".</summary>
    /// <value>Instancia de <see cref="SilenceOptions"/> vigente.</value>
    public SilenceOptions Silence { get; set; } = new();

    /// <summary>
    /// Por defecto se acotan los hilos: pasado cierto punto el cuello de botella deja de ser la CPU.
    /// Medido sobre 43 MP3 reales, de 1 a 4 hilos se gana 3,1×, de 4 a 8 solo 1,27× más, y por
    /// encima el rendimiento se aplana porque domina el arranque de procesos y la E/S de disco.
    /// </summary>
    /// <value>Grado de paralelismo por defecto, entre 1 y 8 según los núcleos disponibles.</value>
    public static int DefaultThreadCount => Math.Clamp(Environment.ProcessorCount, 1, 8);

    /// <summary>Ruta del archivo de configuración en disco.</summary>
    /// <value>Ruta absoluta a <c>%APPDATA%\EchoCut\settings.json</c>.</value>
    public static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "EchoCut",
        "settings.json");

    /// <summary>Carga la configuración persistida, o unos valores por defecto si no hay ninguna o es ilegible.</summary>
    /// <returns>
    /// Instancia cargada desde <see cref="FilePath"/>, con <see cref="Silence"/> ya acotado por
    /// <see cref="SilenceOptions.Normalize"/>; o una instancia con los valores por defecto si el
    /// archivo no existe, está corrupto o no se puede leer.
    /// </returns>
    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                AppSettings? loaded = JsonSerializer.Deserialize<AppSettings>(
                    File.ReadAllText(FilePath),
                    SerializerOptions);

                if (loaded is not null)
                {
                    loaded.Silence ??= new SilenceOptions();

                    // El archivo es texto editable a mano y puede venir de una versión anterior con
                    // parámetros que ya no existen o con valores fuera de rango. Acotarlo al cargar
                    // es lo que evita que un JSON retocado reviente el análisis mucho más adelante.
                    loaded.Silence.Normalize();
                    loaded.ThreadCount = Math.Clamp(loaded.ThreadCount, 1, 64);
                    return loaded;
                }
            }
        }
        catch (Exception exception) when (exception is IOException or JsonException or UnauthorizedAccessException)
        {
            // Una configuración corrupta o ilegible no debe impedir arrancar: se usan los valores
            // por defecto y el archivo se reescribirá en el próximo guardado.
        }

        return new AppSettings();
    }

    /// <summary>Guarda la configuración actual en <see cref="FilePath"/>.</summary>
    /// <remarks>
    /// Los fallos de E/S se silencian a propósito: guardar preferencias es accesorio y no debe
    /// interrumpir el trabajo del usuario ni hacer fallar el resto de la operación en curso.
    /// </remarks>
    public void Save()
    {
        try
        {
            string? directory = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, SerializerOptions));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Guardar preferencias es accesorio; no merece interrumpir el trabajo del usuario.
        }
    }
}
