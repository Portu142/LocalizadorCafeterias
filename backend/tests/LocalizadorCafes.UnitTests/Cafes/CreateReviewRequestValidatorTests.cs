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
            new CreateReviewRequest("Ana", 5, 5, "Un café excelente."));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_ShouldFail_WhenAuthorIsEmpty(string author)
    {
        var result = _validator.TestValidate(
            new CreateReviewRequest(author, 5, 5, "Un café excelente."));

        result.ShouldHaveValidationErrorFor(request => request.Author);
    }

    [Fact]
    public void Validate_ShouldFail_WhenAuthorIsTooLong()
    {
        var result = _validator.TestValidate(
            new CreateReviewRequest(new string('a', 81), 5, 5, "Un café excelente."));

        result.ShouldHaveValidationErrorFor(request => request.Author);
    }

    [Fact]
    public void Validate_ShouldFail_WhenAuthorIsTooShort()
    {
        var result = _validator.TestValidate(
            new CreateReviewRequest("A", 5, 5, "Un café excelente."));

        result.ShouldHaveValidationErrorFor(request => request.Author);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Validate_ShouldFail_WhenRatingIsOutOfRange(int rating)
    {
        var result = _validator.TestValidate(
            new CreateReviewRequest("Ana", rating, 5, "Un café excelente."));

        result.ShouldHaveValidationErrorFor(request => request.Rating);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Validate_ShouldFail_WhenMenuRatingIsOutOfRange(int menuRating)
    {
        var result = _validator.TestValidate(
            new CreateReviewRequest("Ana", 5, menuRating, "Un café excelente."));

        result.ShouldHaveValidationErrorFor(request => request.MenuRating);
    }

    [Fact]
    public void Validate_ShouldFail_WhenCommentIsEmpty()
    {
        var result = _validator.TestValidate(
            new CreateReviewRequest("Ana", 5, 5, " "));

        result.ShouldHaveValidationErrorFor(request => request.Comment);
    }

    [Fact]
    public void Validate_ShouldFail_WhenCommentIsTooLong()
    {
        var result = _validator.TestValidate(
            new CreateReviewRequest("Ana", 5, 5, new string('a', 501)));

        result.ShouldHaveValidationErrorFor(request => request.Comment);
    }

    [Fact]
    public void Validate_ShouldFail_WhenCommentIsTooShort()
    {
        var result = _validator.TestValidate(
            new CreateReviewRequest("Ana", 5, 5, "Corto"));

        result.ShouldHaveValidationErrorFor(request => request.Comment);
    }
}
