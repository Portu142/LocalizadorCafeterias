using LocalizadorCafes.Domain.Entities;
using NetTopologySuite.Geometries;

namespace LocalizadorCafes.Infrastructure.Osm;

/// <summary>
/// Convierte las etiquetas de un elemento de OpenStreetMap en una cafetería.
/// Las reglas de servicios son las mismas que usa el frontend (osmTags.ts).
/// </summary>
public static class OsmCafeMapper
{
    private static readonly HashSet<string> Yes = new(StringComparer.OrdinalIgnoreCase)
    {
        "yes", "only", "designated"
    };

    /// <summary>Null si el elemento no es una cafetería con nombre.</summary>
    public static Cafe? ToCafe(IReadOnlyDictionary<string, string> tags, double latitude, double longitude)
    {
        if (!tags.TryGetValue("amenity", out var amenity) || amenity != "cafe" ||
            !tags.TryGetValue("name", out var rawName) || string.IsNullOrWhiteSpace(rawName))
        {
            return null;
        }

        string Tag(string key) => tags.TryGetValue(key, out var value) ? value.Trim() : string.Empty;
        string FirstOf(params string[] keys) =>
            keys.Select(Tag).FirstOrDefault(value => value.Length > 0) ?? string.Empty;

        var street = string.Join(' ', new[] { Tag("addr:street"), Tag("addr:housenumber") }
            .Where(value => value.Length > 0));
        var address = string.Join(", ", new[] { street, Tag("addr:postcode") }
            .Where(value => value.Length > 0));

        return new Cafe
        {
            Name = Truncate(rawName.Trim(), 120),
            Address = Truncate(address.Length > 0 ? address : "Sin dirección registrada", 200),
            Neighborhood = Truncate(
                FirstOf("addr:suburb", "addr:neighbourhood", "addr:quarter", "addr:district"), 80),
            Phone = Truncate(FirstOf("phone", "contact:phone"), 40),
            Hours = Truncate(Tag("opening_hours"), 200),
            Location = new Point(longitude, latitude) { SRID = 4326 },
            Tags = MapTags(tags)
        };
    }

    public static List<string> MapTags(IReadOnlyDictionary<string, string> tags)
    {
        bool IsYes(string key) => tags.TryGetValue(key, out var value) && Yes.Contains(value);

        var result = new List<string>();

        if (tags.TryGetValue("internet_access", out var internet) &&
            internet.ToLowerInvariant() is "wlan" or "wifi" or "yes")
        {
            result.Add(CafeTags.Wifi);
        }

        if (IsYes("outdoor_seating"))
        {
            result.Add(CafeTags.Terrace);
        }

        if (IsYes("power_supply") ||
            tags.Any(tag => tag.Key.StartsWith("socket:", StringComparison.Ordinal) && tag.Value != "no"))
        {
            result.Add(CafeTags.PowerOutlets);
        }

        if (tags.TryGetValue("wheelchair", out var wheelchair) && wheelchair == "yes")
        {
            result.Add(CafeTags.Accessible);
        }

        if (IsYes("takeaway"))
        {
            result.Add(CafeTags.Takeaway);
        }

        if (IsYes("dog"))
        {
            result.Add(CafeTags.PetFriendly);
        }

        if (IsYes("diet:vegan"))
        {
            result.Add(CafeTags.Vegan);
        }

        return result;
    }

    private static string Truncate(string value, int max) => value.Length <= max ? value : value[..max];
}
