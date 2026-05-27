// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Val Melamed

namespace vm2.Benchmarks.Ulid;

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
