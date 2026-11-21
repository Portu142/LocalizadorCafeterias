using LocalizadorCafes.Domain.Common;

namespace LocalizadorCafes.Domain.Entities;

public class Review : BaseEntity
{
    public Guid CafeId { get; set; }
    public Cafe Cafe { get; set; } = null!;
    public string Author { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public ICollection<MenuItemRating> MenuItemRatings { get; set; } = new List<MenuItemRating>();
}
