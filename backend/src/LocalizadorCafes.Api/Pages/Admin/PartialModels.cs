using LocalizadorCafes.Application.Admin;

namespace LocalizadorCafes.Api.Pages.Admin;

/// <summary>Datos de los parciales reutilizados por varias páginas del panel.</summary>
public record DishTableModel(IReadOnlyList<AdminDishRow> Dishes, string ReturnUrl, bool ShowCafe = true);

public record PhotoGridModel(IReadOnlyList<AdminPhotoRow> Photos, string ReturnUrl, bool ShowCafe = true);

public record ReviewListModel(IReadOnlyList<AdminReviewRow> Reviews, string ReturnUrl, bool ShowCafe = true);

public record PagerModel(int Page, int TotalPages, int Total, Func<int, string> Url);
