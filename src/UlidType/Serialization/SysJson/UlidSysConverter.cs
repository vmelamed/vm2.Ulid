// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Val Melamed

namespace vm2.Serialization.SysJson;

using System.Text.Json.Serialization;

/// <summary>
/// Provides functionality to convert <see cref="Ulid"/> values to and from JSON format.
/// Implements the System.Text.Json.Serialization.<see cref="JsonConverter{T}"/>".
/// </summary>
/// <remarks>
/// This converter is used to serialize and deserialize <see cref="Ulid"/> values when working with JSON. It ensures that
/// <see cref="Ulid"/> instances are correctly represented as strings in JSON and parsed back into <see cref="Ulid"/> objects
/// during deserialization.
/// </remarks>
public class UlidSysConverter : JsonConverter<Ulid>
{
    /// <summary>
    /// Reads and converts the JSON representation of a ULID (Universally Unique Lexicographically Sortable Identifier).
    /// </summary>
    /// <param name="reader">The <see cref="Utf8JsonReader"/> to read the JSON data from.</param>
    /// <param name="_">The type of the object to convert. This parameter is ignored as this method always converts to a <see
    /// cref="Ulid"/>.</param>
    /// <param name="__">The serializer __ to use during deserialization. This parameter is not used in this implementation.</param>
    /// <returns>The <see cref="Ulid"/> value parsed from the JSON data.</returns>
    /// <exception cref="JsonException">Thrown if the JSON data does not represent a valid ULID.</exception>
    public override Ulid Read(ref Utf8JsonReader reader, Type _, JsonSerializerOptions __)
    {
        try
        {
            return TryParse(reader.ValueSpan, out var ulid)
                        ? ulid
                        : throw new JsonException("Could not parse ULID value.");
        }
        catch (Exception ex) when (ex is not JsonException)
        {
            throw new JsonException("Could not parse ULID value.", ex);
        }
    }

    /// <summary>
    /// Writes the specified <see cref="Ulid"/> value as a raw JSON string using the provided <see cref="Utf8JsonWriter"/>.
    /// </summary>
    /// <param name="writer">
    /// The <see cref="Utf8JsonWriter"/> to which the <see cref="Ulid"/> value will be written. Cannot be <c>null</c>.
    /// </param>
    /// <param name="value">The <see cref="Ulid"/> value to write.</param>
    /// <param name="_">The <see cref="JsonSerializerOptions"/> to use during serialization. This parameter is not used in this
    /// implementation but is required by the method signature.</param>
    public override void Write(
        Utf8JsonWriter writer,
        Ulid value,
        JsonSerializerOptions _)
    {
        Span<byte> utf8Chars = stackalloc byte[UlidStringLength];
        var success = value.TryWrite(utf8Chars, true);

        if (!success)
            throw new JsonException("Could not serialize ULID value.");

        writer.WriteStringValue(utf8Chars);
    }
}
