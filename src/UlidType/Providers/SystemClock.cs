namespace vm2.Providers;

/// <summary>
/// Provides the current system time in Coordinated Universal Time (UTC) as a Unix timestamp in milliseconds.
/// </summary>
/// <remarks>
/// This class implements the <see cref="IClock"/> interface and retrieves the current UTC time using the system clock.</remarks>
public class SystemClock : IClock
{
    /// <summary>
    /// Gets the current UTC time as the number of milliseconds that have elapsed since the Unix epoch.
    /// </summary>
    /// <returns>The number of milliseconds that have elapsed since January 1, 1970, 00:00:00 UTC.</returns>
    public long UnixTimeMilliseconds() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}
