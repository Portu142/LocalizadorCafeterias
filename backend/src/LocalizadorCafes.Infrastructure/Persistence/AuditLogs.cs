using LocalizadorCafes.Application.Common.Interfaces;
using LocalizadorCafes.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LocalizadorCafes.Infrastructure.Persistence;

/// <summary>Registro de actividad del panel en PostgreSQL.</summary>
public class EfAuditLog : IAuditLog
{
    private readonly AppDbContext _dbContext;

    public EfAuditLog(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task WriteAsync(AdminAuditEntry entry, CancellationToken cancellationToken = default)
    {
        _dbContext.AdminAuditLog.Add(entry);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<AdminAuditEntry> Items, int Total)> ListAsync(
        string? targetType, int skip, int take, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.AdminAuditLog.AsNoTracking();
        if (targetType is not null)
        {
            query = query.Where(entry => entry.TargetType == targetType);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(entry => entry.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return (items, total);
    }
}

/// <summary>Registro de actividad en memoria (modo demo; se pierde al reiniciar).</summary>
public sealed class InMemoryAuditLog : IAuditLog
{
    private readonly object _sync = new();
    private readonly List<AdminAuditEntry> _entries = new();

    public Task WriteAsync(AdminAuditEntry entry, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            _entries.Add(entry);
        }
        return Task.CompletedTask;
    }

    public Task<(IReadOnlyList<AdminAuditEntry> Items, int Total)> ListAsync(
        string? targetType, int skip, int take, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            var matches = _entries
                .Where(entry => targetType is null || entry.TargetType == targetType)
                .OrderByDescending(entry => entry.CreatedAt)
                .ToList();

            IReadOnlyList<AdminAuditEntry> page = matches.Skip(skip).Take(take).ToList();
            return Task.FromResult((page, matches.Count));
        }
    }
}
