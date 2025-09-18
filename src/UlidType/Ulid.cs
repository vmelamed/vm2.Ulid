namespace vm2;

/// <summary>
/// Represents a Universally Unique Lexicographically Sortable Identifier (ULID).
/// </summary>
/// <remarks>
/// A ULID is a 128-bit identifier that combines a timestamp with a random component, ensuring both uniqueness and lexicographical<br/>
/// lexicographical order. This struct provides methods for creating, parsing, and manipulating ULIDs, as well as converting them<br/>
/// to other formats such as strings or GUIDs. ULIDs are commonly used in distributed systems where unique, sortable identifiers<br/>
/// are required.
/// </remarks>
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
    /// <summary>
    /// The value where all bits of the Ulid value are set to zero. Also, represents the smallest possible value of the
    /// <see cref="Ulid"/> type.
    /// </summary>
    public static readonly Ulid Empty = new();

    /// <summary>
    /// The value where all bits of the Ulid value are set to one. Also, represents the greatest possible value of the
    /// <see cref="Ulid"/> type.
    /// </summary>
    public static readonly Ulid AllBitsSet = new([ 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF ]);

    static UlidFactory? _defaultFactory;

    readonly ReadOnlyMemory<byte> _ulidBytes;

    /// <summary>
    /// Gets a read-only span of bytes representing the underlying data of the ULID.
    /// </summary>
    public readonly ReadOnlySpan<byte> Bytes => _ulidBytes.Span;

    /// <summary>
    /// Extracts and converts the ULID's timestamp component into a <see cref="DateTimeOffset"/> representation.
    /// </summary>
    /// <remarks>
    /// The returned <see cref="DateTimeOffset"/> represents the timestamp encoded in the ULID, which is based on the ulidAsNumber of <br/>
    /// milliseconds since the Unix epoch (January 1, 1970, 00:00:00 UTC).
    /// </remarks>
    public readonly DateTimeOffset Timestamp
    {
        get
        {
            Span<byte> timestampBytes = stackalloc byte[sizeof(long)];

            _ulidBytes
                .Span[TimestampBegin..TimestampEnd]
                .CopyTo(timestampBytes.Slice((BitConverter.IsLittleEndian ? 2 : 0), TimestampLength));

            return DateTimeOffset.FromUnixTimeMilliseconds(ReadInt64BigEndian(timestampBytes));
        }
    }

    /// <summary>
    /// Returns the bytes of the random component from the current ULID instance.
    /// </summary>
    /// <remarks>
    /// The returned byte array represents the random portion of the ULID, which is independent of the timestamp component.
    /// </remarks>
    public readonly ReadOnlySpan<byte> RandomBytes => Bytes[RandomBegin..RandomEnd];

    /// <summary>
    /// Generates a new unique ULID (Universally Unique Lexicographically Sortable Identifier).
    /// </summary>
    /// <remarks>
    /// This method uses an internal, default <see cref="UlidFactory"/> instance to create a new ULID.<br/>
    /// Consider using your own factories that generate separate sequences of ULIDs, e.g. a factory per DB table or per entity type.<br/>
    /// </remarks>
    /// <returns>A new <see cref="Ulid"/> instance representing a unique identifier.</returns>
    public static Ulid NewUlid(IUlidRandomProvider? ulidRandomProvider = null)
        => (_defaultFactory ??= new UlidFactory(ulidRandomProvider)).NewUlid();

    /// <summary>
    /// Initializes a new instance of the <see cref="Ulid"/> struct using the passed in <paramref name="bytes"/>.<br/>
    /// If the length of the input span is <see cref="UlidBytesLength"/> it accepts them as the underlying ULID bytes (used by the <see cref="UlidFactory"/>).<br/>
    /// If the specified span of bytes is <see cref="UlidStringLength"/> long, it attempts to parse them as a span of UTF-8 characters.<br/>
    /// Any other length throws an <see cref="ArgumentException"/>.
    /// </summary>
    /// <remarks>
    /// This constructor creates a ULID from the provided bytes. The caller must ensure that the span contains a valid ULID representation<br/>
    /// either as raw bytes or as a UTF-8 encoded string.<br/>
    /// The data is copied into an internal buffer, so changes to the source byte span after construction do not affect
    /// the ULID instance.
    /// </remarks>
    /// <param name="bytes">
    /// A read-only span of bytes representing the raw bytes or UTF-8 encoded string of a valid ULID. The span must be exactly
    /// <see cref="UlidBytesLength"/> or <see cref="UlidStringLength"/> bytes long.
    /// </param>
    public Ulid(in ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length == UlidBytesLength)
        {
            _ulidBytes = new ReadOnlyMemory<byte>(bytes.ToArray());
            return;
        }

        if (bytes.Length == UlidStringLength)
        {
            if (TryParse(bytes, out this))
                return;
            throw new ArgumentException("The byte span does not represent a valid ULID string.", nameof(bytes));
        }

        throw new ArgumentException(
                    $"The byte span must contain exactly {nameof(Ulid)}.{nameof(UlidBytesLength)} or {nameof(Ulid)}.{nameof(UlidStringLength)} ({UlidBytesLength} or {UlidStringLength}) bytes.",
                    nameof(bytes));
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
    /// <param name="source">The string representation of the ULID to parse. Must be a valid ULID string.</param>
    public Ulid(Guid source)
        : this(source.ToByteArray())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Ulid"/> struct using the specified timestamp and random bytes.
    /// </summary>
    /// <remarks>
    /// <b>Hint:</b> use this constructor to generate predictable sequences of ULIDs, e.g. in unit tests.<br/>
    /// </remarks>
    /// <param name="dateTime">The timestamp representing the creation time of the ULID.</param>
    /// <param name="randomBytes">A read-only span of 10 bytes representing the unique identifier portion of the ULID.</param>
    public Ulid(DateTimeOffset dateTime, ReadOnlySpan<byte> randomBytes)
    {
        if (randomBytes.Length != RandomLength)
            throw new ArgumentException(
                        $"The random bytes argument must contain exactly {nameof(Ulid)}.{nameof(RandomLength)} ({RandomLength}) bytes.",
                        nameof(randomBytes));

        var bytes = new byte[UlidBytesLength];

        var ulidSpan = bytes.AsSpan();
        var timestampNow = dateTime.ToUnixTimeMilliseconds();

        CopyTimeStampToUlid(timestampNow, ulidSpan);

        randomBytes.CopyTo(ulidSpan[RandomBegin..RandomEnd]);
        _ulidBytes = new ReadOnlyMemory<byte>(bytes);
    }

    internal static void CopyTimeStampToUlid(long timestamp, Span<byte> ulidSpan)
    {
        if (!BitConverter.IsLittleEndian)
            timestamp <<= 2*8; // 0x0000010203040506 << 16 => 0x0102030405060000

        BitConverter.GetBytes(timestamp)[TimestampBegin..TimestampEnd].CopyTo(ulidSpan);

        if (BitConverter.IsLittleEndian)
            ulidSpan[TimestampBegin..TimestampEnd].Reverse();   // 0x0605040302010000.Reverse(0..6) => 0x0102030405060000
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Ulid"/> struct using the specified timestamp and identifier randomBytes.
    /// </summary>
    /// <remarks>
    /// <b>Hint:</b> use this constructor to generate predictable sequences of ULIDs, e.g. in unit tests.<br/>
    /// </remarks>
    /// <param name="dateTime">
    /// The timestamp representing the creation time of the ULID. Should be in UTC, otherwise assumes local time.
    /// </param>
    /// <param name="randomBytes">A read-only span of 10 randomBytes representing the unique identifier portion of the ULID.</param>
    public Ulid(DateTime dateTime, ReadOnlySpan<byte> randomBytes)
        : this(new DateTimeOffset(dateTime), randomBytes)
    {
    }

    /// <summary>
    /// Converts the current ULID value to its equivalent Guid representation.
    /// </summary>
    /// <returns></returns>
    public readonly Guid ToGuid() => new(Bytes);

    /// <summary>
    /// Converts the current ULID instance to its equivalent Base32 (the default) string representation.
    /// </summary>
    /// <remarks>
    /// The string representation follows the standard ULID format, which is a 26-character case-sensitive alphanumeric string.<br/>
    /// This method is optimized for performance and avoids unnecessary allocations.
    /// </remarks>
    /// <returns>A 26-character string that represents the current ULID instance.</returns>
    public override string ToString()
    {
        Span<char> span = stackalloc char[UlidStringLength];

        var r = TryWrite(span);

        Debug.Assert(r is true);
        return new string(span);
    }

    /// <summary>
    /// Attempts to write the string representation of the ULID to the specified character span using the Crockford Base32 encoding.
    /// </summary>
    /// <remarks>
    /// The method encodes the ULID as a 26-character string using the Crockford Base32 alphabetSpan. The caller must ensure that the<br/>
    /// <paramref name="destination"/> span has sufficient capacity to hold the resulting string. If the span is smaller than<br/>
    /// <see cref="UlidStringLength"/>, the method returns <see langword="false"/>  and does not modify the destination span.
    /// </remarks>
    /// <param name="destination">
    /// The span of characters where the ULID string representation will be written. The span must have a length of<br/>
    /// <see cref="UlidStringLength"/> or more.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the ULID string representation was successfully written to the  <paramref name="destination"/> span; otherwise,<br/>
    /// <see langword="false"/> if the span is too small.
    /// </returns>
    public readonly bool TryWrite(Span<char> destination)
    {
        if (destination.Length < UlidStringLength)
            return false;

        var ulidAsNumber = ReadUInt128BigEndian(Bytes);

        for (var i = 0; i < UlidStringLength; i++)
        {
            // get the least significant 5 bits from the number and convert it to character
            destination[UlidStringLength-i-1] = CrockfordDigits[(byte)ulidAsNumber & UlidCharMask];
            ulidAsNumber >>>= BitsPerUlidDigit;
        }

        Debug.Assert(ulidAsNumber == 0);
        return true;
    }

    /// <summary>
    /// Attempts to write the ULID bytes to the specified destination buffer.
    /// </summary>
    /// <remarks>
    /// This method does not throw an exception if the destination buffer is too small. Instead, it returns <see langword="false"/>
    /// to indicate failure.
    /// </remarks>
    /// <param name="destination">
    /// The buffer to which the ULID randomBytes will be written. Must have a length of at least <see cref="UlidBytesLength"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the ULID randomBytes were successfully written to the destination buffer; otherwise, <see langword="false"/>.
    /// </returns>
    public readonly bool TryWrite(Span<byte> destination)
    {
        if (destination.Length < UlidBytesLength)
            return false;

        Bytes.CopyTo(destination);
        return true;
    }

    /// <summary>
    /// Attempts to parse the specified UTF-16 string representation of a ULID.
    /// </summary>
    /// <remarks>
    /// This method does not throw an exception if the parsing fails. Instead, it returns <see langword="false"/> and sets <paramref name="result"/><br/>
    /// to <see langword="null"/>.
    /// </remarks>
    /// <param name="sourceSpan">
    /// The string as a read-only char span to parse as a ULID. This value can be <see langword="null"/>.
    /// </param>
    /// <param name="result">
    /// When this method returns, contains the parsed <see cref="Ulid"/> if the parsing succeeded; otherwise, <see langword="null"/>.
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
            if (i > 0)
                ulidAsNumber *= UlidRadix;

            var crockfordIndex = sourceSpan[i] - '0';

            if (crockfordIndex < 0
                || crockfordIndex >= CrockfordDigitValues.Length)
                return false;

            var digitValue = CrockfordDigitValues[crockfordIndex];

            if (digitValue >= 32)
                return false;

            ulidAsNumber += digitValue;
        }

        // get the randomBytes of the UInt128 value
        var ulidSpan = BitConverter.GetBytes(ulidAsNumber).AsSpan();

        // make sure they are big-endian
        if (BitConverter.IsLittleEndian)
            ulidSpan.Reverse();

        // this is our ULID
        result = new Ulid(ulidSpan);
        return true;
    }

    /// <summary>
    /// Attempts to parse the specified UTF-8 string representation of a ULID.
    /// </summary>
    /// <remarks>
    /// This method does not throw an exception if the parsing fails. Instead, it returns <see langword="false"/> and sets <paramref name="result"/><br/>
    /// to <see langword="null"/>.
    /// </remarks>
    /// <param name="sourceSpan">
    /// The string as a read-only char span to parse as a ULID. This value can be <see langword="null"/>.
    /// </param>
    /// <param name="result">
    /// When this method returns, contains the parsed <see cref="Ulid"/> if the parsing succeeded; otherwise, <see langword="null"/>.
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
            if (i > 0)
                ulidAsNumber *= UlidRadix;

            var crockfordIndex = sourceSpan[i] - (byte)'0';

            if (crockfordIndex < 0 || crockfordIndex >= CrockfordDigitValues.Length)
                return false;

            var digitValue = CrockfordDigitValues[crockfordIndex];

            if (digitValue >= 32)
                return false;

            ulidAsNumber += digitValue;
        }

        // get the randomBytes of the UInt128 value
        var ulidSpan = BitConverter.GetBytes(ulidAsNumber).AsSpan();

        // make sure the number is written big-endian
        if (BitConverter.IsLittleEndian)
            ulidSpan.Reverse();

        // this is our ULID
        result = new Ulid(ulidSpan);
        return true;
    }

    /// <summary>
    /// Determines whether the current instance is equal to the specified <see cref="Ulid"/> instance.
    /// </summary>
    /// <param name="other">The <see cref="Ulid"/> instance to compare with the current instance.</param>
    /// <returns>
    /// <see langword="true"/> if the current instance is equal to the specified <see cref="Ulid"/> instance; otherwise, <see langword="false"/>.
    /// </returns>
    public bool Equals(Ulid other) => Bytes.SequenceCompareTo(other.Bytes) == 0;

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
        foreach (var b in Bytes)
            hc.Add(b);
        return hc.ToHashCode();
    }

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

    #region IMinMaxValues<Ulid>
    /// <inheritdoc/>
    public static Ulid MaxValue => AllBitsSet;

    /// <inheritdoc/>
    public static Ulid MinValue => Empty;
    #endregion

    #region IParseable
    /// <summary>
    /// Parses the specified string representation of a ULID and returns the corresponding <see cref="Ulid"/> instance.
    /// </summary>
    /// <param name="s">The string representation of the ULID to parse.</param>
    /// <param name="formatProvider">An optional format _, which is ignored in this implementation.</param>
    /// <returns>The <see cref="Ulid"/> instance that corresponds to the parsed string.</returns>
    /// <exception cref="ArgumentException">Thrown if the input string <paramref name="s"/> cannot be parsed as a valid ULID.</exception>
    public static Ulid Parse(string s, IFormatProvider? formatProvider = null)
        => TryParse(s, formatProvider, out var u) ? u : throw new ArgumentException("The input source does not represent a valid ULID.", nameof(s));

    /// <summary>
    /// Attempts to parse the specified string representation of a ULID (Universally Unique Lexicographically Sortable Identifier)<br/>
    /// and returns a value indicating whether the operation succeeded.
    /// </summary>
    /// <remarks>
    /// The method validates the input string against the ULID format and attempts to parse it into a <see cref="Ulid"/> instance.<br/>
    /// If the input string does not conform to the ULID format, the method returns <see langword="false"/> and the <paramref name="result"/><br/>
    /// parameter is set to <see langword="null"/>.
    /// </remarks>
    /// <param name="source">
    /// The string representation of the ULID to parse. The string must conform to the ULID format.
    /// </param>
    /// <param name="_">
    /// The format provider is not used here.
    /// </param>
    /// <param name="result">
    /// When this method returns, contains the parsed <see cref="Ulid"/> value if the parsing succeeded; otherwise, <see langword="null"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the string was successfully parsed into a valid <see cref="Ulid"/>; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool TryParse(
        [NotNullWhen(true)] string? source,
        IFormatProvider? _,
        out Ulid result)
    {
        result = new();
        if (string.IsNullOrWhiteSpace(source))
            return false;
        return TryParse(source, out result);
    }
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
                return new Ulid(span);

        throw new OverflowException("Ulid overflowed.");
    }
    #endregion
}
