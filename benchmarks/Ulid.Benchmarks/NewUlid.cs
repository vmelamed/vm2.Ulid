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
