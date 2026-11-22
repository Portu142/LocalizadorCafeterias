using LocalizadorCafes.Domain.Common;

namespace LocalizadorCafes.Domain.Entities;

/// <summary>
/// Foto de una cafetería con su atribución. Las imágenes con licencia libre
/// (p. ej. Wikimedia Commons) exigen citar autor, licencia y origen.
/// </summary>
public class CafePhoto : BaseEntity
{
    public Guid CafeId { get; set; }
    public Cafe Cafe { get; set; } = null!;
    public string Url { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string License { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
}
