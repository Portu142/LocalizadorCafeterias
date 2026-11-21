using FluentValidation.TestHelper;
using LocalizadorCafes.Application.Cafes.Dtos;
using LocalizadorCafes.Application.Cafes.Validators;

namespace LocalizadorCafes.UnitTests.Cafes;

public class CreateReviewRequestValidatorTests
{
    private readonly CreateReviewRequestValidator _validator = new();

    [Fact]
    public void Validate_ShouldPass_WithValidRequest()
    {
        var result = _validator.TestValidate(
            new CreateReviewRequest("Ana", 5, "Un café excelente."));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ShouldPass_WithItemRatings()
    {
        var result = _validator.TestValidate(
            new CreateReviewRequest(
                "Ana",
                5,
                "Un café excelente.",
                new[]
                {
                    new CreateMenuItemRating(Guid.NewGuid(), 5),
                    new CreateMenuItemRating(Guid.NewGuid(), 3)
                }));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_ShouldFail_WhenAuthorIsEmpty(string author)
    {
        var result = _validator.TestValidate(
            new CreateReviewRequest(author, 5, "Un café excelente."));

        result.ShouldHaveValidationErrorFor(request => request.Author);
    }

    [Fact]
    public void Validate_ShouldFail_WhenAuthorIsTooLong()
    {
        var result = _validator.TestValidate(
            new CreateReviewRequest(new string('a', 81), 5, "Un café excelente."));

        result.ShouldHaveValidationErrorFor(request => request.Author);
    }

    [Fact]
    public void Validate_ShouldFail_WhenAuthorIsTooShort()
    {
        var result = _validator.TestValidate(
            new CreateReviewRequest("A", 5, "Un café excelente."));

        result.ShouldHaveValidationErrorFor(request => request.Author);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Validate_ShouldFail_WhenRatingIsOutOfRange(int rating)
    {
        var result = _validator.TestValidate(
            new CreateReviewRequest("Ana", rating, "Un café excelente."));

        result.ShouldHaveValidationErrorFor(request => request.Rating);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Validate_ShouldFail_WhenItemScoreIsOutOfRange(int score)
    {
        var result = _validator.TestValidate(
            new CreateReviewRequest(
                "Ana",
                5,
                "Un café excelente.",
                new[] { new CreateMenuItemRating(Guid.NewGuid(), score) }));

        result.ShouldHaveValidationErrorFor("ItemRatings[0].Score");
    }

    [Fact]
    public void Validate_ShouldFail_WhenItemIsRepeated()
    {
        var menuItemId = Guid.NewGuid();

        var result = _validator.TestValidate(
            new CreateReviewRequest(
                "Ana",
                5,
                "Un café excelente.",
                new[]
                {
                    new CreateMenuItemRating(menuItemId, 5),
                    new CreateMenuItemRating(menuItemId, 4)
                }));

        result.ShouldHaveValidationErrorFor(request => request.ItemRatings);
    }

    [Fact]
    public void Validate_ShouldFail_WhenItemIdIsEmpty()
    {
        var result = _validator.TestValidate(
            new CreateReviewRequest(
                "Ana",
                5,
                "Un café excelente.",
                new[] { new CreateMenuItemRating(Guid.Empty, 5) }));

        result.ShouldHaveValidationErrorFor("ItemRatings[0].MenuItemId");
    }

    [Fact]
    public void Validate_ShouldFail_WhenCommentIsEmpty()
    {
        var result = _validator.TestValidate(
            new CreateReviewRequest("Ana", 5, " "));

        result.ShouldHaveValidationErrorFor(request => request.Comment);
    }

    [Fact]
    public void Validate_ShouldFail_WhenCommentIsTooLong()
    {
        var result = _validator.TestValidate(
            new CreateReviewRequest("Ana", 5, new string('a', 501)));

        result.ShouldHaveValidationErrorFor(request => request.Comment);
    }

    [Fact]
    public void Validate_ShouldFail_WhenCommentIsTooShort()
    {
        var result = _validator.TestValidate(
            new CreateReviewRequest("Ana", 5, "Corto"));

        result.ShouldHaveValidationErrorFor(request => request.Comment);
    }
}
