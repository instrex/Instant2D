using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.Utils.Serialization {
    public class RectangleConverter : JsonConverter<Rectangle> {
        public override Rectangle ReadJson(JsonReader reader, Type objectType, Rectangle existingValue, bool hasExistingValue, JsonSerializer serializer) {
            try {
                var arr = serializer.Deserialize<int[]>(reader);
                return new(arr[0], arr[1], arr[2], arr[3]);
            } catch (Exception ex) {
                throw new InvalidOperationException("Couldn't parse Rectangle.", ex);
            }
        }

        public override void WriteJson(JsonWriter writer, Rectangle value, JsonSerializer serializer) {
            writer.WriteStartArray();
            writer.WriteRaw($"{value.X}, {value.Y}, {value.Width}, {value.Height}");
            writer.WriteEndArray();
        }
    }
}
