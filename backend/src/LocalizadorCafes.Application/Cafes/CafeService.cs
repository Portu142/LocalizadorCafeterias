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

    public CafeService(ICafeRepository cafeRepository, IPhotoStorage photoStorage)
    {
        _cafeRepository = cafeRepository;
        _photoStorage = photoStorage;
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

    public async Task<CafeDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var cafe = await _cafeRepository.GetByIdAsync(id, cancellationToken);
        return cafe is null ? null : ToDetail(cafe);
    }

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

        var menuNames = cafe.Menu.ToDictionary(item => item.Id, item => item.Name);
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
            UploadedByUserId = uploader.UserId
        };

        await _cafeRepository.AddPhotoAsync(photo, cancellationToken);

        return new CafePhotoDto(photo.Id, photo.Url, photo.Author, photo.License, photo.SourceUrl);
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
            cafe.Hours);
    }

    private static List<string> KnownTags(Cafe cafe) =>
        cafe.Tags.Where(CafeTags.All.Contains).Distinct().ToList();

    private static CafeDetailDto ToDetail(Cafe cafe)
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
                .OrderBy(photo => photo.CreatedAt)
                .Select(photo => new CafePhotoDto(
                    photo.Id,
                    photo.Url,
                    photo.Author,
                    photo.License,
                    photo.SourceUrl))
                .ToList(),
            cafe.Menu
                .OrderBy(item => item.Name)
                .Select(item => new MenuItemDto(item.Id, item.Name, item.Price))
                .ToList(),
            reviews.Select(review => ToReviewDto(review, menuNames)).ToList());
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
