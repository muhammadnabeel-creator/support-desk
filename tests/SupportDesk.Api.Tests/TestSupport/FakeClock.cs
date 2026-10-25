using SupportDesk.Api.Infrastructure;

namespace SupportDesk.Api.Tests.TestSupport;

/// <summary>A clock tests control: it starts at a fixed UTC time and only moves when told to.</summary>
public sealed class FakeClock : IClock
{
    public FakeClock(DateTime? start = null)
    {
        UtcNow = start ?? new DateTime(2026, 10, 20, 9, 0, 0, DateTimeKind.Utc);
    }

    public DateTime UtcNow { get; private set; }

    public void Advance(TimeSpan by) => UtcNow = UtcNow.Add(by);
}
