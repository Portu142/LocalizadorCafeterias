namespace LocalizadorCafes.Application.Cafes.Dtos;

/// <summary>
/// Guarda en la base de datos un café de OpenStreetMap. Solo se envía su id
/// («node/123» o «way/456»): el resto de datos los descarga el servidor de
/// OSM, así que nadie puede inventarse cafeterías.
/// </summary>
public record ImportOsmCafeRequest(string OsmId);
