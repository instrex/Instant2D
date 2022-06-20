using Instant2D.Assets.Sprites;
using Instant2D.Core;
using Instant2D.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Instant2D.Assets.Loaders {
    /// <summary>
    /// Implementation of <see cref="SpriteLoader"/> used for development, has spritesheet generation and lazy loading support. <br/>
    /// By default, loads each separate <see cref="Sprite"/> as its own <see cref="Texture2D"/> on-demand.
    /// </summary>
    public class DevSpriteLoader : SpriteLoader, ILazyAssetLoader {
        enum LoaderMode { 
            LoadOnDemand,
            GenerateSpritesheet
        }

        LoaderMode _mode;
        int _maxSpritesheetSize;
        TexturePacker.Page[] _pages;
        List<Texture2D> _loadedTextures;

        /// <summary>
        /// Flips the loader mode to spritesheet mode: the initial or subsequent loading times will be longer, but the rendering performance should improve.
        /// </summary>
        public DevSpriteLoader SetSpritesheetMode(int maxSpritesheetSize = 2048) {
            if (!System.Numerics.BitOperations.IsPow2(maxSpritesheetSize)) {
                InstantGame.Instance.Logger.Warning($"Spritesheet size should preferably be power of 2, {maxSpritesheetSize}x{maxSpritesheetSize} was provided instead.");
            }

            _mode = LoaderMode.GenerateSpritesheet;
            _maxSpritesheetSize = maxSpritesheetSize;

            return this;
        }

        protected override IEnumerable<SpriteManifest> LoadManifests(AssetManager assets) {
            foreach (var file in Directory.EnumerateFiles(SPRITES_PATH, "*.json", SearchOption.AllDirectories)) {
                InstantGame.Instance.Logger.Info($"Reading SpriteManifest: '{Path.GetFileName(file)}'");
                if (TryParseManifest(File.ReadAllText(file), out var manifest, Path.GetFileName(file)))
                    yield return manifest;
            }
        }

        protected override IEnumerable<SpriteDef> LoadAdditionalDefinitions(AssetManager assets) {
            foreach (var file in Directory.EnumerateFiles(SPRITES_PATH, "*.png", SearchOption.AllDirectories)) {
                var key = file[(file.IndexOf("sprites/") + 8)..]
                    .Replace(".png", "")
                    .Replace('\\', '/');

                // add an empty definition for sprites not defined in any manifests
                if (!SpriteDefinitions.ContainsKey(key)) {
                    yield return new SpriteDef {
                        fileName = key,
                        key = key
                    };
                }
            }
        }

        protected override IEnumerable<Asset> LoadSpriteAssets(AssetManager assets) {
            // generate spritesheets when needed
            if (_mode == LoaderMode.GenerateSpritesheet) {
                InstantGame.Instance.Logger.Info($"Preparing to generate spritesheets...");
                
                // pack the textures to sprite pages
                var textures = LoadTextures().ToArray();
                _pages = new TexturePacker(textures, _maxSpritesheetSize).GetPages();

                // generate assets
                foreach (var page in _pages) {
                    foreach (var packed in page.textures) {
                        var def = SpriteDefinitions[packed.tex.Tag as string];

                        // process the def and obtain the assets
                        var result = ProcessSpriteDef(def, page.sheet, packed.rect, out var animation);
                        foreach (var sprite in result) {
                            yield return new Asset<Sprite>(sprite.Key, sprite);
                        }

                        // save the animation as its own asset first
                        if (animation is SpriteAnimation spriteAnimation) {
                            yield return new Asset<SpriteAnimation>(def.key, spriteAnimation);
                        }
                    }
                }

                // free some memory
                InstantGame.Instance.Logger.Info($"Freeing textures...");
                foreach (var tex in textures) {
                    tex.Dispose();
                }

                yield break;
            }

            // lazily load all assets when on-demand mode is set
            foreach (var (_, def) in SpriteDefinitions) {
                var size = def.split.type == SpriteSplitOptions.BySize && TryGetPngSize(def.fileName, out var dim) ? dim : new(0, 0);

                // get and map the data to typed LazyAssets for later retrieval
                var data = GetProducedAssetData(def, size)
                    .Select<(AssetOutputType type, string key), LazyAsset>(info => info.type switch {
                        AssetOutputType.Animation => new LazyAsset<SpriteAnimation>(info.key, this),
                        _ => new LazyAsset<Sprite>(info.key, this)
                    }).ToArray();

                for (var i = 0; i < data.Length; i++) {
                    if (i > 0) {
                        // set dependent assets
                        data[i].DependsOn = data[0];
                    }

                    yield return data[i];
                }
            }
        }

        public void LoadOnDemand(LazyAsset asset) {
            _loadedTextures ??= new(64);

            using var fileStream = TitleContainer.OpenStream(Path.Combine(SPRITES_PATH, asset.Key + ".png"));

            // open the stream for asset data retrieval
            var tex = Texture2D.FromStream(InstantGame.Instance.GraphicsDevice, fileStream);
            _loadedTextures.Add(tex);

            // proccess the spritedef and load the sprite back to the asset
            var sprites = ProcessSpriteDef(SpriteDefinitions[asset.Key], tex, new(0, 0, tex.Width, tex.Height), out var animationDef);

            // if asset is a sprite, simply return it
            if (asset is LazyAsset<Sprite> spriteAsset) {
                spriteAsset.Content = sprites.FirstOrDefault();
                return;
            }

            // load the sprite animation on-demand
            if (asset is LazyAsset<SpriteAnimation> animationAsset) {
                if (animationDef is not SpriteAnimation anim) {
                    InstantGame.Instance.Logger.Error($"Couldn't load animation '{asset.Key}', skipping.");
                    return;
                }

                animationAsset.Content = anim;
            }
        }

        List<Texture2D> LoadTextures() {
            var textures = new List<Texture2D>(SpriteDefinitions.Count);
            foreach (var (key, def) in SpriteDefinitions) {
                using var fileStream = TitleContainer.OpenStream(Path.Combine(SPRITES_PATH, key + ".png"));

                // load the texture and set its tag to the key of SpriteDef
                var tex = Texture2D.FromStream(InstantGame.Instance.GraphicsDevice, fileStream);
                tex.Tag = key;

                // save the texture in a buffer
                textures.Add(tex);

                // occasionally notify of the progress (each 10%)
                if (textures.Count % Math.Max(textures.Capacity / 10, 10) == 0) {
                    InstantGame.Instance.Logger.Info($"Loading textures... {textures.Count / (float)SpriteDefinitions.Count:p2}");
                }
            }

            return textures;
        }

        static bool TryGetPngSize(string key, out Point size) {
            try {
                using var fileStream = TitleContainer.OpenStream(Path.Combine(SPRITES_PATH, key, ".png"));
                using var reader = new BinaryReader(fileStream);
                reader.BaseStream.Seek(16, SeekOrigin.Begin);

                // read PNG metadata
                Span<byte> buffer = stackalloc byte[sizeof(int)];
                for (var i = 0; i < sizeof(int); i++) {
                    buffer[sizeof(int) - 1 - i] = reader.ReadByte();
                }

                var width = BitConverter.ToInt32(buffer);
                for (var i = 0; i < sizeof(int); i++) {
                    buffer[sizeof(int) - 1 - i] = reader.ReadByte();
                }

                var height = BitConverter.ToInt32(buffer);
                size = new(width, height);

                return true;

            } catch {
                size = new(0, 0);
                return false;
            }
        }
    }
}
