using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System;

namespace Instant2D.Assets.Sprites {
    public enum SpriteOriginType {
        /// <summary>
        /// Manifest's default sprite origin.
        /// </summary>
        Default,

        /// <summary>
        /// Origin in floating point values between [0.0, 1.0]
        /// </summary>
        Normalized,

        /// <summary>
        /// Origin in integer pixel space.
        /// </summary>
        Absolute
    }

    public struct SpriteOrigin {
        public SpriteOriginType type;
        public Vector2 origin;

        public class Converter : JsonConverter<SpriteOrigin> {
            public override SpriteOrigin ReadJson(JsonReader reader, Type objectType, SpriteOrigin existingValue, bool hasExistingValue, JsonSerializer serializer) {
                if (reader.TokenType != JsonToken.StartArray)
                    return default;

                try {
                    reader.Read();

                    var type = reader.TokenType switch {
                        JsonToken.Integer => SpriteOriginType.Absolute,
                        JsonToken.Float => SpriteOriginType.Normalized,
                        _ => default
                    };

                    var origin = new Vector2(Convert.ToSingle(reader.Value), (float)reader.ReadAsDouble());

                    // skip EndArray
                    reader.Read();

                    return new() {
                        type = type,
                        origin = origin
                    };

                } catch (Exception ex) {
                    throw new InvalidOperationException("Couldn't parse SpriteOrigin.", ex);
                }
                
            }

            public override void WriteJson(JsonWriter writer, SpriteOrigin value, JsonSerializer serializer) {
                throw new NotImplementedException();
            }
        }
    }

}
