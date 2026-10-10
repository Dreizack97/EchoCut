using EchoCut.Library;
using EchoCut.Loudness;
using EchoCut.Processing;
using System.Globalization;

namespace EchoCut.Objects;

/// <summary>En qué punto está una canción dentro de la ventana de volumen.</summary>
public enum VolumeRowState
{
    /// <summary>Todavía no se ha medido.</summary>
    Pending,

    /// <summary>Se está midiendo, ajustando o restaurando.</summary>
    Working,

    /// <summary>Medida; la propuesta depende del objetivo.</summary>
    Measured,

    /// <summary>Se le acaba de aplicar el ajuste.</summary>
    Adjusted,

    /// <summary>Se le acaba de devolver su volumen original.</summary>
    Restored,

    /// <summary>La última operación falló.</summary>
    Failed,

    /// <summary>La última operación se detuvo antes de llegar a ella.</summary>
    Cancelled,
}

/// <summary>
/// Una canción de la ventana «Regularizar volumen»: su medida, la propuesta para el objetivo
/// vigente y lo que se le hizo, ya convertido en los textos y el color de su fila.
/// </summary>
/// <remarks>
/// <para>
/// Tiene dos vistas. Mientras hay una propuesta, «Volumen» es el nivel actual, «Ajuste» lo que se
/// propone y «Resultado» el nivel previsto. Justo después de aplicar o restaurar, «Volumen» es el
/// nivel de antes, «Ajuste» lo aplicado y «Resultado» el nivel con el que quedó: así se ve cómo
/// quedó la canción. Cambiar el objetivo vuelve a la primera vista si la canción necesita otro ajuste.
/// </para>
/// <para>
/// El nivel resultante no se vuelve a medir: cambiar <c>global_gain</c> escala todas las muestras
/// por el mismo factor, así que <see cref="ReplayGainResult.AfterSteps"/> lo calcula exactamente.
/// </para>
/// </remarks>
/// <param name="track">Canción que representa la fila.</param>
public sealed class VolumeRow(TrackInfo track)
{
    private const string Missing = "—";

    private Mp3GainScan? _scan;
    private ReplayGainResult? _current;
    private ReplayGainResult? _before;
    private int _changeSteps;
    private int? _adjustedSteps;
    private string _workingText = string.Empty;

    /// <value>La canción, releída del disco tras cada cambio.</value>
    public TrackInfo Track { get; private set; } = track;

    /// <value>Punto en el que está la fila.</value>
    public VolumeRowState State { get; private set; } = VolumeRowState.Pending;

    /// <value>Mensaje del último error, o <c>null</c> si no lo hubo.</value>
    public string? Error { get; private set; }

    /// <value>Pasos propuestos para el objetivo vigente, o <c>null</c> si aún no se midió.</value>
    public GainPlan? Plan { get; private set; }

    /// <value>Medida del audio tal como está ahora, o <c>null</c> si aún no se midió.</value>
    public ReplayGainResult? Current => _current;

    /// <value><c>true</c> si hay una propuesta distinta de cero que se puede aplicar.</value>
    public bool CanApply =>
        _current is not null
        && Plan is { AppliedSteps: not 0 }
        && State is VolumeRowState.Measured or VolumeRowState.Adjusted or VolumeRowState.Restored;

    /// <value>
    /// <c>true</c> si puede tener un ajuste que restaurar: lo tiene según sus etiquetas, o todavía no
    /// se han leído.
    /// </value>
    public bool CanRestore => _adjustedSteps is not 0 && State is not VolumeRowState.Working;

    /// <value><c>true</c> si la fila enseña lo que se acaba de hacer en lugar de una propuesta.</value>
    private bool ShowsChange => State is VolumeRowState.Adjusted or VolumeRowState.Restored;

    /// <summary>Marca la fila como en curso.</summary>
    /// <param name="text">Lo que se está haciendo, para la columna «Estado».</param>
    public void StartWork(string text)
    {
        _workingText = text;
        State = VolumeRowState.Working;
        Error = null;
    }

    /// <summary>Adopta una medida nueva.</summary>
    /// <param name="measurement">Lo medido.</param>
    /// <param name="targetDb">Objetivo vigente.</param>
    public void Measure(LoudnessMeasurement measurement, double targetDb)
    {
        ArgumentNullException.ThrowIfNull(measurement);
        Track = measurement.Track;
        _scan = measurement.Scan;
        _current = measurement.Measured;
        _adjustedSteps = measurement.AdjustedSteps;
        _before = null;
        State = VolumeRowState.Measured;
        UpdatePlan(targetDb);
    }

    /// <summary>Registra un ajuste aplicado.</summary>
    /// <param name="change">Lo aplicado.</param>
    /// <param name="targetDb">Objetivo vigente.</param>
    public void Apply(GainChange change, double targetDb)
    {
        ArgumentNullException.ThrowIfNull(change);
        Shift(change, VolumeRowState.Adjusted);
        UpdatePlan(targetDb);
    }

    /// <summary>Registra la vuelta al volumen original.</summary>
    /// <param name="change">Lo aplicado para restaurar; 0 pasos si no había ajuste.</param>
    /// <param name="targetDb">Objetivo vigente.</param>
    public void Restore(GainChange change, double targetDb)
    {
        ArgumentNullException.ThrowIfNull(change);
        if (change.Steps == 0)
        {
            _adjustedSteps = 0;
            State = _current is null ? VolumeRowState.Pending : VolumeRowState.Measured;
            UpdatePlan(targetDb);
            return;
        }

        Shift(change, VolumeRowState.Restored);
        _adjustedSteps = 0;
        UpdatePlan(targetDb);
    }

