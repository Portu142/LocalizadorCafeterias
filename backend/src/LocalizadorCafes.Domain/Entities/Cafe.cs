using LocalizadorCafes.Domain.Common;
using NetTopologySuite.Geometries;

namespace LocalizadorCafes.Domain.Entities;

public class Cafe : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Neighborhood { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Hours { get; set; } = string.Empty;
    public Point Location { get; set; } = null!;
    public ICollection<MenuItem> Menu { get; set; } = new List<MenuItem>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}
