using LocalizadorCafes.Application.Admin;
using LocalizadorCafes.Application.Cafes;
using Microsoft.AspNetCore.Mvc;

namespace LocalizadorCafes.Api.Pages.Admin;

public class CafesModel : AdminPageModel
{
    public CafesModel(CafeService cafeService, AdminService adminService)
        : base(cafeService, adminService)
    {
    }

    [BindProperty(SupportsGet = true, Name = "q")]
    public string? Query { get; set; }

    [BindProperty(SupportsGet = true, Name = "pagina")]
    public int Pagina { get; set; } = 1;

    public PagedResult<AdminCafeRow> Result { get; private set; } = new(Array.Empty<AdminCafeRow>(), 0, 1, AdminService.PageSize);

    public async Task OnGetAsync(CancellationToken cancellationToken) =>
        Result = await AdminService.SearchCafesAsync(Query, Pagina, cancellationToken);
}
