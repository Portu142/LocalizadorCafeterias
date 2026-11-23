using FluentAssertions;
using LocalizadorCafes.Application.Presence;

namespace LocalizadorCafes.UnitTests.Presence;

public class PresenceTrackerTests
{
    /// <summary>Reloj manual para controlar el paso del tiempo en las pruebas.</summary>
    private sealed class ManualClock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2026, 11, 23, 10, 0, 30, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private readonly ManualClock _clock = new();
    private PresenceTracker CreateTracker() => new(_clock);

    [Fact]
    public void Snapshot_ShouldCountRegisteredAndAnonymousVisitors()
    {
        var tracker = CreateTracker();
        var ana = Guid.NewGuid();

        tracker.Beat(Guid.NewGuid(), null, null, null);
        tracker.Beat(Guid.NewGuid(), null, "osm-node/1", "Café A");
        tracker.Beat(Guid.NewGuid(), ana, "osm-node/1", "Café A");

        var snapshot = tracker.Snapshot();

        snapshot.Total.Should().Be(3);
        snapshot.Registered.Should().Be(1);
        snapshot.Anonymous.Should().Be(2);
        snapshot.Users.Should().ContainSingle().Which.UserId.Should().Be(ana);
        snapshot.Cafes.Should().ContainSingle().Which.Should().BeEquivalentTo(new { Name = "Café A", Viewers = 2 });
    }

    [Fact]
    public void Snapshot_ShouldCountTheSameAccountOnceAcrossTabs()
    {
        var tracker = CreateTracker();
        var ana = Guid.NewGuid();

        tracker.Beat(Guid.NewGuid(), ana, null, null);
        tracker.Beat(Guid.NewGuid(), ana, null, null);

        tracker.Snapshot().Total.Should().Be(1);
    }

    [Fact]
    public void Visitors_ShouldExpireAfterTheActiveWindow()
    {
        var tracker = CreateTracker();
        tracker.Beat(Guid.NewGuid(), null, null, null);

        _clock.Now += PresenceTracker.ActiveWindow + TimeSpan.FromSeconds(1);

        tracker.Snapshot().Total.Should().Be(0);
    }

    [Fact]
    public void History_ShouldCountUniqueVisitorsPerMinute()
    {
        var tracker = CreateTracker();
        var visitor = Guid.NewGuid();

        tracker.Beat(visitor, null, null, null);
        tracker.Beat(visitor, null, null, null); // mismo minuto: cuenta una vez
        _clock.Now += TimeSpan.FromMinutes(1);
        tracker.Beat(visitor, null, null, null);
        tracker.Beat(Guid.NewGuid(), null, null, null);

        var history = tracker.Snapshot().History;

        history.Should().HaveCount(PresenceTracker.HistoryMinutes);
        history[^1].Visitors.Should().Be(2);
        history[^2].Visitors.Should().Be(1);
        history.Take(PresenceTracker.HistoryMinutes - 2).Should().OnlyContain(minute => minute.Visitors == 0);
    }

    [Fact]
    public void Beat_ShouldTruncateLongCafeNames()
    {
        var tracker = CreateTracker();

        tracker.Beat(Guid.NewGuid(), null, "osm-node/1", new string('x', 500));

        tracker.Snapshot().Cafes.Single().Name.Should().HaveLength(120);
    }
}
