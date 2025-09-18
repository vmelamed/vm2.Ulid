namespace vm2.UlidRandomProviders;

/// <summary>
/// A random provider that uses a pseudo-random number generator.
/// </summary>
public class PseudoRandom : IUlidRandomProvider
{
    /// <summary>
    /// Fills the provided byte span with pseudo-random data.
    /// </summary>
    public void Fill(Span<byte> bytes) => Random.Shared.NextBytes(bytes);
}
