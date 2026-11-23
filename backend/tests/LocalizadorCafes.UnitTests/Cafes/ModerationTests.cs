using FluentAssertions;
using FluentValidation;
using LocalizadorCafes.Application.Cafes;
using LocalizadorCafes.Application.Cafes.Dtos;
using LocalizadorCafes.Application.Common.Interfaces;
using LocalizadorCafes.Domain.Entities;
using Moq;
using NetTopologySuite.Geometries;

namespace LocalizadorCafes.UnitTests.Cafes;

public class ModerationTests
{
    private static readonly ReviewAuthor Ana = new(Guid.NewGuid(), "Ana");
    private static readonly Guid OtherUser = Guid.NewGuid();

    [Fact]
    public async Task AddMenuItemAsync_ShouldCreatePendingDish()
    {
        var cafe = CreateCafe();
        var (service, repository) = CreateService(cafe);

        var result = await service.AddMenuItemAsync(cafe.Id, new CreateMenuItemRequest("Latte", 3m), Ana);

        result!.Status.Should().Be("pending");
        repository.Verify(x => x.AddMenuItemAsync(
            It.Is<MenuItem>(item => item.Status == ModerationStatus.Pending), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task AddPhotoAsync_ShouldCreatePendingPhoto()
    {
        var cafe = CreateCafe();
        var repository = new Mock<ICafeRepository>();
        repository.Setup(x => x.GetByIdAsync(cafe.Id, It.IsAny<CancellationToken>())).ReturnsAsync(cafe);
        var storage = new Mock<IPhotoStorage>();
        storage.Setup(x => x.SaveAsync(It.IsAny<Stream>(), "jpg", It.IsAny<CancellationToken>())).ReturnsAsync("/api/photos/a.jpg");
        var service = new CafeService(repository.Object, storage.Object, Mock.Of<IOsmCafeSource>());
        var jpeg = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0, 0, 0, 0, 0, 0, 0, 0 };

        var result = await service.AddPhotoAsync(cafe.Id, new MemoryStream(jpeg), jpeg.Length, Ana);

        result!.Status.Should().Be("pending");
        repository.Verify(x => x.AddPhotoAsync(
            It.Is<CafePhoto>(photo => photo.Status == ModerationStatus.Pending), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task GetByIdAsync_ShouldHidePendingContentFromEveryoneButItsAuthor()
    {
        var cafe = CreateCafe();
        cafe.Menu.Add(new MenuItem { Name = "Aprobado", Price = 2m, Status = ModerationStatus.Approved });
        cafe.Menu.Add(new MenuItem { Name = "Pendiente de Ana", Price = 2m, Status = ModerationStatus.Pending, AddedByUserId = Ana.UserId });
        cafe.Menu.Add(new MenuItem { Name = "Rechazado", Price = 2m, Status = ModerationStatus.Rejected, AddedByUserId = Ana.UserId });
        cafe.Photos.Add(new CafePhoto { Url = "/a.jpg", Author = "Ana", License = "x", Status = ModerationStatus.Pending, UploadedByUserId = Ana.UserId });
        var (service, _) = CreateService(cafe);

        var anonymous = await service.GetByIdAsync(cafe.Id);
        var other = await service.GetByIdAsync(cafe.Id, OtherUser);
        var author = await service.GetByIdAsync(cafe.Id, Ana.UserId);

        anonymous!.Menu.Select(item => item.Name).Should().Equal("Aprobado");
        anonymous.Photos.Should().BeEmpty();
        other!.Menu.Select(item => item.Name).Should().Equal("Aprobado");
        author!.Menu.Select(item => item.Name).Should().BeEquivalentTo("Aprobado", "Pendiente de Ana");
        author.Photos.Should().ContainSingle().Which.Status.Should().Be("pending");
    }

    [Fact]
    public async Task AddReviewAsync_ShouldNotAllowRatingPendingDishes()
    {
        var cafe = CreateCafe();
        var pending = new MenuItem { CafeId = cafe.Id, Name = "Nuevo", Price = 2m, Status = ModerationStatus.Pending };
        cafe.Menu.Add(pending);
        var (service, _) = CreateService(cafe);

        var act = () => service.AddReviewAsync(
            cafe.Id,
            new CreateReviewRequest(null, 5, "Muy buen café.", new[] { new CreateMenuItemRating(pending.Id, 5) }),
            Ana);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task AddMenuItemAsync_ShouldAllowReproposingARejectedDish()
    {
        var cafe = CreateCafe();
        cafe.Menu.Add(new MenuItem { Name = "Latte", Price = 2m, Status = ModerationStatus.Rejected });
        var (service, _) = CreateService(cafe);

        var result = await service.AddMenuItemAsync(cafe.Id, new CreateMenuItemRequest("latte", 3m), Ana);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task ReviewDishAsync_ShouldApproveOrReject()
    {
        var (service, repository) = CreateService(CreateCafe());
        var id = Guid.NewGuid();
        repository.Setup(x => x.SetMenuItemStatusAsync(id, It.IsAny<ModerationStatus>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        (await service.ReviewDishAsync(id, approve: true)).Should().BeTrue();
        (await service.ReviewDishAsync(id, approve: false)).Should().BeTrue();

        repository.Verify(x => x.SetMenuItemStatusAsync(id, ModerationStatus.Approved, It.IsAny<CancellationToken>()));
        repository.Verify(x => x.SetMenuItemStatusAsync(id, ModerationStatus.Rejected, It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task GetModerationQueueAsync_ShouldListPendingDishesAndPhotosWithCafe()
    {
        var cafe = CreateCafe();
        var (service, repository) = CreateService(cafe);
        repository.Setup(x => x.GetPendingMenuItemsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<MenuItem>
        {
            new() { CafeId = cafe.Id, Cafe = cafe, Name = "Latte", Price = 3m, AddedByUserId = Ana.UserId, Status = ModerationStatus.Pending }
        });
        repository.Setup(x => x.GetPendingPhotosAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<CafePhoto>
        {
            new() { CafeId = cafe.Id, Cafe = cafe, Url = "/api/photos/a.jpg", Author = "Ana", License = "x", Status = ModerationStatus.Pending }
        });

        var queue = await service.GetModerationQueueAsync();

        queue.Dishes.Should().ContainSingle().Which.CafeName.Should().Be("Test Cafe");
        queue.Photos.Should().ContainSingle().Which.Url.Should().Be("/api/photos/a.jpg");
    }

    private static (CafeService, Mock<ICafeRepository>) CreateService(Cafe cafe)
    {
        var repository = new Mock<ICafeRepository>();
        repository.Setup(x => x.GetByIdAsync(cafe.Id, It.IsAny<CancellationToken>())).ReturnsAsync(cafe);
        return (new CafeService(repository.Object, Mock.Of<IPhotoStorage>(), Mock.Of<IOsmCafeSource>()), repository);
    }

    private static Cafe CreateCafe() => new()
    {
        Name = "Test Cafe",
        Address = "Calle Test 1",
        Neighborhood = "Centro",
        Location = new Point(-3.7038, 40.4168) { SRID = 4326 }
    };
}
