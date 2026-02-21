// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Val Melamed

namespace vm2.UlidType.Tests;

[ExcludeFromCodeCoverage]
public partial class UlidTests
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
        str.Should().MatchRegex(UlidStringRegex);
        var utfStr = Encoding.UTF8.GetBytes(str);

        var parsed = new Ulid(utfStr, true);

        parsed.Should().Be(ulid);
    }

    [Fact]
    public void TryWrite_WithUtf8Destination()
    {
        var ulid = new Ulid("01K5MVTR82AF7EA4DNPKQAVE3T"u8, true);
        Span<byte> buffer = stackalloc byte[UlidStringLength+1];

        ulid.TryWriteUtf8(buffer[..(UlidStringLength-1)]).Should().BeFalse();

        ulid.TryWriteUtf8(buffer[..UlidStringLength]).Should().BeTrue();
        new Ulid(buffer, true).Should().Be(ulid);

        ulid.TryWriteUtf8(buffer).Should().BeTrue();
        new Ulid(buffer, true).Should().Be(ulid);
    }


    [Fact]
    public void TryWrite_WithBinDestination()
    {
        var ulid = new Ulid("01K5MVTR82AF7EA4DNPKQAVE3T"u8, true);
        Span<byte> buffer = stackalloc byte[UlidBytesLength+1];

        ulid.TryWrite(buffer[..(UlidBytesLength-1)]).Should().BeFalse();

        ulid.TryWrite(buffer[..UlidBytesLength]).Should().BeTrue();
        new Ulid(buffer, false).Should().Be(ulid);

        ulid.TryWrite(buffer).Should().BeTrue();
        new Ulid(buffer, false).Should().Be(ulid);
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
    public void Parse_Roundtrip()
    {
        var ulid = new UlidFactory().NewUlid();
        var str = ulid.ToString();

        Ulid result = Parse(str);
        result.Should().Be(ulid);

        result = Parse(Encoding.UTF8.GetBytes(str).AsSpan());
        result.Should().Be(ulid);
    }

    [Fact]
    public void TryParse_Roundtrip()
    {
        var ulid = new UlidFactory().NewUlid();
        var str = ulid.ToString();

        TryParse(Encoding.UTF8.GetBytes(str), out var result).Should().BeTrue();
        result.Should().Be(ulid);

        TryParse(str, out result).Should().BeTrue();
        result.Should().Be(ulid);

        TryParse(str+"a", out result).Should().BeTrue();
        TryParse(str.AsSpan(0, 1), out result).Should().BeFalse();

        TryParse(Encoding.UTF8.GetBytes(str+"a"), out result).Should().BeTrue();
        TryParse(Encoding.UTF8.GetBytes(str[..1]), out result).Should().BeFalse();
    }


    [Fact]
    public void CaseInsensitive_Roundtrip()
    {
        var ulid = new UlidFactory().NewUlid();
        var str = ulid.ToString();

        Ulid result = Parse(str);

        // case-insensitive parsing
        TryParse(str.ToLowerInvariant(), out result).Should().BeTrue();
        result.Should().Be(ulid);

        // case-insensitive parsing
        TryParse(Encoding.UTF8.GetBytes(str.ToLowerInvariant()), out result).Should().BeTrue();
        result.Should().Be(ulid);
    }


    public static TheoryData<char> WrongUlidChars = [ (char)0, (char)31, (char)127, (char)1000, '!', 'U', 'l', ']', '}' ];

    [Theory]
    [MemberData(nameof(WrongUlidChars))]
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

    [Theory]
    [MemberData(nameof(TimeAndRandoms))]
    public void UlidFactory_Increments_Correctly_Random(
        (TimeAndRandom last, TimeAndRandom next) data)
    {
        var ulidFactory = new UlidFactory(new Test_IUlidRandomProvider(data.last.Random),
                                          new Test_IClock(data.last.UnixTime));
        var throws = data.next.Throws;

        var _ = ulidFactory.NewUlid();

        if (throws)
        {
            Action act = () => ulidFactory.NewUlid();
            act.Should().Throw<OverflowException>();
        }
        else
        {
            var ulid2 = ulidFactory.NewUlid();
            ulid2.Timestamp.ToUnixTimeMilliseconds().Should().Be(data.next.UnixTime);
            ulid2.RandomBytes.ToArray().Should().Equal(data.next.Random);
        }
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

    public static TheoryData<int> WrongUtf8Lengths = [ 0, 1, UlidStringLength-1 ];

    [Theory]
    [MemberData(nameof(WrongUtf8Lengths))]
    public void NewUlid_FromUtf8Bytes_WithWrongLength_Throws(int length)
    {
        Action act = () => new Ulid(new byte[length], true);
        act.Should().Throw<ArgumentException>();
    }

    public static TheoryData<int> WrongBytesLengths = [ 0, 1, UlidBytesLength-1 ];

    [Theory]
    [MemberData(nameof(WrongBytesLengths))]
    public void NewUlid_FromBytes_WithWrongLength_Throws(int length)
    {
        Action act = () => new Ulid(new byte[length], false);
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [MemberData(nameof(WrongUlidChars))]
    public void NewUlid_FromBytes_WithWrongByteUtf8_Throws(char wrongChar)
    {
        var bytes = new byte[UlidStringLength];
        Action act = () => new Ulid(bytes, true);

        for (var i = 0; i < bytes.Length; i++)
            bytes[i] = 0xFF;
        act.Should().Throw<ArgumentException>();

        for (var i = 0; i < bytes.Length; i++)
            bytes[i] = (byte)wrongChar;
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [MemberData(nameof(WrongUlidChars))]
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

    public static TheoryData<string> KnownGoodValues =
    [
        "01ARZ3NDEKTSV4RRFFQ69G5FAV",
        "01BX5ZZKBKACTAV9WEVGEMMVRZ",
        "01BX5ZZKBKACTAV9WEVGEMMVS0",
        "01K5ETWXTDG0ZK9PP9WMC6V4HY",
        "01K5ETWXTWEQPSJ8AB1PSFVGCR",
        "01K5ETWXTWEQPSJ8AB1PSFVGCS",
        "01K5ETWXV32QSSJWA7WKQZ7D0K",
        "01K5ETWXVDNC94EGFBNK30GBSV",
        "01K5ETWXVDNC94EGFBNK30GBSW",
        "01K5ETWXVDNC94EGFBNK30GBSX",
    ];

    [Theory]
    [MemberData(nameof(KnownGoodValues))]
    public void RoundTrip_Known_GoodValues(string s)
    {
        var ulid = Parse(s);

        ulid.ToString().Should().Be(s);

        var bytes = ulid.Bytes.ToArray();

        bytes.Length.Should().Be(UlidBytesLength);

        var fromBytes = new Ulid(bytes, false);

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

    [Fact]
    public void ImplicitConversion_ToAndFrom_Guid_Works_As_Expected()
    {
        var factory = new UlidFactory();
        var ulid = factory.NewUlid();

        Guid guid = ulid;
        guid.ToByteArray().Should().Equal(ulid.Bytes.ToArray());

        Ulid ulid2 = guid;
        ulid2.Should().Be(ulid);
    }

    [Fact]
    public void ImplicitConversion_ToAndFrom_String_Works_As_Expected()
    {
        var factory = new UlidFactory();
        var ulid = factory.NewUlid();

        string str = ulid;
        str.Should().Be(ulid.ToString());

        Ulid ulid2 = str;
        ulid2.Should().Be(ulid);
    }
}
