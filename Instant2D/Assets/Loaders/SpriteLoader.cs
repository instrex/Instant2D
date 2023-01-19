using Instant2D.Assets.Containers;
using Instant2D.Assets.Sprites;
using Instant2D.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using static Instant2D.Assets.Sprites.SpriteDefinition;

namespace Instant2D.Assets.Loaders {
    public class SpriteLoader : IAssetLoader, ILazyAssetLoader, IHotReloader {
        const string DIRECTORY = "sprites";

        readonly Dictionary<string, SpriteManifest> _manifests = new();
        readonly Dictionary<string, SpriteDefinition[]> _spritesByManifest = new();
        readonly List<SpriteDefinition> _strayDefinitions = new();

        public IEnumerable<Asset> Load(AssetManager assets) {
            var definedKeys = new HashSet<string>();
            foreach (var path in assets.EnumerateFiles(DIRECTORY, "*.json", true)) {
                try {
                    if (_spritesByManifest.ContainsKey(path)) {
                        // this probably shouldn't happen... ?
                        throw new InvalidOperationException($"Manifest with a similar name '{path}' has already been defined.");
                    }

                    using var stream = assets.OpenStream(path);
                    using var reader = new StreamReader(stream);

                    var manifest = SpriteManifestParser.Parse(path, reader.ReadToEnd());
                    _spritesByManifest.Add(manifest.Key, manifest.Items);
                    _manifests.Add(manifest.Key, manifest);

                    // mark keys as defined for the next step
                    foreach (var item in manifest.Items) {
                        definedKeys.Add(item.Key);
                    }

                } catch (Exception ex) {
                    InstantApp.Logger.Error($"Couldn't load Sprite Manifest at {Path.GetFileName(path)}. ({ex.Message})");
                }
            }

            // load stray sprites not defined by manifests
            foreach (var path in assets.EnumerateFiles(DIRECTORY, "*.png", true)) {
                var key = path.Replace(".png", "");

                if (definedKeys.Contains(key[(DIRECTORY.Length + 1)..]))
                    continue;

                // save 'stray' definition for use later
                _strayDefinitions.Add(new SpriteDefinition {
                    // remove 'sprites/' from the key
                    Key = key[(DIRECTORY.Length + 1)..]
                });
            }

            var creationQueue = new List<DefinitionManifestPair>();
            creationQueue.AddRange(_strayDefinitions.Select(def => new DefinitionManifestPair(def, null)));

            // append pairs of sprites with manifests attached to them
            foreach (var (manifestName, definitionArray) in _spritesByManifest) {
                creationQueue.AddRange(definitionArray.Select(def => new DefinitionManifestPair(def, _manifests[manifestName])));
            }

            // produce assets from sprite definitions
            foreach (var asset in CreateAssets(creationQueue))
                yield return asset;
        }

        record struct DefinitionManifestPair(SpriteDefinition Definition, SpriteManifest? Manifest = default);

        IEnumerable<Asset> CreateAssets(IEnumerable<DefinitionManifestPair> definitions) {
            foreach (var (def, manifest) in definitions) {
                var key = $"sprites/{def.Key}";

                // root asset for the animation/spritesheet
                Asset assetRoot = def.Animation != null ?
                    new LazyAsset<SpriteAnimation>(key, this) :
                    new LazyAsset<Sprite>(key, this);

                // save def and manifest for later
                assetRoot.Data = (def, manifest);

                // yield the root asset first
                yield return assetRoot;

                // create child assets
                switch (def.SplitOptions) {
                    // save sub sprite properties into asset.Data
                    case { Type: SplitType.BySubSprites, SubSprites: var subSprites }:
                        foreach (var subSprite in subSprites) {
                            var child = new LazyAsset<Sprite>(string.Format(manifest?.NamingFormat ?? SpriteManifest.DefaultNamingFormat, key, subSprite.Key), this) {
                                Data = subSprite // save sub sprite for loading it later
                            };

                            assetRoot.AddChild(child);

                            // load each sub sprite as individual asset
                            yield return child;
                        }

                        break;

                    // save sprite index as data
                    case { Type: SplitType.ByCount, WidthOrFrameCount: var frameCount }:
                        for (var i = 0; i < frameCount; i++) {
                            var child = new LazyAsset<Sprite>(string.Format(manifest?.NamingFormat ?? SpriteManifest.DefaultNamingFormat, key, i), this) {
                                Data = i // save frame index for later
                            };

                            assetRoot.AddChild(child);

                            // load each frame as individual asset
                            yield return child;
                        }

                        break;

                    // save source rect as data
                    case { Type: SplitType.BySize, WidthOrFrameCount: var width, Height: var height }:
                        if (!TryGetPngSize(key, out var imageDimensions)) {
                            InstantApp.Logger.Warn($"Couldn't get image dimensions for sprite '{key}'. This is required because Split.BySize was used.");
                            break;
                        }

                        var spriteCount = 0;
                        for (var x = 0; x < imageDimensions.X; x += width) {
                            for (var y = 0; y < imageDimensions.Y; y += height) {
                                var child = new LazyAsset<Sprite>(string.Format(manifest?.NamingFormat ?? SpriteManifest.DefaultNamingFormat, key, spriteCount++), this) {
                                    // calculate the source rect while at it
                                    Data = new Rectangle(x, y, width, height)
                                };

                                assetRoot.AddChild(child);

                                yield return child;
                            }
                        }

                        break;
                }
            }
        }

