// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Val Melamed

namespace vm2;

/// <summary>
/// Provides constants and utility methods related to the Universally Unique Lexicographically Sortable Identifier (ULID) format.
/// </summary>
/// <remarks>
/// This class defines constants for the structure and validation of ULIDs, including offsets, lengths, and the Crockford Base32<br/>
/// alphabet. It also provides a precompiled regular expression for validating ULID strings.
/// </remarks>
public readonly partial struct Ulid
{
    /// <summary>
    /// Represents the offset of the timestamp bytes in a ULID.
    /// </summary>
    public const int TimestampBegin = 0;

    /// <summary>
    /// Represents the length of the timestamp bytes in a ULID.
    /// </summary>
    public const int TimestampLength = 6;

    /// <summary>
    /// Represents the length of the timestamp bytes in a ULID - 6 bytes.
    /// </summary>
    public const int TimestampEnd = TimestampBegin + TimestampLength;

    /// <summary>
    /// Represents the offset of the random bytes in a ULID.
    /// </summary>
    public const int RandomBegin = TimestampEnd;

    /// <summary>
    /// Represents the length of the random bytes in a ULID.
    /// </summary>
    public const int RandomLength = 10;

    /// <summary>
    /// Represents the offset of the random bytes in a ULID.
    /// </summary>
    public const int RandomEnd = RandomBegin + RandomLength;

    /// <summary>
    /// Represents the total length, in bytes (16), of a ULID (Universally Unique Lexicographically Sortable Identifier).
    /// </summary>
    public const int UlidBytesLength = TimestampLength + RandomLength;

    /// <summary>
    /// Represents the total length, in bits (128), of a ULID (Universally Unique Lexicographically Sortable Identifier).
    /// </summary>
    public const int UlidBitsLength = UlidBytesLength * 8;

    /// <summary>
    /// Represents the Crockford Base32 alphabet, a character set used for encoding data (here ULID data) in a <b>case-insensitive</b> manner.
    /// </summary>
    /// <remarks>
    /// The Crockford Base32 alphabet excludes characters that are easily confused, such as 'I', 'L', 'O', and 'U'. This alphabet<br/>
    /// is commonly used in applications such as unique identifier generation and human-readable encoding.
    /// </remarks>
    public const string CrockfordDigits = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

    /// <summary>
    /// Represents the Crockford Base32 alphabet, a character set used for encoding data (here ULID data) in a case-insensitive manner.
    /// </summary>
    /// <remarks>
    /// The Crockford Base32 alphabet excludes characters that are easily confused, such as 'I', 'L', 'O', and 'U'. This alphabet<br/>
    /// is commonly used in applications such as unique identifier generation and human-readable encoding.
    /// </remarks>
    public static ReadOnlySpan<byte> CrockfordDigitsUtf8 => "0123456789ABCDEFGHJKMNPQRSTVWXYZ"u8;

    /// <summary>
    /// The number of unique symbols used to represent ULID-s, a.k.a. the <see cref="CrockfordDigits"/>. Just like the number of unique<br/>
    /// symbols (the decimal digits) used to represent decimal numbers is 10 (Radix 10), the number of the Crockford digits is 32<br/>
    /// (Radix 32).<br/>
    /// </summary>
    public const uint UlidRadix = 32; // = CrockfordDigits.Length - the radix of a Crockford number

    /// <summary>
    /// The number of bits that represent a digit in a ULID.
    /// </summary>
    /// <remarks>
    /// This constant defines the number of bits to shift in binary to string operations involving ULIDs. It is primarily used <br/>
    /// to adjust or manipulate ULID components during encoding or decoding processes.
    /// </remarks>
    public const int BitsPerUlidDigit = 5; // 1 << BitsPerUlidDigit == CrockfordDigits.Length

    /// <summary>
    /// Represents the bit-mask (<c>0b_0001_1111 / 0x1F / 31</c>) used to extract the least significant digit of a ULID value. If<br/>
    /// we shift it left by <see cref="BitsPerUlidDigit"/> and mask again, we get the mask of the second least significant digit,
    /// etc.
    /// </summary>
    /// <remarks>
    /// This constant is derived from the bit shift value defined by <see cref="BitsPerUlidDigit"/> and is used in operations <br/>
    /// involving ULID character manipulation.
    /// </remarks>
    public const int UlidDigitMask = 0b_0001_1111; // the numeric value of the digit CrockfordDigits.Last()

    /// <summary>
    /// Represents the fixed length of a ULID (Universally Unique Lexicographically Sortable Identifier) string. It is 26 characters long.
    /// </summary>
    public const int UlidStringLength = 26; // (int)Math.Ceiling((float)UlidBitsLength / BitsPerUlidDigit);

    /// <summary>
    /// The value where all bits of the Ulid value are set to zero. Also, represents the smallest possible value of the
    /// <see cref="Ulid"/> type.
    /// </summary>
    public static readonly Ulid Empty = new();

    /// <summary>
    /// The value where all bits of the Ulid value are set to one. Also, represents the greatest possible value of the
    /// <see cref="Ulid"/> type.
    /// </summary>
    public static readonly Ulid AllBitsSet = new([ 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF ], false);

    /// <summary>
    /// The regular expression pattern for validating ULID strings.
    /// </summary>
    public const string UlidStringRegex = $"(?i)[{CrockfordDigits}]{{26}}";

    /// <summary>
    /// The 16 low bits of _hi belong to the random part
    /// </summary>
    const int timestampShift = 16;

    /// <summary>
    ///
    /// </summary>
    const ulong low60BitsMask = (1UL << 60) - 1;

    /// <summary>
    ///
    /// </summary>
    const ulong low56BitsMask = (1UL << 56) - 1;
}
