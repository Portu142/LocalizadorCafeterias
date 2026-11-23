using LocalizadorCafes.Domain.Common;

namespace LocalizadorCafes.Domain.Entities;

/// <summary>
/// Registro de una acción del panel de administración: quién hizo qué, sobre
/// qué elemento y cuándo. Solo se añaden entradas; nunca se editan.
/// </summary>
public class AdminAuditEntry : BaseEntity
{
    public Guid? AdminUserId { get; set; }

    /// <summary>Nombre del administrador en el momento de la acción.</summary>
    public string AdminName { get; set; } = string.Empty;

    /// <summary>Acción legible: «Aprobar plato», «Bloquear usuario»…</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>Tipo de elemento: «plato», «foto», «reseña», «cafetería», «usuario».</summary>
    public string TargetType { get; set; } = string.Empty;

    public Guid? TargetId { get; set; }

    /// <summary>Descripción breve del elemento (nombre del plato, email…).</summary>
    public string Details { get; set; } = string.Empty;
}
