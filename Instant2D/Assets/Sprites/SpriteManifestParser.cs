using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Instant2D.Assets.Sprites {
    public static class SpriteManifestParser {
        public static SpriteManifest Parse(string name, string json) {
            var node = JsonNode.Parse(json, documentOptions: new() {
                CommentHandling = System.Text.Json.JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            });

            var buffer = new Dictionary<string, SpriteDefinition>();
            var defaultNamingFormat = SpriteManifest.DefaultNamingFormat;
            var defaultOrigin = SpriteManifest.DefaultSpriteOrigin;

            var obj = node.AsObject();
            foreach (var (key, value) in obj) {
                if (key.StartsWith('$')) {
                    switch (key) {
                        default: throw new InvalidOperationException($"Invalid property '{key}'.");

                        case "$default_origin":
                            defaultOrigin = ParseVector2(value);
                            break;

                        case "$name_format":
                            defaultNamingFormat = value.GetValue<string>();
                            break;
                    }

                    continue;
                }

                if (value is not JsonObject defObj)
                    throw new InvalidOperationException("Sprite definition should be an object.");

                var def = ParseDefinition(key, defObj);

                if (def.Inherit != null) {
                    if (!buffer.TryGetValue(def.Inherit, out var other))
                        throw new InvalidOperationException($"Attempted to inherit unknown sprite definition '{def.Inherit}' for '{def.Key}'. It should be defined before this one in order for it to be copied.");

                    def = def with {
                        Origin = def.Origin == default ? other.Origin : def.Origin,
                        Animation = def.Animation == default ? other.Animation : def.Animation,
                        SplitOptions = def.SplitOptions == default ? other.SplitOptions : def.SplitOptions,
                        Points = def.Points != null ? def.Points.Concat(other.Points).ToDictionary(k => k.Key, v => v.Value) : other.Points
                    };
                }

                buffer.Add(def.Key, def);
            }

            return new SpriteManifest {
                Key = name,
                Items = buffer.Values.ToArray(),
                SpriteOrigin = defaultOrigin,
                NamingFormat = defaultNamingFormat
            };
        }

        internal static SpriteDefinition ParseDefinition(string key, JsonObject obj) {
            string inheritDef = default;
            if (obj["inherit"] is JsonValue inheritValue && !inheritValue.TryGetValue(out inheritDef)) {
                throw new InvalidOperationException("'inherit' property should be a string key of another definition to copy.");
            }

            return new() {
                Key = key,
                Inherit = inheritDef,
                Animation = obj["animation"] is JsonNode animNode ? ParseAnimation(animNode) : null,
                Points = obj["points"] is JsonNode pointsNode ? ParseSpritePoints(pointsNode) : null,
                Origin = obj["origin"] is JsonNode originNode ? ParseOrigin(originNode) : default,
                SplitOptions = obj["split"] is JsonNode splitNode ? ParseSplit(splitNode) : null
            };
        }

        internal static SpriteDefinition.SpriteSplitOptions ParseSplit(JsonNode node) {
            if (node is JsonArray array) {
                if (array.Count != 2 || !array[0].AsValue().TryGetValue(out int width) || !array[1].AsValue().TryGetValue(out int height))
                    throw new InvalidOperationException($"Expected number of rows and columns to split.");

                return new(SpriteDefinition.SplitType.BySize, width, height, null);
            }

            if (node is JsonObject obj) {
                var list = new List<SpriteDefinition.SubSprite>(); // TODO: pool this
                foreach (var (key, val) in obj) {
                    list.Add(ParseSubSprite(key, val));
                }

                return new(SpriteDefinition.SplitType.BySubSprites, 0, 0, list.ToArray());
            }

            if (node is JsonValue value && value.TryGetValue(out int frameCount)) {
                return new(SpriteDefinition.SplitType.ByCount, frameCount, 0, null);
            }

            throw new InvalidOperationException($"Invalid split options, should either be a number indicating how many frames are there vertically; an array with the frame size, or a sub sprite collection.");
        }

        internal static SpriteDefinition.SubSprite ParseSubSprite(string key, JsonNode node) {
            if (node is JsonArray) {
                return new() {
                    Region = ParseRect(node),
                    Key = key,
                };
            }

            var obj = node.AsObject();

            if (obj["region"] is not JsonArray regionArray)
                throw new InvalidOperationException("Sub sprites require 'region' property containing their source rectangle relative to parent sprite.");

            var region = ParseRect(regionArray);

            SpriteDefinition.OriginDefinition origin = new();
            if (obj["origin"] is JsonNode originNode) {
                origin = ParseOrigin(originNode);
            }

            return new() {
                Key = key,
                Region = region,
                Origin = origin
            };
        }

        internal static SpriteDefinition.OriginDefinition ParseOrigin(JsonNode node) {
            if (node is not JsonArray array || array.Count != 2)
                throw new InvalidOperationException($"'origin' property requires an array of two numbers.");

            var (a, b) = (array[0].AsValue(), array[1].AsValue());

            return new() {
                OriginValue = new((float)a, (float)b),

                // if any of the values is float, origin is probably normalized
                IsNormalized = !a.TryGetValue<int>(out _) || !b.TryGetValue<int>(out _)
            };
        }

        internal static SpriteDefinition.AnimationEvent ParseAnimationEvent(JsonNode node) {
            if (node is not JsonArray arr || arr.Count < 2)
                throw new InvalidOperationException("Animation event should be an array containing: frame index, event key and optional arguments.");

            var frameIndex = arr[0].GetValue<int>();
            var key = arr[1].GetValue<string>();
            var args = ParsePrimitiveArray(arr).Skip(2).ToArray();

            return new() {
                FrameIndex = frameIndex,
                Args = args,
                Key = key,
            };
        }

        internal static SpriteDefinition.AnimationDefinition ParseAnimation(JsonNode node) {
            if (node is not JsonObject obj)
                throw new InvalidOperationException("Animation should be an object containing 'fps' and optionally 'events' properties.");

            if (obj["fps"] is not JsonValue fpsNode || !fpsNode.TryGetValue<int>(out var fps))
                throw new InvalidOperationException("Animation object should have integer 'fps' property defined.");

            SpriteDefinition.AnimationEvent[] events = default;

            if (obj["events"] is JsonNode eventsNode) {
                if (eventsNode is not JsonArray eventsArray)
                    throw new InvalidOperationException("Animation 'events' property should be an array.");

                var buffer = new List<SpriteDefinition.AnimationEvent>(); // TODO: pool this as well
                foreach (var value in eventsArray) {
                    buffer.Add(ParseAnimationEvent(value));
                }

                events = buffer.ToArray();
            }

            return new SpriteDefinition.AnimationDefinition {
                Fps = fps,
                Events = events
            };
        }

        internal static object[] ParsePrimitiveArray(JsonNode node) {
            if (node is not JsonArray array)
                throw new InvalidOperationException($"Expected an array.");

            var buffer = new List<object>(); // TODO: pool this too
            foreach (var item in array) {
                if (item is JsonArray arr) {
                    switch (arr.Count) {
                        default:
                            // parse recursive array
                            buffer.Add(ParsePrimitiveArray(arr));
                            break;

                        // parse vector2
                        case 2 when arr[0].AsValue().TryGetValue<float>(out var x) && arr[1].AsValue().TryGetValue<float>(out var y):
                            buffer.Add(new Vector2(x, y));
                            break;

                        // parse rectangle
                        case 4 when arr[0].AsValue().TryGetValue<int>(out var x) && arr[1].AsValue().TryGetValue<int>(out var y) &&
                            arr[2].AsValue().TryGetValue<int>(out var w) && arr[3].AsValue().TryGetValue<int>(out var h):
                            buffer.Add(new Rectangle(x, y, w, h));
                            break;
                    }

                    continue;
                }

                if (item is null) {
                    buffer.Add(null);
                    continue;
                }

                if (item is not JsonValue value) {
                    throw new InvalidOperationException($"Objects are not supported in primitive arrays.");
                }

                if (value.TryGetValue<int>(out var i)) {
                    buffer.Add(i);
                } else if (value.TryGetValue<float>(out var f)) {
                    buffer.Add(f);
                } else if (value.TryGetValue<bool>(out var b)) {
                    buffer.Add(b);
                } else if (value.TryGetValue<string>(out var str)) {
                    buffer.Add(str);
                } else {
                    throw new InvalidOperationException($"Value type is not supported.");
                }
            }

            return buffer.ToArray();
        }

        internal static Dictionary<string, Point> ParseSpritePoints(JsonNode node) {
            if (node is not JsonObject pointsObj)
                throw new InvalidOperationException($"'points' property should be an object.");

            return pointsObj.Select(kv => new KeyValuePair<string, Point>(kv.Key, ParsePoint(kv.Value)))
                .ToDictionary(k => k.Key, v => v.Value);
        }

        internal static Point ParsePoint(JsonNode node) {
            if (node is not JsonArray array || array.Count != 2)
                throw new InvalidOperationException($"Expected an array of 2 integer numbers.");

            return new((int)array[0], (int)array[1]);
        }

        internal static Vector2 ParseVector2(JsonNode node) {
            if (node is not JsonArray array || array.Count != 2)
                throw new InvalidOperationException($"Expected an array of 2 float numbers.");

            return new((float)array[0], (float)array[1]);
        }

        internal static Rectangle ParseRect(JsonNode node) {
            if (node is not JsonArray array || array.Count != 4)
                throw new InvalidOperationException($"Expected an array of 4 integer numbers.");

            return new((int)array[0], (int)array[1], (int)array[2], (int)array[3]);
        }
    }
}
