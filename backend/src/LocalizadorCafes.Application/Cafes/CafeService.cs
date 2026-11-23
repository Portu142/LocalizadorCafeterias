using FluentValidation;
using FluentValidation.Results;
using LocalizadorCafes.Application.Cafes.Dtos;
using LocalizadorCafes.Application.Common.Interfaces;
using LocalizadorCafes.Domain.Entities;

namespace LocalizadorCafes.Application.Cafes;

public class CafeService
{
    private readonly ICafeRepository _cafeRepository;
    private readonly IPhotoStorage _photoStorage;
    private readonly IOsmCafeSource _osmCafeSource;

    public CafeService(
        ICafeRepository cafeRepository,
        IPhotoStorage photoStorage,
        IOsmCafeSource osmCafeSource)
    {
        _cafeRepository = cafeRepository;
        _photoStorage = photoStorage;
        _osmCafeSource = osmCafeSource;
    }

    /// <summary>
    /// Devuelve el café guardado para ese elemento de OSM o lo importa
    /// descargando sus datos de OpenStreetMap. Null si no existe o no es un café.
    /// </summary>
    public async Task<CafeDetailDto?> ImportOsmCafeAsync(
        string osmId,
        CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeOsmId(osmId)
            ?? throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(osmId), "El identificador de OpenStreetMap no es válido.")
            });

        var existing = await _cafeRepository.GetByOsmIdAsync(normalized, cancellationToken);
        if (existing is not null)
        {
            return ToDetail(existing, viewerId: null);
        }

        var cafe = await _osmCafeSource.FetchAsync(normalized, cancellationToken);
        if (cafe is null)
        {
            return null;
        }

        cafe.OsmId = normalized;
        await _cafeRepository.AddCafeAsync(cafe, cancellationToken);
        return ToDetail(cafe, viewerId: null);
    }

    /// <summary>«node/123» o «way/456» (también acepta el prefijo «osm-» del frontend).</summary>
    public static string? NormalizeOsmId(string? value)
    {
        var raw = value?.Trim().ToLowerInvariant() ?? string.Empty;
        if (raw.StartsWith("osm-", StringComparison.Ordinal))
        {
            raw = raw[4..];
        }

        var parts = raw.Split('/');
        return parts.Length == 2 &&
            parts[0] is "node" or "way" &&
            parts[1].Length is > 0 and <= 15 &&
            parts[1].All(char.IsAsciiDigit)
            ? $"{parts[0]}/{parts[1]}"
            : null;
    }

    /// <summary>Añade un plato a la carta. Null si la cafetería no existe.</summary>
    public async Task<MenuItemDto?> AddMenuItemAsync(
        Guid cafeId,
        CreateMenuItemRequest request,
        ReviewAuthor author,
        CancellationToken cancellationToken = default)
    {
        var cafe = await _cafeRepository.GetByIdAsync(cafeId, cancellationToken);
        if (cafe is null)
        {
            return null;
        }

        var name = request.Name.Trim();
        var activeMenu = cafe.Menu.Where(item => item.Status != ModerationStatus.Rejected).ToList();
        if (activeMenu.Any(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(request.Name), "Ese plato ya está en la carta.")
            });
        }

        if (activeMenu.Count >= 100)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(request.Name), "La carta ya tiene el máximo de 100 platos.")
            });
        }

        var item = new MenuItem
        {
            CafeId = cafeId,
            Name = name,
            Price = request.Price,
            AddedByUserId = author.UserId,
            // No es público hasta que lo apruebe un administrador.
            Status = ModerationStatus.Pending
        };

        await _cafeRepository.AddMenuItemAsync(item, cancellationToken);
        return ToMenuItemDto(item);
    }

    public enum DeleteMenuItemResult { Deleted, NotFound, Forbidden, AlreadyRated }

    /// <summary>
    /// Borra un plato. Solo puede hacerlo quien lo añadió y mientras nadie más lo
    /// haya puntuado (borrarlo eliminaría las puntuaciones de otros usuarios).
    /// </summary>
    public async Task<DeleteMenuItemResult> DeleteMenuItemAsync(
        Guid cafeId,
        Guid itemId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var cafe = await _cafeRepository.GetByIdAsync(cafeId, cancellationToken);
        var item = cafe?.Menu.FirstOrDefault(menuItem => menuItem.Id == itemId);
        if (cafe is null || item is null)
        {
            return DeleteMenuItemResult.NotFound;
        }

        if (item.AddedByUserId != userId)
        {
            return DeleteMenuItemResult.Forbidden;
        }

        var ratedByOthers = cafe.Reviews.Any(review =>
            review.UserId != userId &&
            review.MenuItemRatings.Any(rating => rating.MenuItemId == itemId));
        if (ratedByOthers)
        {
            return DeleteMenuItemResult.AlreadyRated;
        }

        await _cafeRepository.RemoveMenuItemAsync(item, cancellationToken);
        return DeleteMenuItemResult.Deleted;
    }

    public async Task<IReadOnlyList<CafeSummaryDto>> SearchAsync(
        string? search,
        double? latitude,
        double? longitude,
        double? radiusKm,
        CancellationToken cancellationToken = default)
    {
        var cafes = await _cafeRepository.SearchAsync(
            search,
            latitude,
            longitude,
            radiusKm,
            cancellationToken);

        return cafes.Select(ToSummary).ToList();
    }

    /// <param name="viewerId">Usuario que consulta: ve también sus platos y fotos pendientes.</param>
    public async Task<CafeDetailDto?> GetByIdAsync(
        Guid id,
        Guid? viewerId = null,
        CancellationToken cancellationToken = default)
    {
        var cafe = await _cafeRepository.GetByIdAsync(id, cancellationToken);
        return cafe is null ? null : ToDetail(cafe, viewerId);
    }

    // --- Moderación (panel de administración) -------------------------------

    public async Task<ModerationQueueDto> GetModerationQueueAsync(CancellationToken cancellationToken = default)
    {
        var dishes = await _cafeRepository.GetPendingMenuItemsAsync(cancellationToken);
        var photos = await _cafeRepository.GetPendingPhotosAsync(cancellationToken);

        return new ModerationQueueDto(
            dishes
                .Select(item => new PendingDishDto(
                    item.Id, item.CafeId, item.Cafe.Name, item.Name, item.Price,
                    item.AddedByUserId, string.Empty, item.CreatedAt))
                .ToList(),
            photos
                .Select(photo => new PendingPhotoDto(
                    photo.Id, photo.CafeId, photo.Cafe.Name, photo.Url, photo.Author, photo.CreatedAt))
                .ToList());
    }

    public Task<bool> ReviewDishAsync(Guid id, bool approve, CancellationToken cancellationToken = default) =>
        _cafeRepository.SetMenuItemStatusAsync(
            id, approve ? ModerationStatus.Approved : ModerationStatus.Rejected, cancellationToken);

    public Task<bool> ReviewPhotoAsync(Guid id, bool approve, CancellationToken cancellationToken = default) =>
        _cafeRepository.SetPhotoStatusAsync(
            id, approve ? ModerationStatus.Approved : ModerationStatus.Rejected, cancellationToken);

    public async Task<IReadOnlyList<AdminReviewDto>> GetRecentReviewsAsync(
        int take,
        CancellationToken cancellationToken = default)
    {
        var reviews = await _cafeRepository.GetRecentReviewsAsync(Math.Clamp(take, 1, 200), cancellationToken);

        return reviews
            .Select(item => new AdminReviewDto(
                item.Review.Id, item.Cafe.Id, item.Cafe.Name, item.Review.Author,
                item.Review.Rating, item.Review.Comment, item.Review.CreatedAt))
            .ToList();
    }

    public Task<bool> DeleteReviewAsync(Guid id, CancellationToken cancellationToken = default) =>
        _cafeRepository.DeleteReviewAsync(id, cancellationToken);

    public async Task<ReviewDto?> AddReviewAsync(
        Guid cafeId,
        CreateReviewRequest request,
        ReviewAuthor author,
        CancellationToken cancellationToken = default)
    {
        var cafe = await _cafeRepository.GetByIdAsync(cafeId, cancellationToken);
        if (cafe is null)
        {
            return null;
        }

        // Solo se pueden puntuar los platos aprobados.
        var menuNames = cafe.Menu
            .Where(item => item.Status == ModerationStatus.Approved)
            .ToDictionary(item => item.Id, item => item.Name);
        var itemRatings = request.ItemRatings ?? Array.Empty<CreateMenuItemRating>();

        if (itemRatings.Any(rating => !menuNames.ContainsKey(rating.MenuItemId)))
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(
                    nameof(request.ItemRatings),
                    "Alguna de las puntuaciones no corresponde a un plato de esta cafetería.")
            });
        }

        var review = new Review
        {
            CafeId = cafeId,
            UserId = author.UserId,
            Author = author.DisplayName.Trim(),
            Rating = request.Rating,
            Comment = request.Comment.Trim(),
            MenuItemRatings = itemRatings
                .Select(rating => new MenuItemRating
                {
                    MenuItemId = rating.MenuItemId,
                    Score = rating.Score
                })
                .ToList()
        };

        await _cafeRepository.AddReviewAsync(review, cancellationToken);
        return ToReviewDto(review, menuNames);
    }

    public async Task<IReadOnlyList<UserReviewDto>> GetUserReviewsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var reviews = await _cafeRepository.GetReviewsByUserAsync(userId, cancellationToken);

        return reviews
            .Select(item => new UserReviewDto(
                item.Review.Id,
                item.Cafe.Id,
                item.Cafe.Name,
                item.Review.Rating,
                item.Review.Comment,
                item.Review.CreatedAt))
            .ToList();
    }

    /// <summary>
    /// Guarda una foto subida por un usuario. Devuelve null si la cafetería no
    /// existe y lanza <see cref="ValidationException"/> si el archivo no es una
    /// imagen JPEG, PNG o WebP de hasta 5 MB.
    /// </summary>
    public async Task<CafePhotoDto?> AddPhotoAsync(
        Guid cafeId,
        Stream content,
        long length,
        ReviewAuthor uploader,
        CancellationToken cancellationToken = default)
    {
        if (length <= 0 || length > PhotoFormat.MaxBytes)
        {
            throw PhotoError("La foto debe pesar como máximo 5 MB.");
        }

        var cafe = await _cafeRepository.GetByIdAsync(cafeId, cancellationToken);
        if (cafe is null)
        {
            return null;
        }

        // Se copia a memoria (máx. 5 MB) para leer la firma y guardar el mismo
        // contenido que se ha validado.
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);

        if (buffer.Length > PhotoFormat.MaxBytes)
        {
            throw PhotoError("La foto debe pesar como máximo 5 MB.");
        }

        var extension = PhotoFormat.DetectExtension(buffer)
            ?? throw PhotoError("Solo se admiten fotos JPEG, PNG o WebP.");

        buffer.Position = 0;
        var url = await _photoStorage.SaveAsync(buffer, extension, cancellationToken);

        var photo = new CafePhoto
        {
            CafeId = cafeId,
            Url = url,
            Author = uploader.DisplayName.Trim(),
            License = "Foto de la comunidad",
            SourceUrl = string.Empty,
            UploadedByUserId = uploader.UserId,
            // No es pública hasta que la apruebe un administrador.
            Status = ModerationStatus.Pending
        };

        await _cafeRepository.AddPhotoAsync(photo, cancellationToken);

        return ToPhotoDto(photo);
    }

    private static ValidationException PhotoError(string message) =>
        new(new[] { new ValidationFailure("photo", message) });

    private static CafeSummaryDto ToSummary(Cafe cafe)
    {
        var reviews = cafe.Reviews.ToList();
        var rating = CalculateRating(reviews);

        return new CafeSummaryDto(
            cafe.Id,
            cafe.Name,
            cafe.Address,
            cafe.Neighborhood,
            rating,
            reviews.Count,
            cafe.Location.Y,
            cafe.Location.X,
            KnownTags(cafe),
            cafe.Hours,
            cafe.OsmId);
    }

    private static List<string> KnownTags(Cafe cafe) =>
        cafe.Tags.Where(CafeTags.All.Contains).Distinct().ToList();

    private static string StatusName(ModerationStatus status) => status switch
    {
        ModerationStatus.Pending => "pending",
        ModerationStatus.Rejected => "rejected",
        _ => "approved"
    };

    private static MenuItemDto ToMenuItemDto(MenuItem item) =>
        new(item.Id, item.Name, item.Price, item.AddedByUserId, StatusName(item.Status));

    private static CafePhotoDto ToPhotoDto(CafePhoto photo) =>
        new(photo.Id, photo.Url, photo.Author, photo.License, photo.SourceUrl, StatusName(photo.Status));

    /// <summary>
    /// Ficha pública: solo platos y fotos aprobados, más los pendientes del
    /// propio usuario que consulta (para que sepa que están en revisión).
    /// </summary>
    private static CafeDetailDto ToDetail(Cafe cafe, Guid? viewerId)
    {
        var reviews = cafe.Reviews
            .OrderByDescending(review => review.CreatedAt)
            .ToList();
        var menuNames = cafe.Menu.ToDictionary(item => item.Id, item => item.Name);

        return new CafeDetailDto(
            cafe.Id,
            cafe.Name,
            cafe.Address,
            cafe.Neighborhood,
            cafe.Phone,
            cafe.Hours,
            CalculateRating(reviews),
            reviews.Count,
            cafe.Location.Y,
            cafe.Location.X,
            KnownTags(cafe),
            cafe.Photos
                .Where(photo => photo.Status == ModerationStatus.Approved ||
                    (photo.Status == ModerationStatus.Pending && viewerId is not null && photo.UploadedByUserId == viewerId))
                .OrderBy(photo => photo.CreatedAt)
                .Select(ToPhotoDto)
                .ToList(),
            cafe.Menu
                .Where(item => item.Status == ModerationStatus.Approved ||
                    (item.Status == ModerationStatus.Pending && viewerId is not null && item.AddedByUserId == viewerId))
                .OrderBy(item => item.Name)
                .Select(ToMenuItemDto)
                .ToList(),
            reviews.Select(review => ToReviewDto(review, menuNames)).ToList(),
            cafe.OsmId);
    }

    private static double CalculateRating(IReadOnlyCollection<Review> reviews)
    {
        return reviews.Count == 0
            ? 0
            : Math.Round(reviews.Average(review => review.Rating), 1);
    }

    private static ReviewDto ToReviewDto(
        Review review,
        IReadOnlyDictionary<Guid, string> menuNames)
    {
        return new ReviewDto(
            review.Id,
            review.Author,
            review.Rating,
            review.Comment,
            review.CreatedAt,
            review.MenuItemRatings
                .Select(rating => new MenuItemRatingDto(
                    rating.MenuItemId,
                    menuNames.GetValueOrDefault(rating.MenuItemId, "Plato"),
                    rating.Score))
                .OrderBy(rating => rating.MenuItemName)
                .ToList());
    }
}
