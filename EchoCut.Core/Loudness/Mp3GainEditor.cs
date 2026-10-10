namespace EchoCut.Loudness;

/// <summary>Lo que hay en las tramas de audio de un MP3, leído sin decodificar.</summary>
/// <param name="SampleRate">Frecuencia de muestreo de la primera trama, en Hz.</param>
/// <param name="FrameCount">Tramas de audio encontradas.</param>
/// <param name="MinGain">
/// <c>global_gain</c> más bajo distinto de cero, o 0 si todos lo son. Los ceros marcan gránulos de
/// silencio digital, que la ganancia no toca.
/// </param>
/// <param name="MaxGain"><c>global_gain</c> más alto.</param>
public sealed record Mp3GainScan(int SampleRate, int FrameCount, int MinGain, int MaxGain)
{
    /// <summary>El resumen tras sumar pasos a todos los gránulos que no son silencio.</summary>
    /// <param name="steps">Pasos aplicados, ya acotados para que ningún gránulo se sature.</param>
    /// <returns>El rango desplazado; sin cambios si todo es silencio digital.</returns>
    public Mp3GainScan AfterSteps(int steps) =>
        MaxGain == 0 ? this : this with { MinGain = MinGain + steps, MaxGain = MaxGain + steps };
}

/// <summary>
/// Cambia el volumen de un MP3 sin decodificarlo ni recodificarlo, a la manera de MP3Gain: suma un
/// número entero de pasos al campo <c>global_gain</c> de cada gránulo.
/// </summary>
/// <remarks>
/// <para>
/// <c>global_gain</c> es el exponente del cuantificador del gránulo: el decodificador escala las
/// muestras por 2^(global_gain/4). Sumar <c>n</c> multiplica la amplitud por 2^(n/4), es decir,
/// <c>n</c> × 1.505 dB, y lo hace sin tocar los datos cuantizados. Por eso el cambio no pierde
/// calidad y se deshace restando lo mismo, pero solo admite pasos de 1.5 dB.
/// </para>
/// <para>
/// Nunca se escribe directamente sobre el original: se trabaja sobre una copia temporal en la misma
/// carpeta y, si todo sale bien, sustituye al original con <see cref="File.Replace(string, string, string?, bool)"/>.
/// Cancelar o fallar a mitad deja el original intacto, cosa que MP3Gain no garantiza.
/// </para>
/// <para>
/// El cambio aplicado se anota en la etiqueta APE con los mismos campos que MP3Gain
/// (<c>MP3GAIN_UNDO</c>, <c>MP3GAIN_MINMAX</c>), así que tanto EchoCut como MP3Gain pueden
/// deshacerlo; los valores ReplayGain existentes se corrigen para que los reproductores que los
/// respetan no apliquen la ganancia dos veces.
/// </para>
/// </remarks>
public static class Mp3GainEditor
{
    /// <summary>Decibelios de un paso de <c>global_gain</c>: 5·log10(2).</summary>
    public const double StepDecibels = 1.5051499783199060;

    /// <summary>Valor máximo del campo de 8 bits.</summary>
    public const int MaxGlobalGain = 255;

    private const string MimeType = "audio/mpeg";
    private const string TemporarySuffix = ".echocut-gain.tmp";
    private const int BufferSize = 64 * 1024;

    /// <summary>Lee la frecuencia y el rango de <c>global_gain</c> de las tramas.</summary>
    /// <param name="filePath">Ruta del MP3.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>El resumen de las tramas.</returns>
    /// <exception cref="InvalidDataException">Se lanza si no hay tramas MPEG Layer III válidas.</exception>
    /// <exception cref="OperationCanceledException">Se lanza si se cancela.</exception>
    public static Mp3GainScan Scan(string filePath, CancellationToken cancellationToken)
    {
        (long start, long end) = AudioBounds(filePath);
        using FileStream stream = new(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize);
        return Walk(stream, start, end, steps: 0, cancellationToken);
    }

