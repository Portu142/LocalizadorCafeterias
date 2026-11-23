using LocalizadorCafes.Domain.Entities;

namespace LocalizadorCafes.Application.Common.Interfaces;

/// <summary>Registro de actividad del panel de administración.</summary>
public interface IAuditLog
{
    Task WriteAsync(AdminAuditEntry entry, CancellationToken cancellationToken = default);

    /// <summary>Entradas de la más reciente a la más antigua, opcionalmente por tipo de elemento.</summary>
    Task<(IReadOnlyList<AdminAuditEntry> Items, int Total)> ListAsync(
        string? targetType,
        int skip,
        int take,
        CancellationToken cancellationToken = default);
}
