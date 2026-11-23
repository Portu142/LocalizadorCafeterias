using LocalizadorCafes.Application.Admin;
using LocalizadorCafes.Application.Cafes;
using LocalizadorCafes.Domain.Entities;
using LocalizadorCafes.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LocalizadorCafes.Api.Pages.Admin;

public class IndexModel : AdminPageModel
{
    public const int Weeks = 8;

    private readonly UserManager<AppUser> _userManager;

    public IndexModel(CafeService cafeService, AdminService adminService, UserManager<AppUser> userManager)
        : base(cafeService, adminService)
    {
        _userManager = userManager;
    }

    public int UserCount { get; private set; }
    public int CafeCount { get; private set; }
    public IReadOnlyList<WeeklyCount> NewUsers { get; private set; } = Array.Empty<WeeklyCount>();
    public IReadOnlyList<WeeklyCount> NewReviews { get; private set; } = Array.Empty<WeeklyCount>();
    public IReadOnlyList<WeeklyCount> NewDishes { get; private set; } = Array.Empty<WeeklyCount>();
    public IReadOnlyList<WeeklyCount> NewPhotos { get; private set; } = Array.Empty<WeeklyCount>();
    public IReadOnlyList<AdminAuditEntry> RecentActions { get; private set; } = Array.Empty<AdminAuditEntry>();
    public IReadOnlyList<CommunityEvent> RecentCommunity { get; private set; } = Array.Empty<CommunityEvent>();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        // En UTC: PostgreSQL (Npgsql) rechaza fechas con otra zona horaria en
        // columnas «timestamp with time zone» y la página daba error 500.
        var since = DateTimeOffset.UtcNow.AddDays(-7 * Weeks - 7);

        UserCount = await _userManager.Users.CountAsync(cancellationToken);
        CafeCount = (await AdminService.SearchCafesAsync(null, 1, cancellationToken)).Total;

        var userDates = await _userManager.Users
            .Where(user => user.CreatedAt >= since)
            .Select(user => user.CreatedAt)
            .ToListAsync(cancellationToken);
        var activity = await AdminService.GetActivityDatesAsync(since, cancellationToken);

        NewUsers = AdminService.GroupByWeek(userDates, today, Weeks);
        NewReviews = AdminService.GroupByWeek(activity.Reviews, today, Weeks);
        NewDishes = AdminService.GroupByWeek(activity.Dishes, today, Weeks);
        NewPhotos = AdminService.GroupByWeek(activity.Photos, today, Weeks);

        RecentActions = (await AdminService.GetAuditLogAsync(null, 1, cancellationToken)).Items.Take(8).ToList();
        RecentCommunity = await AdminService.GetCommunityFeedAsync(8, cancellationToken);
    }
}
