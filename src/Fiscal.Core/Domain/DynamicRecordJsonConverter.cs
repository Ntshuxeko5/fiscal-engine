using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fiscal.Core.Domain
{
    /// <summary>
    /// Teaches System.Text.Json how to serialize DynamicRecord.
    /// Without this, the serializer sees only public properties on
    /// DynamicRecord (there are none) and outputs {}.
    /// This converter reads the private _fields dictionary via the
    /// public Get/Set API and writes each entry as a JSON property.
    /// </summary>
    public class DynamicRecordJsonConverter : JsonConverter<DynamicRecord>
    {
        public override DynamicRecord Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            var record = new DynamicRecord();

            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("Expected start of object.");
            }

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    return record;
                }

                string key = reader.GetString()
                    ?? throw new JsonException("Expected property name.");

                reader.Read();
                object? value = ReadValue(ref reader, options);
                record.Set(key, value);
            }

            return record;
        }

        public override void Write(
            Utf8JsonWriter writer,
            DynamicRecord value,
            JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            WriteRecord(writer, value, options);
            writer.WriteEndObject();
        }

        private static void WriteRecord(
            Utf8JsonWriter writer,
            DynamicRecord record,
            JsonSerializerOptions options)
        {
            // We expose the fields via a helper - add Keys property to DynamicRecord
            foreach (string key in record.Keys)
            {
                writer.WritePropertyName(key);
                WriteValue(writer, record.Get(key), options);
            }
        }

        private static void WriteValue(
            Utf8JsonWriter writer,
            object? value,
            JsonSerializerOptions options)
        {
            switch (value)
            {
                case null:
                    writer.WriteNullValue();
                    break;

                case DynamicRecord nested:
                    writer.WriteStartObject();
                    WriteRecord(writer, nested, options);
                    writer.WriteEndObject();
                    break;

                case List<DynamicRecord> list:
                    writer.WriteStartArray();
                    foreach (var item in list)
                    {
                        writer.WriteStartObject();
                        WriteRecord(writer, item, options);
                        writer.WriteEndObject();
                    }
                    writer.WriteEndArray();
                    break;

                case string s:
                    writer.WriteStringValue(s);
                    break;

                case bool b:
                    writer.WriteBooleanValue(b);
                    break;

                case decimal d:
                    writer.WriteNumberValue(d);
                    break;

                case int i:
                    writer.WriteNumberValue(i);
                    break;

                case double d:
                    writer.WriteNumberValue(d);
                    break;

                default:
                    // Fallback for any other type
                    JsonSerializer.Serialize(writer, value, value.GetType(), options);
                    break;
            }
        }

        private static object? ReadValue(
            ref Utf8JsonReader reader,
            JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.String => reader.GetString(),
                JsonTokenType.True => true,
                JsonTokenType.False => false,
                JsonTokenType.Number => reader.GetDecimal(),
                JsonTokenType.Null => null,
                JsonTokenType.StartObject => ReadRecord(ref reader, options),
                JsonTokenType.StartArray => ReadArray(ref reader, options),
                _ => null
            };
        }

        private static DynamicRecord ReadRecord(
            ref Utf8JsonReader reader,
            JsonSerializerOptions options)
        {
            var record = new DynamicRecord();
            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                string key = reader.GetString()
                    ?? throw new JsonException("Expected property name.");
                reader.Read();
                record.Set(key, ReadValue(ref reader, options));
            }
            return record;
        }

        private static List<DynamicRecord> ReadArray(
            ref Utf8JsonReader reader,
            JsonSerializerOptions options)
        {
            var list = new List<DynamicRecord>();
            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                if (reader.TokenType == JsonTokenType.StartObject)
                {
                    list.Add(ReadRecord(ref reader, options));
                }
            }
            return list;
        }
    }
}
