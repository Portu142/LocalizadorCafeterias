using FluentValidation;
using LocalizadorCafes.Application.Cafes.Dtos;

namespace LocalizadorCafes.Application.Cafes.Validators;

public class CreateReviewRequestValidator : AbstractValidator<CreateReviewRequest>
{
    public CreateReviewRequestValidator()
    {
        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5).WithMessage("La puntuación debe estar entre 1 y 5.");

        RuleFor(x => x.Comment)
            .NotEmpty().WithMessage("El comentario es obligatorio.")
            .MinimumLength(10).WithMessage("El comentario debe tener al menos 10 caracteres.")
            .MaximumLength(500).WithMessage("El comentario no puede superar los 500 caracteres.");

        RuleFor(x => x.ItemRatings)
            .Must(items => items is null || items.Select(item => item.MenuItemId).Distinct().Count() == items.Count)
            .WithMessage("No se puede puntuar el mismo plato más de una vez.");

        RuleFor(x => x.ItemRatings)
            .Must(items => items is null || items.Count <= 50)
            .WithMessage("No se pueden puntuar más de 50 platos en una reseña.");

        RuleForEach(x => x.ItemRatings).ChildRules(item =>
        {
            item.RuleFor(x => x.MenuItemId)
                .NotEmpty().WithMessage("El plato no es válido.");

            item.RuleFor(x => x.Score)
                .InclusiveBetween(1, 5).WithMessage("La puntuación del plato debe estar entre 1 y 5.");
        });
    }
}
