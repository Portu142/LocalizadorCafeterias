using FluentAssertions;
using FluentValidation;
using LocalizadorCafes.Application.Cafes;
using LocalizadorCafes.Application.Cafes.Dtos;
using LocalizadorCafes.Application.Common.Interfaces;
using LocalizadorCafes.Domain.Entities;
using Moq;
using NetTopologySuite.Geometries;

namespace LocalizadorCafes.UnitTests.Cafes;

public class CafeServiceTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly ReviewAuthor Author = new(UserId, "Ana");

    [Fact]
    public async Task SearchAsync_ShouldReturnAverageRatingAndReviewCount()
    {
        var cafe = CreateCafe();
        cafe.Reviews.Add(new Review { Author = "A", Rating = 5, Comment = "Genial" });
        cafe.Reviews.Add(new Review { Author = "B", Rating = 3, Comment = "Normal" });

        var repository = new Mock<ICafeRepository>();
        repository
            .Setup(x => x.SearchAsync(
                It.IsAny<string?>(),
                It.IsAny<double?>(),
                It.IsAny<double?>(),
                It.IsAny<double?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Cafe> { cafe });

        var service = new CafeService(repository.Object, Mock.Of<IPhotoStorage>());

        var result = await service.SearchAsync(null, null, null, null);

        result.Should().HaveCount(1);
        result[0].Rating.Should().Be(4.0);
        result[0].ReviewCount.Should().Be(2);
        result[0].Hours.Should().Be("9-18");
        result[0].Tags.Should().BeEmpty();
    }

    [Fact]
    public async Task AddReviewAsync_ShouldReturnNull_WhenCafeDoesNotExist()
    {
        var repository = new Mock<ICafeRepository>();
        repository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Cafe?)null);

        var service = new CafeService(repository.Object, Mock.Of<IPhotoStorage>());

        var result = await service.AddReviewAsync(
            Guid.NewGuid(),
            new CreateReviewRequest(null, 5, "Bueno"),
            Author);

        result.Should().BeNull();
    }

    [Fact]
    public async Task AddReviewAsync_ShouldUseAccountNameAndTrimFields()
    {
        var cafe = CreateCafe();

        var repository = new Mock<ICafeRepository>();
        repository
            .Setup(x => x.GetByIdAsync(cafe.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cafe);
        repository
            .Setup(x => x.AddReviewAsync(It.IsAny<Review>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new CafeService(repository.Object, Mock.Of<IPhotoStorage>());

        var result = await service.AddReviewAsync(
            cafe.Id,
            // El autor enviado por el cliente se ignora: manda la cuenta.
            new CreateReviewRequest("Suplantador", 5, "  Muy bueno  "),
            new ReviewAuthor(UserId, "  Ana  "));

        result.Should().NotBeNull();
        result!.Author.Should().Be("Ana");
        repository.Verify(x => x.AddReviewAsync(
            It.Is<Review>(review => review.UserId == UserId && review.Author == "Ana"),
            It.IsAny<CancellationToken>()));
        repository.Verify(x => x.AddReviewAsync(
            It.Is<Review>(review => review.UserId == UserId && review.Author == "Ana"),
            It.IsAny<CancellationToken>()));
        result.Comment.Should().Be("Muy bueno");
        result.Rating.Should().Be(5);
        result.ItemRatings.Should().BeEmpty();
    }

    [Fact]
    public async Task AddReviewAsync_ShouldMapItemRatingsToMenuItemNames()
    {
        var cafe = CreateCafe();
        var latte = cafe.Menu.First(item => item.Name == "Café con leche");
        var cake = cafe.Menu.First(item => item.Name == "Tarta de queso");

        Review? stored = null;
        var repository = new Mock<ICafeRepository>();
        repository
            .Setup(x => x.GetByIdAsync(cafe.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cafe);
        repository
            .Setup(x => x.AddReviewAsync(It.IsAny<Review>(), It.IsAny<CancellationToken>()))
            .Callback<Review, CancellationToken>((review, _) => stored = review)
            .Returns(Task.CompletedTask);

        var service = new CafeService(repository.Object, Mock.Of<IPhotoStorage>());

        var result = await service.AddReviewAsync(
            cafe.Id,
            new CreateReviewRequest(
                "Ana",
                5,
                "Todo buenísimo.",
                new[]
                {
                    new CreateMenuItemRating(latte.Id, 4),
                    new CreateMenuItemRating(cake.Id, 5)
                }),
            Author);

        result.Should().NotBeNull();
        result!.ItemRatings.Should().HaveCount(2);
        result.ItemRatings.Should().ContainSingle(rating =>
            rating.MenuItemId == latte.Id && rating.MenuItemName == latte.Name && rating.Score == 4);
        result.ItemRatings.Should().ContainSingle(rating =>
            rating.MenuItemId == cake.Id && rating.MenuItemName == cake.Name && rating.Score == 5);

        stored.Should().NotBeNull();
        stored!.MenuItemRatings.Should().HaveCount(2);
        stored.MenuItemRatings.Select(rating => (rating.MenuItemId, rating.Score))
            .Should().BeEquivalentTo(new[] { (latte.Id, 4), (cake.Id, 5) });
    }

    [Fact]
    public async Task AddReviewAsync_ShouldThrow_WhenItemDoesNotBelongToCafe()
    {
        var cafe = CreateCafe();

        var repository = new Mock<ICafeRepository>();
        repository
            .Setup(x => x.GetByIdAsync(cafe.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cafe);
        repository
            .Setup(x => x.AddReviewAsync(It.IsAny<Review>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new CafeService(repository.Object, Mock.Of<IPhotoStorage>());

        var act = () => service.AddReviewAsync(
            cafe.Id,
            new CreateReviewRequest(
                "Ana",
                5,
                "Comentario válido.",
                new[] { new CreateMenuItemRating(Guid.NewGuid(), 5) }),
            Author);

        await act.Should().ThrowAsync<ValidationException>();

        repository.Verify(
            x => x.AddReviewAsync(It.IsAny<Review>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldMapKnownTagsAndPhotos()
    {
        var cafe = CreateCafe();
        cafe.Tags = new List<string> { CafeTags.Wifi, "desconocida", CafeTags.Wifi, CafeTags.Terrace };
        cafe.Photos.Add(new CafePhoto
        {
            Url = "https://example.org/foto.jpg",
            Author = "Autora",
            License = "CC BY 4.0",
            SourceUrl = "https://example.org/foto"
        });

        var repository = new Mock<ICafeRepository>();
        repository
            .Setup(x => x.GetByIdAsync(cafe.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cafe);

        var service = new CafeService(repository.Object, Mock.Of<IPhotoStorage>());

        var result = await service.GetByIdAsync(cafe.Id);

        result.Should().NotBeNull();
        result!.Tags.Should().Equal(CafeTags.Wifi, CafeTags.Terrace);
        result.Photos.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new
            {
                Url = "https://example.org/foto.jpg",
                Author = "Autora",
                License = "CC BY 4.0",
                SourceUrl = "https://example.org/foto"
            });
    }

    private static Cafe CreateCafe()
    {
        var cafe = new Cafe
        {
            Name = "Test Cafe",
            Address = "Calle Test 1",
            Neighborhood = "Centro",
            Phone = "123",
            Hours = "9-18",
            Location = new Point(-3.7038, 40.4168) { SRID = 4326 }
        };

        cafe.Menu.Add(new MenuItem { CafeId = cafe.Id, Name = "Café con leche", Price = 2.50m });
        cafe.Menu.Add(new MenuItem { CafeId = cafe.Id, Name = "Tarta de queso", Price = 4.50m });

        return cafe;
    }
}
