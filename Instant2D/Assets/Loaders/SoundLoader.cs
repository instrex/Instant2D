using Instant2D.Audio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.Assets.Loaders {
    public class SoundLoader : IAssetLoader {
        const string SOUND_DIRECTORY = "sfx";
        const string MUSIC_DIRECTORY = "music";

        public IEnumerable<Asset> Load(AssetManager assets) {
            var manager = InstantApp.Instance.GetSystem<AudioManager>();

            // search both sfx and music folders for sounds
            foreach (var filename in assets.EnumerateFiles(SOUND_DIRECTORY, "*.ogg", true).Concat(assets.EnumerateFiles(MUSIC_DIRECTORY, "*.ogg", true))) {
                using var stream = assets.OpenStream(filename);
                var data = new byte[stream.Length];
                stream.Read(data);

                // add the sound now
                yield return new Asset<Sound>(filename.Replace(".ogg", ""), new Sound {
                    Manager = manager,
                    Data = data
                });
            }
        }
    }
}
