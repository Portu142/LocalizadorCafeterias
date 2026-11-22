using LocalizadorCafes.Application.Common.Interfaces;

namespace LocalizadorCafes.Infrastructure.Storage;

/// <summary>
/// Guarda las fotos en una carpeta del servidor con un nombre aleatorio; el
/// nombre original del archivo nunca se usa (evita rutas maliciosas). Se
/// sirven desde <c>GET /api/photos/{archivo}</c>.
/// </summary>
public sealed class LocalPhotoStorage : IPhotoStorage
{
    public const string PublicPrefix = "/api/photos/";

    private readonly string _directory;

    public LocalPhotoStorage(string directory)
    {
        _directory = Path.GetFullPath(directory);
        Directory.CreateDirectory(_directory);
    }

    public async Task<string> SaveAsync(
        Stream content,
        string extension,
        CancellationToken cancellationToken = default)
    {
        var fileName = $"{Guid.NewGuid():N}.{extension}";
        var path = Path.Combine(_directory, fileName);

        await using (var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            await content.CopyToAsync(file, cancellationToken);
        }

        return PublicPrefix + fileName;
    }

    /// <summary>Ruta física de una foto, o null si el nombre no es válido o no existe.</summary>
    public string? Resolve(string fileName)
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(fileName, @"^[0-9a-f]{32}\.(jpg|png|webp)$"))
        {
            return null;
        }

        var path = Path.Combine(_directory, fileName);
        return File.Exists(path) ? path : null;
    }
}
