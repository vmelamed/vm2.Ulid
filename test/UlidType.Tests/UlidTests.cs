namespace vm2.UlidType.Tests;

[ExcludeFromCodeCoverage]
public class UlidTests
{
    [Fact]
    public void Ulid_NewUlid_Uses_InternalFactory()
    {
        var ulid1 = NewUlid();
        var ulid2 = NewUlid();

        ulid1.Should().BeLessThan(ulid2);
        ulid2.Timestamp.Should().BeOnOrAfter(ulid1.Timestamp);
    }

    [Fact]
    public void NewUlid_Roundtrip_ToByteArray_ToGuid_ToBase64_ToString()
    {
        var factory = new UlidFactory();
        var ulid = factory.NewUlid();

        var bytes = ulid.Bytes.ToArray();
        bytes.Should().HaveCount(UlidBytesLength);

        var s = ulid.ToString();
        s.Should().NotBeNullOrWhiteSpace();
        s.Length.Should().Be(UlidStringLength);

        var guid = ulid.ToGuid();
        guid.ToByteArray().Should().Equal(bytes);
        new Ulid(guid).Should().Be(ulid);
    }

    [Fact]
    public void NewUlidWithPseudoRandom_Roundtrip_ToByteArray_ToGuid_ToBase64_ToString()
    {
        var factory = new UlidFactory(new PseudoRandom());
        var ulid = factory.NewUlid();

        var bytes = ulid.Bytes.ToArray();
        bytes.Should().HaveCount(UlidBytesLength);

        var s = ulid.ToString();
        s.Should().NotBeNullOrWhiteSpace();
        s.Length.Should().Be(UlidStringLength);

        var guid = ulid.ToGuid();
        guid.ToByteArray().Should().Equal(bytes);
        new Ulid(guid).Should().Be(ulid);
    }

    [Fact]
    public void NewUlid_RoundTrip_ToString_ToUlid()
    {
        var ulid = new UlidFactory().NewUlid();

        var str = ulid.ToString();
        str.Should().MatchRegex(Ulid.UlidStringRegex);
        var utfStr = Encoding.UTF8.GetBytes(str);

        var parsed = new Ulid(utfStr);

        parsed.Should().Be(ulid);
    }

    [Fact]
    public void TryWrite_WithSmallDestination_ReturnsFalse_And_WithCorrectSize_WritesBytes()
    {
        var ulid = new Ulid("01K5MVTR82AF7EA4DNPKQAVE3T"u8);

        Span<byte> buffer = stackalloc byte[UlidStringLength-1];
        ulid.TryWrite(buffer).Should().BeFalse();

        buffer = stackalloc byte[UlidStringLength];
        ulid.TryWrite(buffer).Should().BeTrue();
        new Ulid(buffer).Should().Be(ulid);

        buffer = stackalloc byte[UlidStringLength+1];
        ulid.TryWrite(buffer).Should().BeFalse();

        buffer = stackalloc byte[UlidBytesLength];
        ulid.TryWrite(buffer).Should().BeTrue();
        new Ulid(buffer).Should().Be(ulid);

        buffer = stackalloc byte[UlidBytesLength+1];
        ulid.TryWrite(buffer).Should().BeFalse();
    }

    [Fact]
    public void TryWriteStringify_SpanSizeBehavior_And_Matches_ToString()
    {
        var ulid = new UlidFactory().NewUlid();

        var tooSmall = new char[UlidStringLength - 1];
        ulid.TryWrite(tooSmall.AsSpan()).Should().BeFalse();

        var buffer = new char[UlidStringLength];
        ulid.TryWrite(buffer).Should().BeTrue();

        var written = new string(buffer);
        written.Should().Be(ulid.ToString());
    }

