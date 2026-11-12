using FluentValidation;
using LocalizadorCafes.Application.Cafes.Dtos;

namespace LocalizadorCafes.Application.Cafes.Validators;

public class CreateReviewRequestValidator : AbstractValidator<CreateReviewRequest>
{
    public CreateReviewRequestValidator()
    {
        RuleFor(x => x.Author)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MinimumLength(2).WithMessage("El nombre debe tener al menos 2 caracteres.")
            .MaximumLength(80).WithMessage("El nombre no puede superar los 80 caracteres.");

        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5).WithMessage("La puntuación debe estar entre 1 y 5.");

        RuleFor(x => x.Comment)
            .NotEmpty().WithMessage("El comentario es obligatorio.")
            .MinimumLength(10).WithMessage("El comentario debe tener al menos 10 caracteres.")
            .MaximumLength(500).WithMessage("El comentario no puede superar los 500 caracteres.");
    }
}
