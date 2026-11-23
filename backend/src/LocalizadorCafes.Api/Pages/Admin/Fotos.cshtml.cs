using LocalizadorCafes.Application.Admin;
using LocalizadorCafes.Application.Cafes;
using Microsoft.AspNetCore.Mvc;

namespace LocalizadorCafes.Api.Pages.Admin;

public class FotosModel : AdminPageModel
{
    public FotosModel(CafeService cafeService, AdminService adminService)
        : base(cafeService, adminService)
    {
    }

    [BindProperty(SupportsGet = true, Name = "estado")]
    public string? Estado { get; set; }

    [BindProperty(SupportsGet = true, Name = "pagina")]
    public int Pagina { get; set; } = 1;

    public PagedResult<AdminPhotoRow> Result { get; private set; } = new(Array.Empty<AdminPhotoRow>(), 0, 1, AdminService.PageSize);

    public string EstadoActual => Estado is "publicados" or "rechazados" or "todos" ? Estado : "pendientes";

    public async Task OnGetAsync(CancellationToken cancellationToken) =>
        Result = await AdminService.GetPhotosAsync(ParseStatus(Estado), Pagina, cancellationToken: cancellationToken);
}
