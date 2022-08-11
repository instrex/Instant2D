using Instant2D.Assets.Containers;
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
                    manifest.Name = path;
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
                    key = key
                });
            }

            // produce assets
            foreach (var asset in ProcessAssets(_definitions))
                yield return asset;
        }

        IEnumerable<Asset> ProcessAssets(Dictionary<string, SpriteDef> definitions) {
            foreach (var (key, def) in definitions) {
                Asset baseAsset = def.animation != null ? new LazyAsset<SpriteAnimation>(key, this) : new LazyAsset<Sprite>(key, this);
                baseAsset.Data = def;

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
                        if (!TryGetPngSize(def.key, out var imageDimensions)) {
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

                // yield base asset now
                yield return baseAsset;
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
            // _textures.Add(texture);

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

        IEnumerable<string> IHotReloader.WatcherPatterns { get; } = new[] { "*.png", "*.json" };

        bool IHotReloader.TryReload(string assetKey, out IEnumerable<Asset> updatedAssets) {
            if (!assetKey.StartsWith(DIRECTORY + "/")) {
                updatedAssets = null;
                return false;
            }

            var extension = Path.GetExtension(assetKey);
            var key = assetKey.Replace(extension, "");

            // ooh boy 
            if (extension == ".json") {
                var oldManifest = _manifests.FirstOrDefault(m => m.Name == assetKey);

                var removedDefs = ListPool<SpriteDef>.Get();

                var changedDefs = new Dictionary<string, SpriteDef>();

                // wipe the old manifest from existence
                _manifests.Remove(oldManifest);
                foreach (var item in oldManifest.Items) {
                    var defKey = $"{DIRECTORY}/{item.key}";

                    // clear sprite defs, keeping track of updated ones
                    if (_definitions.Remove(defKey, out var removedDef)) {
                        removedDefs.Add(removedDef);

                        // try to remove assets too
                        AssetManager.Instance.Remove(defKey);
                    }
                }

                // read stream once again
                using var stream = AssetManager.Instance.OpenStream(assetKey);
                using var reader = new StreamReader(stream);

                // deserialize and save the manifest
                var manifest = JsonConvert.DeserializeObject<SpriteManifest>(reader.ReadToEnd(), new SpriteManifest.Converter());
                manifest.Name = assetKey;
                _manifests.Add(manifest);

                // save sprite definitions
                foreach (var item in manifest.Items) {
                    var defKey = $"sprites/{item.key}";
                    var newDef = item with {
                        manifest = manifest,
                        key = defKey,
                    };

                    _definitions.Add(defKey, newDef);

                    // if it's an existing def, add it into updated pool
                    if (removedDefs.Find(d => d.key == defKey) is SpriteDef changedDef) {
                        changedDefs.Add(defKey, newDef);
                        removedDefs.Remove(changedDef);
                    }
                }

                // clear removed defs
                removedDefs.ForEach(r => AssetManager.Instance.Remove(r.key));
                removedDefs.Pool();

                // re-add new sprite assets
                var newAssets = ProcessAssets(changedDefs).ToList();
                foreach (var asset in newAssets) {
                    AssetManager.Instance.Register(asset.Key, asset, false);
                }

                // output
                updatedAssets = newAssets;
                return true;
            }

            if (extension == ".png") {
                // if it's just an image, that'll be easy
                var container = AssetManager.Instance.GetContainer(assetKey.Replace(".png", "")) as LazyAsset;

                // if a new file is added, just skip
                if (container == null) {
                    updatedAssets = null;
                    return false;
                }

                // dispose of previous texture to free some space
                if (container.IsLoaded) {
                    switch (container) {
                        case IAssetContainer<Sprite> sprite:
                            sprite.Content.Texture.Dispose();
                            break;

                        case IAssetContainer<SpriteAnimation> spriteAnimation:
                            spriteAnimation.Content.Frames.FirstOrDefault().Texture?.Dispose();
                            break;
                    }
                }

                container.Unload();

                // add children
                updatedAssets = container.Children == null ?
                    new[] { container } :
                    container.Children.Prepend(container);

                return true;
            }

            updatedAssets = null;
            return false;
        }

        #endregion
    }
}
