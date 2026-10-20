using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace SupportDesk.Api.Infrastructure;

/// <summary>
/// Stores <see cref="DateTime"/> values as UTC and marks values read back as <see cref="DateTimeKind.Utc"/>.
/// SQLite keeps no time zone information, so without this converter values come back as
/// <see cref="DateTimeKind.Unspecified"/>. <see cref="DateTimeOffset"/> is avoided on purpose:
/// the SQLite provider cannot ORDER BY it.
/// </summary>
public sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    /// <summary>Creates the converter.</summary>
    public UtcDateTimeConverter()
        : base(value => ToUtc(value), value => DateTime.SpecifyKind(value, DateTimeKind.Utc))
    {
    }

    /// <summary>
    /// Normalises a value before it is written: local times are converted to UTC,
    /// and unspecified times are taken to be UTC already.
    /// </summary>
    public static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
    };
}