    [Fact]
    public void Parse_And_TryParse_Roundtrip_And_CaseInsensitive()
    {
        var ulid = new UlidFactory().NewUlid();
        var str = ulid.ToString();

        Ulid result = Parse(str);
        result.Should().Be(ulid);

        result = Parse(Encoding.UTF8.GetBytes(str).AsSpan());
        result.Should().Be(ulid);

        TryParse(Encoding.UTF8.GetBytes(str), out result).Should().BeTrue();
        result.Should().Be(ulid);

        TryParse(str, out result).Should().BeTrue();
        result.Should().Be(ulid);

        TryParse(str+"a", out result).Should().BeFalse();
        TryParse(str.AsSpan(0, 1), out result).Should().BeFalse();

        TryParse(Encoding.UTF8.GetBytes(str+"a"), out result).Should().BeFalse();
        TryParse(Encoding.UTF8.GetBytes(str[..1]), out result).Should().BeFalse();

        // case-insensitive parsing
        TryParse(str.ToLowerInvariant(), out result).Should().BeTrue();
        result.Should().Be(ulid);

        // case-insensitive parsing
        TryParse(Encoding.UTF8.GetBytes(str.ToLowerInvariant()), out result).Should().BeTrue();
        result.Should().Be(ulid);
    }

    [Theory]
    [InlineData('!')]
    [InlineData('U')]
    [InlineData('l')]
    [InlineData(']')]
    [InlineData('}')]
    public void Parse_Invalid_Throws_TryParse_ReturnsFalse(char wrong)
    {
        var invalid = new string(wrong, UlidStringLength);

        Action act = () => Parse(invalid);
        act.Should().Throw<ArgumentException>();

        TryParse(invalid, out var r).Should().BeFalse();

        TryParse(Encoding.UTF8.GetBytes(invalid), out r).Should().BeFalse();
    }

    [Fact]
    public void TryParse_Null_ReturnsFalse()
    {
        TryParse("", out _).Should().BeFalse();
    }

    [Fact]
    public void Timestamp_And_Random_Are_Extractable_And_Within_Reasonable_Range()
    {
        var factory = new UlidFactory();
        var ulid = factory.NewUlid();

        var now = DateTimeOffset.UtcNow;

        var ts = ulid.Timestamp;
        ts.Should().BeOnOrBefore(now);
        ts.Should().BeOnOrAfter(now.AddSeconds(-1));

        var bytes = ulid.Bytes.ToArray();
        var random = ulid.RandomBytes.ToArray();
        bytes.Skip(RandomBegin).Take(RandomLength).Should().Equal(random);
    }

    [Fact]
    public void NewUlids_Are_Unique_And_Monotonic_When_Created_Within_Same_Millisecond()
    {
        var factory = new UlidFactory();

        const int count = 10;
        var ulids = Enumerable.Range(0, count).Select(_ => factory.NewUlid()).ToList();

        // All generated ULIDs must be unique
        ulids.Should().OnlyHaveUniqueItems();

        // And they should be strictly increasing (monotonic)
        for (var i = 1; i < ulids.Count; i++)
            ulids[i].Should().BeGreaterThan(ulids[i - 1]);
    }

    [Fact]
    public void Equals_CompareTo_And_Operators_Behave_As_Expected()
    {
        var factory = new UlidFactory();
        var a = factory.NewUlid();
        var b = factory.NewUlid();

        a.Should().NotBe(b);
        (a == b).Should().BeFalse();
        (a != b).Should().BeTrue();

        (a < b).Should().BeTrue();
        (a <= b).Should().BeTrue();
        (b > a).Should().BeTrue();
        (b >= a).Should().BeTrue();

        // equality via same string
        var a2 = Parse(a.ToString());
        a2.Equals((object)a).Should().BeTrue();
        a2.Equals(a, a2).Should().BeTrue();
        a2.CompareTo(a).Should().Be(0);
        (a2 == a).Should().BeTrue();
        (a2 <= a).Should().BeTrue();
        (a2 >= a).Should().BeTrue();
    }

    [Fact]
    public void GetHashCode_Is_Stable_And_Consistent_With_Equality()
    {
        var factory = new UlidFactory();
        var ulid = factory.NewUlid();
        var hash1 = ulid.GetHashCode();
        var hash11 = ulid.GetHashCode();
        hash1.Should().Be(hash11);
        ulid.GetHashCode(ulid).Should().Be(hash1);

        var ulid2 = Parse(ulid.ToString());
        var hash2 = ulid2.GetHashCode();

        hash2.Should().Be(hash1);
    }

