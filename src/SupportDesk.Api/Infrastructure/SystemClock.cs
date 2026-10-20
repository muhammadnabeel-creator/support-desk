namespace SupportDesk.Api.Infrastructure;

/// <summary>
/// Production <see cref="IClock"/> backed by the system clock.
/// </summary>
public sealed class SystemClock : IClock
{
    /// <inheritdoc />
    public DateTime UtcNow => DateTime.UtcNow;
}
