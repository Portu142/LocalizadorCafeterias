using System.Collections.Concurrent;

namespace LocalizadorCafes.Application.Presence;

/// <summary>
/// Visitantes activos en tiempo real, solo en memoria. La web envía un
/// «latido» cada 30 s mientras la pestaña está visible; un visitante cuenta
/// como activo durante <see cref="ActiveWindow"/> tras su último latido.
///
/// Privacidad: los anónimos se identifican con un número aleatorio por sesión
/// del navegador; no se guardan IPs ni nada persistente, y todo caduca solo.
/// </summary>
public sealed class PresenceTracker
{
    public static readonly TimeSpan ActiveWindow = TimeSpan.FromSeconds(75);
    public const int HistoryMinutes = 30;

    /// <summary>Tope de visitantes simultáneos en memoria (protección ante abusos).</summary>
    public const int MaxVisitors = 20_000;

    private readonly TimeProvider _time;
    private readonly ConcurrentDictionary<Guid, Visit> _visits = new();
    private readonly object _historyLock = new();
    private readonly SortedDictionary<long, HashSet<Guid>> _minutes = new();

    public PresenceTracker(TimeProvider time)
    {
        _time = time;
    }

    private sealed record Visit(Guid VisitorId, Guid? UserId, string? CafeId, string? CafeName, DateTimeOffset LastSeen);

    /// <summary>Registra un latido. Devuelve false si se descarta (tope alcanzado).</summary>
    public bool Beat(Guid visitorId, Guid? userId, string? cafeId, string? cafeName)
    {
        var now = _time.GetUtcNow();
        Prune(now);

        if (!_visits.ContainsKey(visitorId) && _visits.Count >= MaxVisitors)
        {
            return false;
        }

        _visits[visitorId] = new Visit(
            visitorId,
            userId,
            Truncate(cafeId, 60),
            Truncate(cafeName, 120),
            now);

        var minute = now.ToUnixTimeSeconds() / 60;
        lock (_historyLock)
        {
            if (!_minutes.TryGetValue(minute, out var set))
            {
                set = new HashSet<Guid>();
                _minutes[minute] = set;
            }
            set.Add(visitorId);
        }

        return true;
    }

    public PresenceSnapshot Snapshot()
    {
        var now = _time.GetUtcNow();
        Prune(now);

        var active = _visits.Values
            .Where(visit => now - visit.LastSeen <= ActiveWindow)
            .ToList();

        var users = active
            .Where(visit => visit.UserId is not null)
            .GroupBy(visit => visit.UserId!.Value)
            .Select(group => group.OrderByDescending(visit => visit.LastSeen).First())
            .Select(visit => new ActiveUser(visit.UserId!.Value, visit.CafeName, visit.LastSeen))
            .ToList();

        var cafes = active
            .Where(visit => !string.IsNullOrEmpty(visit.CafeId))
            .GroupBy(visit => visit.CafeId!)
            .Select(group => new ViewedCafe(
                group.Key,
                group.Select(visit => visit.CafeName).FirstOrDefault(name => !string.IsNullOrEmpty(name)) ?? group.Key,
                group.Count()))
            .OrderByDescending(cafe => cafe.Viewers)
            .ThenBy(cafe => cafe.Name)
            .Take(10)
            .ToList();

        var currentMinute = now.ToUnixTimeSeconds() / 60;
        List<MinuteCount> history;
        lock (_historyLock)
        {
            history = Enumerable.Range(0, HistoryMinutes)
                .Select(offset => currentMinute - (HistoryMinutes - 1) + offset)
                .Select(minute => new MinuteCount(
                    DateTimeOffset.FromUnixTimeSeconds(minute * 60),
                    _minutes.TryGetValue(minute, out var set) ? set.Count : 0))
                .ToList();
        }

        var registered = active.Where(visit => visit.UserId is not null).Select(visit => visit.UserId).Distinct().Count();
        var anonymous = active.Count(visit => visit.UserId is null);

        return new PresenceSnapshot(registered + anonymous, registered, anonymous, users, cafes, history, now);
    }

    /// <summary>Olvida visitantes inactivos y minutos fuera del histórico.</summary>
    private void Prune(DateTimeOffset now)
    {
        foreach (var (id, visit) in _visits)
        {
            if (now - visit.LastSeen > ActiveWindow)
            {
                _visits.TryRemove(id, out _);
            }
        }

        var oldest = now.ToUnixTimeSeconds() / 60 - HistoryMinutes;
        lock (_historyLock)
        {
            foreach (var minute in _minutes.Keys.Where(minute => minute < oldest).ToList())
            {
                _minutes.Remove(minute);
            }
        }
    }

    private static string? Truncate(string? value, int max)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed.Length <= max ? trimmed : trimmed[..max];
    }
}

public record ActiveUser(Guid UserId, string? ViewingCafe, DateTimeOffset LastSeen);

public record ViewedCafe(string CafeId, string Name, int Viewers);

public record MinuteCount(DateTimeOffset Minute, int Visitors);

/// <summary>Foto del momento: activos ahora y visitantes únicos por minuto.</summary>
public record PresenceSnapshot(
    int Total,
    int Registered,
    int Anonymous,
    IReadOnlyList<ActiveUser> Users,
    IReadOnlyList<ViewedCafe> Cafes,
    IReadOnlyList<MinuteCount> History,
    DateTimeOffset At);
