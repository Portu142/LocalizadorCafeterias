namespace LocalizadorCafes.Application.Cafes.Dtos;

/// <param name="AddedByUserId">Quién lo añadió (null en los datos de ejemplo): permite
/// al autor borrar su propio plato.</param>
/// <param name="Status">«approved» o «pending» (este solo lo ve su autor).</param>
public record MenuItemDto(Guid Id, string Name, decimal Price, Guid? AddedByUserId = null, string Status = "approved");
