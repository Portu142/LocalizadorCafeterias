using FluentValidation;
using FluentValidation.Results;
using LocalizadorCafes.Application.Cafes.Dtos;
using LocalizadorCafes.Application.Common.Interfaces;
using LocalizadorCafes.Domain.Entities;

namespace LocalizadorCafes.Application.Cafes;

public class CafeService
{
    private readonly ICafeRepository _cafeRepository;

    public CafeService(ICafeRepository cafeRepository)
    {
        _cafeRepository = cafeRepository;
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
            Author = request.Author.Trim(),
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
            cafe.Location.X);
    }

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
