using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using LocalizadorCafes.Application.Common.Interfaces;
using LocalizadorCafes.Domain.Entities;

namespace LocalizadorCafes.Infrastructure.Osm;

/// <summary>
/// Lee un elemento de la API oficial de OpenStreetMap (api.openstreetmap.org).
/// Es un uso ligero: una petición cuando un usuario guarda un café por
/// primera vez. Las vías (edificios) no traen coordenadas propias, así que se
/// pide la versión completa y se usa el centro de sus nodos.
/// </summary>
public sealed class OsmCafeSource : IOsmCafeSource
{
    private readonly HttpClient _httpClient;

    public OsmCafeSource(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<Cafe?> FetchAsync(string osmId, CancellationToken cancellationToken = default)
    {
        var parts = osmId.Split('/');
        var type = parts[0];
        var path = type == "way" ? $"way/{parts[1]}/full.json" : $"node/{parts[1]}.json";

        OsmResponse? payload;
        try
        {
            using var response = await _httpClient.GetAsync(path, cancellationToken);
            if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone)
            {
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new OsmUnavailableException($"OpenStreetMap respondió {(int)response.StatusCode}.");
            }

            payload = await response.Content.ReadFromJsonAsync<OsmResponse>(cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            throw new OsmUnavailableException("No se pudo conectar con OpenStreetMap.", exception);
        }

        var elements = payload?.Elements ?? new List<OsmElement>();
        var target = elements.FirstOrDefault(element => element.Type == type && $"{element.Type}/{element.Id}" == osmId);
        if (target?.Tags is null)
        {
            return null;
        }

        double latitude;
        double longitude;

        if (type == "node")
        {
            if (target.Lat is null || target.Lon is null)
            {
                return null;
            }

            latitude = target.Lat.Value;
            longitude = target.Lon.Value;
        }
        else
        {
            var nodes = elements.Where(element => element.Type == "node" && element.Lat is not null && element.Lon is not null).ToList();
            if (nodes.Count == 0)
            {
                return null;
            }

            latitude = nodes.Average(node => node.Lat!.Value);
            longitude = nodes.Average(node => node.Lon!.Value);
        }

        return OsmCafeMapper.ToCafe(target.Tags, latitude, longitude);
    }

    private sealed class OsmResponse
    {
        [JsonPropertyName("elements")]
        public List<OsmElement>? Elements { get; set; }
    }

    private sealed class OsmElement
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("lat")]
        public double? Lat { get; set; }

        [JsonPropertyName("lon")]
        public double? Lon { get; set; }

        [JsonPropertyName("tags")]
        public Dictionary<string, string>? Tags { get; set; }
    }
}
