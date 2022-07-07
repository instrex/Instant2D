using Instant2D.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.Assets.Loaders {
    public abstract class FontLoader : IAssetLoader {
        public struct FontDescription {
            public int lineSpacing;
            public char defaultChar;
            public string[] sheets;
            public List<int[]> characters;
            public List<int[]> kerning;

            /// <summary>
            /// Creates a stream of glyphs using information provided by <see cref="characters"/>.
            /// </summary>
            public IEnumerable<InstantFont.Glyph> CreateGlyphs(Texture2D[] sheets) {
                for (var i = 0; i < characters.Count; i++) {
                    var info = characters[i];

                    // glyph descriptions might have an optional argument for page index
                    if (info.Length != 8 && info.Length != 9) {
                        throw new InvalidOperationException($"Couldn't load character #{i}: invalid description.");
                    }

                    yield return new InstantFont.Glyph {
                        character = (char)info[0],
                        sprite = new Sprite(sheets[info.Length == 9 ? info[8] : 0], new(info[1], info[2], info[3], info[4]), Vector2.Zero),
                        offset = new(info[5], info[6]),
                        advanceX = info[7]
                    };
                }
            }
        }

        /// <summary> Attempts to parse <see cref="FontDescription"/> from a json source file. </summary>
        public static bool TryParse(string json, out FontDescription desc) {
            // TODO: messy but idk what to do about it
            try { 
                desc = JsonConvert.DeserializeObject<FontDescription>(json);
                return true;
            } catch {
                desc = default;
                return false;
            }
        }

        public abstract IEnumerable<Asset> Load(AssetManager assets, LoadingProgress progress);
    }
}
