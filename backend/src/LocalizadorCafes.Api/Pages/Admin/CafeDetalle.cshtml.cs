using FluentValidation;
using LocalizadorCafes.Application.Admin;
using LocalizadorCafes.Application.Cafes;
using LocalizadorCafes.Application.Common.Interfaces;
using LocalizadorCafes.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace LocalizadorCafes.Api.Pages.Admin;

public class CafeDetalleModel : AdminPageModel
{
    private readonly ICafeRepository _cafeRepository;

    public CafeDetalleModel(CafeService cafeService, AdminService adminService, ICafeRepository cafeRepository)
        : base(cafeService, adminService)
    {
        _cafeRepository = cafeRepository;
    }

    /// <summary>Texto de cada etiqueta de servicio (mismo vocabulario que la web).</summary>
    public static readonly IReadOnlyList<(string Key, string Label)> TagOptions = new[]
    {
        (CafeTags.Wifi, "Wifi"),
        (CafeTags.Terrace, "Terraza"),
        (CafeTags.PowerOutlets, "Enchufes"),
        (CafeTags.Accessible, "Accesible"),
        (CafeTags.Takeaway, "Para llevar"),
        (CafeTags.PetFriendly, "Admite mascotas"),
        (CafeTags.Vegan, "Opciones veganas")
    };

    public Cafe? Cafe { get; private set; }
    public IReadOnlyList<AdminDishRow> Dishes { get; private set; } = Array.Empty<AdminDishRow>();
    public IReadOnlyList<AdminPhotoRow> Photos { get; private set; } = Array.Empty<AdminPhotoRow>();
    public IReadOnlyList<AdminReviewRow> Reviews { get; private set; } = Array.Empty<AdminReviewRow>();

    [BindProperty] public string Name { get; set; } = string.Empty;
    [BindProperty] public string Address { get; set; } = string.Empty;
    [BindProperty] public string Neighborhood { get; set; } = string.Empty;
    [BindProperty] public string Phone { get; set; } = string.Empty;
    [BindProperty] public string Hours { get; set; } = string.Empty;
    [BindProperty] public List<string> Tags { get; set; } = new();

    public string? Error { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!await LoadAsync(id, cancellationToken))
        {
            return NotFound();
        }

        Name = Cafe!.Name;
        Address = Cafe.Address;
        Neighborhood = Cafe.Neighborhood;
        Phone = Cafe.Phone;
        Hours = Cafe.Hours;
        Tags = Cafe.Tags.ToList();
        return Page();
    }

    public async Task<IActionResult> OnPostSaveAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var edit = new CafeEdit(Name ?? "", Address ?? "", Neighborhood ?? "", Phone ?? "", Hours ?? "", Tags);
            return await AdminService.UpdateCafeAsync(id, edit, Actor, cancellationToken)
                ? Done("Datos de la cafetería guardados.")
                : NotFound();
        }
        catch (ValidationException exception)
        {
            Error = string.Join(" ", exception.Errors.Select(error => error.ErrorMessage));
            return await LoadAsync(id, cancellationToken) ? Page() : NotFound();
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id, string name, CancellationToken cancellationToken)
    {
        if (!await AdminService.DeleteCafeAsync(id, name, Actor, cancellationToken))
        {
            return NotFound();
        }

        Message = $"«{name}» borrada junto con su carta, fotos y reseñas.";
        return Redirect("/admin/cafes");
    }

    private async Task<bool> LoadAsync(Guid id, CancellationToken cancellationToken)
    {
        Cafe = await _cafeRepository.GetByIdAsync(id, cancellationToken);
        if (Cafe is null)
        {
            return false;
        }

        Dishes = (await AdminService.GetDishesAsync(null, null, 1, cafeId: id, cancellationToken: cancellationToken)).Items;
        Photos = (await AdminService.GetPhotosAsync(null, 1, cafeId: id, cancellationToken: cancellationToken)).Items;
        Reviews = (await AdminService.GetReviewsAsync(null, 1, cafeId: id, cancellationToken: cancellationToken)).Items;
        return true;
    }
}
