using FluentValidation;
using LocalizadorCafes.Application.Cafes.Dtos;

namespace LocalizadorCafes.Application.Cafes.Validators;

public class CreateMenuItemRequestValidator : AbstractValidator<CreateMenuItemRequest>
{
    public CreateMenuItemRequestValidator()
    {
        RuleFor(x => x.Name)
            .Must(name => !string.IsNullOrWhiteSpace(name)).WithMessage("Escribe el nombre del plato.")
            .Must(name => name is null || name.Trim().Length >= 2).WithMessage("El nombre debe tener al menos 2 caracteres.")
            .MaximumLength(120).WithMessage("El nombre no puede superar los 120 caracteres.");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("El precio debe ser mayor que 0.")
            .LessThanOrEqualTo(500).WithMessage("El precio no puede superar los 500 €.")
            .Must(price => decimal.Round(price, 2) == price).WithMessage("El precio admite como máximo 2 decimales.");
    }
}
