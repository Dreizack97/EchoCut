namespace EchoCut.Loudness;

/// <summary>
/// Recorre las tramas Layer III de la zona de audio de un MP3 y deja que un visitante lea o
/// modifique su cabecera e información lateral, sin cargar el archivo en memoria.
/// </summary>
/// <remarks>
/// <para>
/// Sigue la estrategia de <c>frameSearch</c> de MP3Gain: la primera cabecera válida fija la
/// versión MPEG y la frecuencia, y a partir de ahí se salta de trama en trama por su longitud; si
/// en la posición esperada no hay una cabecera coherente, se busca byte a byte la siguiente.
/// </para>
/// <para>
/// Dos defensas que MP3Gain no tiene: la zona se acota con las posiciones de TagLibSharp, para que
/// un patrón de sincronía dentro de una etiqueta APE o ID3v1 final nunca se tome por audio, y la
/// primera trama solo se acepta si la siguiente también encaja, para no engancharse a basura
/// después de la etiqueta ID3v2.
/// </para>
/// </remarks>
internal static class Mp3FrameWalker
{
    /// <summary>Recibe cada trama de audio.</summary>
    /// <param name="head">Cabecera, CRC e información lateral de la trama, modificables.</param>
    /// <param name="header">La cabecera ya interpretada.</param>
    /// <returns><c>true</c> si el visitante modificó <paramref name="head"/> y hay que escribirlo.</returns>
    public delegate bool FrameVisitor(Span<byte> head, in Mp3FrameHeader header);

    /// <summary>Recorre las tramas entre dos posiciones.</summary>
    /// <param name="stream">Archivo abierto; con escritura si el visitante modifica tramas.</param>
    /// <param name="start">Primer byte de audio, tras las etiquetas iniciales.</param>
    /// <param name="end">Byte siguiente al último de audio, antes de las etiquetas finales.</param>
    /// <param name="visitor">Lo que se hace con cada trama.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Tramas de audio visitadas; la de información VBR (Xing/Info/VBRI) no cuenta.</returns>
    /// <exception cref="OperationCanceledException">Se lanza si se cancela.</exception>
    public static int Walk(
        Stream stream,
        long start,
        long end,
        FrameVisitor visitor,
        CancellationToken cancellationToken)
    {
        Span<byte> head = stackalloc byte[Mp3FrameHeader.MaxHeadLength];
        Mp3FrameHeader reference = default;
        bool synced = false;
        int frames = 0;
        int steps = 0;
        long position = start;

        while (position + Mp3FrameHeader.Size <= end)
        {
            if ((++steps & 0x3FF) == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            if (!TryReadHeader(stream, position, head, out Mp3FrameHeader header)
                || (synced ? !header.SharesStreamWith(reference) : !IsConfirmed(stream, position, end, header)))
            {
                position++;
                continue;
            }

            int headLength = header.HeadLength;
            if (position + headLength > end || !ReadAt(stream, position, head[..headLength]))
            {
                break;
            }

            bool first = !synced;
            reference = header;
            synced = true;

            // La primera trama de un MP3 VBR suele ser la cabecera Xing/Info o VBRI: lleva la
            // información lateral a cero y no contiene audio, así que no se toca.
            if (!(first && IsVbrInfoFrame(stream, position, header)))
            {
                if (visitor(head[..headLength], header))
                {
                    stream.Position = position;
                    stream.Write(head[..headLength]);
                }

                frames++;
            }

            position += header.FrameLength;
        }

        return frames;
    }

    private static bool TryReadHeader(Stream stream, long position, Span<byte> buffer, out Mp3FrameHeader header)
    {
        header = default;
        return ReadAt(stream, position, buffer[..Mp3FrameHeader.Size])
            && Mp3FrameHeader.TryParse(buffer, out header);
    }

    /// <summary>Si tras la trama candidata viene otra del mismo flujo, o se acaba el audio.</summary>
    private static bool IsConfirmed(Stream stream, long position, long end, in Mp3FrameHeader header)
    {
        long next = position + header.FrameLength;
        if (next + Mp3FrameHeader.Size > end)
        {
            // Es la última trama: no queda otra con la que confirmarla.
            return true;
        }

        Span<byte> buffer = stackalloc byte[Mp3FrameHeader.Size];
        return TryReadHeader(stream, next, buffer, out Mp3FrameHeader following)
            && following.SharesStreamWith(header);
    }

    /// <summary>Si la trama lleva la etiqueta Xing, Info o VBRI en lugar de audio.</summary>
    private static bool IsVbrInfoFrame(Stream stream, long position, in Mp3FrameHeader header)
    {
        Span<byte> tag = stackalloc byte[4];

        // Xing/Info va justo después de la información lateral; VBRI, a 32 bytes de la cabecera.
        return (ReadAt(stream, position + header.HeadLength, tag) && (tag.SequenceEqual("Xing"u8) || tag.SequenceEqual("Info"u8)))
            || (ReadAt(stream, position + Mp3FrameHeader.Size + 32, tag) && tag.SequenceEqual("VBRI"u8));
    }

    private static bool ReadAt(Stream stream, long position, Span<byte> buffer)
    {
        stream.Position = position;
        return stream.ReadAtLeast(buffer, buffer.Length, throwOnEndOfStream: false) == buffer.Length;
    }
}
