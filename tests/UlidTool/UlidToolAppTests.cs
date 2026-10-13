// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Val Melamed

namespace vm2.Tests.UlidTool;

public sealed class UlidToolAppTests(ITestOutputHelper output) : TestBase(output)
{
    private static readonly Regex UlidRegex = new("^[0-9A-HJKMNP-TV-Z]{26}$", RegexOptions.Compiled);

    [Fact]
    public void Run_Default_GeneratesSingleUlid()
    {
        using var writer = new StringWriter();

        var exitCode = UlidToolApp.Run(Array.Empty<string>(), writer);

        exitCode.Should().Be(0);
        var lines = GetNonEmptyLines(writer.ToString());
        lines.Should().HaveCount(1);
        lines[0].Should().MatchRegex(UlidRegex.ToString());
    }

    [Fact]
    public void Run_NumberOption_GeneratesMultipleUlids()
    {
        using var writer = new StringWriter();

        var exitCode = UlidToolApp.Run(["-n", "3"], writer);

        exitCode.Should().Be(0);
        var lines = GetNonEmptyLines(writer.ToString());
        lines.Should().HaveCount(3);
        lines.Should().OnlyContain(line => UlidRegex.IsMatch(line));
    }

    [Fact]
    public void Run_FormatGuid_OutputsGuid()
    {
        using var writer = new StringWriter();

        var exitCode = UlidToolApp.Run(["-f", "guid"], writer);

        exitCode.Should().Be(0);
        var lines = GetNonEmptyLines(writer.ToString());
        lines.Should().HaveCount(1);
        Guid.TryParse(lines[0], out _).Should().BeTrue();
    }

    [Fact]
    public void Run_FormatDetailed_OutputsDetailedLines()
    {
        using var writer = new StringWriter();

        var exitCode = UlidToolApp.Run(["-f", "d"], writer);

        exitCode.Should().Be(0);
        var output = writer.ToString();
        output.Should().Contain("ULID:");
        output.Should().Contain("GUID:");
        output.Should().Contain("Timestamp:");
        output.Should().Contain("Random Bytes:");
    }

    [Fact]
    public void Run_FormatDetailed_ShouldSplitUlidStringIntoTimestampAndRandomParts()
    {
        using var writer = new StringWriter();

        UlidToolApp.Run(["-f", "detailed"], writer);

        var lines = GetNonEmptyLines(writer.ToString());
        var ulid = lines.Single(l => l.StartsWith("ULID:"))["ULID:".Length..].Trim();
        var timestampPart = lines.Single(l => l.Contains("Timestamp:")).Split(' ', StringSplitOptions.RemoveEmptyEntries)[1];
        var randomPart = lines.Single(l => l.Contains("Random Bytes:")).Split(' ', StringSplitOptions.RemoveEmptyEntries)[2];

        timestampPart.Should().HaveLength(10);
        randomPart.Should().HaveLength(16);
        (timestampPart + randomPart).Should().Be(ulid);
    }

    [Theory]
    [InlineData("ULID", 1)]
    [InlineData("U", 1)]
    [InlineData("GUID", 1)]
    [InlineData("Gu", 1)]
    [InlineData("DETAILED", 4)]
    [InlineData("De", 4)]
    public void Run_FormatInAnyCase_ShouldProduceOutput(string format, int expectedLines)
    {
        using var writer = new StringWriter();

        var exitCode = UlidToolApp.Run(["-f", format], writer);

        exitCode.Should().Be(0);
        GetNonEmptyLines(writer.ToString()).Should().HaveCount(expectedLines);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("10001")]
    public void Run_InvalidNumber_ReturnsError(string value)
    {
        using var writer = new StringWriter();

        var exitCode = UlidToolApp.Run(["-n", value], writer);

        exitCode.Should().Be(1);
        var output = writer.ToString();
        output.Should().Contain("Error parsing command line arguments:");
        output.Should().Contain("must be from 1 to 10000");
    }

    [Fact]
    public void Run_InvalidFormat_ReturnsError()
    {
        using var writer = new StringWriter();

        var exitCode = UlidToolApp.Run(["-f", "nope"], writer);

        exitCode.Should().Be(1);
        var output = writer.ToString();
        output.Should().Contain("Error parsing command line arguments:");
        output.Should().Contain("specified format is not valid");
    }

    private static string[] GetNonEmptyLines(string output) =>
        output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
}
