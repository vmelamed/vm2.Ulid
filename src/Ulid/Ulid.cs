// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Val Melamed

using System.Diagnostics.CodeAnalysis;

namespace vm2;

/// <summary>
/// Represents a Universally Unique Lexicographically Sortable Identifier (ULID).
/// </summary>
/// <remarks>
/// A ULID is a 128-bit identifier that combines a timestamp with a random component, ensuring both uniqueness and lexicographical<br/>
/// order. This struct provides methods for creating, parsing, and manipulating ULIDs, as well as converting them to other formats<br/>
/// such as strings or GUIDs. ULIDs are commonly used in distributed systems where unique, sortable identifiers are required.
/// </remarks>
[JsonConverter(typeof(UlidConverter))]
public readonly partial struct Ulid :
    IEquatable<Ulid>,
    IComparable<Ulid>,
    IParsable<Ulid>,
    IEqualityOperators<Ulid, Ulid, bool>,
    IComparisonOperators<Ulid, Ulid, bool>,
    IIncrementOperators<Ulid>,
    IMinMaxValue<Ulid>
{
    static UlidFactory _defaultFactory = new();

    // The ULID is held as a 128-bit unsigned number split into two 64-bit halves:
    //   _hi = bytes 0..7  of the canonical (big-endian) binary form: 48-bit timestamp << 16 | top 16 bits of the random part
    //   _lo = bytes 8..15 of the canonical (big-endian) binary form: the low 64 bits of the random part
    // The halves are numbers, not memory images, so comparisons and arithmetic are endianness-agnostic. Every conversion to
    // or from bytes MUST go through the BinaryPrimitives *BigEndian methods - never through MemoryMarshal or Unsafe casts.
    // Two ulong-s rather than one UInt128: UInt128 is 16-byte aligned on x64, which would pad structs and arrays embedding Ulid.
    readonly ulong _hi;
    readonly ulong _lo;

    const int timestampShift = 16;  // the 16 low bits of _hi belong to the random part
    const ulong low60BitsMask = (1UL << 60) - 1;
    const ulong low56BitsMask = (1UL << 56) - 1;

    Ulid(ulong hi, ulong lo)
    {
        _hi = hi;
        _lo = lo;
    }

    /// <summary>
    /// Gets the 16 bytes of the ULID in their canonical, big-endian order.
    /// </summary>
    /// <remarks>
    /// <b>Note:</b> the ULID no longer stores its bytes, so every call allocates a new 16-byte array. On hot paths, prefer<br/>
    /// <see cref="TryWrite(Span{byte})"/> with a stack-allocated buffer.
    /// </remarks>
    public ReadOnlySpan<byte> Bytes
    {
        get
        {
            var bytes = new byte[UlidBytesLength];
            WriteBytes(bytes);
            return bytes;
        }
    }

    /// <summary>
    /// Extracts and converts the ULID's Unix timestamp component into a <see cref="DateTimeOffset"/> representation.
    /// </summary>
    /// <remarks>
    /// The returned <see cref="DateTimeOffset"/> represents the date and time encoded in the ULID.
    /// </remarks>
    public DateTimeOffset Timestamp => DateTimeOffset.FromUnixTimeMilliseconds((long)(_hi >> timestampShift));

    /// <summary>
    /// Returns the bytes of the random component from the current ULID instance.
    /// </summary>
    /// <remarks>
    /// The returned byte array represents the random portion of the ULID.<br/>
    /// <b>Note:</b> every call allocates a new 10-byte array.
    /// </remarks>
    public ReadOnlySpan<byte> RandomBytes
    {
        get
        {
            var bytes = new byte[RandomLength];
            WriteUInt16BigEndian(bytes, (ushort)_hi);
            WriteUInt64BigEndian(bytes.AsSpan(sizeof(ushort)), _lo);
            return bytes;
        }
    }

    /// <summary>
    /// Generates a new unique ULID (Universally Unique Lexicographically Sortable Identifier) using the default random provider
    /// and the default clock.
    /// </summary>
    /// <remarks>
    /// This method uses an internal, default <see cref="UlidFactory"/> instance to create a new ULID.<br/>
    /// Consider using your own factories that generate separate sequences of ULIDs, e.g. a factory per DB table or per entity type.<br/>
    /// </remarks>
    /// <returns>A new <see cref="Ulid"/> instance representing a unique identifier.</returns>
    public static Ulid NewUlid()
        => _defaultFactory.NewUlid();

    /// <summary>
    /// Initializes a new instance of the <see cref="Ulid"/> struct with all bits set to zero, representing the smallest possible value of the <see cref="Ulid"/> type.
    /// </summary>
    /// <remarks>
    /// <c>new Ulid()</c>, <c>default(Ulid)</c>, and <see cref="Empty"/> are the same value.
    /// </remarks>
    public Ulid()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Ulid"/> struct using the passed in <paramref name="bytes"/>.<br/>
    /// If <paramref name="isUtf8"/> is <see langword="true"/> the length of the input buffer <paramref name="bytes"/> must be at<br/>
    /// least <see cref="UlidStringLength"/> and treats the bytes as a UTF-8 string representation of ULID instance.<br/>
    /// If <paramref name="isUtf8"/> is <see langword="false"/> the length of the input buffer <paramref name="bytes"/> must be at<br/>
    /// least <see cref="UlidBytesLength"/> and treats the bytes as the raw byte representation of a ULID instance.
    /// </summary>
    /// <param name="bytes">
    /// A read-only buffer of bytes representing the raw bytes or UTF-8 encoded string of a valid ULID.
    /// </param>
    /// <param name="isUtf8">
    /// If <see langword="true"/>, the input bytes are treated as a UTF-8 encoded string representation of a ULID and the length<br/>
    /// of the <paramref name="bytes"/> must be at least <see cref="UlidStringLength"/>.<br/>
    /// Otherwise, the input bytes are treated as the raw byte representation of a ULID and the length of the <paramref name="bytes"/><br/>
    /// must be at least <see cref="UlidBytesLength"/>.
    /// </param>
    public Ulid(ReadOnlySpan<byte> bytes, bool isUtf8)
    {
        if (isUtf8)
        {
            if (bytes.Length < UlidStringLength)
                throw new ArgumentException(
                            $"The byte buffer must contain at least {nameof(Ulid)}.{nameof(UlidStringLength)}({UlidStringLength}) bytes when {nameof(isUtf8)} is true.",
                            nameof(bytes));

            if (!TryParse(bytes[0..UlidStringLength], out this))
                throw new ArgumentException("The byte buffer does not represent a valid ULID string.", nameof(bytes));
            return;
        }

        if (bytes.Length < UlidBytesLength)
            throw new ArgumentException(
                        $"The byte buffer must contain at least {nameof(Ulid)}.{nameof(UlidBytesLength)}({UlidBytesLength}) bytes.",
                        nameof(bytes));

        _hi = ReadUInt64BigEndian(bytes);
        _lo = ReadUInt64BigEndian(bytes[sizeof(ulong)..]);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Ulid"/> struct using the specified string representation.
    /// </summary>
    /// <param name="source">The string representation of the ULID to parse. Must be a valid ULID string.</param>
    public Ulid(string source) => this = Parse(source);

    /// <summary>
    /// Initializes a new instance of the <see cref="Ulid"/> struct from the bytes of the specified <see cref="Guid"/>.
    /// </summary>
    /// <remarks>
    /// The ULID bytes are the bytes returned by <see cref="Guid.ToByteArray()"/>, so that<br/>
    /// <c>new Ulid(guid).ToGuid() == guid</c> and <c>guid.ToByteArray()</c> equals <c>new Ulid(guid).Bytes</c>.
    /// </remarks>
    /// <param name="source">A <see cref="Guid"/> to initialize the ULID from.</param>
    public Ulid(Guid source)
    {
        Span<byte> bytes = stackalloc byte[UlidBytesLength];

        source.TryWriteBytes(bytes);
        _hi = ReadUInt64BigEndian(bytes);
        _lo = ReadUInt64BigEndian(bytes[sizeof(ulong)..]);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Ulid"/> struct using the specified Unix timestamp and random bytes.
    /// </summary>
    /// <remarks>
    /// <b>Hint:</b> use this constructor to generate predictable sequences of ULIDs, e.g. in unit tests.<br/>
    /// </remarks>
    /// <param name="unixTimestamp">The timestamp representing the creation time of the ULID.</param>
    /// <param name="randomBytes">A read-only buffer of 10 bytes representing the unique identifier portion of the ULID.</param>
    public Ulid(long unixTimestamp, ReadOnlySpan<byte> randomBytes)
    {
        if (randomBytes.Length != RandomLength)
            throw new ArgumentException(
                        $"The random bytes argument must contain exactly {nameof(Ulid)}.{nameof(RandomLength)} ({RandomLength}) bytes.",
                        nameof(randomBytes));

        _hi = ((ulong)unixTimestamp << timestampShift) | ReadUInt16BigEndian(randomBytes);
        _lo = ReadUInt64BigEndian(randomBytes[sizeof(ushort)..]);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Ulid"/> struct using the specified .NET <see cref="DateTime"/> and identifier randomBytes.
    /// </summary>
    /// <remarks>
    /// <b>Hint:</b> use this constructor to generate predictable sequences of ULIDs, e.g. in unit tests.<br/>
    /// </remarks>
    /// <param name="dateTime">
    /// The .NET <see cref="DateTime"/> representing the creation time of the ULID. Should be in UTC, otherwise assumes local time.
    /// </param>
    /// <param name="randomBytes">A read-only buffer of 10 randomBytes representing the unique identifier portion of the ULID.</param>
    public Ulid(DateTime dateTime, ReadOnlySpan<byte> randomBytes)
        : this(new DateTimeOffset(dateTime).ToUnixTimeMilliseconds(), randomBytes)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Ulid"/> struct using the specified .NET <see cref="DateTimeOffset"/> and
    /// identifier randomBytes.
    /// </summary>
    /// <remarks>
    /// <b>Hint:</b> use this constructor to generate predictable sequences of ULIDs, e.g. in unit tests.<br/>
    /// </remarks>
    /// <param name="dateTime">
    /// The timestamp representing the creation time of the ULID. Should be in UTC, otherwise assumes local time.
    /// </param>
    /// <param name="randomBytes">A read-only buffer of 10 randomBytes representing the unique identifier portion of the ULID.</param>
    public Ulid(DateTimeOffset dateTime, ReadOnlySpan<byte> randomBytes)
        : this(dateTime.ToUnixTimeMilliseconds(), randomBytes)
    {
    }

    /// <summary>
    /// Builds a ULID from a 48-bit timestamp and the 80-bit random part split into its top 16 and low 64 bits.
    /// The caller guarantees the ranges; used by <see cref="UlidFactory"/> on its hot path.
    /// </summary>
    internal static Ulid FromParts(long unixTimestamp, ushort randomHi, ulong randomLo)
        => new(((ulong)unixTimestamp << timestampShift) | randomHi, randomLo);

    /// <summary>
    /// Converts the current ULID value to its equivalent Guid representation.
    /// </summary>
    /// <remarks>
    /// The <see cref="Guid"/> is built from the ULID bytes, i.e. <c>ToGuid().ToByteArray()</c> equals <see cref="Bytes"/>.
    /// </remarks>
    /// <returns>The <see cref="Guid"/> with the same bytes as this ULID.</returns>
    public Guid ToGuid()
        // Guid stores its first three fields (a, b, c) little-endian in ToByteArray() and the remaining 8 bytes in order,
        // so the byte-swapped halves of _hi land the ULID bytes in canonical order. Pure arithmetic: endianness-agnostic.
        => new(ReverseEndianness((uint)(_hi >> 32)),
               ReverseEndianness((ushort)(_hi >> 16)),
               ReverseEndianness((ushort)_hi),
               (byte)(_lo >> 56), (byte)(_lo >> 48), (byte)(_lo >> 40), (byte)(_lo >> 32),
               (byte)(_lo >> 24), (byte)(_lo >> 16), (byte)(_lo >> 8),  (byte)_lo);

    /// <summary>
    /// Converts the current ULID instance to its equivalent Base32 (the default) string representation.
    /// </summary>
    /// <remarks>
    /// The string representation follows the standard ULID format, which is a 26 uppercase alphanumeric string.<br/>
    /// </remarks>
    /// <returns>A 26-character string that represents the current ULID instance.</returns>
    public override string ToString()
        => string.Create(UlidStringLength, this, static (span, ulid) => Encode(ulid._hi, ulid._lo, span, CrockfordDigits));

    #region Implicit conversions
    /// <summary>
    /// Defines an implicit conversion from a <see cref="Ulid"/> to its string representation.
    /// </summary>
    public static implicit operator string(Ulid ulid) => ulid.ToString();

    /// <summary>
    /// Defines an explicit conversion from a string to a <see cref="Ulid"/> instance.
    /// </summary>
    /// <param name="s"></param>
    public static explicit operator Ulid(string s) => new(s);

    /// <summary>
    /// Defines an implicit conversion from a <see cref="Ulid"/> to a <see cref="Guid"/> and vice versa.
    /// </summary>
    /// <param name="ulid"></param>
    public static implicit operator Guid(Ulid ulid) => ulid.ToGuid();

    /// <summary>
    /// Defines an implicit conversion from a <see cref="Guid"/> to a <see cref="Ulid"/> instance.
    /// </summary>
    /// <param name="guid"></param>
    public static implicit operator Ulid(Guid guid) => new(guid);
    #endregion

    /// <summary>
    /// Determines whether the current instance is equal to the specified <see cref="Ulid"/> instance.
    /// </summary>
    /// <param name="other">The <see cref="Ulid"/> instance to compare with the current instance.</param>
    /// <returns>
    /// <see langword="true"/> if the current instance is equal to the specified <see cref="Ulid"/> instance; otherwise, <see langword="false"/>.
    /// </returns>
    public bool Equals(Ulid other) => _hi == other._hi && _lo == other._lo;

    /// <summary>
    /// Determines whether the specified object is equal to the current instance.
    /// </summary>
    /// <param name="obj">The object to compare with the current instance. Can be <see langword="null"/>.</param>
    /// <returns>
    /// <see langword="true"/> if the specified object is a <see cref="Ulid"/> and is equal to the current instance; otherwise, <see langword="false"/>.
    /// </returns>
    public override bool Equals([NotNullWhen(true)] object? obj) => obj is Ulid u && Equals(u);

    /// <summary>
    /// Returns the hash code for the current instance.
    /// </summary>
    /// <remarks>
    /// <see cref="HashCode"/> is randomized per process, which keeps hash-based collections resistant to hash flooding with<br/>
    /// ULIDs crafted by an untrusted party.
    /// </remarks>
    /// <returns>A 32-bit signed integer that serves as the hash code for the current instance.</returns>
    public override int GetHashCode() => HashCode.Combine((uint)_hi, (uint)(_hi >> 32), (uint)_lo, (uint)(_lo >> 32));

    #region IComparable
    /// <summary>
    /// Compares the current instance with another <see cref="Ulid"/> object and returns an integer that indicates their
    /// relative order.
    /// </summary>
    /// <remarks>The comparison is equivalent to comparing the canonical big-endian byte sequences of the ULIDs.</remarks>
    /// <param name="other">The <see cref="Ulid"/> instance to compare to the current instance.</param>
    /// <returns>
    /// A signed integer that indicates the relative order of the objects being compared: <list type="bullet">
    /// <item><description>Less than zero if the current instance precedes <paramref name="other"/> in the sort
    /// order.</description></item> <item><description>Zero if the current instance occurs in the same position as
    /// <paramref name="other"/> in the sort order.</description></item> <item><description>Greater than zero if the
    /// current instance follows <paramref name="other"/> in the sort order.</description></item> </list>
    /// </returns>
    public int CompareTo(Ulid other)
        => _hi != other._hi
                ? _hi < other._hi ? -1 : 1
                : _lo.CompareTo(other._lo);
    #endregion

    /// <summary>
    /// Attempts to write the string representation of the ULID to the specified character buffer using the Crockford Base32 encoding.
    /// </summary>
    /// <remarks>
    /// The method encodes the ULID as a 26-character string using the Crockford Base32 alphabet. The caller must ensure that the<br/>
    /// <paramref name="destination"/> buffer has sufficient capacity to hold the resulting string. If the buffer is smaller than<br/>
    /// <see cref="UlidStringLength"/>, the method returns <see langword="false"/>  and does not modify the destination buffer.
    /// </remarks>
    /// <param name="destination">
    /// The buffer of characters where the ULID string representation will be written. The buffer must have a length of<br/>
    /// <see cref="UlidStringLength"/> or more.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the ULID string representation was successfully written to the <paramref name="destination"/> buffer;<br/>
    /// otherwise, <see langword="false"/> if the buffer is too small.
    /// </returns>
    public bool TryWrite(Span<char> destination)
    {
        if (destination.Length < UlidStringLength)
            return false;

        Encode(_hi, _lo, destination, CrockfordDigits);
        return true;
    }

    /// <summary>
    /// Attempts to write the ULID bytes to the specified destination buffer. The <paramref name="destination"/> must have at least<br/>
    /// <see cref="UlidBytesLength"/> bytes and if it does, the method writes the raw 16-byte representation of the ULID in it.
    /// </summary>
    /// <param name="destination">
    /// The buffer to which the ULID representation will be written.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the ULID was successfully written to the destination buffer; otherwise, <see langword="false"/>.
    /// </returns>
    public bool TryWrite(Span<byte> destination)
    {
        if (destination.Length < UlidBytesLength)
            return false;

        WriteBytes(destination);
        return true;
    }

    /// <summary>
    /// Attempts to write the string representation of the ULID to the specified UTF-8 byte buffer using the Crockford Base32 encoding.
    /// </summary>
    public bool TryWriteUtf8(Span<byte> destination)
    {
        if (destination.Length < UlidStringLength)
            return false;

        Encode(_hi, _lo, destination, CrockfordDigitsUtf8);
        return true;
    }

    /// <summary>
    /// Attempts to parse the specified UTF-16 string representation of a ULID.
    /// </summary>
    /// <remarks>
    /// This method does not throw an exception if the parsing fails. Instead, it returns <see langword="false"/> and sets
    /// <paramref name="result"/> to an empty Ulid (all bytes set to zero).
    /// </remarks>
    /// <param name="sourceSpan">
    /// The string to parse to a ULID.
    /// </param>
    /// <param name="result">
    /// If the parsing succeeded, on return will contain the parsed <see cref="Ulid"/>;
    /// otherwise, an empty Ulid (all bytes set to zero).
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the string was successfully parsed as a ULID; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool TryParse(
        ReadOnlySpan<char> sourceSpan,
        out Ulid result)
        => TryDecode(sourceSpan, out result);

    /// <summary>
    /// Attempts to parse the specified UTF-8 string representation of a ULID.
    /// </summary>
    /// <remarks>
    /// This method does not throw an exception if the parsing fails. Instead, it returns <see langword="false"/> and sets <paramref name="result"/><br/>
    /// to an empty Ulid (all bytes set to zero).
    /// </remarks>
    /// <param name="sourceSpan">
    /// UTF-8 character (byte) buffer to parse as a ULID.
    /// </param>
    /// <param name="result">
    /// When this method returns, contains the parsed <see cref="Ulid"/> if the parsing succeeded; otherwise, an empty Ulid (all bytes set to zero).
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the string was successfully parsed as a ULID; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool TryParse(
        ReadOnlySpan<byte> sourceSpan,
        out Ulid result)
        => TryDecode(sourceSpan, out result);

    /// <summary>
    /// Parses the specified string representation of a ULID and returns the corresponding <see cref="Ulid"/> instance.
    /// </summary>
    /// <param name="s">The string representation of the ULID to parse.</param>
    /// <returns>The <see cref="Ulid"/> instance that corresponds to the parsed string.</returns>
    /// <exception cref="ArgumentException">Thrown if the input string <paramref name="s"/> cannot be parsed as a valid ULID.</exception>
    public static Ulid Parse(ReadOnlySpan<byte> s)
        => TryParse(s, out var ulid) ? ulid : throw new ArgumentException("The input source does not represent a valid ULID.", nameof(s));

    #region IParsable
    /// <summary>
    /// Parses the specified string representation of a ULID and returns the corresponding <see cref="Ulid"/> instance.
    /// </summary>
    /// <param name="s">The string representation of the ULID to parse.</param>
    /// <param name="formatProvider">An optional format _, which is ignored in this implementation.</param>
    /// <returns>The <see cref="Ulid"/> instance that corresponds to the parsed string.</returns>
    /// <exception cref="ArgumentException">Thrown if the input string <paramref name="s"/> cannot be parsed as a valid ULID.</exception>
    public static Ulid Parse(string s, IFormatProvider? formatProvider = null)
        => TryParse(s, formatProvider, out var ulid) ? ulid : throw new ArgumentException("The input source does not represent a valid ULID.", nameof(s));

    /// <summary>
    /// Attempts to parse the specified string representation of a ULID (Universally Unique Lexicographically Sortable Identifier)<br/>
    /// and returns a value indicating whether the operation succeeded.
    /// </summary>
    /// <remarks>
    /// The method validates the input string against the ULID format and attempts to parse it into a <see cref="Ulid"/> instance.<br/>
    /// If the input string does not conform to the ULID format, the method returns <see langword="false"/> and the <paramref name="result"/><br/>
    /// parameter is set to <see cref="Ulid.Empty"/>.
    /// </remarks>
    /// <param name="source">
    /// The string representation of the ULID to parse. The string must conform to the ULID format.
    /// </param>
    /// <param name="_">
    /// The format provider is not used here.
    /// </param>
    /// <param name="result">
    /// When this method returns, contains the parsed <see cref="Ulid"/> value if the parsing succeeded;
    /// otherwise, an empty Ulid (all bytes set to zero).
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the string was successfully parsed into a valid <see cref="Ulid"/>; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool TryParse(
        [NotNullWhen(true)] string? source,
        IFormatProvider? _,
        out Ulid result)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            result = Empty;
            return false;
        }

        return TryParse(source.AsSpan(), out result);
    }
    #endregion

    #region IMinMaxValues<Ulid>
    /// <inheritdoc/>
    public static Ulid MaxValue => AllBitsSet;

    /// <inheritdoc/>
    public static Ulid MinValue => Empty;
    #endregion

    #region IEqualityOperators<Ulid, Ulid, bool>
    /// <inheritdoc/>
    public static bool operator ==(Ulid left, Ulid right) => left.Equals(right);

    /// <inheritdoc/>
    public static bool operator !=(Ulid left, Ulid right) => !(left == right);
    #endregion

    #region IComparisonOperators<Ulid, Ulid, bool>
    /// <inheritdoc/>
    public static bool operator <(Ulid left, Ulid right) => left.CompareTo(right) < 0;

    /// <inheritdoc/>
    public static bool operator <=(Ulid left, Ulid right) => left.CompareTo(right) <= 0;

    /// <inheritdoc/>
    public static bool operator >(Ulid left, Ulid right) => left.CompareTo(right) > 0;

    /// <inheritdoc/>
    public static bool operator >=(Ulid left, Ulid right) => left.CompareTo(right) >= 0;
    #endregion

    #region IIncrementOperators<Ulid>
    /// <inheritdoc/>
    public static Ulid operator ++(Ulid value)
    {
        var lo = value._lo + 1;
        var hi = lo == 0 ? value._hi + 1 : value._hi;

        if (hi == 0 && lo == 0)
            throw new OverflowException("Ulid overflowed.");

        return new(hi, lo);
    }
    #endregion

    void WriteBytes(Span<byte> destination)
    {
        WriteUInt64BigEndian(destination, _hi);
        WriteUInt64BigEndian(destination[sizeof(ulong)..], _lo);
    }

    // The 26 Crockford digits carry 130 bits; the value has 128, so the first digit holds only the top 3 bits.
    // The value is encoded in three groups that each fit a ulong, so no UInt128 arithmetic is needed:
    //   digits  0..1  - bits 127..120 ( 8 bits: 3 + 5)
    //   digits  2..13 - bits 119..60  (60 bits: 12 x 5)
    //   digits 14..25 - bits  59..0   (60 bits: 12 x 5)
    static void Encode<T>(ulong hi, ulong lo, Span<T> destination, ReadOnlySpan<T> digits)
    {
        var top8  = hi >> 56;
        var mid60 = ((hi & low56BitsMask) << 4) | (lo >> 60);
        var low60 = lo & low60BitsMask;

        destination = destination[..UlidStringLength];   // one bounds check here lets the JIT elide the ones below

        destination[0] = digits[(int)(top8 >> BitsPerUlidDigit)];
        destination[1] = digits[(int)(top8 & UlidDigitMask)];

        for (var i = 13; i >= 2; i--, mid60 >>= BitsPerUlidDigit)
            destination[i] = digits[(int)(mid60 & UlidDigitMask)];

        for (var i = 25; i >= 14; i--, low60 >>= BitsPerUlidDigit)
            destination[i] = digits[(int)(low60 & UlidDigitMask)];
    }

    // The inverse of Encode. Invalid digits map to 255 in CrockfordDigitValues; rather than branching on every character, the
    // digit values (and, for UTF-16, the bits above the low byte) are OR-ed into one accumulator that is tested once at the end.
    static bool TryDecode<T>(ReadOnlySpan<T> source, out Ulid result)
        where T : unmanaged, IBinaryInteger<T>
    {
        result = Empty;

        if (source.Length < UlidStringLength)
            return false;

        source = source[..UlidStringLength];

        uint invalid = 0;

        var top10 = DecodeDigits(source[..2], ref invalid);
        var mid60 = DecodeDigits(source[2..14], ref invalid);
        var low60 = DecodeDigits(source[14..], ref invalid);

        // a valid digit is 0..31: any higher bit means an invalid character
        if ((invalid & ~(uint)UlidDigitMask) != 0)
            return false;

        // NOTE: like the previous UInt128 implementation, the 2 bits of top10 above bit 7 (first digit > '7') are silently
        // dropped here, so e.g. "8000..." aliases "0000...". See the code review notes - the spec says such input is invalid.
        result = new((top10 << 56) | (mid60 >> 4), (mid60 << 60) | low60);
        return true;
    }

    static ulong DecodeDigits<T>(ReadOnlySpan<T> chars, ref uint invalid)
        where T : unmanaged, IBinaryInteger<T>
    {
        ulong value = 0;

        foreach (var ch in chars)
        {
            var c = uint.CreateTruncating(ch);
            uint digit = CrockfordDigitValues[(byte)c];

            // c & 0xFF00 keeps a UTF-16 char's high byte at bits 8..15, above the digit mask; shifting it down (c >> 8) would
            // drop e.g. U+0130 into the masked-out low bits and let it alias the digit '0' found in its low byte.
            invalid |= digit | (c & 0xFF00);
            value = (value << BitsPerUlidDigit) | digit;
        }
        return value;
    }
}
