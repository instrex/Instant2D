using Instant2D.Assets.Sprites;
using Instant2D.Utils.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Instant2D.Tests {
    [TestClass]
    public class SpriteManifestTests {
        [TestMethod]
        public void JsonConverters() {
            var json = @"
{ 
    ""a"": 123,
    ""b"": [1, 2],
    ""c"": { 
        ""X"": {
            ""rect"": [1, 2, 3, 4],
            ""origin"": [1, 2]
        },

        ""Y"": {
            ""rect"": [1, 2, 3, 4],
            ""origin"": [0.5, 0.5]
        }
    } 
}";

            var result = JsonConvert.DeserializeObject<Dictionary<string, SpriteSplit>>(json, 
                new SpriteOrigin.Converter(),
                new SpriteSplit.Converter(),
                new RectangleConverter()
            );

            var a = result["a"];
            var b = result["b"];
            var c = result["c"];

            Assert.IsTrue(a is SpriteSplit { 
                type: SpriteSplitOptions.ByCount, 
                widthOrFrameCount: 123 
            });

            Assert.IsTrue(b is SpriteSplit { 
                type: SpriteSplitOptions.BySize, 
                widthOrFrameCount: 1, 
                height: 2 
            });

            Assert.IsTrue(c.type == SpriteSplitOptions.Manual);
            Assert.IsTrue(c.manual.Length == 2);
            Assert.IsTrue(c.manual[0] is ManualSplitItem { 
                key: "X",
                origin.type: SpriteOriginType.Absolute
            } x && x.rect == new Rectangle(1, 2, 3, 4) && x.origin.origin == new Vector2(1, 2));

            Assert.IsTrue(c.manual[1] is ManualSplitItem {
                key: "Y",
                origin: { type: SpriteOriginType.Normalized }
            } y && y.rect == new Rectangle(1, 2, 3, 4) && y.origin.origin == new Vector2(0.5f, 0.5f));

            var ev = JsonConvert.DeserializeObject<AnimationEvent>(@"[1, [""event"", 5, [1, 2, 3], ""argument""]]",
                new SpriteOrigin.Converter(),
                new SpriteSplit.Converter(),
                new AnimationEvent.Converter(),
                new RectangleConverter()
            );
        }

        [TestMethod]
        public void SampleManifest() {
            var obj = JsonConvert.DeserializeObject<SpriteManifest>(Regex.Replace(File.ReadAllText(@"C:\Users\instrex\Desktop\instant2d\sprite_manifest_demo.jsonc"), @"^\s*//.*$", "", RegexOptions.Multiline), new SpriteManifest.Converter());
        }
    }
}