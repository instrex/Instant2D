using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System;

namespace Instant2D.Utils.Serialization {
    public class Vector2Converter : JsonConverter<Vector2> {
        public override Vector2 ReadJson(JsonReader reader, Type objectType, Vector2 existingValue, bool hasExistingValue, JsonSerializer serializer) {
            try {
                var arr = serializer.Deserialize<float[]>(reader);
                return new(arr[0], arr[1]);
            } catch (Exception ex) {
                throw new InvalidOperationException("Couldn't parse Vector2.", ex);
            }
        }

        public override void WriteJson(JsonWriter writer, Vector2 value, JsonSerializer serializer) {
            writer.WriteRaw($"[{value.X}, {value.Y}]");
        }
    }
}
