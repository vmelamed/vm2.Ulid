namespace vm2.UlidType.Benchmarks;

using System.Text;

using vm2;

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
        if (_index == _numberItems)
            _index = 0;
        return _data[_index];
    }

    public T Current => _data[_index];
}

[SimpleJob(RuntimeMoniker.HostProcess)]
[MemoryDiagnoser]
[HtmlExporter]
[CPUUsageDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest, MethodOrderPolicy.Declared)]
public class NewUlid
{
    UlidFactory _factory = null!;

    Action _method = null!;

    [GlobalSetup]
    public void Setup()
    {
        _factory = new UlidFactory();
        _method = () => _factory.NewUlid();
    }

    [Benchmark(Description = "UlidFactory.NewUlid")]
    public void Generate_Ulid() => _factory.NewUlid();
}

[SimpleJob(RuntimeMoniker.HostProcess)]
[MemoryDiagnoser]
[HtmlExporter]
[CPUUsageDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest, MethodOrderPolicy.Declared)]
public class UlidToString
{
    const int MaxDataItems = 1000;
    UlidFactory _factory = null!;
    PreGeneratedData<Ulid> _dataVm = null!;

    [GlobalSetup]
    public void Setup()
    {
        _factory = new();
        _dataVm = new(MaxDataItems, _ => _factory.NewUlid());
    }

    [Benchmark(Description = "Ulid.ToString")]
    public string Ulid_ToString() => _dataVm.GetNext().ToString();
}

[SimpleJob(RuntimeMoniker.HostProcess)]
[MemoryDiagnoser]
[HtmlExporter]
[CPUUsageDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest, MethodOrderPolicy.Declared)]
public class ParseUlid
{
    const int MaxDataItems = 1000;
    UlidFactory _factory = null!;
    PreGeneratedData<string> _data = null!;

    [GlobalSetup]
    public void Setup()
    {
        _factory = new();
        _data = new(MaxDataItems, _ => _factory.NewUlid().ToString());
    }

    [Benchmark(Description = "Ulid.Parse")]
    public Ulid Ulid_Parse() => Ulid.Parse(_data.GetNext());
}

[SimpleJob(RuntimeMoniker.HostProcess)]
[MemoryDiagnoser]
[HtmlExporter]
[CPUUsageDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest, MethodOrderPolicy.Declared)]
public class ParseUtf8Ulid
{
    const int MaxDataItems = 1000;
    UlidFactory _factory = null!;
    PreGeneratedData<byte[]> _data = null!;

    [GlobalSetup]
    public void Setup()
    {
        _factory = new();
        _data = new(MaxDataItems, _ => Encoding.UTF8.GetBytes(_factory.NewUlid().ToString()));
    }

    [Benchmark(Description = "Ulid.Parse")]
    public Ulid Ulid_Parse() => Ulid.TryParse(_data.GetNext(), out var ulid)
                                        ? ulid
                                        : throw new InvalidOperationException($"Failed to parse: {string.Join("-", _data.Current.Select(b => b.ToString("X2")))}");
}
