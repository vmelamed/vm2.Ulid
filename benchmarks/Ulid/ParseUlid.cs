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
    const int operationsPerInvoke = 1000;

    PreGeneratedData<string> _stringData = null!;
    PreGeneratedData<byte[]> _utf8Data = null!;
#if GUID_BASELINE
    PreGeneratedData<string> _guidStringData = null!;
#endif

    [GlobalSetup]
    public void Setup()
    {
        UlidFactory _factory = new();

        _stringData = new(operationsPerInvoke, _ => _factory.NewUlid().ToString());
        _utf8Data = new(operationsPerInvoke, _ => Encoding.UTF8.GetBytes(_factory.NewUlid().ToString()));
#if GUID_BASELINE
        _guidStringData = new(operationsPerInvoke, _ => Guid.NewGuid().ToString());
#endif
    }

    [Benchmark(Description = "Ulid.Parse(StringUtf16)", OperationsPerInvoke = operationsPerInvoke)]
    public Ulid Ulid_Parse_Utf16()
    {
        Ulid suppressOptimizationDiscard = default;

        for (int i = 0; i < operationsPerInvoke; i++)
            suppressOptimizationDiscard = Ulid.Parse(_stringData.GetNext());

        return suppressOptimizationDiscard;
    }

    [Benchmark(Description = "Ulid.Parse(StringUtf8)", OperationsPerInvoke = operationsPerInvoke)]
    public Ulid Ulid_Parse_Utf8()
    {
        Ulid suppressOptimizationDiscard = default;

        for (int i = 0; i < operationsPerInvoke; i++)
            suppressOptimizationDiscard = Ulid.Parse(_utf8Data.GetNext());

        return suppressOptimizationDiscard;
    }

#if GUID_BASELINE
    [Benchmark(Description = "Guid.Parse(string)", OperationsPerInvoke = operationsPerInvoke, Baseline = true)]
    public Guid Guid_Parse()
    {
        Guid suppressOptimizationDiscard = default;

        for (int i = 0; i < operationsPerInvoke; i++)
            suppressOptimizationDiscard = Guid.Parse(_guidStringData.GetNext());

        return suppressOptimizationDiscard;
    }
#endif
}
