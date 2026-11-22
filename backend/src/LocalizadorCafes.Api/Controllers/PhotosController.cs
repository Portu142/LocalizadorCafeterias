using LocalizadorCafes.Application.Cafes;
using LocalizadorCafes.Infrastructure.Storage;
using Microsoft.AspNetCore.Mvc;

namespace LocalizadorCafes.Api.Controllers;

/// <summary>Sirve las fotos que suben los usuarios.</summary>
[ApiController]
[Route("api/photos")]
public class PhotosController : ControllerBase
{
    private readonly LocalPhotoStorage _storage;

    public PhotosController(LocalPhotoStorage storage)
    {
        _storage = storage;
    }

    [HttpGet("{fileName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Get(string fileName)
    {
        var path = _storage.Resolve(fileName);
        if (path is null)
        {
            return NotFound();
        }

        // Los nombres son aleatorios e inmutables: se pueden cachear un año.
        Response.Headers.CacheControl = "public, max-age=31536000, immutable";
        var extension = Path.GetExtension(path).TrimStart('.');
        return PhysicalFile(path, PhotoFormat.ContentTypeFor(extension));
    }
}
