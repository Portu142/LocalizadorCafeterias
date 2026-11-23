using FluentValidation;
using FluentValidation.Results;
using LocalizadorCafes.Application.Common.Interfaces;
using LocalizadorCafes.Domain.Entities;

namespace LocalizadorCafes.Application.Admin;

/// <summary>
/// Operaciones del panel de administración. Cada cambio queda anotado en el
/// registro de actividad con el administrador que lo hizo.
/// </summary>
public class AdminService
{
    public const int PageSize = 25;

    private readonly IAdminRepository _repository;
    private readonly IAuditLog _auditLog;
    private readonly IPhotoStorage _photoStorage;

    public AdminService(IAdminRepository repository, IAuditLog auditLog, IPhotoStorage photoStorage)
    {
        _repository = repository;
        _auditLog = auditLog;
        _photoStorage = photoStorage;
    }

    private static int Skip(int page) => (Math.Max(1, page) - 1) * PageSize;

    // --- Cafeterías -------------------------------------------------------

    public async Task<PagedResult<AdminCafeRow>> SearchCafesAsync(string? term, int page, CancellationToken cancellationToken = default)
    {
        var (items, total) = await _repository.SearchCafesAsync(Clean(term), Skip(page), PageSize, cancellationToken);
        return new PagedResult<AdminCafeRow>(items, total, Math.Max(1, page), PageSize);
    }

    public async Task<bool> UpdateCafeAsync(Guid id, CafeEdit edit, AdminActor actor, CancellationToken cancellationToken = default)
    {
        var errors = new List<ValidationFailure>();
        if (edit.Name.Trim().Length is < 2 or > 120)
        {
            errors.Add(new ValidationFailure(nameof(edit.Name), "El nombre debe tener entre 2 y 120 caracteres."));
        }
        if (edit.Address.Trim().Length is < 2 or > 200)
        {
            errors.Add(new ValidationFailure(nameof(edit.Address), "La dirección debe tener entre 2 y 200 caracteres."));
        }
        if (edit.Neighborhood.Trim().Length > 80 || edit.Phone.Trim().Length > 40 || edit.Hours.Trim().Length > 200)
        {
            errors.Add(new ValidationFailure(nameof(edit.Hours), "Barrio (80), teléfono (40) u horario (200) superan la longitud máxima."));
        }
        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }

        var clean = edit with
        {
            Name = edit.Name.Trim(),
            Address = edit.Address.Trim(),
            Neighborhood = edit.Neighborhood.Trim(),
            Phone = edit.Phone.Trim(),
            Hours = edit.Hours.Trim(),
            Tags = edit.Tags.Where(CafeTags.All.Contains).Distinct().ToList()
        };

