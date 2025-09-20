﻿namespace vm2;

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
    public const int TimestampBegin             = 0;

    /// <summary>
    /// Represents the length of the timestamp bytes in a ULID.
    /// </summary>
    public const int TimestampLength            = 6;

    /// <summary>
    /// Represents the length of the timestamp bytes in a ULID.
    /// </summary>
    public const int TimestampEnd               = TimestampBegin + TimestampLength;

    /// <summary>
    /// Represents the offset of the random bytes in a ULID.
    /// </summary>
    public const int RandomBegin                = TimestampEnd;

    /// <summary>
    /// Represents the length of the random bytes in a ULID.
    /// </summary>
    public const int RandomLength               = 10;

    /// <summary>
    /// Represents the offset of the random bytes in a ULID.
    /// </summary>
    public const int RandomEnd                  = RandomBegin + RandomLength;

    /// <summary>
    /// Represents the total length, in bytes, of a ULID (Universally Unique Lexicographically Sortable Identifier).
    /// </summary>
    public const int UlidBytesLength            = TimestampLength + RandomLength;

    /// <summary>
    /// Represents the total length, in bytes, of a ULID (Universally Unique Lexicographically Sortable Identifier).
    /// </summary>
    public const int UlidBitsLength             = UlidBytesLength * 8;

    /// <summary>
    /// Represents the Crockford Base32 alphabet, a character set used for encoding data (here ULID data) in a case-insensitive manner.
    /// </summary>
    /// <remarks>
    /// The Crockford Base32 alphabet excludes characters that are easily confused, such as 'I', 'L', 'O', and 'U'. This alphabet<br/>
    /// is commonly used in applications such as unique identifier generation and human-readable encoding.
    /// </remarks>
    public const string CrockfordDigits         = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

    /// <summary>
    /// Represents the weight of each Crockford digit in a ULID. Just like the weight of each digit in a decimal number is 10 (Radix 10)<br/>
    /// power the position of the digit in the number starting with 0; the weight of each digit in a ULID is 32 (Radix 32) to the
    /// power of the position of the Crockford digit in the Ulid starting with 0.
    /// </summary>
    /// <remarks>
    /// This constant is derived from the bit shift value defined by <see cref="BitsPerUlidDigit"/> and is used in operations <br/>
    /// involving ULID character manipulation.
    /// </remarks>
    public static readonly uint UlidRadix       = 32; // = CrockfordDigits.Length - the radix of a Crockford number

    /// <summary>
    /// The number of bits that represent a digit in a ULID.
    /// </summary>
    /// <remarks>
    /// This constant defines the number of bits to shift in binary to string operations involving ULIDs. It is primarily used <br/>
    /// to adjust or manipulate ULID components during encoding or decoding processes.
    /// </remarks>
    public static readonly int BitsPerUlidDigit = 5; // 1 << BitsPerUlidDigit == CrockfordDigits.Length

    /// <summary>
    /// Represents the bitmask used to extract the least significant character ULID value. If you shift it left by <see cref="BitsPerUlidDigit"/>,
    /// we get the mask of the second least significant number, etc.
    /// </summary>
    /// <remarks>
    /// This constant is derived from the bit shift value defined by <see cref="BitsPerUlidDigit"/> and is used in operations <br/>
    /// involving ULID character manipulation.
    /// </remarks>
    public static readonly int UlidCharMask     = 0b_0001_1111; // the numeric value of the digit CrockfordDigits.Last()

    /// <summary>
    /// Represents the fixed length of a ULID (Universally Unique Lexicographically Sortable Identifier) string. It is 26 characters long.
    /// </summary>
    public static readonly int UlidStringLength = (UlidBitsLength / BitsPerUlidDigit) + (UlidBitsLength % BitsPerUlidDigit > 0 ? 1 : 0);

    /// <summary>
    /// The regular expression pattern for validating ULID strings.
    /// </summary>
    public const string UlidStringRegex         = $"(?i)[{CrockfordDigits}]{{26}}";

    internal static byte[] CrockfordDigitValues =
    [
          0, // 0
          1, // 1
          2, // 2
          3, // 3
          4, // 4
          5, // 5
          6, // 6
          7, // 7
          8, // 8
          9, // 9
        255, // :
        255, // ;
        255, // <
        255, // =
        255, // >
        255, // ?
        255, // @
         10, // A
         11, // B
         12, // C
         13, // D
         14, // E
         15, // F
         16, // G
         17, // H
        255, // I
         18, // J
         19, // K
        255, // L
         20, // M
         21, // N
        255, // O
         22, // P
         23, // Q
         24, // R
         25, // S
         26, // T
        255, // U
         27, // V
         28, // W
         29, // X
         30, // Y
         31, // Z
        255, // [
        255, // \
        255, // ]
        255, // ^
        255, // _
        255, // `
         10, // a
         11, // b
         12, // c
         13, // d
         14, // e
         15, // f
         16, // g
         17, // h
        255, // i
         18, // j
         19, // k
        255, // l
         20, // m
         21, // n
        255, // o
         22, // p
         23, // q
         24, // r
         25, // s
         26, // t
        255, // u
         27, // v
         28, // w
         29, // x
         30, // y
         31, // z
    ];

    internal static int Zero = '0';
}
