using LocalizadorCafes.Domain.Entities;

namespace LocalizadorCafes.Application.Admin;

/// <summary>Página de resultados con el total para la paginación.</summary>
public record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize)
{
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(Total / (double)PageSize));
}

/// <summary>Fila del listado de cafeterías del panel.</summary>
public record AdminCafeRow(
    Guid Id,
    string Name,
    string Address,
    string Neighborhood,
    string? OsmId,
    int MenuCount,
    int PendingCount,
    int PhotoCount,
    int ReviewCount,
    DateTimeOffset CreatedAt);

/// <summary>Datos editables de una cafetería.</summary>
public record CafeEdit(
    string Name,
    string Address,
    string Neighborhood,
    string Phone,
    string Hours,
    IReadOnlyList<string> Tags);

/// <summary>Plato con su cafetería, para los listados del panel.</summary>
public record AdminDishRow(
    Guid Id,
    Guid CafeId,
    string CafeName,
    string Name,
    decimal Price,
    ModerationStatus Status,
    Guid? AddedByUserId,
    DateTimeOffset CreatedAt);

/// <summary>Foto con su cafetería, para los listados del panel.</summary>
public record AdminPhotoRow(
    Guid Id,
    Guid CafeId,
    string CafeName,
    string Url,
    string Author,
    string License,
    ModerationStatus Status,
    Guid? UploadedByUserId,
    DateTimeOffset CreatedAt);

/// <summary>Reseña con su cafetería, para el panel.</summary>
public record AdminReviewRow(
    Guid Id,
    Guid CafeId,
    string CafeName,
    Guid? UserId,
    string Author,
    int Rating,
    string Comment,
    DateTimeOffset CreatedAt);

/// <summary>Fechas de alta de contenido de la comunidad, para las gráficas.</summary>
public record ActivityDates(
    IReadOnlyList<DateTimeOffset> Reviews,
    IReadOnlyList<DateTimeOffset> Dishes,
    IReadOnlyList<DateTimeOffset> Photos);

/// <summary>Quién realiza una acción del panel (para el registro de actividad).</summary>
public record AdminActor(Guid UserId, string Name);

/// <summary>Semana y recuento para las gráficas del resumen.</summary>
public record WeeklyCount(DateOnly WeekStart, int Count);

/// <summary>Tipo de acontecimiento de la comunidad (pestaña «Comunidad»).</summary>
public enum CommunityEventKind
{
    Review,
    Dish,
    Photo,
    Cafe,
    Account
}

/// <summary>
/// Algo que ha hecho un usuario en la web: reseñar, proponer un plato, subir
/// una foto, guardar un café de OpenStreetMap o crear su cuenta.
/// </summary>
public record CommunityEvent(
    DateTimeOffset When,
    CommunityEventKind Kind,
    string Who,
    Guid? UserId,
    string What,
    string? CafeName,
    Guid? CafeId,
    ModerationStatus? Status = null);
