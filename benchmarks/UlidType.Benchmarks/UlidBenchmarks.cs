// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Val Melamed

namespace vm2.UlidType.Benchmarks;

using System.Text;

using vm2;
using vm2.Providers;

#pragma warning disable CA1822 // The benchmark methods must not be static

class PreGeneratedData<T>
{
    int _numberItems;
    int _index;
    T[] _data = null!;

    public PreGeneratedData(int number, Func<int, T> factory)
    {
        _numberItems = number;
        _index = 0;
        _data = [.. Enumerable.Range(0, _numberItems).Select(factory)];
    }

    public T GetNext()
    {
        if (_index >= _numberItems)
            _index = 0;
        return _data[_index++];
    }

    public T Current => _data[_index];
}

#if SHORT_RUN
[ShortRunJob]
#else
[SimpleJob(RuntimeMoniker.HostProcess)]
#endif
[MemoryDiagnoser]
[JsonExporter]
[Orderer(SummaryOrderPolicy.FastestToSlowest, MethodOrderPolicy.Declared)]
public class NewUlid
{
    [Params(nameof(CryptoRandom), nameof(PseudoRandom))]
    public string RandomProviderType { get; set; } = "";

    IUlidRandomProvider? RandomProvider { get; set; }

    UlidFactory Factory { get; set; } = null!;

    [GlobalSetup]
    public void Setup()
    {
        RandomProvider = RandomProviderType switch {
            nameof(CryptoRandom) => new CryptoRandom(),
            nameof(PseudoRandom) => new PseudoRandom(),
            _ => throw new InvalidOperationException("RandomProviderType is not set"),
        };
        Factory = new(RandomProvider);
    }

    [Benchmark(Description = "Guid.NewGuid", Baseline = true)]
    public Guid Guid_NewGuid() => Guid.NewGuid();

    [Benchmark(Description = "Ulid.NewUlid")]
    public Ulid Ulid_NewUlid() => Ulid.NewUlid(RandomProvider);

    [Benchmark(Description = "Factory.NewUlid")]
    public Ulid Factory_NewUlid() => Factory.NewUlid();
}

#if SHORT_RUN
[ShortRunJob]
#else
[SimpleJob(RuntimeMoniker.HostProcess)]
#endif
[MemoryDiagnoser]
[JsonExporter]
[Orderer(SummaryOrderPolicy.FastestToSlowest, MethodOrderPolicy.Declared)]
public class UlidToString
{
    const int MaxDataItems = 1000;
    PreGeneratedData<Guid> _data1 = null!;
    PreGeneratedData<Ulid> _data2 = null!;

    [GlobalSetup]
    public void Setup()
    {
        UlidFactory _factory = new();

        _data1 = new(MaxDataItems, _ => Guid.NewGuid());
        _data2 = new(MaxDataItems, _ => _factory.NewUlid());
    }

    [Benchmark(Description = "Guid.ToString", Baseline = true)]
    public string Guid_ToString() => _data1.GetNext().ToString();


    [Benchmark(Description = "Ulid.ToString")]
    public string Ulid_ToString() => _data2.GetNext().ToString();
}

#if SHORT_RUN
[ShortRunJob]
#else
[SimpleJob(RuntimeMoniker.HostProcess)]
#endif
[MemoryDiagnoser]
[JsonExporter]
[Orderer(SummaryOrderPolicy.FastestToSlowest, MethodOrderPolicy.Declared)]
public class ParseUlid
{
    const int MaxDataItems = 1000;
    PreGeneratedData<string> _data1 = null!;
    PreGeneratedData<string> _data2 = null!;
    PreGeneratedData<byte[]> _data3 = null!;

    [GlobalSetup]
    public void Setup()
    {
        UlidFactory _factory = new();

        _data1 = new(MaxDataItems, _ => Guid.NewGuid().ToString());
        _data2 = new(MaxDataItems, _ => _factory.NewUlid().ToString());
        _data3 = new(MaxDataItems, _ => Encoding.UTF8.GetBytes(_factory.NewUlid().ToString()));
    }

    [Benchmark(Description = "Guid.Parse(string)", Baseline = true)]
    public Guid Guid_Parse() => Guid.Parse(_data1.GetNext());

    [Benchmark(Description = "Ulid.Parse(StringUtf16)")]
    public Ulid Ulid_Parse_Utf16() => Ulid.Parse(_data2.GetNext());

    [Benchmark(Description = "Ulid.Parse(StringUtf8)")]
    public Ulid Ulid_Parse_Utf8() => Ulid.Parse(_data3.GetNext());
}
