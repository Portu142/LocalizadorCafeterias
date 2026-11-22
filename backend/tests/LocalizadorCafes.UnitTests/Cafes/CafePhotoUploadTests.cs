using FluentAssertions;
using FluentValidation;
using LocalizadorCafes.Application.Cafes;
using LocalizadorCafes.Application.Cafes.Dtos;
using LocalizadorCafes.Application.Common.Interfaces;
using LocalizadorCafes.Domain.Entities;
using Moq;
using NetTopologySuite.Geometries;

namespace LocalizadorCafes.UnitTests.Cafes;

public class CafePhotoUploadTests
{
    private static readonly ReviewAuthor Uploader = new(Guid.NewGuid(), "Ana");

    private static readonly byte[] Jpeg = { 0xFF, 0xD8, 0xFF, 0xE0, 0, 0x10, 0x4A, 0x46, 0x49, 0x46, 0, 1 };
    private static readonly byte[] Png = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0x0D };
    private static readonly byte[] Webp = "RIFF\0\0\0\0WEBPVP8 "u8.ToArray();

    [Theory]
    [MemberData(nameof(ValidImages))]
    public void DetectExtension_ShouldRecognizeSupportedFormats(byte[] header, string expected)
    {
        PhotoFormat.DetectExtension(header).Should().Be(expected);
    }

    public static TheoryData<byte[], string> ValidImages() => new()
    {
        { Jpeg, "jpg" },
        { Png, "png" },
        { Webp, "webp" }
    };

    [Fact]
    public void DetectExtension_ShouldRejectHtmlDisguisedAsImage()
    {
        PhotoFormat.DetectExtension("<html><script>"u8).Should().BeNull();
    }

    [Fact]
    public async Task AddPhotoAsync_ShouldStoreImageAndAttributeUploader()
    {
        var cafe = CreateCafe();
        var (service, repository, storage) = CreateService(cafe);
        storage
            .Setup(x => x.SaveAsync(It.IsAny<Stream>(), "jpg", It.IsAny<CancellationToken>()))
            .ReturnsAsync("/api/photos/abc.jpg");

        var result = await service.AddPhotoAsync(cafe.Id, new MemoryStream(Jpeg), Jpeg.Length, Uploader);

        result.Should().NotBeNull();
        result!.Url.Should().Be("/api/photos/abc.jpg");
        result.Author.Should().Be("Ana");
        repository.Verify(x => x.AddPhotoAsync(
            It.Is<CafePhoto>(photo => photo.CafeId == cafe.Id && photo.UploadedByUserId == Uploader.UserId),
            It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task AddPhotoAsync_ShouldRejectFilesThatAreNotImages()
    {
        var cafe = CreateCafe();
        var (service, repository, storage) = CreateService(cafe);
        var html = "<html>hola</html>"u8.ToArray();

        var act = () => service.AddPhotoAsync(cafe.Id, new MemoryStream(html), html.Length, Uploader);

        await act.Should().ThrowAsync<ValidationException>();
        storage.Verify(x => x.SaveAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        repository.Verify(x => x.AddPhotoAsync(It.IsAny<CafePhoto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddPhotoAsync_ShouldRejectFilesOverFiveMegabytes()
    {
        var cafe = CreateCafe();
        var (service, _, _) = CreateService(cafe);

        var act = () => service.AddPhotoAsync(cafe.Id, Stream.Null, PhotoFormat.MaxBytes + 1, Uploader);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task AddPhotoAsync_ShouldReturnNull_WhenCafeDoesNotExist()
    {
        var (service, _, _) = CreateService(null);

        var result = await service.AddPhotoAsync(Guid.NewGuid(), new MemoryStream(Jpeg), Jpeg.Length, Uploader);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetUserReviewsAsync_ShouldIncludeCafeName()
    {
        var cafe = CreateCafe();
        var review = new Review { CafeId = cafe.Id, UserId = Uploader.UserId, Author = "Ana", Rating = 4, Comment = "Muy rico." };
        var (service, repository, _) = CreateService(cafe);
        repository
            .Setup(x => x.GetReviewsByUserAsync(Uploader.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(Review, Cafe)> { (review, cafe) });

        var result = await service.GetUserReviewsAsync(Uploader.UserId);

        result.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { CafeId = cafe.Id, CafeName = "Test Cafe", Rating = 4 });
    }

    private static (CafeService, Mock<ICafeRepository>, Mock<IPhotoStorage>) CreateService(Cafe? cafe)
    {
        var repository = new Mock<ICafeRepository>();
        repository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cafe);
        var storage = new Mock<IPhotoStorage>();

        return (new CafeService(repository.Object, storage.Object), repository, storage);
    }

    private static Cafe CreateCafe() => new()
    {
        Name = "Test Cafe",
        Address = "Calle Test 1",
        Neighborhood = "Centro",
        Hours = "Mo-Fr 08:00-20:00",
        Location = new Point(-3.7038, 40.4168) { SRID = 4326 }
    };
}
