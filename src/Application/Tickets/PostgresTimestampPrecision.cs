namespace EventHub.Application.Tickets;

internal static class PostgresTimestampPrecision
{
    private const long TicksPerMicrosecond = 10;

    private static readonly long PostgresEpochTicks =
        new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero).Ticks;

    public static DateTimeOffset NormalizeUtc(DateTimeOffset timestamp)
    {
        var utc = timestamp.ToUniversalTime();
        var microsecondsSincePostgresEpoch =
            (utc.Ticks - PostgresEpochTicks) / TicksPerMicrosecond;
        var normalizedTicks = PostgresEpochTicks
            + microsecondsSincePostgresEpoch * TicksPerMicrosecond;

        return new DateTimeOffset(normalizedTicks, TimeSpan.Zero);
    }
}