    /// <summary>
    /// Aplica <paramref name="steps"/> pasos de ganancia y anota el cambio y la medida en las etiquetas.
    /// </summary>
    /// <param name="filePath">Ruta del MP3 original, que se sustituye.</param>
    /// <param name="steps">Pasos de 1.5 dB; negativos para bajar el volumen. Con 0 no se hace nada.</param>
    /// <param name="measured">Medida ReplayGain de la pista antes del cambio.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <exception cref="InvalidDataException">
    /// Se lanza si no hay tramas válidas o si la etiqueta de deshacer de MP3Gain es de un cambio por
    /// canal o con desbordamiento, que no se pueden acumular con uno nuevo.
    /// </exception>
    /// <exception cref="OperationCanceledException">Se lanza si se cancela; el original queda intacto.</exception>
    public static void ChangeGain(string filePath, int steps, ReplayGainResult measured, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(measured);
        if (steps == 0)
        {
            return;
        }

        Rewrite(filePath, steps, cancellationToken, (file, scan) =>
        {
            Mp3GainTags.AddUndo(file, -steps);
            Mp3GainTags.SetMinMax(file, scan);
            Mp3GainTags.SetTrackGain(file, measured, steps);
        });
    }

    /// <summary>Ajuste acumulado que ya tiene el archivo respecto a su volumen original.</summary>
    /// <param name="filePath">Ruta del MP3.</param>
    /// <returns>Pasos de 1.5 dB aplicados en total; 0 si no tiene cambios anotados.</returns>
    /// <exception cref="InvalidDataException">
    /// Se lanza si la etiqueta es de un cambio por canal o con desbordamiento.
    /// </exception>
    public static int ReadAdjustment(string filePath)
    {
        using TagLib.File file = TagLib.File.Create(filePath, MimeType, TagLib.ReadStyle.None);
        return -Mp3GainTags.ReadUndo(file);
    }

    /// <summary>Deshace los cambios de volumen anotados en <c>MP3GAIN_UNDO</c>.</summary>
    /// <param name="filePath">Ruta del MP3 original, que se sustituye.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Pasos aplicados para deshacer; 0 si el archivo no tenía cambios anotados.</returns>
    /// <exception cref="InvalidDataException">
    /// Se lanza si no hay tramas válidas o si la etiqueta es de un cambio por canal o con desbordamiento.
    /// </exception>
    /// <exception cref="OperationCanceledException">Se lanza si se cancela; el original queda intacto.</exception>
    public static int Undo(string filePath, CancellationToken cancellationToken)
    {
        int steps;
        using (TagLib.File file = TagLib.File.Create(filePath, MimeType, TagLib.ReadStyle.None))
        {
            steps = Mp3GainTags.ReadUndo(file);
        }

        if (steps == 0)
        {
            return 0;
        }

        Rewrite(filePath, steps, cancellationToken, (file, scan) =>
        {
            Mp3GainTags.AddUndo(file, -steps);
            Mp3GainTags.SetMinMax(file, scan);
            Mp3GainTags.ShiftReplayGain(file, steps);
        });

        return steps;
    }

    /// <summary>Copia, modifica la copia y la pone en lugar del original.</summary>
    private static void Rewrite(
        string filePath,
        int steps,
        CancellationToken cancellationToken,
        Action<TagLib.File, Mp3GainScan> updateTags)
    {
        string temporary = filePath + TemporarySuffix;
        File.Copy(filePath, temporary, overwrite: true);

        try
        {
            (long start, long end) = AudioBounds(temporary);
            Mp3GainScan scan;
            using (FileStream stream = new(temporary, FileMode.Open, FileAccess.ReadWrite, FileShare.None, BufferSize))
            {
                scan = Walk(stream, start, end, steps, cancellationToken);
            }

            cancellationToken.ThrowIfCancellationRequested();

            using (TagLib.File file = TagLib.File.Create(temporary, MimeType, TagLib.ReadStyle.None))
            {
                updateTags(file, scan);
                file.Save();
            }

            File.Replace(temporary, filePath, destinationBackupFileName: null, ignoreMetadataErrors: true);
        }
        catch
        {
            DeleteQuietly(temporary);
            throw;
        }
    }

