using System.Globalization;
using System.Security.Claims;
using LocalizadorCafes.Application.Admin;
using LocalizadorCafes.Application.Cafes;
using LocalizadorCafes.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace LocalizadorCafes.Api.Pages.Admin;

/// <summary>
/// Base de las páginas del panel: contadores de pendientes para el menú, el
/// administrador actual (para el registro de actividad), formatos en español
/// y un mensaje que sobrevive a la redirección tras cada acción.
/// </summary>
public abstract class AdminPageModel : PageModel
{
    protected static readonly CultureInfo Spanish = CultureInfo.GetCultureInfo("es-ES");

    protected AdminPageModel(CafeService cafeService, AdminService adminService)
    {
        CafeService = cafeService;
        AdminService = adminService;
    }

    protected CafeService CafeService { get; }
    protected AdminService AdminService { get; }

    public int PendingDishCount { get; private set; }
    public int PendingPhotoCount { get; private set; }

    [TempData]
    public string? Message { get; set; }

    /// <summary>Administrador que realiza la acción.</summary>
    protected AdminActor Actor => new(
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty,
        User.Identity?.Name ?? "administrador");

    /// <summary>URL actual, para volver aquí tras una acción.</summary>
    public string CurrentUrl => Request.Path + Request.QueryString;

    public static string Price(decimal value) => value.ToString("C", Spanish);

    public static string Date(DateTimeOffset value) =>
        value.ToLocalTime().ToString("d MMM yyyy, HH:mm", Spanish);

    public static string ShortDate(DateOnly value) => value.ToString("d MMM", Spanish);

    public static string StatusLabel(ModerationStatus status) => status switch
    {
        ModerationStatus.Pending => "Pendiente",
        ModerationStatus.Rejected => "Rechazado",
        _ => "Publicado"
    };

    public static string StatusClass(ModerationStatus status) => status switch
    {
        ModerationStatus.Pending => "estado pendiente",
        ModerationStatus.Rejected => "estado rechazado",
        _ => "estado publicado"
    };

    /// <summary>«pendientes», «publicados», «rechazados» o «todos» → filtro de estado.</summary>
    public static ModerationStatus? ParseStatus(string? value) => value switch
    {
        "publicados" => ModerationStatus.Approved,
        "rechazados" => ModerationStatus.Rejected,
        "todos" => null,
        _ => ModerationStatus.Pending
    };

    /// <summary>La URL actual cambiando (o quitando) un parámetro de la consulta.</summary>
    public string WithQuery(string key, string? value)
    {
        var query = QueryHelpers.ParseQuery(Request.QueryString.Value)
            .Where(pair => !string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase) &&
                           !string.Equals(pair.Key, "pagina", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(pair => pair.Key, pair => (string?)pair.Value.ToString());

        if (!string.IsNullOrEmpty(value))
        {
            query[key] = value;
        }

        return QueryHelpers.AddQueryString(Request.Path, query);
    }

    /// <summary>URL de una página concreta del listado, conservando los filtros.</summary>
    public string PageUrl(int page)
    {
        var query = QueryHelpers.ParseQuery(Request.QueryString.Value)
            .Where(pair => !string.Equals(pair.Key, "pagina", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(pair => pair.Key, pair => (string?)pair.Value.ToString());
        query["pagina"] = page.ToString(CultureInfo.InvariantCulture);
        return QueryHelpers.AddQueryString(Request.Path, query);
    }

    public override async Task OnPageHandlerExecutionAsync(
        PageHandlerExecutingContext context,
        PageHandlerExecutionDelegate next)
    {
        var queue = await CafeService.GetModerationQueueAsync(HttpContext.RequestAborted);
        PendingDishCount = queue.Dishes.Count;
        PendingPhotoCount = queue.Photos.Count;
        await next();
    }

    protected IActionResult Done(string message)
    {
        Message = message;
        return RedirectToPage();
    }

    /// <summary>Vuelve a la página indicada si es del panel; si no, al resumen.</summary>
    protected IActionResult Back(string? returnUrl, string message)
    {
        Message = message;
        return LocalRedirect(
            !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) && returnUrl.StartsWith("/admin", StringComparison.Ordinal)
                ? returnUrl
                : "/admin");
    }
}
