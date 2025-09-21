﻿namespace vm2.UlidType.Tests.SysJson;

using System.Text.Json;

public class UlidSysConverterTests
{

    class Subject
    {
        public Ulid? Id { get; set; }

        public Subject()
        {
        }

        public Subject(string? id) => Id = id is null ? null : new Ulid(id);
    }

    class Subject1
    {
        public Ulid Id { get; set; }

        public Subject1()
        {
        }

        public Subject1(string? id) => Id = id is null ? Ulid.Empty : new Ulid(id);
    }

    [Fact]
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "<Pending>")]
    public void Test_NotNull_Ulid_Serializes_To_Json_With_System_Json()
    {
        var sut = new Subject("01K5N2TW3MA38KG6D7WNFDPAKS");

        var json = JsonSerializer.Serialize(sut);

        json.Should().Contain(sut?.Id?.ToString());
    }

    [Fact]
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "<Pending>")]
    public void Test_NotNull_Ulid_Deserializes_From_Json_With_System_Json()
    {
        var ulid = new Ulid("01K5N3A2GJYH10NGHHTWQR4VBP");
        var json = $@"{{ ""Id"": ""{ulid}"" }}";

        var deserialize = () => JsonSerializer.Deserialize<Subject>(json);

        var sut = deserialize.Should().NotThrow().Which;

        sut.Id.Should().Be(ulid);
    }

    [Fact]
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "<Pending>")]
    public void Test_Null_Ulid_Serializes_To_Json_With_System_Json()
    {
        var sut = new Subject();

        var json = JsonSerializer.Serialize(sut);

        json.Should().Contain(@"""Id"":null");
    }

    [Fact]
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "<Pending>")]
    public void Test_Null_Ulid_Deserializes_From_Json_With_System_Json()
    {
        var json = $@"{{ ""Id"":null }}";

        var deserialize = () => JsonSerializer.Deserialize<Subject>(json);

        var sut = deserialize.Should().NotThrow().Which;

        sut.Id.Should().BeNull();
    }

    [Fact]
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "<Pending>")]
    public void Test_Ulid_Serializes_To_Json_With_System_Json()
    {
        var sut = new Subject1("01K5N2TW3MA38KG6D7WNFDPAKS");

        var json = JsonSerializer.Serialize(sut);

        json.Should().Contain(sut?.Id.ToString());
    }

    [Fact]
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "<Pending>")]
    public void Test_Ulid_Deserializes_From_Json_With_System_Json()
    {
        var ulid = new Ulid("01K5N3A2GJYH10NGHHTWQR4VBP");
        var json = $@"{{ ""Id"": ""{ulid}"" }}";

        var deserialize = () => JsonSerializer.Deserialize<Subject1>(json);

        var sut = deserialize.Should().NotThrow().Which;

        sut.Id.Should().Be(ulid);
    }

    [Fact]
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "<Pending>")]
    public void Test_Ulid_Deserialize_InvalidUlid_Throws()
    {
        var json = @"{ ""Id"": ""U1K5N3A2GJYH10NGHHTWQR4VBP"" }";

        var deserialize = () => JsonSerializer.Deserialize<Subject1>(json);

        deserialize.Should().Throw<JsonException>();
    }
}