    /// <summary>Recalcula la propuesta tras cambiar el objetivo.</summary>
    /// <param name="targetDb">Objetivo nuevo.</param>
    /// <remarks>
    /// Una fila que enseñaba lo que se le hizo vuelve a enseñar su propuesta si con el nuevo objetivo
    /// necesita otro ajuste; si no, sigue enseñando cómo quedó.
    /// </remarks>
    public void Retarget(double targetDb)
    {
        UpdatePlan(targetDb);
        if (ShowsChange && Plan is { AppliedSteps: not 0 })
        {
            State = VolumeRowState.Measured;
            _before = null;
        }
    }

    /// <summary>Registra un error.</summary>
    /// <param name="message">Mensaje para la descripción emergente de la fila.</param>
    public void Fail(string message)
    {
        Error = message;
        State = VolumeRowState.Failed;
    }

    /// <summary>Deshace el estado «en curso» de una fila a la que no llegó una operación detenida.</summary>
    public void Cancel()
    {
        if (State == VolumeRowState.Working)
        {
            State = _current is null ? VolumeRowState.Cancelled : VolumeRowState.Measured;
        }
    }

    // ---------------------------------------------------------------------- Presentación

    /// <value>Texto de la columna «Estado», con un glifo que no depende del color.</value>
    public string StatusText => State switch
    {
        VolumeRowState.Pending => "Pendiente",
        VolumeRowState.Working => _workingText,
        VolumeRowState.Measured when Plan is { AppliedSteps: not 0 } => "● Por ajustar",
        VolumeRowState.Measured => "✔ Al nivel",
        VolumeRowState.Adjusted => "✔ Ajustado",
        VolumeRowState.Restored => "↺ Restaurado",
        VolumeRowState.Failed => "✖ Error",
        _ => "Detenido",
    };

    /// <value>Nivel de partida: el actual, o el de antes del último cambio.</value>
    public string VolumeText => Level(ShowsChange ? _before : _current);

    /// <value>Cambio propuesto o aplicado.</value>
    public string AdjustmentText
    {
        get
        {
            if (ShowsChange)
            {
                return Decibels(_changeSteps * Mp3GainEditor.StepDecibels);
            }

            if (Plan is not { } plan || _current is null)
            {
                return Missing;
            }

            return plan.IsLimited
                ? $"{Decibels(plan.AppliedDecibels)} (pedía {Decibels(plan.SuggestedSteps * Mp3GainEditor.StepDecibels)})"
                : Decibels(plan.AppliedDecibels);
        }
    }

    /// <value>Nivel previsto con la propuesta, o el nivel con el que quedó.</value>
    public string ResultText => Level(Result);

    /// <value>Pico actual en dBFS.</value>
    public string PeakText => _current is { Peak: > 0 } current
        ? string.Create(CultureInfo.CurrentCulture, $"{20 * Math.Log10(current.Peak):+0.0;-0.0;0.0} dBFS")
        : Missing;

    /// <value>Si el resultado satura o si la subida se limitó para no hacerlo.</value>
    public string ClippingText => Result switch
    {
        null => Missing,
        { Peak: > 1.0 } => "⚠ Satura",
        _ when !ShowsChange && Plan is { IsLimited: true } => "▲ Subida limitada",
        _ => "No",
    };

    /// <value>Ajuste que ya tiene la canción respecto a su volumen original.</value>
    public string AccumulatedText => _adjustedSteps switch
    {
        null => Missing,
        0 => "Original",
        { } steps => Decibels(steps * Mp3GainEditor.StepDecibels),
    };

    /// <summary>Color del texto de la fila sobre fondo normal, de la paleta accesible de EchoCut.</summary>
    /// <returns>Ámbar si hay algo por ajustar, verde si se acaba de cambiar, rojo si falló.</returns>
    public Color ForeColor() => State switch
    {
        VolumeRowState.Failed => SongPresentation.Failed,
        VolumeRowState.Adjusted or VolumeRowState.Restored => SongPresentation.Trimmed,
        VolumeRowState.Measured when Plan is { AppliedSteps: not 0 } => SongPresentation.Trimmable,
        VolumeRowState.Measured => SongPresentation.Neutral,
        _ => SongPresentation.Inconclusive,
    };

    private ReplayGainResult? Result => ShowsChange
        ? _current
        : _current?.AfterSteps(Plan?.AppliedSteps ?? 0);

    private void Shift(GainChange change, VolumeRowState state)
    {
        Track = change.Track;
        _before = _current;
        _current = _current?.AfterSteps(change.Steps);
        _scan = _scan?.AfterSteps(change.Steps);
        _adjustedSteps = (_adjustedSteps ?? 0) + change.Steps;
        _changeSteps = change.Steps;
        State = state;
    }

    private void UpdatePlan(double targetDb) =>
        Plan = _current is not null && _scan is not null ? GainPlanner.Plan(_current, targetDb, _scan) : null;

    private static string Level(ReplayGainResult? result) =>
        result is null ? Missing : string.Create(CultureInfo.CurrentCulture, $"{result.LevelDb:0.0} dB");

    private static string Decibels(double value) =>
        string.Create(CultureInfo.CurrentCulture, $"{value:+0.0;-0.0;0.0} dB");
}
