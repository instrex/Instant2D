using Instant2D.Utils;
using Instant2D.Utils.Serialization;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Instant2D.Assets.Sprites {
    public enum SpriteSplitOptions {
        None,

        /// <summary>
        /// Split options were defined as dimensions of the frame.
        /// </summary>
        BySize,

        /// <summary>
        /// Split options were defined as frame count.
        /// </summary>
        ByCount,

        /// <summary>
        /// Split options were defined manually.
        /// </summary>
        Manual
    }

    public struct ManualSplitItem {
        [JsonConverter(typeof(RectangleConverter))]
        public Rectangle rect;

        [JsonConverter(typeof(SpriteOrigin.Converter))]
        public SpriteOrigin origin;
        public string key;

        public class Converter : JsonConverter<ManualSplitItem> {
            public override ManualSplitItem ReadJson(JsonReader reader, Type objectType, ManualSplitItem existingValue, bool hasExistingValue, JsonSerializer serializer) {
                // inline rect definition
                if (reader.TokenType == JsonToken.StartArray) {
                    var rect = serializer.Deserialize<Rectangle>(reader);
                    return new ManualSplitItem { rect = rect };
                }

                return JToken.ReadFrom(reader).ToObject<ManualSplitItem>();
            }

            public override void WriteJson(JsonWriter writer, ManualSplitItem value, JsonSerializer serializer) {
                throw new NotImplementedException();
            }
        }
    }

    public struct SpriteSplit {
        public SpriteSplitOptions type;
        public int widthOrFrameCount, height;
        public ManualSplitItem[] manual;

        public class Converter : JsonConverter<SpriteSplit> {
            readonly List<ManualSplitItem> _manualSplitBuffer = new(12);
            public override SpriteSplit ReadJson(JsonReader reader, Type objectType, SpriteSplit existingValue, bool hasExistingValue, JsonSerializer serializer) {
                // that was easy...
                if (reader.TokenType == JsonToken.Integer) {
                    return new() { 
                        type = SpriteSplitOptions.ByCount, 
                        widthOrFrameCount = (int)(long)reader.Value 
                    };
                }
                
                // that should be pretty easy as well...
                if (reader.TokenType == JsonToken.StartArray) {
                    // skip the '['
                    reader.Read();

                    var x = (int)(long)reader.Value;

                    // skip the ','
                    reader.Read();

                    var y = (int)(long)reader.Value;

                    // skip the ']'
                    reader.Read();

                    return new() {
                        type = SpriteSplitOptions.BySize,
                        widthOrFrameCount = x,
                        height = y
                    };
                }

                // uhhh
                if (reader.TokenType == JsonToken.StartObject) {
                    var items = serializer.Deserialize<Dictionary<string, ManualSplitItem>>(reader);

                    _manualSplitBuffer.Clear();
                    _manualSplitBuffer.AddRange(items.Select(pair => pair.Value with { key = pair.Key }));

                    return new() {
                        type = SpriteSplitOptions.Manual,
                        manual = _manualSplitBuffer.ToArray()
                    };
                }

                return new();
            }

            public override void WriteJson(JsonWriter writer, SpriteSplit value, JsonSerializer serializer) {
                if (value.type == SpriteSplitOptions.Manual) {
                    serializer.Serialize(writer, value.manual.ToDictionary(i => i.key, i => i));
                    return;
                }

                writer.WriteRaw(value switch { 
                    { type: SpriteSplitOptions.ByCount, widthOrFrameCount: var count} => count.ToString(), 
                    { type: SpriteSplitOptions.BySize, widthOrFrameCount: var w, height: var h } => $"[{w}, {h}]", 
                    _ => "null"
                });
            }
        }
    }
}
