using Instant2D.Assets.Sprites;
using Instant2D.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.Assets.Loaders {
    /// <summary>
    /// Loads all sprites and generates the spritesheet using RectpackSharp library. Cool!
    /// </summary>
    public class DebugSpriteLoader : IAssetLoader {
        const string SPRITES_PATH = "Assets/sprites/";
        const int MAX_SPRITE_SPACE = 2048 * 2048;

        readonly List<SpriteManifest> _manifests = new(4);

        public IEnumerable<Asset> Load(AssetManager assets, LoadingProgress progress) {
            // find all sprite manifests
            foreach (var file in Directory.EnumerateFiles(SPRITES_PATH, "*.json")) {
                InstantGame.Instance.Logger.Info($"Reading SpriteManifest: '{Path.GetFileName(file)}'");

                try {
                    var manifest = JsonConvert.DeserializeObject<SpriteManifest>(File.ReadAllText(file), new SpriteManifest.Converter());
                    _manifests.Add(manifest);

                } catch (Exception ex) {
                    InstantGame.Instance.Logger.Error($"'{Path.GetFileName(file)}': " + ex.Message);
                }
            }


            yield break;


        }
    }
}
