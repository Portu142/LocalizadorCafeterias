using LocalizadorCafes.Domain.Entities;

namespace LocalizadorCafes.Application.Common.Interfaces;

/// <summary>Descarga de OpenStreetMap los datos de una cafetería.</summary>
public interface IOsmCafeSource
{
    /// <summary>
    /// Café con nombre, dirección, coordenadas, horario y servicios, o null si
    /// el elemento no existe o no es una cafetería. Lanza
    /// <see cref="OsmUnavailableException"/> si OSM no responde.
    /// </summary>
    Task<Cafe?> FetchAsync(string osmId, CancellationToken cancellationToken = default);
}

public sealed class OsmUnavailableException : Exception
{
    public OsmUnavailableException(string message, Exception? inner = null)
        : base(message, inner)
    {
    }
}
