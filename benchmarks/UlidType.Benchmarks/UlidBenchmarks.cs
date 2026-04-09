// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Val Melamed

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
[JsonExporterAttribute.BriefCompressed]
[MarkdownExporterAttribute.GitHub]
[MemoryDiagnoser]
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

#if GUID_BASELINE
    [Benchmark(Description = "Guid.NewGuid", Baseline = true)]
    public Guid Guid_NewGuid() => Guid.NewGuid();
#endif

    [Benchmark(Description = "Ulid.NewUlid")]
    public Ulid Ulid_NewUlid() => Ulid.NewUlid();

    [Benchmark(Description = "Factory.NewUlid")]
    public Ulid Factory_NewUlid() => Factory.NewUlid();
}

#if SHORT_RUN
[ShortRunJob]
#else
[SimpleJob(RuntimeMoniker.HostProcess)]
#endif
public class UlidToString
{
    const int MaxDataItems = 1000;

#if GUID_BASELINE
    PreGeneratedData<Guid> _data1 = null!;
#endif
    PreGeneratedData<Ulid> _data2 = null!;

    [GlobalSetup]
    public void Setup()
    {
        UlidFactory _factory = new();

#if GUID_BASELINE
        _data1 = new(MaxDataItems, _ => Guid.NewGuid());
#endif
        _data2 = new(MaxDataItems, _ => _factory.NewUlid());
    }

#if GUID_BASELINE
    [Benchmark(Description = "Guid.ToString", Baseline = true)]
    public string Guid_ToString() => _data1.GetNext().ToString();
#endif

    [Benchmark(Description = "Ulid.ToString")]
    public string Ulid_ToString() => _data2.GetNext().ToString();
}

#if SHORT_RUN
[ShortRunJob]
#else
[SimpleJob(RuntimeMoniker.HostProcess)]
#endif
public class ParseUlid
{
    const int MaxDataItems = 1000;
    PreGeneratedData<string> _data2 = null!;
    PreGeneratedData<byte[]> _data3 = null!;
#if GUID_BASELINE
    PreGeneratedData<string> _data1 = null!;
#endif

    [GlobalSetup]
    public void Setup()
    {
        UlidFactory _factory = new();

        _data2 = new(MaxDataItems, _ => _factory.NewUlid().ToString());
        _data3 = new(MaxDataItems, _ => Encoding.UTF8.GetBytes(_factory.NewUlid().ToString()));
#if GUID_BASELINE
        _data1 = new(MaxDataItems, _ => Guid.NewGuid().ToString());
#endif
    }

    [Benchmark(Description = "Ulid.Parse(StringUtf16)")]
    public Ulid Ulid_Parse_Utf16() => Ulid.Parse(_data2.GetNext());

    [Benchmark(Description = "Ulid.Parse(StringUtf8)")]
    public Ulid Ulid_Parse_Utf8() => Ulid.Parse(_data3.GetNext());

#if GUID_BASELINE
    [Benchmark(Description = "Guid.Parse(string)", Baseline = true)]
    public Guid Guid_Parse() => Guid.Parse(_data1.GetNext());
#endif
}
