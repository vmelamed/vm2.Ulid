namespace vm2.NsJson;

using Newtonsoft.Json;

/// <summary>
/// Provides functionality to convert <see cref="Ulid"/> values to and from JSON format.
/// Implements the Newtonsoft.Json.<see cref="JsonConverter{T}"/>".
/// </summary>
/// <remarks>This converter is used to serialize and deserialize <see cref="Ulid"/> values in JSON format. It
/// ensures that <see cref="Ulid"/> instances are correctly represented as strings in JSON and parsed back into <see
/// cref="Ulid"/> objects during deserialization.</remarks>
public class UlidNsConverter : JsonConverter<Nullable<Ulid>>
{
    /// <summary>
    /// Reads a JSON value and converts it into an instance of <see cref="Ulid"/>.
    /// </summary>
    /// <param name="reader">The <see cref="JsonReader"/> used to read the JSON data.</param>
    /// <param name="_">The type of the object to deserialize. This parameter is ignored in this implementation.</param>
    /// <param name="__">The existing <see cref="Ulid"/> value, if any. This parameter is ignored in this implementation.</param>
    /// <param name="___">A value indicating whether an existing value is provided. This parameter is ignored in this implementation.</param>
    /// <param name="____">The <see cref="JsonSerializer"/> used for deserialization.</param>
    /// <returns>The deserialized <see cref="Ulid"/> instance.</returns>
    public override Ulid? ReadJson(
        JsonReader reader,
        Type _,
        Ulid? __,
        bool ___,
        JsonSerializer ____)
    {
        try
        {
            if (reader.TokenType is JsonToken.Null)
                return null;

            var succeeded = TryParse(reader.Value?.ToString(), out var ulid);

            if (!succeeded)
                throw new JsonReaderException("Could not parse ULID value.");

            return ulid;
        }
        catch (Exception ex) when (ex is not JsonReaderException)
        {
            throw new JsonReaderException("Could not parse ULID value.", ex);
        }
    }

    /// <summary>
    /// Writes a ULID value to a JSON stream using the specified <see cref="JsonWriter"/>.
    /// </summary>
    /// <param name="writer">The <see cref="JsonWriter"/> used to write the JSON output. Cannot be <see langword="null"/>.</param>
    /// <param name="value">The <see cref="Ulid"/> value to write. Cannot be <see langword="null"/>.</param>
    /// <param name="serializer">The <see cref="JsonSerializer"/> used to customize the serialization process. Cannot be <see langword="null"/>.</param>
    public override void WriteJson(
        JsonWriter writer,
        Ulid? value,
        JsonSerializer serializer)
    {
        if (value is null)
        {
            writer.WriteNull();
            return;
        }

        writer.WriteValue(value.Value.ToString());
    }
}
