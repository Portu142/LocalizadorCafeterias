using LocalizadorCafes.Domain.Common;

namespace LocalizadorCafes.Domain.Entities;

public class MenuItem : BaseEntity
{
    public Guid CafeId { get; set; }
    public Cafe Cafe { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public ICollection<MenuItemRating> Ratings { get; set; } = new List<MenuItemRating>();
}
