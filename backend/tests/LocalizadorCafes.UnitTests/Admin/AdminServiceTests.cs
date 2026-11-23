using FluentAssertions;
using FluentValidation;
using LocalizadorCafes.Application.Admin;
using LocalizadorCafes.Application.Common.Interfaces;
using LocalizadorCafes.Domain.Entities;
using Moq;

namespace LocalizadorCafes.UnitTests.Admin;

public class AdminServiceTests
{
    private static readonly AdminActor Admin = new(Guid.NewGuid(), "admin@localizador.local");

    private readonly Mock<IAdminRepository> _repository = new();
    private readonly Mock<IAuditLog> _auditLog = new();
    private readonly Mock<IPhotoStorage> _storage = new();

    private AdminService CreateService() => new(_repository.Object, _auditLog.Object, _storage.Object);

    [Fact]
    public void GroupByWeek_ShouldCountByMondayAndFillEmptyWeeks()
    {
        // Lunes 23 nov 2026; la semana actual empieza ese día.
        var today = new DateOnly(2026, 11, 25);
        var dates = new[]
        {
            new DateTimeOffset(2026, 11, 23, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 11, 25, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 11, 10, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2025, 1, 1, 10, 0, 0, TimeSpan.Zero) // fuera del rango
        };

        var weeks = AdminService.GroupByWeek(dates, today, 4);

        weeks.Should().HaveCount(4);
        weeks.Select(week => week.WeekStart).Should().Equal(
            new DateOnly(2026, 11, 2), new DateOnly(2026, 11, 9), new DateOnly(2026, 11, 16), new DateOnly(2026, 11, 23));
        weeks.Select(week => week.Count).Should().Equal(0, 1, 0, 2);
    }

    [Fact]
    public async Task UpdateCafeAsync_ShouldCleanTagsAndAudit()
    {
        var id = Guid.NewGuid();
        CafeEdit? saved = null;
        _repository
            .Setup(x => x.UpdateCafeAsync(id, It.IsAny<CafeEdit>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, CafeEdit, CancellationToken>((_, edit, _) => saved = edit)
            .ReturnsAsync(true);

        var result = await CreateService().UpdateCafeAsync(
            id,
            new CafeEdit("  Café Nuevo ", "Calle Mayor 1", "Sol", "", "Mo-Fr 08:00-20:00", new[] { "wifi", "inventada", "wifi" }),
            Admin);

        result.Should().BeTrue();
        saved!.Name.Should().Be("Café Nuevo");
        saved.Tags.Should().Equal("wifi");
        _auditLog.Verify(x => x.WriteAsync(
            It.Is<AdminAuditEntry>(entry => entry.Action == "Editar cafetería" && entry.AdminUserId == Admin.UserId && entry.TargetId == id),
            It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task UpdateCafeAsync_ShouldRejectInvalidData()
    {
        var act = () => CreateService().UpdateCafeAsync(
            Guid.NewGuid(), new CafeEdit("A", "", "", "", "", Array.Empty<string>()), Admin);

        await act.Should().ThrowAsync<ValidationException>();
        _repository.Verify(x => x.UpdateCafeAsync(It.IsAny<Guid>(), It.IsAny<CafeEdit>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("A", 2)]
    [InlineData("Latte", 0)]
    [InlineData("Latte", 2.555)]
    public async Task UpdateDishAsync_ShouldRejectInvalidValues(string name, double price)
    {
        var act = () => CreateService().UpdateDishAsync(Guid.NewGuid(), name, (decimal)price, Admin);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task DeletePhotoAsync_ShouldDeleteFileAndAudit()
    {
        var id = Guid.NewGuid();
        _repository
            .Setup(x => x.DeletePhotoAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CafePhoto { Url = "/api/photos/abc.jpg", Author = "Ana" });

        var result = await CreateService().DeletePhotoAsync(id, Admin);

        result.Should().BeTrue();
        _storage.Verify(x => x.DeleteAsync("/api/photos/abc.jpg", It.IsAny<CancellationToken>()));
        _auditLog.Verify(x => x.WriteAsync(It.Is<AdminAuditEntry>(entry => entry.Action == "Borrar foto"), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task DeleteCafeAsync_ShouldRemoveUploadedPhotoFiles()
    {
        var id = Guid.NewGuid();
        _repository
            .Setup(x => x.GetPhotosAsync(null, null, id, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<AdminPhotoRow>
            {
                new(Guid.NewGuid(), id, "Café", "/api/photos/a.jpg", "Ana", "x", ModerationStatus.Approved, null, DateTimeOffset.UtcNow)
            }, 1));
        _repository.Setup(x => x.DeleteCafeAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await CreateService().DeleteCafeAsync(id, "Café", Admin);

        result.Should().BeTrue();
        _storage.Verify(x => x.DeleteAsync("/api/photos/a.jpg", It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task DeleteDishAsync_ShouldNotAudit_WhenNothingWasDeleted()
    {
        _repository.Setup(x => x.DeleteDishAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await CreateService().DeleteDishAsync(Guid.NewGuid(), "Latte", Admin);

        result.Should().BeFalse();
        _auditLog.Verify(x => x.WriteAsync(It.IsAny<AdminAuditEntry>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetCommunityFeedAsync_ShouldMergeUserContributionsNewestFirst()
    {
        var user = Guid.NewGuid();
        var cafe = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        _repository
            .Setup(x => x.GetReviewsAsync(null, null, null, 0, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<AdminReviewRow> { new(Guid.NewGuid(), cafe, "Café OSM", user, "Ana", 4, "Muy rico", now.AddMinutes(-1)) }, 1));
        _repository
            .Setup(x => x.GetDishesAsync(null, null, null, null, 0, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<AdminDishRow>
            {
                new(Guid.NewGuid(), cafe, "Café OSM", "Latte", 3m, ModerationStatus.Pending, user, now),
                new(Guid.NewGuid(), cafe, "Café OSM", "De ejemplo", 2m, ModerationStatus.Approved, null, now) // sin autor: no cuenta
            }, 2));
        _repository
            .Setup(x => x.GetPhotosAsync(null, null, null, 0, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<AdminPhotoRow>(), 0));
        _repository
            .Setup(x => x.SearchCafesAsync(null, 0, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<AdminCafeRow>
            {
                new(cafe, "Café OSM", "Calle 1", "", "node/1", 1, 1, 0, 1, now.AddMinutes(-2)),
                new(Guid.NewGuid(), "Ejemplo", "Calle 2", "", null, 4, 0, 0, 2, now.AddDays(-30)) // de ejemplo: no cuenta
            }, 2));

        var feed = await CreateService().GetCommunityFeedAsync(10);

        feed.Select(item => item.Kind).Should().Equal(
            CommunityEventKind.Dish, CommunityEventKind.Review, CommunityEventKind.Cafe);
        feed[0].Status.Should().Be(ModerationStatus.Pending);
    }

    [Fact]
    public async Task SearchCafesAsync_ShouldPageResults()
    {
        _repository
            .Setup(x => x.SearchCafesAsync("sol", 25, AdminService.PageSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<AdminCafeRow>(), 60));

        var page = await CreateService().SearchCafesAsync("  sol ", 2);

        page.Page.Should().Be(2);
        page.TotalPages.Should().Be(3);
    }
}
