namespace vm2;

using vm2.Providers;

/// <summary>
/// Provides functionality to generate unique lexicographically sortable identifiers (ULIDs).
/// </summary>
/// <remarks>
/// This factory ensures that ULIDs generated within the same millisecond are unique by incrementing the random portion of the ULID.<br/>
/// In the extremely unlikely event that where incrementing the random portion overflows, an <see cref="OverflowException"/> is <br/>
/// thrown. The generated ULIDs are compliant with the ULID specification and are suitable for use in distributed systems where <br/>
/// uniqueness and ordering are required.<br/>
/// <b>Hint:</b> you may have more than one factory in your program representing separate sequences of ULID-s. E.g. a factory<br/>
/// per DB table.
/// </remarks>
public sealed class UlidFactory(IUlidRandomProvider? randomProvider = null, IClock? clock = null)
{
    IUlidRandomProvider _rng = randomProvider ?? new CryptoRandom();
    IClock _clock = clock ?? new SystemClock();
    byte[] _lastRandom = new byte[RandomLength];
    long _lastTimestamp;
    Lock _lock = new();

    /// <summary>
    /// Generates a new Universally Unique Lexicographically Sortable Identifier (ULID).
    /// </summary>
    /// <remarks>
    /// This method creates a ULID based on the current UTC timestamp and a random component. If called multiple times within
    /// the same millisecond, the random component is incremented to ensure uniqueness. The method is thread-safe.
    /// </remarks>
    /// <returns>A new <see cref="Ulid"/> instance representing the generated ULID.</returns>
    public Ulid NewUlid()
    {
        var randomSpan  = _lastRandom.AsSpan();
        var timestampNow = _clock.UnixTimeMilliseconds();

        lock (_lock)
        {
            if (_lastTimestamp == timestampNow)
            {
                // increment the random part with carry over for monotonicity
                for (var i = randomSpan.Length-1; i >= 0; i--)
                    if (unchecked(++randomSpan[i]) != 0)
                        return new Ulid(_lastTimestamp, randomSpan);

                // this is extremely unlikely case - we ran out of consecutive values for this millisecond.
                // This is 1 in 2^80 chance of happening.
                throw new OverflowException("Random component overflowed; cannot generate more ULIDs for this millisecond.");
            }

            _rng.Fill(randomSpan);
            _lastTimestamp = timestampNow;

            // create a new ULID from the bytes
            return new Ulid(_lastTimestamp, randomSpan);
        }
    }
}