    [Fact]
    public void NewUlid_FromBytes_WithWrongLength_Throws()
    {
        Action act = () => new Ulid(new byte[UlidBytesLength - 1]);
        act.Should().Throw<ArgumentException>();
        act = () => new Ulid(new byte[UlidBytesLength + 1]);
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData('!')]
    [InlineData('U')]
    [InlineData('l')]
    [InlineData(']')]
    [InlineData('}')]
    public void NewUlid_FromBytes_WithWrongByteUtf8_Throws(char wrongChar)
    {
        var bytes = new byte[UlidStringLength];

        for (var i = 0; i < bytes.Length; i++)
            bytes[i] = 0xFF;
        Action act = () => new Ulid(bytes);
        act.Should().Throw<ArgumentException>();

        for (var i = 0; i < bytes.Length; i++)
            bytes[i] = (byte)wrongChar;

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData('!')]
    [InlineData('U')]
    [InlineData('l')]
    [InlineData(']')]
    [InlineData('}')]
    public void NewUlid_FromString_WithWrongChar_Throws(char wrongChar)
    {
        Action act = () => new Ulid(new string(wrongChar, UlidStringLength));
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void NewUlid_FromDateTimeOffsetAndRandom_Roundtrips_As_Expected()
    {
        var factory = new UlidFactory();
        var ulid = factory.NewUlid();
        var ts = ulid.Timestamp;
        var random = ulid.RandomBytes.ToArray();

        var ulid2 = new Ulid(ts, random);
        var ulid3 = new Ulid(new DateTime(ts.Ticks, DateTimeKind.Utc), random);

        ulid2.Should().Be(ulid);
        ulid3.Should().Be(ulid);
    }

    [Fact]
    public void NewUlid_FromDateTimeOffsetAndRandomWrongLength_Throws()
    {
        var factory = new UlidFactory();
        var ulid = factory.NewUlid();
        var ts = ulid.Timestamp;
        var random = new byte[RandomLength + 1];
        ulid.RandomBytes.CopyTo(random.AsSpan());

        Action act = () => new Ulid(ts, random.AsSpan(0, RandomLength - 1).ToArray());
        act.Should().Throw<ArgumentException>();

        act = () => new Ulid(ts, random.AsSpan(0, RandomLength + 1).ToArray());
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void NewUlid_FromNullOrEmptyString_Throws()
    {
        Action act = () => new Ulid(null!);
        act.Should().Throw<ArgumentException>();

        act = () => new Ulid("");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void IncrementUlid_Works_As_Expected()
    {
        var factory = new UlidFactory();
        var ulid = factory.NewUlid();
        var incremented = ulid;
        incremented++;

        (incremented < ulid).Should().BeFalse();
        (incremented == ulid).Should().BeFalse();
        (incremented > ulid).Should().BeTrue();

        // Incrementing the max value overflows
        var maxUlid = Ulid.AllBitsSet;
        Action act = () => maxUlid++;
        act.Should().Throw<OverflowException>();
    }

    [Theory]
    [InlineData("01ARZ3NDEKTSV4RRFFQ69G5FAV")]
    [InlineData("01BX5ZZKBKACTAV9WEVGEMMVRZ")]
    [InlineData("01BX5ZZKBKACTAV9WEVGEMMVS0")]
    [InlineData("01K5ETWXTDG0ZK9PP9WMC6V4HY")]
    [InlineData("01K5ETWXTWEQPSJ8AB1PSFVGCR")]
    [InlineData("01K5ETWXTWEQPSJ8AB1PSFVGCS")]
    [InlineData("01K5ETWXV32QSSJWA7WKQZ7D0K")]
    [InlineData("01K5ETWXVDNC94EGFBNK30GBSV")]
    [InlineData("01K5ETWXVDNC94EGFBNK30GBSW")]
    [InlineData("01K5ETWXVDNC94EGFBNK30GBSX")]
    public void RoundTrip_WellKnown_GoodValues(string s)
    {
        var ulid = Parse(s);

        ulid.ToString().Should().Be(s);

        var bytes = ulid.Bytes.ToArray();

        bytes.Length.Should().Be(UlidBytesLength);

        var fromBytes = new Ulid(bytes);

        fromBytes.Should().Be(ulid);

        var guid = ulid.ToGuid();
        var fromGuid = new Ulid(guid);

        fromGuid.Should().Be(ulid);
    }

    [Fact]
    public void UlidMinAndMaxValue_EmptyAndAllBitsSet()
    {
        MinValue.Should().Be(Empty);
        MaxValue.Should().Be(AllBitsSet);
    }
}