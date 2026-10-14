// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Val Melamed

namespace vm2.Tests.Ulid;

using System.Text.Unicode;

using vm2;

public partial class UlidTests
{
    const string KnownUlid = "01ARZ3NDEKTSV4RRFFQ69G5FAV";

    [Fact]
    public void Interpolation_ShouldProduceCanonicalString()
    {
        var ulid = Parse(KnownUlid);

        var interpolated = $"id={ulid}.";

        interpolated.Should().Be($"id={KnownUlid}.");
    }

    [Fact]
    public void Utf8Interpolation_ShouldProduceCanonicalUtf8()
    {
        var ulid = Parse(KnownUlid);
        Span<byte> buffer = stackalloc byte[64];

        Utf8.TryWrite(buffer, $"id={ulid}.", out var written).Should().BeTrue();

        Encoding.UTF8.GetString(buffer[..written]).Should().Be($"id={KnownUlid}.");
    }

    [Theory]
    [InlineData(UlidStringLength - 1, false, 0)]
    [InlineData(UlidStringLength, true, UlidStringLength)]
    [InlineData(UlidStringLength + 10, true, UlidStringLength)]
    public void TryFormat_Chars_ShouldWriteOnlyWhenTheBufferIsLargeEnough(int length, bool expected, int expectedWritten)
    {
        var ulid = Parse(KnownUlid);
        var buffer = new char[length];

        var result = ulid.TryFormat(buffer, out var written);

        result.Should().Be(expected);
        written.Should().Be(expectedWritten);
        if (expected)
            new string(buffer, 0, written).Should().Be(KnownUlid);
    }

    [Theory]
    [InlineData(UlidStringLength - 1, false, 0)]
    [InlineData(UlidStringLength, true, UlidStringLength)]
    [InlineData(UlidStringLength + 10, true, UlidStringLength)]
    public void TryFormat_Utf8_ShouldWriteOnlyWhenTheBufferIsLargeEnough(int length, bool expected, int expectedWritten)
    {
        var ulid = Parse(KnownUlid);
        var buffer = new byte[length];

        var result = ulid.TryFormat(buffer, out var written);

        result.Should().Be(expected);
        written.Should().Be(expectedWritten);
        if (expected)
            Encoding.UTF8.GetString(buffer, 0, written).Should().Be(KnownUlid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ToString_WithDefaultFormat_ShouldEqualToString(string? format)
    {
        var ulid = Parse(KnownUlid);

        ulid.ToString(format, CultureInfo.InvariantCulture).Should().Be(KnownUlid);
    }

    [Theory]
    [InlineData("G")]
    [InlineData("D")]
    [InlineData("x")]
    public void Formatting_WithUnsupportedFormat_ShouldThrowFormatException(string format)
    {
        var ulid = Parse(KnownUlid);

        Action toString = () => ulid.ToString(format, null);
        Action tryFormatChars = () => ulid.TryFormat(new char[UlidStringLength], out _, format);
        Action tryFormatUtf8 = () => ulid.TryFormat(new byte[UlidStringLength], out _, format);
        Action composite = () => string.Format(CultureInfo.InvariantCulture, $"{{0:{format}}}", ulid);

        toString.Should().Throw<FormatException>();
        tryFormatChars.Should().Throw<FormatException>();
        tryFormatUtf8.Should().Throw<FormatException>();
        composite.Should().Throw<FormatException>();
    }

    [Fact]
    public void TryFormat_ShouldNotAllocate()
    {
        var ulid = Parse(KnownUlid);
        Span<char> chars = stackalloc char[UlidStringLength];
        Span<byte> bytes = stackalloc byte[UlidStringLength];
        ulid.TryFormat(chars, out _);       // warm up: the first call may JIT and load types
        ulid.TryFormat(bytes, out _);

        var before = GC.GetAllocatedBytesForCurrentThread();
        ulid.TryFormat(chars, out _);
        ulid.TryFormat(bytes, out _);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        allocated.Should().Be(0);
    }

    [Fact]
    public void GenericSpanParsing_ShouldRoundTrip()
    {
        var expected = Parse(KnownUlid);

        ParseChars<Ulid>(KnownUlid).Should().Be(expected);
        ParseUtf8<Ulid>(Encoding.UTF8.GetBytes(KnownUlid)).Should().Be(expected);
        TryParseChars<Ulid>("not a ULID", out _).Should().BeFalse();
        TryParseUtf8<Ulid>("not a ULID"u8, out _).Should().BeFalse();
    }

    [Fact]
    public void GenericSpanFormatting_ShouldWriteCanonicalString()
    {
        var ulid = Parse(KnownUlid);

        FormatChars(ulid).Should().Be(KnownUlid);
        FormatUtf8(ulid).Should().Be(KnownUlid);
    }

    // These go through the interfaces, the way generic BCL and framework code would call them.
    static T ParseChars<T>(string s) where T : ISpanParsable<T>
        => T.Parse(s.AsSpan(), CultureInfo.InvariantCulture);

    static T ParseUtf8<T>(ReadOnlySpan<byte> s) where T : IUtf8SpanParsable<T>
        => T.Parse(s, CultureInfo.InvariantCulture);

    static bool TryParseChars<T>(string s, out T? result) where T : ISpanParsable<T>
        => T.TryParse(s.AsSpan(), CultureInfo.InvariantCulture, out result);

    static bool TryParseUtf8<T>(ReadOnlySpan<byte> s, out T? result) where T : IUtf8SpanParsable<T>
        => T.TryParse(s, CultureInfo.InvariantCulture, out result);

    static string FormatChars<T>(T value) where T : ISpanFormattable
    {
        Span<char> buffer = stackalloc char[64];
        value.TryFormat(buffer, out var written, default, CultureInfo.InvariantCulture).Should().BeTrue();
        return new string(buffer[..written]);
    }

    static string FormatUtf8<T>(T value) where T : IUtf8SpanFormattable
    {
        Span<byte> buffer = stackalloc byte[64];
        value.TryFormat(buffer, out var written, default, CultureInfo.InvariantCulture).Should().BeTrue();
        return Encoding.UTF8.GetString(buffer[..written]);
    }
}