        void LoadSprite(LazyAsset asset) {
            // load the texture
            using var stream = AssetManager.Instance.OpenStream(asset.Key + ".png");
            var texture = Texture2D.FromStream(InstantApp.Instance.GraphicsDevice, stream);
            texture.Tag = asset.Key;

            var (def, manifest) = ((SpriteDefinition, SpriteManifest?))asset.Data;

            var sourceRect = new Rectangle(0, 0, texture.Width, texture.Height);

            // create a sprite
            var sprite = new Sprite(texture, sourceRect, def.Origin.Transform(sourceRect, manifest), asset.Key) { Points = def.Points };

            var frames = new List<Sprite>();

            if (asset.Children != null) {
                Sprite childSprite = default;

                // load child assets (animation frames, etc)
                for (var i = 0; i < asset.Children.Count; i++) {
                    var child = asset.Children[i] as LazyAsset<Sprite>;
                    switch (child.Data) {
                        case SubSprite subSprite:
                            child.Content = new Sprite(texture, subSprite.Region, subSprite.Origin.Transform(subSprite.Region, manifest, def.Origin), child.Key);
                            break;

                        case Rectangle rect:
                            childSprite = new Sprite(texture, rect, def.Origin.Transform(rect, manifest), child.Key);
                            child.Content = childSprite;
                            frames.Add(childSprite);
                            break;

                        case int index:
                            var frame = new Rectangle(0, texture.Height / asset.Children.Count * index, texture.Width, texture.Height);
                            childSprite = new Sprite(texture, frame, def.Origin.Transform(frame, manifest), child.Key);
                            child.Content = childSprite;
                            frames.Add(childSprite);
                            break;
                    }
                }
            }

            switch (asset) {
                default:
                    InstantApp.Logger.Warn($"Unknown sprite asset type. ({asset.GetType().Name})");
                    break;

                case LazyAsset<SpriteAnimation> animationAsset when def.Animation is AnimationDefinition animation:
                    animationAsset.Content = new SpriteAnimation(animation.Fps, frames.ToArray(), animation.Events, animationAsset.Key);
                    break;

                case LazyAsset<Sprite> spriteAsset:
                    spriteAsset.Content = sprite;
                    break;
            }
        }

        public void LoadOnDemand(LazyAsset asset) {
            // the parent should take care of loading
            if (asset.Parent is LazyAsset parent) {
                parent.Load();
                return;
            }

            try {
                LoadSprite(asset);
            } catch (Exception ex) {
                InstantApp.Logger.Error($"Couldn't load Sprite '{asset.Key}'. ({ex.Message})");
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

        bool ReloadManifest(string assetPath, out IEnumerable<Asset> updatedAssets) {
            using var stream = AssetManager.Instance.OpenStream(assetPath);
            using var reader = new StreamReader(stream);

            // read manifest file to ensure it is valid first
            var manifest = SpriteManifestParser.Parse(assetPath, reader.ReadToEnd());
            var assets = CreateAssets(manifest.Items.Select(i => new DefinitionManifestPair(i, manifest)))
                .ToArray();

            // clear previous assets
            if (_manifests.TryGetValue(assetPath, out var oldManifest)) {
                foreach (var item in oldManifest.Items) {
                    var key = $"sprites/{item.Key}";
                    if (AssetManager.Instance.GetContainer(key) is not Asset asset) 
                        continue;

                    // keep track of cleared assets for updatedAssets
                    AssetManager.Instance.Remove(key);
                }

                // remove from registries too
                _spritesByManifest.Remove(assetPath);
                _manifests.Remove(assetPath);
            }

            // register new manifest
            _spritesByManifest.Add(assetPath, manifest.Items);
            _manifests.Add(assetPath, manifest);

            // upload new assets
            foreach (var asset in assets) {
                AssetManager.Instance.Register(asset.Key, asset, false);
            }

            // display the updated assets
            updatedAssets = assets;
            return true;
        }

        bool IHotReloader.TryReload(string assetKey, out IEnumerable<Asset> updatedAssets) {
            if (!assetKey.StartsWith(DIRECTORY + "/")) {
                updatedAssets = null;
                return false;
            }

            var extension = Path.GetExtension(assetKey);
            var key = assetKey.Replace(extension, "");

            // ooh boy 
            if (extension == ".json") {
                try {
                    return ReloadManifest(assetKey, out updatedAssets);
                } catch (Exception ex) {
                    InstantApp.Logger.Error($"Couldn't hot reload Sprite Manifest '{assetKey}'. ({ex.Message})");
                    updatedAssets = null;
                    return false;
                }
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
