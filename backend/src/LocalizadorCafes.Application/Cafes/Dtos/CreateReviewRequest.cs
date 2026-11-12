namespace LocalizadorCafes.Application.Cafes.Dtos;

public record CreateReviewRequest(string Author, int Rating, string Comment);
