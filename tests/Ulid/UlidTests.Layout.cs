// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Val Melamed

namespace vm2.Tests.Ulid;

using vm2;

// These tests pin the canonical big-endian layout with vectors computed independently of this library (Python
// int.from_bytes(..., "big") and uuid.UUID(bytes_le=...)). They pass on a little-endian machine trivially for a byte-array
// implementation; their value is catching a numeric (two-ulong) implementation that leaks host byte order - run them on a
// big-endian host (e.g. s390x) to prove it.
public partial class UlidTests
{
    static readonly byte[] SequentialBytes = [.. Enumerable.Range(0, UlidBytesLength).Select(i => (byte)i)];

    [Fact]
    public void Layout_FromBytes_ShouldMatchIndependentVectors()
    {
        var ulid = new Ulid(SequentialBytes, false);

        ulid.ToString().Should().Be("00041061050R3GG28A1C60T3GF");
        ulid.Bytes.ToArray().Should().Equal(SequentialBytes);
        ulid.RandomBytes.ToArray().Should().Equal(SequentialBytes[RandomBegin..]);
        ulid.Timestamp.ToUnixTimeMilliseconds().Should().Be(0x000102030405L);
        ulid.ToGuid().Should().Be(Guid.Parse("03020100-0504-0706-0809-0a0b0c0d0e0f"));
    }

    [Fact]
    public void Layout_TryWrite_ShouldWriteCanonicalBytes()
    {
        var ulid = Parse("00041061050R3GG28A1C60T3GF");
        Span<byte> buffer = stackalloc byte[UlidBytesLength];

        ulid.TryWrite(buffer).Should().BeTrue();

        buffer.ToArray().Should().Equal(SequentialBytes);
    }

    [Fact]
    public void Default_ShouldBeEmpty()
    {
        Ulid ulid = default;

        ulid.Should().Be(Empty);
        ulid.CompareTo(Empty).Should().Be(0);
        ulid.GetHashCode().Should().Be(Empty.GetHashCode());
        ulid.ToString().Should().Be(new string('0', UlidStringLength));
        ulid.ToGuid().Should().Be(Guid.Empty);
        new Ulid().Should().Be(ulid);
    }

    [Fact]
    public void AllBitsSet_ShouldEncodeAsMaximumString()
        => AllBitsSet.ToString().Should().Be("7ZZZZZZZZZZZZZZZZZZZZZZZZZ");

    [Fact]
    public void Increment_WhenLow64BitsAreAllSet_ShouldCarryIntoHigh64Bits()
    {
        var ulid = Parse("0000000000000FZZZZZZZZZZZZ");     // 2^64 - 1: bytes 8..15 all 0xFF

        ulid++;

        ulid.ToString().Should().Be("0000000000000G000000000000");  // 2^64
    }

    [Fact]
    public void Factory_WhenLow64RandomBitsOverflow_ShouldCarryIntoHigh16RandomBits()
    {
        byte[] random = [0x00, 0x01, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF];
        var factory = new UlidFactory(new Test_IUlidRandomProvider(random), new Test_TimeProvider(1758851704339L));

        var first = factory.NewUlid();
        var second = factory.NewUlid();

        second.RandomBytes.ToArray().Should().Equal(0x00, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00);
        second.Timestamp.Should().Be(first.Timestamp);
        second.Should().BeGreaterThan(first);
    }

    [Fact]
    public void Factory_AfterOverflow_ShouldKeepThrowingInsteadOfWrappingAround()
    {
        byte[] random = [0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF];
        var factory = new UlidFactory(new Test_IUlidRandomProvider(random), new Test_TimeProvider(1758851704339L));
        factory.NewUlid();

        Action act = () => factory.NewUlid();

        act.Should().Throw<OverflowException>();
        act.Should().Throw<OverflowException>();
    }

    [Fact]
    public void CompareTo_ShouldAgreeWithByteWiseOrder()
    {
        var rng = new Random(42);
        var a = new byte[UlidBytesLength];
        var b = new byte[UlidBytesLength];

        for (var i = 0; i < 10_000; i++)
        {
            rng.NextBytes(a);
            rng.NextBytes(b);
            // force ties in the high half every other iteration to exercise the low-half comparison
            if (i % 2 == 0)
                a.AsSpan(0, 8).CopyTo(b);

            var expected = Math.Sign(a.AsSpan().SequenceCompareTo(b));

            Math.Sign(new Ulid(a, false).CompareTo(new Ulid(b, false))).Should().Be(expected);
        }
    }

    // Folding each ulong to 32 bits before the seeded mix ((int)v ^ (int)(v >> 32)) maps every _lo = (n << 32) | n to 0,
    // so these ULIDs all collided regardless of HashCode's per-process seed.
    [Fact]
    public void GetHashCode_WhenHalvesOfEachUlongAreEqual_ShouldNotCollide()
    {
        Span<byte> bytes = stackalloc byte[UlidBytesLength];
        var hashes = new HashSet<int>();

        for (uint n = 1; n <= 1000; n++)
        {
            bytes.Clear();
            bytes[0] = 1;
            System.Buffers.Binary.BinaryPrimitives.WriteUInt64BigEndian(bytes[8..], ((ulong)n << 32) | n);
            hashes.Add(new Ulid(bytes, false).GetHashCode());
        }

        // 1000 random 32-bit hashes collide with probability ~1e-4; allow a few rather than demand perfection
        hashes.Count.Should().BeGreaterThan(990);
    }

    [Fact]
    public void Equality_ShouldBeReflexiveSymmetricTransitiveAndConsistentWithHashCode()
    {
        var x = new Ulid(SequentialBytes, false);
        var y = Parse(x.ToString());
        var z = new Ulid(x.ToGuid());
        var other = new Ulid(SequentialBytes.Reverse().ToArray(), false);

        x.Equals(x).Should().BeTrue();
        (x.Equals(y) && y.Equals(x)).Should().BeTrue();
        (x.Equals(y) && y.Equals(z) && x.Equals(z)).Should().BeTrue();
        (x == y).Should().Be(x.Equals(y));
        (x != other).Should().BeTrue();
        x.Equals((object?)null).Should().BeFalse();
        new[] { x.GetHashCode(), y.GetHashCode(), z.GetHashCode() }.Distinct().Should().ContainSingle();
    }
}
