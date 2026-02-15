#!/usr/bin/env dotnet

// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Val Melamed

#:property TargetFramework=net10.0
#:project ../src/UlidType/UlidType.csproj

using static System.Console;
using static System.Text.Encoding;

using vm2;

WriteLine($"New Ulid:");
WriteLine($"--------------------------");

var ulid = Ulid.NewUlid();

Display(ulid);

WriteLine($"Ulid from UTF-8 string: \"{ulid}\" (round-trip)");
WriteLine($"-----------------------------------------");
var ulid2 = new Ulid(UTF8.GetBytes(ulid.ToString()), true);

Display(ulid2);

WriteLine(Ulid.NewUlid().ToString());
WriteLine(Ulid.NewUlid().ToString());
WriteLine(Ulid.NewUlid().ToString());
WriteLine($"--------------------------");
Task.Delay(1).Wait();   // wait 1ms to ensure different timestamp
WriteLine(Ulid.NewUlid().ToString());
WriteLine(Ulid.NewUlid().ToString());
WriteLine(Ulid.NewUlid().ToString());
WriteLine($"--------------------------");
Task.Delay(1).Wait();
WriteLine(Ulid.NewUlid().ToString());
WriteLine(Ulid.NewUlid().ToString());
WriteLine(Ulid.NewUlid().ToString());
WriteLine();

static void Display(Ulid ulid)
{
    byte[] bytes = ulid.Bytes.ToArray();
    DateTimeOffset timestamp = ulid.Timestamp;
    byte[] randomBytes = ulid.RandomBytes.ToArray();

    WriteLine($"As ULID string:  \"{ulid}\"");
    WriteLine($"  u.Timestamp:   {timestamp:o} ({timestamp.ToUnixTimeMilliseconds()})");
    WriteLine($"  u.RandomBytes: [ 0x{string.Join(", 0x", randomBytes.Select(b => b.ToString("X2")))} ]");
    WriteLine($"As byte array:   [ 0x{string.Join(", 0x", bytes.Select(b => b.ToString("X2")))} ]");
    WriteLine($"As Guid:         {ulid.ToGuid()}");
    WriteLine();
}
