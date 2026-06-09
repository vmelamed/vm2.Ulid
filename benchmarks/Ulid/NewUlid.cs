// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Val Melamed

namespace vm2.Benchmarks.Ulid;

using vm2;

#if SHORT_RUN
[ShortRunJob]
#else
[SimpleJob(RuntimeMoniker.HostProcess)]
#endif
public class NewUlid
{
    const int operationsPerInvoke = 1000;

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
    [Benchmark(Description = "Guid.NewGuid", OperationsPerInvoke = operationsPerInvoke, Baseline = true)]
    public Guid Guid_NewGuid()
    {
        Guid id = default;

        for (int i = 0; i < operationsPerInvoke; i++)
            id = Guid.NewGuid();

        return id;
    }
#endif

    [Benchmark(Description = "Ulid.NewUlid", OperationsPerInvoke = operationsPerInvoke)]
    public Ulid Ulid_NewUlid()
    {
        Ulid id = default;

        for (int i = 0; i < operationsPerInvoke; i++)
            id = Ulid.NewUlid();

        return id;
    }

    [Benchmark(Description = "Factory.NewUlid", OperationsPerInvoke = operationsPerInvoke)]
    public Ulid Factory_NewUlid()
    {
        Ulid id = default;

        for (int i = 0; i < operationsPerInvoke; i++)
            id = Factory.NewUlid();

        return id;
    }
}
