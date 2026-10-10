namespace EchoCut.Loudness;

/// <summary>Cuántos pasos de 1.5 dB pide una pista y cuántos se le aplican.</summary>
/// <param name="SuggestedSteps">Pasos que la llevarían al objetivo.</param>
/// <param name="AppliedSteps">Pasos que se aplican tras protegerla de la saturación.</param>
public sealed record GainPlan(int SuggestedSteps, int AppliedSteps)
{
    /// <value>Cambio aplicado, en dB.</value>
    public double AppliedDecibels => AppliedSteps * Mp3GainEditor.StepDecibels;

    /// <value><c>true</c> si se aplicó menos de lo sugerido para no saturar.</value>
    public bool IsLimited => AppliedSteps != SuggestedSteps;
}

/// <summary>Decide la ganancia de cada pista a partir de su medida, como <c>mp3gain -r -k</c>.</summary>
public static class GainPlanner
{
    /// <summary>Objetivo por defecto: la referencia de ReplayGain y de MP3Gain.</summary>
    public const double DefaultTargetDb = ReplayGainAnalyzer.ReferenceLevelDb;

    /// <summary>Objetivo mínimo admitido.</summary>
    public const double MinimumTargetDb = 75.0;

    /// <summary>
    /// Objetivo máximo admitido. Por encima, casi toda la música comercial saturaría y la
    /// protección dejaría las pistas sin igualar.
    /// </summary>
    public const double MaximumTargetDb = 105.0;

    /// <summary>Calcula los pasos para llevar una pista al objetivo sin saturarla.</summary>
    /// <param name="measured">Medida ReplayGain de la pista.</param>
    /// <param name="targetDb">Nivel objetivo, en dB.</param>
    /// <param name="scan">Rango de <c>global_gain</c> de sus tramas.</param>
    /// <returns>Los pasos sugeridos y los aplicables.</returns>
    /// <remarks>
    /// <para>
    /// Los pasos se redondean al entero más próximo, alejándose de cero en el empate, igual que
    /// MP3Gain: la pista queda a menos de 0.75 dB del objetivo.
    /// </para>
    /// <para>
    /// La protección solo frena subidas: una subida se limita a los pasos que caben antes de que el
    /// pico llegue a 0 dBFS (<c>floor(4·log2(1/pico))</c>), y si el pico ya pasa de 0 dBFS no se
    /// sube nada. A diferencia de <c>mp3gain -k</c>, nunca se baja una pista por debajo de lo
    /// sugerido: la saturación que ya trae no empeora, y bajarla la dejaría desigualada.
    /// </para>
    /// <para>
    /// Además, el resultado se mantiene dentro de 1–255 en todos los gránulos: si se acotara alguno,
    /// el cambio dejaría de ser exactamente reversible.
    /// </para>
    /// </remarks>
    public static GainPlan Plan(ReplayGainResult measured, double targetDb, Mp3GainScan scan)
    {
        ArgumentNullException.ThrowIfNull(measured);
        ArgumentNullException.ThrowIfNull(scan);

        double gain = measured.GainDb + (targetDb - ReplayGainAnalyzer.ReferenceLevelDb);
        int suggested = (int)Math.Round(gain / Mp3GainEditor.StepDecibels, MidpointRounding.AwayFromZero);

        if (scan.MaxGain == 0)
        {
            // Todo el archivo es silencio digital: no hay nada que escalar.
            return new GainPlan(suggested, 0);
        }

        int applied = suggested;
        if (applied > 0 && measured.Peak > 0)
        {
            int headroom = (int)Math.Floor(4.0 * Math.Log2(1.0 / measured.Peak));
            applied = Math.Min(applied, Math.Max(headroom, 0));
        }

        applied = Math.Clamp(applied, 1 - scan.MinGain, Mp3GainEditor.MaxGlobalGain - scan.MaxGain);
        return new GainPlan(suggested, applied);
    }
}
