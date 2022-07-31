using Instant2D.Assets.Sprites;
using Instant2D.Core;
using Instant2D.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace Instant2D.Assets.Loaders {
    public class SpriteLoader : IAssetLoader, ILazyAssetLoader, IHotReloader {
        const string DIRECTORY = "sprites";

        readonly Dictionary<string, SpriteDef> _definitions = new();
        readonly List<SpriteManifest> _manifests = new();
        readonly List<Texture2D> _textures = new();

        public IEnumerable<Asset> Load(AssetManager assets) {
            // load all json sprite manifests
            foreach (var path in assets.EnumerateFiles(DIRECTORY, "*.json", true)) {
                try {
                    using var stream = assets.OpenStream(path);
                    using var reader = new StreamReader(stream);

                    // deserialize and save the manifest
                    var manifest = JsonConvert.DeserializeObject<SpriteManifest>(reader.ReadToEnd(), new SpriteManifest.Converter());
                    _manifests.Add(manifest);

                    // save sprite definitions
                    foreach (var item in manifest.Items) {
                        var key = $"sprites/{item.key}";
                        _definitions.Add(key, item with {
                            manifest = manifest,
                            key = key,
                        });
                    }

                } catch (Exception ex) {
                    Logger.WriteLine($"Could not load sprite manifest '{Path.GetFileName(path)}': {ex.Message}", Logger.Severity.Error);
                }
            }

            // load stray sprites not defined by manifests
            foreach (var path in assets.EnumerateFiles(DIRECTORY, "*.png", true)) {
                var key = path.Replace(".png", "");

                // don't replace existing defs
                if (_definitions.ContainsKey(key))
                    continue;

                _definitions.TryAdd(key, new SpriteDef {
                    fileName = key,
                    key = key
                });
            }

            // produce assets
            foreach (var (key, def) in _definitions) {
                Asset baseAsset = def.animation != null ? new LazyAsset<SpriteAnimation>(key, this) : new LazyAsset<Sprite>(key, this);
                baseAsset.Data = def;

                // yield base asset now
                yield return baseAsset;

                // switch blocks are dumb ;-;
                var i = 0;

                // produce additional child assets from animations or splits
                switch (def.split) {
                    // manual sprite split
                    case { type: SpriteSplitOptions.Manual, manual: var manualSplit }:
                        for (i = 0; i < manualSplit.Length; i++) {
                            var child = new LazyAsset<Sprite>(def.FormatFrameKey(manualSplit[i].key), this) {
                                // save the data for later processing
                                Data = manualSplit[i]
                            };

                            baseAsset.AddChild(child);

                            yield return child;
                        }

                        break;

                    // split using frame cound
                    case { type: SpriteSplitOptions.ByCount, widthOrFrameCount: var count }:
                        for (i = 0; i < count; i++) {
                            var child = new LazyAsset<Sprite>(def.FormatFrameKey(i.ToString()), this);
                            baseAsset.AddChild(child);

                            yield return child;
                        }

                        break;

                    // split using frame size (requires image dimensions)
                    case { type: SpriteSplitOptions.BySize, widthOrFrameCount: var width, height: var height }:
                        if (!TryGetPngSize(def.fileName, out var imageDimensions)) {
                            Logger.WriteLine($"Couldn't get image dimensions for sprite '{key}'", Logger.Severity.Warning);
                            break;
                        }

                        // crop the image by width x height 
                        for (var x = 0; x < imageDimensions.X; x += width) {
                            for (var y = 0; y < imageDimensions.Y; y += height) {
                                var child = new LazyAsset<Sprite>(def.FormatFrameKey(i++.ToString()), this) {
                                    // calculate the source rect while at it
                                    Data = new Rectangle(x, y, width, height)
                                };

                                baseAsset.AddChild(child);

                                yield return child;
                            }
                        }

                        break;
                }
            }
        }

        public void LoadOnDemand(LazyAsset asset) {
            // the parent should take care of loading
            if (asset.Parent is LazyAsset parent) {
                parent.Load();
                return;
            }

            // load the texture
            using var stream = AssetManager.Instance.OpenStream(asset.Key + ".png");
            var texture = Texture2D.FromStream(InstantGame.Instance.GraphicsDevice, stream);
            texture.Tag = asset.Key;

            // make cool sprite
            var def = asset.Data as SpriteDef;
            var sprite = new Sprite(texture, new Rectangle(0, 0, texture.Width, texture.Height), new Vector2(texture.Width, texture.Height) * 0.5f, asset.Key);

            // obtain the framebuffer when animation is needed
            var frameBuffer = def.animation != null && def.split.type != SpriteSplitOptions.Manual ? ListPool<Sprite>.Get() : null;

            // process children
            if (asset.Children != null) {
                for (var i = 0; i < asset.Children.Count; i++) {
                    var child = asset.Children[i] as LazyAsset<Sprite>;
                    switch (child.Data) {
                        // split by size produces source rects in Load phase
                        case Rectangle rect:
                            var splitBySizeSprite = new Sprite(texture, rect, SpriteDef.TransformOrigin(def.origin, rect, def.manifest), child.Key);
                            child.Content = splitBySizeSprite;
                            frameBuffer?.Add(splitBySizeSprite);
                            break;

                        // manual split comes packaged with a nice struct
                        case ManualSplitItem split:
                            child.Content = new Sprite(texture, split.rect, SpriteDef.TransformOrigin(split.origin, split.rect, def.manifest, def.origin), child.Key);
                            break;

                        // and what does split by count get?
                        default:
                            var height = texture.Height / asset.Children.Count;
                            var newRect = new Rectangle(0, height * i, texture.Width, height);
                            var splitByCountSprite = new Sprite(texture, newRect, SpriteDef.TransformOrigin(def.origin, newRect, def.manifest));
                            child.Content = splitByCountSprite;
                            frameBuffer?.Add(splitByCountSprite);
                            break;
                    }
                }
            }

            // add the texture for later disposal
            _textures.Add(texture);

            // save asset data
            switch (asset) {
                default:
                    Logger.WriteLine($"Unknown asset type '{asset.GetType().Name}' encountered by SpriteLoader", Logger.Severity.Warning);
                    break;

                case LazyAsset<SpriteAnimation> animationAsset when def.animation is SpriteAnimationDef animation:
                    animationAsset.Content = new SpriteAnimation(animation.fps, frameBuffer.ToArray(), animation.events, def.key);
                    break;

                case LazyAsset<Sprite> spriteAsset:
                    spriteAsset.Content = sprite;
                    break;
            }
        }

        static bool TryGetPngSize(string key, out Point size) {
            try {
                using var fileStream = AssetManager.Instance.OpenStream(key + ".png");
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

        #region Hot Reloading

        IEnumerable<string> IHotReloader.GetFileFormats() {
            yield return ".json";
            yield return ".png";
        }

        bool IHotReloader.TryReload(string assetKey, string filename) {
            throw new NotImplementedException();
        }

        #endregion
    }
}
