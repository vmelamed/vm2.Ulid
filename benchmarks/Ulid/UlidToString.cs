// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Val Melamed

namespace vm2.Benchmarks.Ulid;

using vm2;

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
    [Benchmark(Description = "Guid.ToString", OperationsPerInvoke = 1000, Baseline = true)]
    public string Guid_ToString() => _data1.GetNext().ToString();
#endif

    [Benchmark(Description = "Ulid.ToString", OperationsPerInvoke = 1000)]
    public string Ulid_ToString() => _data2.GetNext().ToString();
}
