// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Val Melamed

global using System;
global using System.Diagnostics;
global using System.Diagnostics.CodeAnalysis;
global using System.Numerics;
global using System.Security.Cryptography;
global using System.Text.Json;
global using System.Diagnostics.CodeAnalysis;
global using System.Numerics;
global using System.Runtime.Serialization;

global using BenchmarkDotNet.Attributes;

global using vm2;
global using vm2.SemVerSerialization.NsJson;
global using vm2.SemVerSerialization.SysJson;

global using vm2.UlidSerialization.NsJson;
global using vm2.UlidSerialization.SysJson;

global using static System.Buffers.Binary.BinaryPrimitives;
global using static vm2.Ulid;
