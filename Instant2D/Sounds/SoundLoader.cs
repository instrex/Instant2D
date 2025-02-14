using Instant2D.Assets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.Sounds;

public class SoundLoader : IAssetLoader, ILazyAssetLoader {
    /// <summary>
    /// Default sound folder.
    /// </summary>
    public string SoundDirectory { get; set; } = "sfx";

    /// <summary>
    /// Default music folder. Sounds from here will always be created as streamed instances.
    /// </summary>
    public string MusicDirectory { get; set; } = "music";

    IEnumerable<Asset> IAssetLoader.Load(AssetManager assets) {
        var oggPattern = "*.ogg";

        // get files from both folders
        var files = assets.EnumerateFiles(SoundDirectory, oggPattern, true)
            .Concat(assets.EnumerateFiles(MusicDirectory, oggPattern, true));

        foreach (var file in files) {
            yield return new LazyAsset<Sound>(file.Replace(".ogg", ""), this);
        }
    }

    void ILazyAssetLoader.LoadOnDemand(LazyAsset asset) {
        var assetPath = Path.ChangeExtension(asset.Key, "ogg");

        // read file contents
        using var stream = AssetManager.Instance.OpenStream(assetPath);
        var bytes = new byte[stream.Length];
        stream.Read(bytes, 0, bytes.Length);

        // load epic new sound
        (asset as LazyAsset<Sound>).Content = new Sound(bytes) { 
            Key = asset.Key,
            PreferStreaming = assetPath.Contains(MusicDirectory),
        };
    }
}