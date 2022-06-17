using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System;

namespace Instant2D.Assets.Sprites {
    public struct AnimationEvent {
        /// <summary>
        /// The frame on which the event(s) occurs.
        /// </summary>
        public int frame;

        /// <summary>
        /// Name of the event.
        /// </summary>
        public string key;

        /// <summary>
        /// Arguments to the event.
        /// </summary>
        public object[] args;

        public class Converter : JsonConverter<AnimationEvent> {
            public override AnimationEvent ReadJson(JsonReader reader, Type objectType, AnimationEvent existingValue, bool hasExistingValue, JsonSerializer serializer) {
                try {
                    var frame = reader.ReadAsInt32().Value;
                    reader.Read();

                    var args = serializer.Deserialize<object[]>(reader);
                    for (var i = 0; i < args.Length; i++) {
                        // replace float[2] with Vector2's
                        if (args[i] is float[] floatArr && floatArr.Length == 2)
                            args[i] = new Vector2(floatArr[0], floatArr[1]);

                        // replace int[2] with Vector2's
                        if (args[i] is int[] intArr && intArr.Length == 2)
                            args[i] = new Vector2(intArr[0], intArr[1]);
                    }

                    reader.Read();

                    return new() {
                        frame = frame,
                        key = args[0] as string,
                        args = args[1..]
                    };

                } catch (Exception ex) {
                    throw new InvalidOperationException("Couldn't parse AnimationEvent.", ex);
                }
            }

            public override void WriteJson(JsonWriter writer, AnimationEvent value, JsonSerializer serializer) {
                throw new NotImplementedException();
            }
        }
    }

    public struct SpriteAnimationDef {
        /// <summary>
        /// Animation speed in frames per second.
        /// </summary>
        public int fps;

        /// <summary>
        /// Per-frame events of this animation.
        /// </summary>
        public AnimationEvent[] events;
    }
}
