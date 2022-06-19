using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Instant2D.Utils.Serialization {
    /// <summary>
    /// Simple converter used to parse dynamically typed arrays of primitive types.
    /// </summary>
    public class ObjectArrayConverter : JsonConverter<object[]> {
        public override object[] ReadJson(JsonReader reader, Type objectType, object[] existingValue, bool hasExistingValue, JsonSerializer serializer) {
            // skip '['
            reader.Read();

            var buffer = new List<object>();
            while (reader.TokenType != JsonToken.EndArray) {
                switch (reader.TokenType) {
                    case JsonToken.String:
                        buffer.Add((string)reader.Value);
                        break;

                    case JsonToken.Integer:
                        buffer.Add((int)(long)reader.Value);
                        break;

                    case JsonToken.Float:
                        buffer.Add((float)(double)reader.Value);
                        break;

                    case JsonToken.Null:
                        buffer.Add(null);
                        break;

                    case JsonToken.StartArray:
                        buffer.Add(serializer.Deserialize<object[]>(reader));
                        break;
                }

                reader.Read();
            }

            return buffer.ToArray();
        }

        public override void WriteJson(JsonWriter writer, object[] value, JsonSerializer serializer) {
            writer.WriteStartArray();
            foreach (var item in value) {
                serializer.Serialize(writer, item);
            }

            writer.WriteEndArray();
        }
    }
}
