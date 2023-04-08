using Instant2D.Assets.Loaders;
using Instant2D.Coroutines;
using Instant2D.Graphics;
using Microsoft.Xna.Framework;

namespace Instant2D.Samples;

public class Instant2dSamples : InstantApp {
    public Instant2dSamples() {
        AddDefaultModules();

        AddModule<AssetManager>(assets => {
            assets.SetupHotReload();
            assets.AddLoader<SpriteLoader>();
            assets.AddLoader<SoundLoader>();
        });
    }

    protected override void Initialize() {
        Logger.Info("Hello, i2d!");
    }
}