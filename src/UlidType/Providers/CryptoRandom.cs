// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Val Melamed

namespace vm2.Providers;

/// <summary>
/// A random provider that uses cryptographic random number generation.
/// </summary>
public sealed class CryptoRandom : IUlidRandomProvider
{
    /// <summary>
    /// Fills the provided byte span with cryptographically secure random data.
    /// </summary>
    public void Fill(Span<byte> bytes) => RandomNumberGenerator.Fill(bytes);
}
