// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Val Melamed

namespace vm2.Providers;

/// <summary>
/// A random provider that uses a pseudo-random number generator.
/// </summary>
public sealed class PseudoRandom : IUlidRandomProvider
{
    /// <summary>
    /// Fills the provided byte span with pseudo-random data.
    /// </summary>
    public void Fill(Span<byte> bytes) => Random.Shared.NextBytes(bytes);
}
