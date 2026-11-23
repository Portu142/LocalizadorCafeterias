using LocalizadorCafes.Domain.Common;

namespace LocalizadorCafes.Domain.Entities;

public class MenuItem : BaseEntity
{
    public Guid CafeId { get; set; }
    public Cafe Cafe { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }

    /// <summary>Usuario que añadió el plato (null en los datos de ejemplo).</summary>
    public Guid? AddedByUserId { get; set; }

    /// <summary>Los platos de los datos de ejemplo e importados nacen aprobados.</summary>
    public ModerationStatus Status { get; set; } = ModerationStatus.Approved;
    public ICollection<MenuItemRating> Ratings { get; set; } = new List<MenuItemRating>();
}
