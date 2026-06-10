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
    const int operationsPerInvoke = 1000;

#if GUID_BASELINE
    PreGeneratedData<Guid> _data1 = null!;
#endif
    PreGeneratedData<Ulid> _data2 = null!;

    [GlobalSetup]
    public void Setup()
    {
        UlidFactory _factory = new();

#if GUID_BASELINE
        _data1 = new(operationsPerInvoke, _ => Guid.NewGuid());
#endif
        _data2 = new(operationsPerInvoke, _ => _factory.NewUlid());
    }

#if GUID_BASELINE
    [Benchmark(Description = "Guid.ToString", OperationsPerInvoke = operationsPerInvoke, Baseline = true)]
    public string Guid_ToString()
    {
        string suppressOptimizationDiscard = "";

        for (int i = 0; i < operationsPerInvoke; i++)
            suppressOptimizationDiscard = _data1.GetNext().ToString();

        return suppressOptimizationDiscard;
    }
#endif

    [Benchmark(Description = "Ulid.ToString", OperationsPerInvoke = operationsPerInvoke)]
    public string Ulid_ToString()
    {
        string suppressOptimizationDiscard = "";

        for (int i = 0; i < operationsPerInvoke; i++)
            suppressOptimizationDiscard = _data2.GetNext().ToString();

        return suppressOptimizationDiscard;
    }
}
