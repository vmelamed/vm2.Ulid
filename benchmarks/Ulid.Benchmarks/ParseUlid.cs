// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Val Melamed

namespace vm2.Benchmarks.Ulid;

using vm2;

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
