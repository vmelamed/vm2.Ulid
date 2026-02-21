// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Val Melamed

namespace vm2;

/// <summary>
/// Represents a Universally Unique Lexicographically Sortable Identifier (ULID).
/// </summary>
/// <remarks>
/// A ULID is a 128-bit identifier that combines a timestamp with a random component, ensuring both uniqueness and lexicographical<br/>
/// order. This struct provides methods for creating, parsing, and manipulating ULIDs, as well as converting them to other formats<br/>
/// such as strings or GUIDs. ULIDs are commonly used in distributed systems where unique, sortable identifiers are required.
/// </remarks>
[Newtonsoft.Json.JsonConverter(typeof(UlidNsConverter))]
[System.Text.Json.Serialization.JsonConverter(typeof(UlidSysConverter))]
public readonly partial struct Ulid :
    IEquatable<Ulid>,
    IComparable<Ulid>,
    IParsable<Ulid>,
    IEqualityComparer<Ulid>,
    IEqualityOperators<Ulid, Ulid, bool>,
    IComparisonOperators<Ulid, Ulid, bool>,
    IIncrementOperators<Ulid>,
    IMinMaxValue<Ulid>
{
    static UlidFactory _defaultFactory = new();

    readonly ReadOnlyMemory<byte> _ulidBytes;

    /// <summary>
    /// Gets a read-only buffer of bytes representing the underlying data of the ULID.
    /// </summary>
    public ReadOnlySpan<byte> Bytes => _ulidBytes.Span;

    /// <summary>
    /// Extracts and converts the ULID's Unix timestamp component into a <see cref="DateTimeOffset"/> representation.
    /// </summary>
    /// <remarks>
    /// The returned <see cref="DateTimeOffset"/> represents the date and time encoded in the ULID.
    /// </remarks>
    public DateTimeOffset Timestamp
    {
        get
        {
            Span<byte> ts = stackalloc byte[sizeof(long)];

            _ulidBytes[TimestampBegin..TimestampEnd].Span.CopyTo(ts[2..8]);
            return DateTimeOffset.FromUnixTimeMilliseconds(ReadInt64BigEndian(ts));
        }
    }

    /// <summary>
    /// Returns the bytes of the random component from the current ULID instance.
    /// </summary>
    /// <remarks>
    /// The returned byte array represents the random portion of the ULID.
    /// </remarks>
    public ReadOnlySpan<byte> RandomBytes => Bytes[RandomBegin..RandomEnd];

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
    public Ulid()
        => _ulidBytes = new ReadOnlyMemory<byte>(new byte[UlidBytesLength]);

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

        _ulidBytes = new ReadOnlyMemory<byte>(bytes[0..UlidBytesLength].ToArray());
        return;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Ulid"/> struct using the specified string representation.
    /// </summary>
    /// <param name="source">The string representation of the ULID to parse. Must be a valid ULID string.</param>
    public Ulid(string source)
        => _ulidBytes = Parse(source)._ulidBytes;

    /// <summary>
    /// Initializes a new instance of the <see cref="Ulid"/> struct from the bytes of the specified <see cref="Guid"/>.
    /// </summary>
    /// <param name="source">A <see cref="Guid"/> to initialize the ULID from.</param>
    public Ulid(Guid source)
        : this(source.ToByteArray(), false)
    {
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

        var ulidBytes = new byte[UlidBytesLength];
        var ulidSpan = ulidBytes.AsSpan();

        Span<byte> ts = stackalloc byte[sizeof(long)];

        WriteInt64BigEndian(ts, unixTimestamp);
        ts[2..8].CopyTo(ulidSpan[TimestampBegin..TimestampEnd]);

        randomBytes.CopyTo(ulidSpan[RandomBegin..RandomEnd]);

        _ulidBytes = new ReadOnlyMemory<byte>(ulidBytes);
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
    /// Converts the current ULID value to its equivalent Guid representation.
    /// </summary>
    /// <returns></returns>
    public Guid ToGuid() => new(Bytes);

    /// <summary>
    /// Converts the current ULID instance to its equivalent Base32 (the default) string representation.
    /// </summary>
    /// <remarks>
    /// The string representation follows the standard ULID format, which is a 26 uppercase alphanumeric string.<br/>
    /// </remarks>
    /// <returns>A 26-character string that represents the current ULID instance.</returns>
    public override string ToString() => string.Create(UlidStringLength, this, static (span, ulid) => ulid.TryWrite(span));

    #region Implicit conversions
    /// <summary>
    /// Defines an implicit conversion from a <see cref="Ulid"/> to its string representation.
    /// </summary>
    public static implicit operator string(Ulid ulid) => ulid.ToString();

    /// <summary>
    /// Defines an implicit conversion from a string to a <see cref="Ulid"/> instance.
    /// </summary>
    /// <param name="s"></param>
    public static implicit operator Ulid(string s) => new(s);

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
    public bool Equals(Ulid other) => Bytes.SequenceEqual(other.Bytes);

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
    /// <remarks>The hash code is computed based on the underlying byte array representing the ULID.</remarks>
    /// <returns>A 32-bit signed integer that serves as the hash code for the current instance.</returns>
    public override int GetHashCode()
    {
        HashCode hc = new();
        hc.AddBytes(Bytes);
        return hc.ToHashCode();
    }

    #region IComparable
    /// <summary>
    /// Compares the current instance with another <see cref="Ulid"/> object and returns an integer that indicates their
    /// relative order.
    /// </summary>
    /// <remarks>The comparison is performed based on the byte sequence of the underlying ULID
    /// values.</remarks>
    /// <param name="other">The <see cref="Ulid"/> instance to compare to the current instance.</param>
    /// <returns>
    /// A signed integer that indicates the relative order of the objects being compared: <list type="bullet">
    /// <item><description>Less than zero if the current instance precedes <paramref name="other"/> in the sort
    /// order.</description></item> <item><description>Zero if the current instance occurs in the same position as
    /// <paramref name="other"/> in the sort order.</description></item> <item><description>Greater than zero if the
    /// current instance follows <paramref name="other"/> in the sort order.</description></item> </list>
    /// </returns>
    public int CompareTo(Ulid other) => Bytes.SequenceCompareTo(other.Bytes);
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

        var ulidAsNumber = ReadUInt128BigEndian(Bytes);

        for (var i = 0; i < UlidStringLength; i++)
        {
            // get the least significant 5 bits from the number and convert it to character
            destination[UlidStringLength-i-1] = CrockfordDigits[(byte)ulidAsNumber & UlidDigitMask];
            ulidAsNumber >>>= BitsPerUlidDigit;
        }

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

        Bytes.CopyTo(destination);
        return true;
    }

    /// <summary>
    /// Attempts to write the string representation of the ULID to the specified UTF-8 byte buffer using the Crockford Base32 encoding.
    /// </summary>
    public bool TryWriteUtf8(Span<byte> destination)
    {
        if (destination.Length < UlidStringLength)
            return false;

        var ulidAsNumber = ReadUInt128BigEndian(Bytes);

        for (var i = 0; i < UlidStringLength; i++)
        {
            // get the least significant 5 bits from the number and convert it to character
            destination[UlidStringLength-i-1] = CrockfordDigitsUtf8[(byte)ulidAsNumber & UlidDigitMask];
            ulidAsNumber >>>= BitsPerUlidDigit;
        }

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
    {
        result = Empty;

        if (sourceSpan.Length < UlidStringLength)
            return false;

        // parse the string into a UInt128 value first
        UInt128 ulidAsNumber = 0;

        for (var i = 0; i < UlidStringLength; i++)
        {
            var crockfordChar = sourceSpan[i];

            if (crockfordChar >= CrockfordDigitValuesLength)
                return false;

            var digitValue = CrockfordDigitValues[crockfordChar];

            if (digitValue >= 32)
                return false;

            ulidAsNumber = (ulidAsNumber << BitsPerUlidDigit) | digitValue;
        }

        // get the randomBytes of the UInt128 value
        Span<byte> ulidSpan = stackalloc byte[UlidBytesLength];

        WriteUInt128BigEndian(ulidSpan, ulidAsNumber);

        // this is our ULID
        result = new Ulid(ulidSpan, false);
        return true;
    }

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
    {
        result = Empty;

        if (sourceSpan.Length < UlidStringLength)
            return false;

        // parse the string into a UInt128 value first
        UInt128 ulidAsNumber = 0;

        for (var i = 0; i < UlidStringLength; i++)
        {
            var digitValue = CrockfordDigitValues[sourceSpan[i]];
            if (digitValue >= 32)
                return false;
            ulidAsNumber = (ulidAsNumber << BitsPerUlidDigit) | digitValue;
        }

        // get the randomBytes of the UInt128 value
        Span<byte> ulidSpan = stackalloc byte[UlidBytesLength];

        WriteUInt128BigEndian(ulidSpan, ulidAsNumber);

        // this is our ULID
        result = new Ulid(ulidSpan, false);
        return true;
    }

    /// <summary>
    /// Parses the specified string representation of a ULID and returns the corresponding <see cref="Ulid"/> instance.
    /// </summary>
    /// <param name="s">The string representation of the ULID to parse.</param>
    /// <returns>The <see cref="Ulid"/> instance that corresponds to the parsed string.</returns>
    /// <exception cref="ArgumentException">Thrown if the input string <paramref name="s"/> cannot be parsed as a valid ULID.</exception>
    public static Ulid Parse(ReadOnlySpan<byte> s)
        => TryParse(s, out var u)
                ? u
                : throw new ArgumentException("The input source does not represent a valid ULID.", nameof(s));

    #region IParsable
    /// <summary>
    /// Parses the specified string representation of a ULID and returns the corresponding <see cref="Ulid"/> instance.
    /// </summary>
    /// <param name="s">The string representation of the ULID to parse.</param>
    /// <param name="formatProvider">An optional format _, which is ignored in this implementation.</param>
    /// <returns>The <see cref="Ulid"/> instance that corresponds to the parsed string.</returns>
    /// <exception cref="ArgumentException">Thrown if the input string <paramref name="s"/> cannot be parsed as a valid ULID.</exception>
    public static Ulid Parse(string s, IFormatProvider? formatProvider = null)
        => TryParse(s, formatProvider, out var u)
                ? u
                : throw new ArgumentException("The input source does not represent a valid ULID.", nameof(s));

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

        return TryParse(source, out result);
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
    public static bool operator !=(Ulid left, Ulid right) => !(left==right);
    #endregion

    #region IComparisonOperators<Ulid, Ulid, bool>
    /// <inheritdoc/>
    public static bool operator <(Ulid left, Ulid right) => left.CompareTo(right)<0;

    /// <inheritdoc/>
    public static bool operator <=(Ulid left, Ulid right) => left.CompareTo(right)<=0;

    /// <inheritdoc/>
    public static bool operator >(Ulid left, Ulid right) => left.CompareTo(right)>0;

    /// <inheritdoc/>
    public static bool operator >=(Ulid left, Ulid right) => left.CompareTo(right)>=0;
    #endregion

    #region IEqualityComparer<Ulid>
    /// <inheritdoc/>
    public bool Equals(Ulid x, Ulid y) => x.Equals(y);

    /// <inheritdoc/>
    public int GetHashCode(Ulid obj) => obj.GetHashCode();
    #endregion

    #region IIncrementOperators<Ulid>
    /// <inheritdoc/>
    public static Ulid operator ++(Ulid value)
    {
        var newUlidBytes = value.Bytes.ToArray();
        var span = newUlidBytes.AsSpan();

        var i = span.Length-1;
        for (; i >= 0; i--)
            if (unchecked(++span[i]) != 0)
                return new Ulid(span, false);

        throw new OverflowException("Ulid overflowed.");
    }
    #endregion
}
