namespace LocalizadorCafes.Application.Common.Interfaces;

/// <summary>Almacén de las fotos que suben los usuarios.</summary>
public interface IPhotoStorage
{
    /// <summary>Guarda la imagen y devuelve la ruta pública con la que se sirve.</summary>
    Task<string> SaveAsync(Stream content, string extension, CancellationToken cancellationToken = default);

    /// <summary>Borra una foto subida por su ruta pública; ignora las URLs externas.</summary>
    Task DeleteAsync(string publicUrl, CancellationToken cancellationToken = default);
}
