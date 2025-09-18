namespace vm2.UlidType.Benchmarks;

using System.Text;

using vm2;
using vm2.UlidRandomProviders;

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

[SimpleJob(RuntimeMoniker.HostProcess)]
[MemoryDiagnoser]
[JsonExporter]
[Orderer(SummaryOrderPolicy.FastestToSlowest, MethodOrderPolicy.Declared)]
public class NewUlid
{
    [Params(typeof(CryptoRandom), typeof(PseudoRandom))]
    public Type RandomProviderType { get; set; } = null!;

    IUlidRandomProvider? RandomProvider { get; set; }

    UlidFactory Factory { get; set; } = null!;

    [GlobalSetup]
    public void Setup()
    {
        RandomProvider = Activator.CreateInstance(RandomProviderType) as IUlidRandomProvider
                                ?? throw new InvalidOperationException($"Failed to create instance of {RandomProviderType}");
        Factory = new(RandomProvider);
    }

    [Benchmark(Description = "Guid.NewGuid", Baseline = true)]
    public Guid Generate_Guid() => Guid.NewGuid();

    [Benchmark(Description = "Ulid.NewUlid")]
    public Ulid GenerateUlid_Ulid() => Ulid.NewUlid(RandomProvider);

    [Benchmark(Description = "UlidFactory.NewUlid")]
    public Ulid Generate_Ulid() => Factory.NewUlid();
}

[SimpleJob(RuntimeMoniker.HostProcess)]
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

[SimpleJob(RuntimeMoniker.HostProcess)]
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

    [Benchmark(Description = "Guid.Parse", Baseline = true)]
    public Guid Guid_Parse() => Guid.Parse(_data1.GetNext());

    [Benchmark(Description = "Ulid.ParseString")]
    public Ulid Ulid_Parse() => Ulid.Parse(_data2.GetNext());

    [Benchmark(Description = "Ulid.ParseUtf8String")]
    public Ulid Ulid_ParseUtf8() => Ulid.TryParse(_data3.GetNext(), out var ulid)
                                        ? ulid
                                        : throw new InvalidOperationException($"Failed to parse: {string.Join("-", _data3.Current.Select(b => b.ToString("X2")))}");
}
