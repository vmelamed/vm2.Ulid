namespace vm2;

/// <summary>
/// Represents a clock that provides the current time in milliseconds since the Unix epoch (January 1, 1970, UTC).
/// </summary>
public interface IClock
{
    /// <summary>
    /// Gets the current UTC time as the number of milliseconds that have elapsed since the Unix epoch (January 1, 1970,
    /// 00:00:00 UTC).
    /// </summary>
    long UnixTimeMilliseconds();
}
