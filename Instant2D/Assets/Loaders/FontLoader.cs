using Instant2D.Assets.Fonts;
using Instant2D.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.Assets.Loaders;

public class FontLoader : IAssetLoader {
    public const string DIRECTORY = "fonts";

    public IEnumerable<Asset> Load(AssetManager assets) {
        foreach (var fntPath in assets.EnumerateFiles(DIRECTORY, "*.fnt", true)) {
            using var stream = assets.OpenStream(fntPath);
            using var reader = new StreamReader(stream);

            var key = $"{DIRECTORY}/{Path.GetFileNameWithoutExtension(fntPath)}";

            I2dFont font = default;

            try {
                font = FontParser.LoadFnt(reader.ReadToEnd());
            } catch (Exception ex) {
                InstantApp.Logger.Error($"Couldn't load '{key}': {ex.Message}");
                continue;
            }

            yield return new Asset<I2dFont>(key, font);
        }

    }
}