        var updated = await _repository.UpdateCafeAsync(id, clean, cancellationToken);
        if (updated)
        {
            await AuditAsync(actor, "Editar cafetería", "cafetería", id, clean.Name, cancellationToken);
        }
        return updated;
    }

    public async Task<bool> DeleteCafeAsync(Guid id, string name, AdminActor actor, CancellationToken cancellationToken = default)
    {
        // Las fotos subidas se borran también del disco.
        var (photos, _) = await _repository.GetPhotosAsync(null, null, id, 0, 1000, cancellationToken);
        var deleted = await _repository.DeleteCafeAsync(id, cancellationToken);
        if (!deleted)
        {
            return false;
        }

        foreach (var photo in photos)
        {
            await _photoStorage.DeleteAsync(photo.Url, cancellationToken);
        }

        await AuditAsync(actor, "Borrar cafetería", "cafetería", id, name, cancellationToken);
        return true;
    }

    // --- Platos -----------------------------------------------------------

    public async Task<PagedResult<AdminDishRow>> GetDishesAsync(
        ModerationStatus? status, string? term, int page,
        Guid? userId = null, Guid? cafeId = null, CancellationToken cancellationToken = default)
    {
        var take = cafeId is null && userId is null ? PageSize : 500;
        var skip = cafeId is null && userId is null ? Skip(page) : 0;
        var (items, total) = await _repository.GetDishesAsync(status, Clean(term), userId, cafeId, skip, take, cancellationToken);
        return new PagedResult<AdminDishRow>(items, total, Math.Max(1, page), take);
    }

    public async Task<bool> UpdateDishAsync(Guid id, string name, decimal price, AdminActor actor, CancellationToken cancellationToken = default)
    {
        var cleanName = name?.Trim() ?? string.Empty;
        if (cleanName.Length is < 2 or > 120 || price <= 0 || price > 500 || decimal.Round(price, 2) != price)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(name), "Nombre de 2 a 120 caracteres y precio entre 0,01 y 500 € con 2 decimales.")
            });
        }

        var updated = await _repository.UpdateDishAsync(id, cleanName, price, cancellationToken);
        if (updated)
        {
            await AuditAsync(actor, "Editar plato", "plato", id, $"{cleanName} ({price:0.00} €)", cancellationToken);
        }
        return updated;
    }

    public async Task<bool> DeleteDishAsync(Guid id, string name, AdminActor actor, CancellationToken cancellationToken = default)
    {
        var deleted = await _repository.DeleteDishAsync(id, cancellationToken);
        if (deleted)
        {
            await AuditAsync(actor, "Borrar plato", "plato", id, name, cancellationToken);
        }
        return deleted;
    }

    // --- Fotos ------------------------------------------------------------

    public async Task<PagedResult<AdminPhotoRow>> GetPhotosAsync(
        ModerationStatus? status, int page,
        Guid? userId = null, Guid? cafeId = null, CancellationToken cancellationToken = default)
    {
        var take = cafeId is null && userId is null ? PageSize : 500;
        var skip = cafeId is null && userId is null ? Skip(page) : 0;
        var (items, total) = await _repository.GetPhotosAsync(status, userId, cafeId, skip, take, cancellationToken);
        return new PagedResult<AdminPhotoRow>(items, total, Math.Max(1, page), take);
    }

    public async Task<bool> DeletePhotoAsync(Guid id, AdminActor actor, CancellationToken cancellationToken = default)
    {
        var photo = await _repository.DeletePhotoAsync(id, cancellationToken);
        if (photo is null)
        {
            return false;
        }

        await _photoStorage.DeleteAsync(photo.Url, cancellationToken);
        await AuditAsync(actor, "Borrar foto", "foto", id, $"de {photo.Author}", cancellationToken);
        return true;
    }

    // --- Reseñas ----------------------------------------------------------

    public async Task<PagedResult<AdminReviewRow>> GetReviewsAsync(
        string? term, int page,
        Guid? userId = null, Guid? cafeId = null, CancellationToken cancellationToken = default)
    {
        var take = cafeId is null && userId is null ? PageSize : 500;
        var skip = cafeId is null && userId is null ? Skip(page) : 0;
        var (items, total) = await _repository.GetReviewsAsync(Clean(term), userId, cafeId, skip, take, cancellationToken);
        return new PagedResult<AdminReviewRow>(items, total, Math.Max(1, page), take);
    }

    // --- Estadísticas y registro -------------------------------------------

    /// <summary>
    /// Recuentos por semana (lunes) de las últimas <paramref name="weeks"/>
    /// semanas, incluida la actual, rellenando con 0 las semanas sin datos.
    /// </summary>
    public static IReadOnlyList<WeeklyCount> GroupByWeek(IEnumerable<DateTimeOffset> dates, DateOnly today, int weeks)
    {
        var currentWeek = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
        var first = currentWeek.AddDays(-7 * (weeks - 1));

        var counts = dates
            .Select(date => DateOnly.FromDateTime(date.LocalDateTime))
            .Select(day => day.AddDays(-(((int)day.DayOfWeek + 6) % 7)))
            .Where(week => week >= first && week <= currentWeek)
            .GroupBy(week => week)
            .ToDictionary(group => group.Key, group => group.Count());

        return Enumerable.Range(0, weeks)
            .Select(index => first.AddDays(7 * index))
            .Select(week => new WeeklyCount(week, counts.GetValueOrDefault(week)))
            .ToList();
    }

    /// <remarks>La fecha se pasa a UTC: Npgsql solo admite UTC en «timestamp with time zone».</remarks>
    /// <summary>
    /// Últimos acontecimientos de la comunidad guardados en la base de datos
    /// (sin las cuentas nuevas, que añade la página porque viven en Identity),
    /// del más reciente al más antiguo.
    /// </summary>
    public async Task<IReadOnlyList<CommunityEvent>> GetCommunityFeedAsync(int take, CancellationToken cancellationToken = default)
    {
        var (reviews, _) = await _repository.GetReviewsAsync(null, null, null, 0, take, cancellationToken);
        var (dishes, _) = await _repository.GetDishesAsync(null, null, null, null, 0, take * 4, cancellationToken);
        var (photos, _) = await _repository.GetPhotosAsync(null, null, null, 0, take * 4, cancellationToken);
        var (cafes, _) = await _repository.SearchCafesAsync(null, 0, 5000, cancellationToken);

        var events = new List<CommunityEvent>();

        events.AddRange(reviews.Select(review => new CommunityEvent(
            review.CreatedAt, CommunityEventKind.Review, review.Author, review.UserId,
            $"{review.Rating}/5 · «{(review.Comment.Length > 90 ? review.Comment[..90] + "…" : review.Comment)}»",
            review.CafeName, review.CafeId)));

        // Solo lo aportado por usuarios: los platos y fotos de los datos de
        // ejemplo o de Wikimedia Commons no tienen autor registrado.
        events.AddRange(dishes
            .Where(dish => dish.AddedByUserId is not null)
            .Select(dish => new CommunityEvent(
                dish.CreatedAt, CommunityEventKind.Dish, string.Empty, dish.AddedByUserId,
                $"{dish.Name} ({dish.Price:0.00} €)", dish.CafeName, dish.CafeId, dish.Status)));

        events.AddRange(photos
            .Where(photo => photo.UploadedByUserId is not null)
            .Select(photo => new CommunityEvent(
                photo.CreatedAt, CommunityEventKind.Photo, photo.Author, photo.UploadedByUserId,
                "Foto subida", photo.CafeName, photo.CafeId, photo.Status)));

        events.AddRange(cafes
            .Where(cafe => cafe.OsmId is not null)
            .Select(cafe => new CommunityEvent(
                cafe.CreatedAt, CommunityEventKind.Cafe, string.Empty, null,
                "Cafetería de OpenStreetMap guardada en la base de datos", cafe.Name, cafe.Id)));

        return events
            .OrderByDescending(item => item.When)
            .Take(take)
            .ToList();
    }

    public Task<ActivityDates> GetActivityDatesAsync(DateTimeOffset since, CancellationToken cancellationToken = default) =>
        _repository.GetActivityDatesAsync(since.ToUniversalTime(), cancellationToken);

    public async Task<PagedResult<AdminAuditEntry>> GetAuditLogAsync(string? targetType, int page, CancellationToken cancellationToken = default)
    {
        var (items, total) = await _auditLog.ListAsync(Clean(targetType), Skip(page), PageSize, cancellationToken);
        return new PagedResult<AdminAuditEntry>(items, total, Math.Max(1, page), PageSize);
    }

    /// <summary>Anota en el registro una acción hecha fuera de este servicio (usuarios, moderación).</summary>
    public Task AuditAsync(
        AdminActor actor, string action, string targetType, Guid? targetId, string details,
        CancellationToken cancellationToken = default) =>
        _auditLog.WriteAsync(new AdminAuditEntry
        {
            AdminUserId = actor.UserId,
            AdminName = actor.Name,
            Action = action,
            TargetType = targetType,
            TargetId = targetId,
            Details = details.Length > 300 ? details[..300] : details
        }, cancellationToken);

    private static string? Clean(string? term) =>
        string.IsNullOrWhiteSpace(term) ? null : term.Trim()[..Math.Min(term.Trim().Length, 100)];
}
