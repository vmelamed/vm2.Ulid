// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Val Melamed

namespace vm2.Benchmarks.Ulid;

using vm2;

#if SHORT_RUN
[ShortRunJob]
#else
[SimpleJob(RuntimeMoniker.HostProcess)]
#endif
public class UlidOperations
{
    const int operationsPerInvoke = 1000;

    Ulid[] _ulids = null!;
    Ulid[] _copies = null!;
    Ulid[] _sortBuffer = null!;
    Guid[] _guids = null!;
    byte[][] _bytes = null!;
    HashSet<Ulid> _set = null!;

    [GlobalSetup]
    public void Setup()
    {
        var factory = new UlidFactory(new Providers.Ulid.PseudoRandom());

        _ulids      = [.. Enumerable.Range(0, operationsPerInvoke).Select(_ => factory.NewUlid())];
        Random.Shared.Shuffle(_ulids);
        // equal values backed by different storage, so Equals cannot short-circuit on reference identity
        _copies     = [.. _ulids.Select(u => Ulid.Parse(u.ToString()))];
        _guids      = [.. _ulids.Select(u => u.ToGuid())];
        _bytes      = [.. _ulids.Select(u => u.Bytes.ToArray())];
        _set        = [.. _ulids];
        _sortBuffer = new Ulid[operationsPerInvoke];
    }

    [Benchmark(Description = "Ulid.Equals", OperationsPerInvoke = operationsPerInvoke)]
    public int Ulid_Equals()
    {
        var n = 0;
        for (var i = 0; i < operationsPerInvoke; i++)
            if (_ulids[i].Equals(_copies[i]))
                n++;
        return n;
    }

    [Benchmark(Description = "Ulid.CompareTo", OperationsPerInvoke = operationsPerInvoke)]
    public int Ulid_CompareTo()
    {
        var n = 0;
        for (var i = 1; i < operationsPerInvoke; i++)
            n += _ulids[i].CompareTo(_ulids[i - 1]);
        return n;
    }

    [Benchmark(Description = "Ulid.GetHashCode", OperationsPerInvoke = operationsPerInvoke)]
    public int Ulid_GetHashCode()
    {
        var n = 0;
        for (var i = 0; i < operationsPerInvoke; i++)
            n ^= _ulids[i].GetHashCode();
        return n;
    }

    [Benchmark(Description = "HashSet<Ulid>.Contains", OperationsPerInvoke = operationsPerInvoke)]
    public int HashSet_Contains()
    {
        var n = 0;
        for (var i = 0; i < operationsPerInvoke; i++)
            if (_set.Contains(_copies[i]))
                n++;
        return n;
    }

    [Benchmark(Description = "Array.Sort(Ulid[1000]) per element", OperationsPerInvoke = operationsPerInvoke)]
    public Ulid Array_Sort()
    {
        _ulids.CopyTo(_sortBuffer, 0);
        Array.Sort(_sortBuffer);
        return _sortBuffer[0];
    }

    [Benchmark(Description = "Ulid.ToGuid", OperationsPerInvoke = operationsPerInvoke)]
    public Guid Ulid_ToGuid()
    {
        Guid g = default;
        for (var i = 0; i < operationsPerInvoke; i++)
            g = _ulids[i].ToGuid();
        return g;
    }

    [Benchmark(Description = "new Ulid(Guid)", OperationsPerInvoke = operationsPerInvoke)]
    public Ulid Ulid_FromGuid()
    {
        Ulid u = default;
        for (var i = 0; i < operationsPerInvoke; i++)
            u = new Ulid(_guids[i]);
        return u;
    }

    [Benchmark(Description = "new Ulid(bytes)", OperationsPerInvoke = operationsPerInvoke)]
    public Ulid Ulid_FromBytes()
    {
        Ulid u = default;
        for (var i = 0; i < operationsPerInvoke; i++)
            u = new Ulid(_bytes[i], false);
        return u;
    }

    [Benchmark(Description = "Ulid.Timestamp", OperationsPerInvoke = operationsPerInvoke)]
    public long Ulid_Timestamp()
    {
        long n = 0;
        for (var i = 0; i < operationsPerInvoke; i++)
            n += _ulids[i].Timestamp.UtcTicks;
        return n;
    }

    [Benchmark(Description = "Ulid++", OperationsPerInvoke = operationsPerInvoke)]
    public Ulid Ulid_Increment()
    {
        var u = _ulids[0];
        for (var i = 0; i < operationsPerInvoke; i++)
            u++;
        return u;
    }
}
