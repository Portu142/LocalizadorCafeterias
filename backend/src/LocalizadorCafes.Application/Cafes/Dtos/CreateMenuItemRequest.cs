namespace LocalizadorCafes.Application.Cafes.Dtos;

/// <summary>Plato que añade un usuario a la carta de una cafetería.</summary>
public record CreateMenuItemRequest(string Name, decimal Price);
