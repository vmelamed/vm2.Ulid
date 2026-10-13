// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Val Melamed

namespace vm2;

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
public sealed class UlidFactory(IUlidRandomProvider? randomProvider = null, TimeProvider? timeProvider = null)
{
    readonly IUlidRandomProvider _rng = randomProvider ?? new CryptoRandom();
    readonly TimeProvider _clock = timeProvider ?? TimeProvider.System;
    readonly Lock _lock = new();
    long _lastTimestamp;
    ushort _lastRandomHi;   // the top 16 bits of the last 80-bit random part
    ulong _lastRandomLo;    // the low 64 bits of the last 80-bit random part

    /// <summary>
    /// Generates a new Universally Unique Lexicographically Sortable Identifier (ULID).
    /// </summary>
    /// <remarks>
    /// This method creates a ULID based on the current UTC timestamp and a random component. If called multiple times within
    /// the same millisecond, the random component is incremented to ensure uniqueness. If the clock moves backwards (e.g. an<br/>
    /// NTP step), the factory keeps the last timestamp and increments the random component, so the generated ULIDs remain<br/>
    /// strictly increasing. The method is thread-safe.
    /// </remarks>
    /// <returns>A new <see cref="Ulid"/> instance representing the generated ULID.</returns>
    public Ulid NewUlid()
    {
        Span<byte> random = stackalloc byte[RandomLength];

        lock (_lock)
        {
            // The clock MUST be read inside the lock: a thread that read an earlier time but acquired the lock later would
            // otherwise emit a ULID smaller than the one emitted just before it.
            var timestampNow = _clock.GetUtcNow().ToUnixTimeMilliseconds();

            if (timestampNow <= _lastTimestamp)
            {
                // We ran out of consecutive values for this millisecond. Check before incrementing, so that the state is
                // not wrapped around to zero and the next call cannot emit a smaller ULID.
                if (_lastRandomLo == ulong.MaxValue && _lastRandomHi == ushort.MaxValue)
                    throw new OverflowException("Random component overflowed; cannot generate more ULIDs for this millisecond.");

                // increment the 80-bit random part with carry over for monotonicity
                if (++_lastRandomLo == 0)
                    _lastRandomHi++;

                return Ulid.FromParts(_lastTimestamp, _lastRandomHi, _lastRandomLo);
            }

            _rng.Fill(random);
            _lastTimestamp = timestampNow;
            _lastRandomHi  = ReadUInt16BigEndian(random);
            _lastRandomLo  = ReadUInt64BigEndian(random[sizeof(ushort)..]);

            return Ulid.FromParts(_lastTimestamp, _lastRandomHi, _lastRandomLo);
        }
    }
}
