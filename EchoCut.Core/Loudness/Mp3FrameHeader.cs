namespace EchoCut.Loudness;

/// <summary>
/// Cabecera de 4 bytes de una trama MPEG-1/2/2.5 Layer III, con lo necesario para llegar al campo
/// <c>global_gain</c> de su información lateral.
/// </summary>
/// <remarks>
/// Solo se aceptan tramas Layer III con tasa y frecuencia válidas. El formato libre (índice de
/// tasa 0) se rechaza, como en MP3Gain: sin tasa declarada no se puede calcular dónde empieza la
/// trama siguiente sin decodificar.
/// </remarks>
internal readonly struct Mp3FrameHeader
{
    /// <summary>Bytes de la cabecera.</summary>
    public const int Size = 4;

    /// <summary>Bytes del CRC opcional que sigue a la cabecera.</summary>
    public const int CrcSize = 2;

    /// <summary>Lo más largo que puede ser cabecera + CRC + información lateral (MPEG-1 estéreo).</summary>
    public const int MaxHeadLength = Size + CrcSize + 32;

    private const int VersionMpeg25 = 0;
    private const int VersionMpeg2 = 2;
    private const int VersionMpeg1 = 3;
    private const int LayerIII = 1;
    private const int ChannelModeMono = 3;

    private static readonly int[] Mpeg1Bitrates = [0, 32, 40, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320];
    private static readonly int[] Mpeg2Bitrates = [0, 8, 16, 24, 32, 40, 48, 56, 64, 80, 96, 112, 128, 144, 160];
    private static readonly int[] Mpeg1Rates = [44100, 48000, 32000];
    private static readonly int[] Mpeg2Rates = [22050, 24000, 16000];
    private static readonly int[] Mpeg25Rates = [11025, 12000, 8000];

    private readonly int _version;
    private readonly int _rateIndex;

    private Mp3FrameHeader(int version, int rateIndex, bool hasCrc, int channels, int sampleRate, int frameLength)
    {
        _version = version;
        _rateIndex = rateIndex;
        HasCrc = hasCrc;
        Channels = channels;
        SampleRate = sampleRate;
        FrameLength = frameLength;
    }

    /// <value><c>true</c> en MPEG-1, con dos gránulos por trama; MPEG-2 y 2.5 tienen uno.</value>
    public bool IsMpeg1 => _version == VersionMpeg1;

    /// <value><c>true</c> si la trama lleva CRC, que habrá que recalcular al tocar la información lateral.</value>
    public bool HasCrc { get; }

    /// <value>1 en mono y 2 en el resto de modos (estéreo, conjunto y dual).</value>
    public int Channels { get; }

    /// <value>Frecuencia de muestreo, en Hz.</value>
    public int SampleRate { get; }

    /// <value>Longitud total de la trama en bytes, relleno incluido.</value>
    public int FrameLength { get; }

    /// <value>Gránulos por trama.</value>
    public int Granules => IsMpeg1 ? 2 : 1;

    /// <value>Bytes de la información lateral, que contiene los <c>global_gain</c>.</value>
    public int SideInfoLength => (IsMpeg1, Channels) switch
    {
        (true, 1) => 17,
        (true, _) => 32,
        (false, 1) => 9,
        (false, _) => 17,
    };

    /// <value>Cabecera + CRC + información lateral: lo que hay que leer para cambiar la ganancia.</value>
    public int HeadLength => Size + (HasCrc ? CrcSize : 0) + SideInfoLength;

    /// <summary>Interpreta los primeros 4 bytes como cabecera Layer III.</summary>
    /// <param name="bytes">Al menos 4 bytes desde la posición candidata.</param>
    /// <param name="header">La cabecera, si es válida.</param>
    /// <returns><c>true</c> si hay una cabecera Layer III válida.</returns>
    public static bool TryParse(ReadOnlySpan<byte> bytes, out Mp3FrameHeader header)
    {
        header = default;
        if (bytes.Length < Size || bytes[0] != 0xFF || (bytes[1] & 0xE0) != 0xE0)
        {
            return false;
        }

        int version = (bytes[1] >> 3) & 0x03;
        int layer = (bytes[1] >> 1) & 0x03;
        int bitrateIndex = bytes[2] >> 4;
        int rateIndex = (bytes[2] >> 2) & 0x03;

        if (version == 1 || layer != LayerIII || bitrateIndex is 0 or 15 || rateIndex == 3)
        {
            return false;
        }

        int[] rates = version switch
        {
            VersionMpeg1 => Mpeg1Rates,
            VersionMpeg2 => Mpeg2Rates,
            _ => Mpeg25Rates,
        };

        bool mpeg1 = version == VersionMpeg1;
        int bitrate = (mpeg1 ? Mpeg1Bitrates : Mpeg2Bitrates)[bitrateIndex] * 1000;
        int sampleRate = rates[rateIndex];
        int padding = (bytes[2] >> 1) & 0x01;

        // 1152 muestras por trama en MPEG-1 y 576 en MPEG-2/2.5, entre 8 bits por byte.
        int frameLength = ((mpeg1 ? 144 : 72) * bitrate / sampleRate) + padding;

        // El bit de protección a 0 significa que sí hay CRC.
        bool hasCrc = (bytes[1] & 0x01) == 0;
        int channels = (bytes[3] >> 6) == ChannelModeMono ? 1 : 2;

        header = new Mp3FrameHeader(version, rateIndex, hasCrc, channels, sampleRate, frameLength);
        return true;
    }

    /// <summary>Si otra cabecera pertenece al mismo flujo.</summary>
    /// <param name="other">Cabecera de referencia, normalmente la primera trama.</param>
    /// <returns><c>true</c> si coinciden la versión MPEG y la frecuencia.</returns>
    /// <remarks>
    /// Un flujo no cambia de versión ni de frecuencia a mitad: si cambian, el patrón de
    /// sincronía era una coincidencia dentro de datos de audio y hay que seguir buscando.
    /// </remarks>
    public bool SharesStreamWith(in Mp3FrameHeader other) =>
        _version == other._version && _rateIndex == other._rateIndex;

    /// <summary>Posición en bits del <c>global_gain</c> de un gránulo y canal, desde el inicio de la trama.</summary>
    /// <param name="granule">Gránulo, de 0 a <see cref="Granules"/> − 1.</param>
    /// <param name="channel">Canal, de 0 a <see cref="Channels"/> − 1.</param>
    /// <returns>Desplazamiento en bits del campo de 8 bits.</returns>
    /// <remarks>
    /// MPEG-1: <c>main_data_begin</c> (9), bits privados (5 en mono, 3 si no), <c>scfsi</c> (4 por
    /// canal) y 59 bits por gránulo y canal. MPEG-2: <c>main_data_begin</c> (8), privados (1 o 2) y
    /// 63 bits por canal. Dentro de cada bloque, <c>global_gain</c> va tras <c>part2_3_length</c>
    /// (12) y <c>big_values</c> (9).
    /// </remarks>
    public int GlobalGainBit(int granule, int channel)
    {
        int sideInfoStart = (Size + (HasCrc ? CrcSize : 0)) * 8;
        int offset = IsMpeg1
            ? 9 + (Channels == 1 ? 5 : 3) + (4 * Channels) + (((granule * Channels) + channel) * 59)
            : 8 + (Channels == 1 ? 1 : 2) + (channel * 63);

        return sideInfoStart + offset + 21;
    }
}
