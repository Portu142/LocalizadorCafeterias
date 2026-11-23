using LocalizadorCafes.Application.Admin;
using LocalizadorCafes.Domain.Entities;

namespace LocalizadorCafes.Application.Common.Interfaces;

/// <summary>Consultas y cambios exclusivos del panel de administración.</summary>
public interface IAdminRepository
{
    Task<(IReadOnlyList<AdminCafeRow> Items, int Total)> SearchCafesAsync(
        string? term, int skip, int take, CancellationToken cancellationToken = default);

    Task<bool> UpdateCafeAsync(Guid id, CafeEdit edit, CancellationToken cancellationToken = default);

    Task<bool> DeleteCafeAsync(Guid id, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<AdminDishRow> Items, int Total)> GetDishesAsync(
        ModerationStatus? status, string? term, Guid? userId, Guid? cafeId,
        int skip, int take, CancellationToken cancellationToken = default);

    Task<bool> UpdateDishAsync(Guid id, string name, decimal price, CancellationToken cancellationToken = default);

    Task<bool> DeleteDishAsync(Guid id, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<AdminPhotoRow> Items, int Total)> GetPhotosAsync(
        ModerationStatus? status, Guid? userId, Guid? cafeId,
        int skip, int take, CancellationToken cancellationToken = default);

    /// <summary>Borra la foto y la devuelve (para eliminar también el archivo).</summary>
    Task<CafePhoto?> DeletePhotoAsync(Guid id, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<AdminReviewRow> Items, int Total)> GetReviewsAsync(
        string? term, Guid? userId, Guid? cafeId,
        int skip, int take, CancellationToken cancellationToken = default);

    Task<ActivityDates> GetActivityDatesAsync(DateTimeOffset since, CancellationToken cancellationToken = default);
}
