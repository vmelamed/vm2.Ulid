namespace vm2.NsJson;

using Newtonsoft.Json;

/// <summary>
/// Provides functionality to convert <see cref="Ulid"/> values to and from JSON format.
/// Implements the Newtonsoft.Json.<see cref="JsonConverter{T}"/>".
/// </summary>
/// <remarks>This converter is used to serialize and deserialize <see cref="Ulid"/> values in JSON format. It
/// ensures that <see cref="Ulid"/> instances are correctly represented as strings in JSON and parsed back into <see
/// cref="Ulid"/> objects during deserialization.</remarks>
public class UlidNsConverter : JsonConverter
{
    /// <summary>
    /// Determines whether the specified type can be converted to or from a <see cref="Ulid"/>.
    /// </summary>
    /// <param name="objectType">The type to evaluate for conversion compatibility.</param>
    /// <returns>
    /// <see langword="true"/> if the specified type is <see cref="Ulid"/> or <see cref="Nullable{Ulid}"/>; otherwise, <see langword="false"/>.
    /// </returns>
    public override bool CanConvert(Type objectType)
        => objectType == typeof(Ulid) || objectType == typeof(Ulid?);

    /// <summary>
    /// Writes the JSON representation of the specified object using the provided <see cref="JsonWriter"/>.
    /// </summary>
    /// <param name="writer">The <see cref="JsonWriter"/> used to write the JSON output. Cannot be <c>null</c>.</param>
    /// <param name="value">The object to serialize. Can be <c>null</c>, in which case a JSON null value is written.</param>
    /// <param name="serializer">The <see cref="JsonSerializer"/> used to customize the serialization process. Cannot be <c>null</c>.</param>
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value is null)
        {
            writer.WriteNull();
            return;
        }

        if (value is Ulid ulid)
        {
            writer.WriteValue(ulid.ToString());
            return;
        }

        throw new JsonWriterException($"Expected value to be of type {typeof(Ulid)} or null, but got {value.GetType()}.");
    }

    /// <summary>
    /// Reads JSON data from the specified <see cref="JsonReader"/> and converts it into an object of the specified
    /// type.
    /// </summary>
    /// <param name="reader">The <see cref="JsonReader"/> to read JSON data from.</param>
    /// <param name="objectType">The type of the object to deserialize the JSON data into.</param>
    /// <param name="existingValue">An existing object to populate with the JSON data, or <c>null</c> to create a new object.</param>
    /// <param name="serializer">The <see cref="JsonSerializer"/> used to deserialize the JSON data.</param>
    /// <returns>The deserialized object, or <c>null</c> if the JSON data is empty or cannot be deserialized.</returns>
    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        try
        {
            if (reader.TokenType is JsonToken.Null ||
                reader.Value is null ||
                reader.Value.ToString() is null)
                return null;

            if (reader.TokenType is not JsonToken.String)
                throw new JsonReaderException($"Expected token type to be {JsonToken.String} or {JsonToken.Null}, but got {reader.TokenType}.");

            return Parse(reader.Value.ToString()!);
        }
        catch (Exception ex) when (ex is not JsonReaderException)
        {
            throw new JsonReaderException("Could not parse ULID value.", ex);
        }
    }
}
