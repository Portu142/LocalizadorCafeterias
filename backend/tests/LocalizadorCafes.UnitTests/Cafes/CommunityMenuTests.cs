using FluentAssertions;
using FluentValidation;
using FluentValidation.TestHelper;
using LocalizadorCafes.Application.Cafes;
using LocalizadorCafes.Application.Cafes.Dtos;
using LocalizadorCafes.Application.Cafes.Validators;
using LocalizadorCafes.Application.Common.Interfaces;
using LocalizadorCafes.Domain.Entities;
using LocalizadorCafes.Infrastructure.Osm;
using Moq;
using NetTopologySuite.Geometries;

namespace LocalizadorCafes.UnitTests.Cafes;

public class CommunityMenuTests
{
    private static readonly ReviewAuthor Ana = new(Guid.NewGuid(), "Ana");
    private static readonly ReviewAuthor Luis = new(Guid.NewGuid(), "Luis");

    [Fact]
    public async Task AddMenuItemAsync_ShouldStoreDishWithAuthor()
    {
        var cafe = CreateCafe();
        var (service, repository, _) = CreateService(cafe);

        var result = await service.AddMenuItemAsync(cafe.Id, new CreateMenuItemRequest("  Flat white ", 3.2m), Ana);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Flat white");
        result.AddedByUserId.Should().Be(Ana.UserId);
        repository.Verify(x => x.AddMenuItemAsync(
            It.Is<MenuItem>(item => item.CafeId == cafe.Id && item.Price == 3.2m && item.AddedByUserId == Ana.UserId),
            It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task AddMenuItemAsync_ShouldRejectDuplicateNamesIgnoringCase()
    {
        var cafe = CreateCafe();
        cafe.Menu.Add(new MenuItem { CafeId = cafe.Id, Name = "Cappuccino", Price = 3m });
        var (service, _, _) = CreateService(cafe);

        var act = () => service.AddMenuItemAsync(cafe.Id, new CreateMenuItemRequest("cappuccino", 3m), Ana);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Theory]
    [InlineData("", 2.5)]
    [InlineData("A", 2.5)]
    [InlineData("Tostada", 0)]
    [InlineData("Tostada", 501)]
    [InlineData("Tostada", 2.555)]
    public void Validator_ShouldRejectInvalidDishes(string name, double price)
    {
        var result = new CreateMenuItemRequestValidator()
            .TestValidate(new CreateMenuItemRequest(name, (decimal)price));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_ShouldAcceptValidDish()
    {
        new CreateMenuItemRequestValidator()
            .TestValidate(new CreateMenuItemRequest("Tostada con tomate", 2.5m))
            .IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteMenuItemAsync_ShouldOnlyLetTheAuthorDelete()
    {
        var cafe = CreateCafe();
        var item = new MenuItem { CafeId = cafe.Id, Name = "Brownie", Price = 3m, AddedByUserId = Ana.UserId };
        cafe.Menu.Add(item);
        var (service, repository, _) = CreateService(cafe);

        (await service.DeleteMenuItemAsync(cafe.Id, item.Id, Luis.UserId))
            .Should().Be(CafeService.DeleteMenuItemResult.Forbidden);
        (await service.DeleteMenuItemAsync(cafe.Id, item.Id, Ana.UserId))
            .Should().Be(CafeService.DeleteMenuItemResult.Deleted);
        repository.Verify(x => x.RemoveMenuItemAsync(item, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteMenuItemAsync_ShouldRefuse_WhenOthersAlreadyRatedIt()
    {
        var cafe = CreateCafe();
        var item = new MenuItem { CafeId = cafe.Id, Name = "Brownie", Price = 3m, AddedByUserId = Ana.UserId };
        cafe.Menu.Add(item);
        var review = new Review { CafeId = cafe.Id, UserId = Luis.UserId, Author = "Luis", Rating = 5, Comment = "Muy rico todo." };
        review.MenuItemRatings.Add(new MenuItemRating { MenuItemId = item.Id, Score = 5 });
        cafe.Reviews.Add(review);
        var (service, repository, _) = CreateService(cafe);

        (await service.DeleteMenuItemAsync(cafe.Id, item.Id, Ana.UserId))
            .Should().Be(CafeService.DeleteMenuItemResult.AlreadyRated);
        repository.Verify(x => x.RemoveMenuItemAsync(It.IsAny<MenuItem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("osm-node/123", "node/123")]
    [InlineData("WAY/45", "way/45")]
    [InlineData("relation/1", null)]
    [InlineData("node/12a", null)]
    [InlineData("node/../1", null)]
    [InlineData("", null)]
    public void NormalizeOsmId_ShouldOnlyAcceptNodesAndWays(string input, string? expected)
    {
        CafeService.NormalizeOsmId(input).Should().Be(expected);
    }

    [Fact]
    public async Task ImportOsmCafeAsync_ShouldReuseAlreadyImportedCafe()
    {
        var cafe = CreateCafe();
        cafe.OsmId = "node/7";
        var (service, repository, source) = CreateService(null);
        repository.Setup(x => x.GetByOsmIdAsync("node/7", It.IsAny<CancellationToken>())).ReturnsAsync(cafe);

        var result = await service.ImportOsmCafeAsync("osm-node/7");

        result!.Id.Should().Be(cafe.Id);
        source.Verify(x => x.FetchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        repository.Verify(x => x.AddCafeAsync(It.IsAny<Cafe>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ImportOsmCafeAsync_ShouldDownloadAndSaveNewCafe()
    {
        var downloaded = CreateCafe();
        var (service, repository, source) = CreateService(null);
        source.Setup(x => x.FetchAsync("way/9", It.IsAny<CancellationToken>())).ReturnsAsync(downloaded);

        var result = await service.ImportOsmCafeAsync("osm-way/9");

        result!.OsmId.Should().Be("way/9");
        repository.Verify(x => x.AddCafeAsync(
            It.Is<Cafe>(saved => saved.OsmId == "way/9"), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task ImportOsmCafeAsync_ShouldRejectInvalidIds()
    {
        var (service, _, _) = CreateService(null);

        var act = () => service.ImportOsmCafeAsync("relation/1");

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public void OsmCafeMapper_ShouldMapAddressHoursAndServices()
    {
        var cafe = OsmCafeMapper.ToCafe(new Dictionary<string, string>
        {
            ["amenity"] = "cafe",
            ["name"] = "Café Pruebas",
            ["addr:street"] = "Calle Mayor",
            ["addr:housenumber"] = "1",
            ["addr:postcode"] = "28013",
            ["addr:suburb"] = "Sol",
            ["opening_hours"] = "Mo-Fr 08:00-20:00",
            ["internet_access"] = "wlan",
            ["outdoor_seating"] = "yes",
            ["socket:schuko"] = "4"
        }, 40.41, -3.70);

        cafe.Should().NotBeNull();
        cafe!.Address.Should().Be("Calle Mayor 1, 28013");
        cafe.Neighborhood.Should().Be("Sol");
        cafe.Hours.Should().Be("Mo-Fr 08:00-20:00");
        cafe.Tags.Should().Equal(CafeTags.Wifi, CafeTags.Terrace, CafeTags.PowerOutlets);
        cafe.Location.Y.Should().Be(40.41);
    }

    [Fact]
    public void OsmCafeMapper_ShouldRejectElementsThatAreNotCafes()
    {
        OsmCafeMapper.ToCafe(new Dictionary<string, string> { ["amenity"] = "bar", ["name"] = "Bar" }, 0, 0)
            .Should().BeNull();
        OsmCafeMapper.ToCafe(new Dictionary<string, string> { ["amenity"] = "cafe" }, 0, 0)
            .Should().BeNull();
    }

    private static (CafeService, Mock<ICafeRepository>, Mock<IOsmCafeSource>) CreateService(Cafe? cafe)
    {
        var repository = new Mock<ICafeRepository>();
        repository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cafe);
        var source = new Mock<IOsmCafeSource>();

        return (new CafeService(repository.Object, Mock.Of<IPhotoStorage>(), source.Object), repository, source);
    }

    private static Cafe CreateCafe() => new()
    {
        Name = "Test Cafe",
        Address = "Calle Test 1",
        Neighborhood = "Centro",
        Location = new Point(-3.7038, 40.4168) { SRID = 4326 }
    };
}
