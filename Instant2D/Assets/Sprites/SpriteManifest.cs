﻿using Instant2D.Utils.Serialization;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.Assets.Sprites {
    /// <summary>
    /// A class that houses all the sprite information you've defined in 'sprites/*' asset folder.
    /// </summary>
    public class SpriteManifest {
        /// <summary>
        /// Default origin used for sprites that don't override it. Defaults to [0, 0].
        /// </summary>
        [JsonProperty("$default_origin")]
        public Vector2 DefaultOrigin { get; set; }

        /// <summary>
        /// Naming format used for sprites with multiple frames. Defaults to '{0}_{1}'.
        /// </summary>
        [JsonProperty("$frame_format")]
        public string FrameFormat { get; set; } = "{0}_{1}";

        /// <summary>
        /// All of the definitions explicitly defined in this manifest.
        /// </summary>
        public SpriteDef[] Items { get; set; }

        public class Converter : JsonConverter<SpriteManifest> {
            readonly List<SpriteDef> _spriteBuffer = new(64);
            public override SpriteManifest ReadJson(JsonReader reader, Type objectType, SpriteManifest existingValue, bool hasExistingValue, JsonSerializer serializer) {
                // setup converters
                serializer.Converters.Add(new RectangleConverter());
                serializer.Converters.Add(new Vector2Converter());
                serializer.Converters.Add(new AnimationEvent.Converter());
                serializer.Converters.Add(new SpriteOrigin.Converter());
                serializer.Converters.Add(new SpriteSplit.Converter());
                
                // skip '{'
                reader.Read();

                // create manifest
                var manifest = new SpriteManifest();
                var allowMetaElements = true;
                while (reader.TokenType == JsonToken.PropertyName) {
                    var key = (string) reader.Value;
                    reader.Read();

                    // process meta elements
                    if (key.StartsWith("$")) {
                        if (!allowMetaElements) {
                            throw new InvalidOperationException("Meta elements should be defined at the top of the manifest.");
                        }

                        switch (key) {
                            case "$default_origin":
                                manifest.DefaultOrigin = serializer.Deserialize<Vector2>(reader);
                                reader.Read();
                                break;

                            case "$frame_format":
                                manifest.FrameFormat = (string)reader.Value;
                                reader.Read();
                                break;
                        }

                        continue;
                    }

                    allowMetaElements = false;

                    var def = serializer.Deserialize<SpriteDef>(reader);
                    def.fileName ??= key;
                    def.key ??= key;

                    _spriteBuffer.Add(def);

                    reader.Read();
                }

                manifest.Items = _spriteBuffer.ToArray();
                _spriteBuffer.Clear();

                // skip '}'
                reader.Read();

                return manifest;
            }

            public override void WriteJson(JsonWriter writer, SpriteManifest value, JsonSerializer serializer) {
                throw new NotImplementedException();
            }
        }
    }
}
