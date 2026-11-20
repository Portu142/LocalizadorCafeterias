using FluentAssertions;
using LocalizadorCafes.Application.Cafes;
using LocalizadorCafes.Application.Cafes.Dtos;
using LocalizadorCafes.Application.Common.Interfaces;
using LocalizadorCafes.Domain.Entities;
using Moq;
using NetTopologySuite.Geometries;

namespace LocalizadorCafes.UnitTests.Cafes;

public class CafeServiceTests
{
    [Fact]
    public async Task SearchAsync_ShouldReturnAverageRatingAndReviewCount()
    {
        var cafe = CreateCafe();
        cafe.Reviews.Add(new Review { Author = "A", Rating = 5, MenuRating = 5, Comment = "Genial" });
        cafe.Reviews.Add(new Review { Author = "B", Rating = 3, MenuRating = 3, Comment = "Normal" });

        var repository = new Mock<ICafeRepository>();
        repository
            .Setup(x => x.SearchAsync(
                It.IsAny<string?>(),
                It.IsAny<double?>(),
                It.IsAny<double?>(),
                It.IsAny<double?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Cafe> { cafe });

        var service = new CafeService(repository.Object);

        var result = await service.SearchAsync(null, null, null, null);

        result.Should().HaveCount(1);
        result[0].Rating.Should().Be(4.0);
        result[0].ReviewCount.Should().Be(2);
    }

    [Fact]
    public async Task AddReviewAsync_ShouldReturnNull_WhenCafeDoesNotExist()
    {
        var repository = new Mock<ICafeRepository>();
        repository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Cafe?)null);

        var service = new CafeService(repository.Object);

        var result = await service.AddReviewAsync(
            Guid.NewGuid(),
            new CreateReviewRequest("Ana", 5, 4, "Bueno"));

        result.Should().BeNull();
    }

    [Fact]
    public async Task AddReviewAsync_ShouldTrimFieldsAndReturnReview()
    {
        var cafe = CreateCafe();

        var repository = new Mock<ICafeRepository>();
        repository
            .Setup(x => x.GetByIdAsync(cafe.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cafe);
        repository
            .Setup(x => x.AddReviewAsync(It.IsAny<Review>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new CafeService(repository.Object);

        var result = await service.AddReviewAsync(
            cafe.Id,
            new CreateReviewRequest("  Ana  ", 5, 4, "  Muy bueno  "));

        result.Should().NotBeNull();
        result!.Author.Should().Be("Ana");
        result.Comment.Should().Be("Muy bueno");
        result.Rating.Should().Be(5);
        result.MenuRating.Should().Be(4);
    }

    private static Cafe CreateCafe()
    {
        return new Cafe
        {
            Name = "Test Cafe",
            Address = "Calle Test 1",
            Neighborhood = "Centro",
            Phone = "123",
            Hours = "9-18",
            Location = new Point(-3.7038, 40.4168) { SRID = 4326 }
        };
    }
}
