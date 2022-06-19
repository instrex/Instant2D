using Instant2D.Utils.Serialization;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

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
                    serializer.Converters.Add(new ObjectArrayConverter());

                    var items = serializer.Deserialize<object[]>(reader);

                    // read frame number
                    if (items[0] is not int frame) {
                        throw new ArgumentException($"Expected event's frame number as first item.");
                    }

                    // pre-process the arguments
                    for (var i = 2; i < items.Length; i++) {

                        // replace some args with vectors
                        if (items[i] is object[] untypedArray && untypedArray.Length == 2) {
                            // replace int[2] with Vector2's
                            if (untypedArray[0] is int x && untypedArray[1] is int y)
                                items[i] = new Vector2(x, y);

                            // replace float[2] with Vector2's
                            if (untypedArray[0] is float fx && untypedArray[1] is float fy)
                                items[i] = new Vector2(fx, fy);
                        }
                    }

                    return new() {
                        frame = frame,
                        key = items[1] as string,
                        args = items[2..]
                    };

                } catch (Exception ex) {
                    throw new InvalidOperationException($"Couldn't parse AnimationEvent: {ex.Message}", ex);
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
