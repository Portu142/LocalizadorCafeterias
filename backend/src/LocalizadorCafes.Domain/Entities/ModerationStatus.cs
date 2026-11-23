namespace LocalizadorCafes.Domain.Entities;

/// <summary>
/// Estado de revisión del contenido que aportan los usuarios (platos y fotos).
/// Solo lo aprobado es público; lo pendiente lo ve únicamente su autor.
/// </summary>
public enum ModerationStatus
{
    Pending,
    Approved,
    Rejected
}
