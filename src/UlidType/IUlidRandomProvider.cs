namespace vm2;

/// <summary>
/// Interface for filling a byte span with random data. The provider must be thread-safe.
/// </summary>
public interface IUlidRandomProvider
{
    /// <summary>
    /// Fills the provided byte span with random data.
    /// </summary>
    /// <param name="bytes">The span of bytes to fill.</param>
    void Fill(Span<byte> bytes);
}
