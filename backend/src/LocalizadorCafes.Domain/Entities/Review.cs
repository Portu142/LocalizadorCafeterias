using LocalizadorCafes.Domain.Common;

namespace LocalizadorCafes.Domain.Entities;

public class Review : BaseEntity
{
    public Guid CafeId { get; set; }
    public Cafe Cafe { get; set; } = null!;
    public string Author { get; set; } = string.Empty;
    public int Rating { get; set; }

    /// <summary>Puntuación del menú (1-5), independiente de la valoración general.</summary>
    public int MenuRating { get; set; }

    public string Comment { get; set; } = string.Empty;
}
