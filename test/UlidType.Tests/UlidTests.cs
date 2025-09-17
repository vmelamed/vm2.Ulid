namespace vm2.UlidType.Tests;

using vm2.UlidRandomProviders;

[ExcludeFromCodeCoverage]
public class UlidTests
{
    [Fact]
    public void Ulid_NewUlid_Uses_InternalFactory()
    {
        var ulid1 = Ulid.NewUlid();
        var ulid2 = Ulid.NewUlid();

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
        var ulid = new UlidFactory().NewUlid();

        var small = new byte[UlidBytesLength - 1];
        ulid.TryWrite(small.AsSpan()).Should().BeFalse();

        var destination = new byte[UlidBytesLength];
        ulid.TryWrite(destination.AsSpan()).Should().BeTrue();
        destination.Should().Equal(ulid.Bytes.ToArray());
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

        var parsed = Ulid.Parse(str);
        parsed.Should().Be(ulid);

        Ulid.TryParse(str, out var result).Should().BeTrue();
        result.Should().Be(ulid);

        // case-insensitive parsing
        Ulid.TryParse(str.ToLowerInvariant(), out var lower).Should().BeTrue();
        lower.Should().Be(ulid);
    }

    [Fact]
    public void Parse_Invalid_Throws_TryParse_ReturnsFalse()
    {
        var invalid = new string('!', UlidStringLength);

        Action act = () => Ulid.Parse(invalid);
        act.Should().Throw<ArgumentException>();

        Ulid.TryParse(invalid, out var r).Should().BeFalse();
    }

    [Fact]
    public void TryParse_Null_ReturnsFalse()
    {
        Ulid.TryParse("", out _).Should().BeFalse();
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
    public void NewUlid_Generates_Unique_And_Monotonic_Ulids_When_Incrementing_Within_Same_Millisecond()
    {
        var factory = new UlidFactory();

        // Force the factory into the "same millisecond" increment path by setting private state:
        var lastTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // Prepare bytes similar to what the factory would write for the timestamp portion
        var lastUlidBytes = new byte[UlidBytesLength];
        BitConverter.TryWriteBytes(lastUlidBytes.AsSpan(), lastTimestamp);
        if (BitConverter.IsLittleEndian)
            lastUlidBytes.AsSpan(TimestampBegin, TimestampLength).Reverse();

        // Inject _lastTimestamp and _lastUlid via reflection
        var type = typeof(UlidFactory);
        var tsField = type.GetField("_lastTimestamp", BindingFlags.Instance | BindingFlags.NonPublic);
        var ulidField = type.GetField("_lastUlid", BindingFlags.Instance | BindingFlags.NonPublic);

        tsField.Should().NotBeNull();
        ulidField.Should().NotBeNull();

        tsField!.SetValue(factory, lastTimestamp);
        ulidField!.SetValue(factory, lastUlidBytes);

        const int count = 10;
        var ulids = Enumerable.Range(0, count).Select(_ => factory.NewUlid()).ToList();

        // All generated ULIDs must be unique
        ulids.Should().OnlyHaveUniqueItems();

        // And they should be strictly increasing (monotonic)
        for (var i = 1; i < ulids.Count; i++)
            (ulids[i] > ulids[i - 1]).Should().BeTrue();
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
        var a2 = Ulid.Parse(a.ToString());
        a2.Equals((object)a).Should().BeTrue();
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

        var ulid2 = Ulid.Parse(ulid.ToString());
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

    [Fact]
    public void NewUlid_FromBytes_WithWrongByteUtf8_Throws()
    {
        var bytes = new byte[UlidStringLength];

        for (var i = 0; i < bytes.Length; i++)
            bytes[i] = 0xFF;
        Action act = () => new Ulid(bytes);
        act.Should().Throw<ArgumentException>();

        for (var i = 0; i < bytes.Length; i++)
            bytes[i] = (byte)'U';

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void NewUlid_FromString_WithWrongChar_Throws()
    {
        Action act = () => new Ulid(new string('!', UlidStringLength));
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
        var random = ulid.RandomBytes.ToArray();
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
}