namespace vm2.UlidRandomProviders;

/// <summary>
/// A random provider that uses cryptographic random number generation.
/// </summary>
public sealed class CryptoRandom : IUlidRandomProvider
{
    /// <summary>
    /// Fills the provided byte span with cryptographically secure random data.
    /// </summary>
    public void Fill(Span<byte> bytes) => RandomNumberGenerator.Fill(bytes);
}