    /// <summary>
    /// Recorre las tramas, sumando <paramref name="steps"/> a cada <c>global_gain</c> si no es 0, y
    /// devuelve el resumen resultante.
    /// </summary>
    private static Mp3GainScan Walk(Stream stream, long start, long end, int steps, CancellationToken cancellationToken)
    {
        int sampleRate = 0;
        int min = int.MaxValue;
        int max = 0;

        int frames = Mp3FrameWalker.Walk(
            stream,
            start,
            end,
            (Span<byte> head, in Mp3FrameHeader header) =>
            {
                if (sampleRate == 0)
                {
                    sampleRate = header.SampleRate;
                }

                bool modified = false;
                for (int granule = 0; granule < header.Granules; granule++)
                {
                    for (int channel = 0; channel < header.Channels; channel++)
                    {
                        int bit = header.GlobalGainBit(granule, channel);
                        int gain = ReadByte(head, bit);

                        // Un global_gain de 0 marca un gránulo de silencio digital: MP3Gain no lo
                        // toca para que siga siéndolo y para que el cambio pueda deshacerse.
                        if (gain == 0)
                        {
                            continue;
                        }

                        if (steps != 0)
                        {
                            int changed = Math.Clamp(gain + steps, 0, MaxGlobalGain);
                            if (changed != gain)
                            {
                                WriteByte(head, bit, changed);
                                modified = true;
                                gain = changed;
                            }
                        }

                        min = Math.Min(min, gain);
                        max = Math.Max(max, gain);
                    }
                }

                if (modified && header.HasCrc)
                {
                    WriteCrc(head, header);
                }

                return modified;
            },
            cancellationToken);

        if (frames == 0)
        {
            throw new InvalidDataException("No se encontraron tramas MP3 (MPEG Layer III) válidas.");
        }

        return new Mp3GainScan(sampleRate, frames, min == int.MaxValue ? 0 : min, max);
    }

    /// <summary>Zona de audio del archivo, sin las etiquetas del principio ni del final.</summary>
    private static (long Start, long End) AudioBounds(string filePath)
    {
        using TagLib.File file = TagLib.File.Create(filePath, MimeType, TagLib.ReadStyle.None);
        return (file.InvariantStartPosition, file.InvariantEndPosition);
    }

    private static int ReadByte(ReadOnlySpan<byte> bytes, int bit)
    {
        int index = bit >> 3;
        int word = (bytes[index] << 8) | bytes[index + 1];
        return (word >> (8 - (bit & 7))) & 0xFF;
    }

    private static void WriteByte(Span<byte> bytes, int bit, int value)
    {
        int index = bit >> 3;
        int shift = 8 - (bit & 7);
        int word = (bytes[index] << 8) | bytes[index + 1];
        word = (word & ~(0xFF << shift)) | (value << shift);
        bytes[index] = (byte)(word >> 8);
        bytes[index + 1] = (byte)word;
    }

    /// <summary>
    /// Recalcula el CRC-16 de la trama: cubre los dos últimos bytes de la cabecera y la información
    /// lateral, justo lo que cambia al tocar <c>global_gain</c>.
    /// </summary>
    private static void WriteCrc(Span<byte> head, in Mp3FrameHeader header)
    {
        const int Polynomial = 0x8005;
        int crc = 0xFFFF;

        void Update(byte value)
        {
            int data = value << 8;
            for (int i = 0; i < 8; i++)
            {
                data <<= 1;
                crc <<= 1;
                if (((crc ^ data) & 0x10000) != 0)
                {
                    crc ^= Polynomial;
                }

                crc &= 0xFFFF;
            }
        }

        Update(head[2]);
        Update(head[3]);
        int sideInfoStart = Mp3FrameHeader.Size + Mp3FrameHeader.CrcSize;
        foreach (byte value in head.Slice(sideInfoStart, header.SideInfoLength))
        {
            Update(value);
        }

        head[4] = (byte)(crc >> 8);
        head[5] = (byte)crc;
    }

    private static void DeleteQuietly(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Si no se puede borrar la copia temporal, el error original sigue siendo lo que hay que contar.
        }
    }
}
