using FluentValidation;
using LocalizadorCafes.Application.Admin;
using LocalizadorCafes.Application.Cafes;
using Microsoft.AspNetCore.Mvc;

namespace LocalizadorCafes.Api.Pages.Admin;

/// <summary>
/// Acciones sobre platos, fotos y reseñas compartidas por todas las páginas
/// del panel (listados, ficha de cafetería, ficha de usuario). Cada formulario
/// envía «volver» con la página a la que regresar y todo queda registrado.
/// </summary>
public class AccionesModel : AdminPageModel
{
    public AccionesModel(CafeService cafeService, AdminService adminService)
        : base(cafeService, adminService)
    {
    }

    public IActionResult OnGet() => Redirect("/admin");

    public async Task<IActionResult> OnPostApproveDishAsync(Guid id, string name, string? volver, CancellationToken cancellationToken)
    {
        if (!await CafeService.ReviewDishAsync(id, approve: true, cancellationToken))
        {
            return Back(volver, "Ese plato ya no existe.");
        }

        await AdminService.AuditAsync(Actor, "Publicar plato", "plato", id, name, cancellationToken);
        return Back(volver, $"«{name}» publicado en la carta.");
    }

    public async Task<IActionResult> OnPostRejectDishAsync(Guid id, string name, string? volver, CancellationToken cancellationToken)
    {
        if (!await CafeService.ReviewDishAsync(id, approve: false, cancellationToken))
        {
            return Back(volver, "Ese plato ya no existe.");
        }

        await AdminService.AuditAsync(Actor, "Rechazar plato", "plato", id, name, cancellationToken);
        return Back(volver, $"«{name}» rechazado: ya no se muestra.");
    }

    public async Task<IActionResult> OnPostEditDishAsync(
        Guid id, string name, string price, string? volver, CancellationToken cancellationToken)
    {
        var parsed = decimal.TryParse(price?.Replace(',', '.'), System.Globalization.NumberStyles.Number,
            System.Globalization.CultureInfo.InvariantCulture, out var value);

        try
        {
            if (!parsed)
            {
                return Back(volver, "El precio no es válido (usa por ejemplo 2,50).");
            }

            return await AdminService.UpdateDishAsync(id, name, value, Actor, cancellationToken)
                ? Back(volver, $"«{name.Trim()}» guardado.")
                : Back(volver, "Ese plato ya no existe.");
        }
        catch (ValidationException exception)
        {
            return Back(volver, exception.Errors.First().ErrorMessage);
        }
    }

    public async Task<IActionResult> OnPostDeleteDishAsync(Guid id, string name, string? volver, CancellationToken cancellationToken) =>
        await AdminService.DeleteDishAsync(id, name, Actor, cancellationToken)
            ? Back(volver, $"«{name}» borrado definitivamente.")
            : Back(volver, "Ese plato ya no existe.");

    public async Task<IActionResult> OnPostApprovePhotoAsync(Guid id, string? volver, CancellationToken cancellationToken)
    {
        if (!await CafeService.ReviewPhotoAsync(id, approve: true, cancellationToken))
        {
            return Back(volver, "Esa foto ya no existe.");
        }

        await AdminService.AuditAsync(Actor, "Publicar foto", "foto", id, string.Empty, cancellationToken);
        return Back(volver, "Foto publicada en la ficha.");
    }

    public async Task<IActionResult> OnPostRejectPhotoAsync(Guid id, string? volver, CancellationToken cancellationToken)
    {
        if (!await CafeService.ReviewPhotoAsync(id, approve: false, cancellationToken))
        {
            return Back(volver, "Esa foto ya no existe.");
        }

        await AdminService.AuditAsync(Actor, "Rechazar foto", "foto", id, string.Empty, cancellationToken);
        return Back(volver, "Foto rechazada: ya no se muestra.");
    }

    public async Task<IActionResult> OnPostDeletePhotoAsync(Guid id, string? volver, CancellationToken cancellationToken) =>
        await AdminService.DeletePhotoAsync(id, Actor, cancellationToken)
            ? Back(volver, "Foto borrada definitivamente.")
            : Back(volver, "Esa foto ya no existe.");

    public async Task<IActionResult> OnPostDeleteReviewAsync(Guid id, string author, string? volver, CancellationToken cancellationToken)
    {
        if (!await CafeService.DeleteReviewAsync(id, cancellationToken))
        {
            return Back(volver, "Esa reseña ya no existe.");
        }

        await AdminService.AuditAsync(Actor, "Borrar reseña", "reseña", id, $"de {author}", cancellationToken);
        return Back(volver, $"Reseña de {author} borrada.");
    }
}
