using LocalizadorCafes.Domain.Common;

namespace LocalizadorCafes.Domain.Entities;

/// <summary>Puntuación de un plato del menú dentro de una reseña.</summary>
public class MenuItemRating : BaseEntity
{
    public Guid ReviewId { get; set; }
    public Review Review { get; set; } = null!;
    public Guid MenuItemId { get; set; }
    public MenuItem MenuItem { get; set; } = null!;
    public int Score { get; set; }
}
