using LocalizadorCafes.Application.Admin;
using LocalizadorCafes.Application.Cafes;
using Microsoft.AspNetCore.Mvc;

namespace LocalizadorCafes.Api.Pages.Admin;

public class ResenasModel : AdminPageModel
{
    public ResenasModel(CafeService cafeService, AdminService adminService)
        : base(cafeService, adminService)
    {
    }

    [BindProperty(SupportsGet = true, Name = "q")]
    public string? Query { get; set; }

    [BindProperty(SupportsGet = true, Name = "pagina")]
    public int Pagina { get; set; } = 1;

    public PagedResult<AdminReviewRow> Result { get; private set; } = new(Array.Empty<AdminReviewRow>(), 0, 1, AdminService.PageSize);

    public async Task OnGetAsync(CancellationToken cancellationToken) =>
        Result = await AdminService.GetReviewsAsync(Query, Pagina, cancellationToken: cancellationToken);
}
