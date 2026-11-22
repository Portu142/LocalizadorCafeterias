namespace LocalizadorCafes.Application.Cafes;

/// <summary>
/// Detecta el formato real de una imagen por su firma (magic bytes). No se
/// confía en la extensión ni en el Content-Type que envía el cliente: un
/// archivo HTML renombrado a .jpg se rechaza.
/// </summary>
public static class PhotoFormat
{
    public const long MaxBytes = 5 * 1024 * 1024;

    /// <summary>Bytes necesarios para reconocer cualquiera de los formatos.</summary>
    public const int HeaderLength = 12;

    public static string? DetectExtension(MemoryStream content)
    {
        var length = (int)Math.Min(content.Length, HeaderLength);
        return DetectExtension(content.GetBuffer().AsSpan(0, length));
    }

    public static string? DetectExtension(ReadOnlySpan<byte> header)
    {
        if (header.Length >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
        {
            return "jpg";
        }

        if (header.Length >= 8 &&
            header[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
        {
            return "png";
        }

        if (header.Length >= 12 &&
            header[..4].SequenceEqual("RIFF"u8) &&
            header[8..12].SequenceEqual("WEBP"u8))
        {
            return "webp";
        }

        return null;
    }

    public static string ContentTypeFor(string extension) => extension switch
    {
        "jpg" => "image/jpeg",
        "png" => "image/png",
        "webp" => "image/webp",
        _ => "application/octet-stream"
    };
}
