// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Val Melamed

namespace vm2.UlidType.Tests;

using Xunit.Sdk;

public partial class UlidTests
{
    public partial record TimeAndRandom(long unixTime, byte[] random, bool throws = false) : IXunitSerializable
    {
        public long UnixTime { get; set; } = unixTime;
        public byte[] Random { get; set; } = random;
        public bool Throws { get; set; } = throws;

        public TimeAndRandom()
            : this(0, [], false)
        {
        }

        public void Deserialize(IXunitSerializationInfo info)
        {
            UnixTime = info.GetValue<long>(nameof(UnixTime));
            Random   = info.GetValue<byte[]>(nameof(Random)) ?? [];
            Throws   = info.GetValue<bool>(nameof(Throws));
        }

        public void Serialize(IXunitSerializationInfo info)
        {
            info.AddValue(nameof(UnixTime), UnixTime);
            info.AddValue(nameof(Random), Random);
            info.AddValue(nameof(Throws), Throws);
        }
    }

    public static TheoryData<(TimeAndRandom, TimeAndRandom)> TimeAndRandoms =
    [
        (new TimeAndRandom( 1758851704339L, [0x94, 0x35, 0x28, 0x71, 0x11, 0xE0, 0x66, 0xD6, 0x4A, 0xFF] ),
         new TimeAndRandom( 1758851704339L, [0x94, 0x35, 0x28, 0x71, 0x11, 0xE0, 0x66, 0xD6, 0x4B, 0x00] )),

        (new TimeAndRandom( 1758851704339L, [0x94, 0x35, 0x28, 0x71, 0x11, 0xE0, 0x66, 0xD6, 0xFF, 0xFF] ),
         new TimeAndRandom( 1758851704339L, [0x94, 0x35, 0x28, 0x71, 0x11, 0xE0, 0x66, 0xD7, 0x00, 0x00] )),

        (new TimeAndRandom( 1758851704339L, [0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF], true ),
         new TimeAndRandom( 1758851704339L, [0x94, 0x35, 0x28, 0x71, 0x11, 0xE0, 0x66, 0xD7, 0x00, 0x00], true )),
    ];

    class Test_IUlidRandomProvider : IUlidRandomProvider
    {
        private readonly byte[] _bytes;

        public Test_IUlidRandomProvider(byte[] param) => _bytes = param;

        public void Fill(Span<byte> buffer) => _bytes.AsSpan(0, buffer.Length).CopyTo(buffer);
    }

    class Test_IClock : IClock
    {
        private readonly long _unixTimeMilliseconds;

        public Test_IClock(long param) => _unixTimeMilliseconds = param;

        public long UnixTimeMilliseconds() => _unixTimeMilliseconds;
    }

}