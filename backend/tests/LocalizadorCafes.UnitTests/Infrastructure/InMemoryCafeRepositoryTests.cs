using FluentAssertions;
using LocalizadorCafes.Domain.Entities;
using LocalizadorCafes.Infrastructure.Persistence;
using LocalizadorCafes.Infrastructure.Persistence.Repositories;

namespace LocalizadorCafes.UnitTests.Infrastructure;

public class InMemoryCafeRepositoryTests
{
    private const double SolLatitude = 40.4168;
    private const double SolLongitude = -3.7038;

    [Fact]
    public async Task SearchAsync_WithoutFilters_ReturnsAllDemoCafes()
    {
        var repository = CreateRepository();

        var result = await repository.SearchAsync(null, null, null, null);

        result.Should().HaveCount(6);
    }

    [Fact]
    public async Task SearchAsync_ShouldIgnoreCaseInSearchTerm()
    {
        var repository = CreateRepository();

        var result = await repository.SearchAsync("malasaña", null, null, null);

        result.Should().OnlyContain(cafe => cafe.Neighborhood == "Malasaña");
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task SearchAsync_ShouldFilterByRadius()
    {
        var repository = CreateRepository();

        var far = await repository.SearchAsync(null, SolLatitude, SolLongitude, 0.1);
        var near = await repository.SearchAsync(null, SolLatitude, SolLongitude, 5);

        far.Should().BeEmpty();
        near.Should().HaveCount(6);
    }

    [Fact]
    public async Task AddReviewAsync_ShouldAppendReviewToCafe()
    {
        var repository = CreateRepository();
        var cafe = (await repository.GetByIdAsync(
            (await repository.SearchAsync(null, null, null, null))[0].Id))!;
        var previousCount = cafe.Reviews.Count;

        var review = new Review
        {
            CafeId = cafe.Id,
            Author = "Prueba",
            Rating = 4,
            Comment = "Comentario de prueba"
        };

        await repository.AddReviewAsync(review);

        var updated = await repository.GetByIdAsync(cafe.Id);
        updated!.Reviews.Should().HaveCount(previousCount + 1);
        updated.Reviews.Should().ContainSingle(item => item.Author == "Prueba");
    }

    [Fact]
    public void DemoData_ShouldExposeSeedCafesAndReviews()
    {
        var cafes = DemoData.CreateCafes();

        cafes.Should().HaveCount(6);
        cafes.SelectMany(cafe => cafe.Reviews).Should().HaveCount(12);
        cafes.Should().OnlyContain(cafe => cafe.Menu.Count == 4);
    }

    private static InMemoryCafeRepository CreateRepository() =>
        new(DemoData.CreateCafes());
}
