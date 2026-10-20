namespace SupportDesk.Api.Infrastructure;

/// <summary>
/// Supplies the current time in UTC, so services that stamp timestamps can be tested with a fixed clock.
/// </summary>
public interface IClock
{
    /// <summary>The current time, with <see cref="DateTimeKind.Utc"/>.</summary>
    DateTime UtcNow { get; }
}
