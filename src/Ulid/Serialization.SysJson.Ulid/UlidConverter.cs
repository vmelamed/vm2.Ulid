// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Val Melamed

namespace vm2.Serialization.SysJson.Ulid;

/// <summary>
/// Provides functionality to convert <see cref="vm2.Ulid"/> values to and from JSON format.
/// Implements the System.Text.Json.Serialization.<see cref="JsonConverter{T}"/>".
/// </summary>
/// <remarks>
/// This converter is used to serialize and deserialize <see cref="vm2.Ulid"/> values when working with JSON. It ensures that
/// <see cref="vm2.Ulid"/> instances are correctly represented as strings in JSON and parsed back into <see cref="vm2.Ulid"/> objects
/// during deserialization.
/// </remarks>
public class UlidConverter : JsonConverter<vm2.Ulid>
{
    /// <summary>
    /// Writes the specified <see cref="vm2.Ulid"/> value as a raw JSON string using the provided <see cref="Utf8JsonWriter"/>.
    /// </summary>
    /// <param name="writer">
    /// The <see cref="Utf8JsonWriter"/> to which the <see cref="vm2.Ulid"/> value will be written. Cannot be <c>null</c>.
    /// </param>
    /// <param name="value">The <see cref="vm2.Ulid"/> value to write.</param>
    /// <param name="_">The <see cref="JsonSerializerOptions"/> to use during serialization. This parameter is not used in this
    /// implementation but is required by the method signature.</param>
    public override void Write(
        [NotNull] Utf8JsonWriter writer,
        vm2.Ulid value,
        JsonSerializerOptions _)
    {
        ArgumentNullException.ThrowIfNull(writer, nameof(writer));

        Span<byte> utf8Chars = stackalloc byte[UlidStringLength];

        if (!value.TryWriteUtf8(utf8Chars))
            // Debug.Assert(false, "This should never happen because Ulid.TryWrite should only return false if the buffer is too small, and we are providing a buffer of the correct size.");
            throw new JsonException("Could not serialize ULID value.");

        writer.WriteStringValue(utf8Chars);
    }

    /// <summary>
    /// Reads and converts the JSON representation of a ULID (Universally Unique Lexicographically Sortable Identifier).
    /// </summary>
    /// <param name="reader">The <see cref="Utf8JsonReader"/> to read the JSON data from.</param>
    /// <param name="_">The type of the object to convert. This parameter is ignored as this method always converts to a <see
    /// cref="vm2.Ulid"/>.</param>
    /// <param name="__">The serializer __ to use during deserialization. This parameter is not used in this implementation.</param>
    /// <returns>The <see cref="vm2.Ulid"/> value parsed from the JSON data.</returns>
    /// <exception cref="JsonException">Thrown if the JSON data does not represent a valid ULID.</exception>
    public override vm2.Ulid Read(
        ref Utf8JsonReader reader,
        Type _,
        JsonSerializerOptions __)
            => TryParse(reader.ValueSpan, out var ulid) ? ulid : throw new JsonException("Could not parse ULID value.");
}
