using Instant2D.Assets.Sprites;
using Instant2D.Core;
using Instant2D.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Instant2D.Assets.Loaders {
    /// <summary>
    /// Base loader class for handling sprites in <see cref="SpriteManifest"/> format.
    /// </summary>
    public abstract class SpriteLoader : IAssetLoader {
        protected const string SPRITES_PATH = "Assets/sprites/";

        /// <summary>
        /// Enum used to decide produced asset types for lazy loading support or anything that needs to know it before loading.
        /// </summary>
        public enum AssetOutputType {
            Sprite,
            Animation
        }

        /// <summary>
        /// Currently loaded manifests.
        /// </summary>
        public IReadOnlyList<SpriteManifest> Manifests => _manifests;

        /// <summary>
        /// Currently loaded <see cref="SpriteDef"/>initions, may be modified for better control.
        /// </summary>
        public readonly Dictionary<string, SpriteDef> SpriteDefinitions = new();

        readonly List<Sprite> _spriteBuffer = new(16);
        SpriteManifest[] _manifests;

        public IEnumerable<Asset> Load(AssetManager assets, LoadingProgress progress) {
            _manifests = LoadManifests(assets).ToArray();
            for (var i = 0; i < _manifests.Length; i++) {
                foreach (var item in _manifests[i].Items) {
                    // check for double definitions
                    if (SpriteDefinitions.TryGetValue(item.key, out var existing)) {
                        InstantGame.Instance.Logger.Warning($"Sprite '{item.key}' is defined in both '{existing.manifest.Name}' and '{_manifests[i].Name}' manifests, skipping the latter.");
                        continue;
                    }

                    SpriteDefinitions.Add(item.key, item with { manifest = _manifests[i] });
                }
            }

            // load additional definitions
            foreach (var item in LoadAdditionalDefinitions(assets)) {
                if (!SpriteDefinitions.TryAdd(item.key, item)) {
                    InstantGame.Instance.Logger.Warning($"Failed to load Sprite '{item.key}': element with the same key already exists.");
                }
            }

            // load the sprite assets
            foreach (var asset in LoadSpriteAssets(assets))
                yield return asset;
        }

        #region API

        /// <summary>
        /// Provides a way to specify how instances of <see cref="SpriteManifest"/> are loaded. 
        /// You can access the results in <see cref="Manifests"/>.
        /// </summary>
        protected abstract IEnumerable<SpriteManifest> LoadManifests(AssetManager assets);

        /// <summary>
        /// Perform general <see cref="Asset{T}"/> loading tasks there.
        /// </summary>
        protected abstract IEnumerable<Asset> LoadSpriteAssets(AssetManager assets);

        /// <summary>
        /// If required, you can load additional <see cref="SpriteDef"/> instances in there. Called after <see cref="LoadManifests(AssetManager)"/>.
        /// </summary>
        protected virtual IEnumerable<SpriteDef> LoadAdditionalDefinitions(AssetManager assets) {
            yield break;
        }

        #endregion

        protected static bool TryParseManifest(string json, out SpriteManifest manifest, string name = default) {
            try {
                var result = JsonConvert.DeserializeObject<SpriteManifest>(json, new SpriteManifest.Converter());
                manifest = result;

                return true;
            } catch (Exception ex) {
                InstantGame.Instance.Logger.Error($"Couldn't parse SpriteManifest{(name == null ? $" ({name})" : "")}: {ex.Message}");

                manifest = null;
                return false;
            }
        }

        protected static Vector2 GetOrigin(SpriteOrigin origin, Rectangle sourceRect, SpriteManifest manifest = default, SpriteOrigin? parent = default) {
            var size = new Vector2(sourceRect.Width, sourceRect.Height);
            return origin.type switch {
                SpriteOriginType.Absolute => origin.value,
                SpriteOriginType.Normalized => size * origin.value,
                SpriteOriginType.Default when parent is not null => GetOrigin(parent.Value, sourceRect, manifest),
                SpriteOriginType.Default when manifest is not null => size * manifest.DefaultOrigin,
                _ => size * new Vector2(0.5f),
            };
        } 

        /// <summary>
        /// Attempts to get all asset information that may be produced by provided <see cref="SpriteDef"/>.
        /// That includes additional sprites created by 'split' option, layers and animations.
        /// </summary>
        /// <remarks> 
        /// NOTE: <see cref="SpriteSplitOptions.BySize"/> will only work if <paramref name="imageDimensions"/> are not (0, 0), as it requires image data to determine how many sprites will be created.
        /// </remarks>
        protected static List<(AssetOutputType type, string key)> GetProducedAssetData(in SpriteDef def, Point imageDimensions = default) {
            var result = new List<(AssetOutputType type, string key)>();

            // switch blocks are dumb lol
            int i = 0;
            switch (def.split.type) {
                // only 1 asset (and its layers when finished) will be produced
                case SpriteSplitOptions.None:
                    result.Add((AssetOutputType.Sprite, def.key));
                    break;

                // produces assets for each manual rectangle
                case SpriteSplitOptions.Manual:
                    result.Add((def.animation != null ? AssetOutputType.Animation : AssetOutputType.Sprite, def.key));
                    for (i = 0; i < def.split.manual.Length; i++) {
                        result.Add((AssetOutputType.Sprite, def.FormatFrameKey(def.split.manual[i].key)));
                    }

                    break;

                // produces assets based on frame count
                case SpriteSplitOptions.ByCount:
                    result.Add((def.animation != null ? AssetOutputType.Animation : AssetOutputType.Sprite, def.key));
                    for (i = 0; i < def.split.widthOrFrameCount; i++) {
                        result.Add((AssetOutputType.Sprite, def.FormatFrameKey(i.ToString())));
                    }

                    break;

                // produces assets by analyzing image dimensions
                case SpriteSplitOptions.BySize when imageDimensions != default:
                    result.Add((def.animation != null ? AssetOutputType.Animation : AssetOutputType.Sprite, def.key));

                    var width = def.split.widthOrFrameCount;
                    var height = def.split.height;
                    for (var x = 0; x < imageDimensions.X; x += width) {
                        for (var y = 0; y < imageDimensions.Y; y += height) {
                            result.Add((AssetOutputType.Sprite, def.FormatFrameKey(i++.ToString())));
                        }
                    }

                    break;
            }

            return result;
        }

        /// <summary>
        /// Attempts to process specified <see cref="SpriteDef"/>, returning all the resulting sprites in a list. <br/>
        /// Optionally sets <paramref name="animation"/>, if <see cref="SpriteDef"/> contains animation tag.
        /// Otherwise, the entire sprite sheet will be added as individual <see cref="Sprite"/>.
        /// </summary>
        /// <remarks>
        /// NOTE: returned List is pooled between each method call, avoid storing it long-term.
        /// </remarks>
        protected List<Sprite> ProcessSpriteDef(in SpriteDef def, Texture2D texture, Rectangle sourceRect, out SpriteAnimation? animation) {
            animation = null;

            _spriteBuffer.Clear();
            switch (def.split.type) {
                case SpriteSplitOptions.None:
                    _spriteBuffer.Add(new Sprite(texture, sourceRect, GetOrigin(def.origin, sourceRect, def.manifest), def.key));
                    break;

                case SpriteSplitOptions.Manual:
                    // since order for manual sprites cannot be defined,
                    // we just ignore animations and warn the user about it
                    if (def.animation != null) {
                        InstantGame.Instance.Logger.Warning($"Sprite '{def.key}' with custom 'split' cannot have animation, adding as a spritesheet.");
                    }

                    // add all rects
                    _spriteBuffer.Add(new Sprite(texture, sourceRect, GetOrigin(def.origin, sourceRect, def.manifest), def.key));
                    for (var i = 0; i < def.split.manual.Length; i++) {
                        var split = def.split.manual[i];
                        var frameRect = new Rectangle(sourceRect.X + split.rect.X, sourceRect.Y + split.rect.Y, split.rect.Width, split.rect.Height);
                        _spriteBuffer.Add(new Sprite(texture, frameRect, GetOrigin(split.origin, frameRect, def.manifest, def.origin), def.FormatFrameKey(split.key)));
                    }

                    break;

                case SpriteSplitOptions.BySize:
                case SpriteSplitOptions.ByCount:
                    if (def.animation == null) {
                        // if animation is null, we add the spritesheet as its own sprite
                        _spriteBuffer.Add(new Sprite(texture, sourceRect, GetOrigin(def.origin, sourceRect, def.manifest), def.key));
                    }

                    // prepare frame data for parsing
                    var width = def.split.type == SpriteSplitOptions.BySize ? def.split.widthOrFrameCount : sourceRect.Width;
                    var height = def.split.type == SpriteSplitOptions.ByCount ? sourceRect.Height / def.split.widthOrFrameCount : def.split.height;
                    var frameIndex = 0;
                    for (var x = sourceRect.X; x < sourceRect.Right; x += width) {
                        for (var y = sourceRect.Y; y < sourceRect.Bottom; y += height) {
                            var frameRect = new Rectangle(x, y, width, height);
                            _spriteBuffer.Add(new Sprite(texture, frameRect, GetOrigin(def.origin, sourceRect, def.manifest), def.FormatFrameKey(frameIndex++.ToString())));
                        }
                    }

                    // save the animation in case it exists
                    if (def.animation is SpriteAnimationDef splitAnimDef) {
                        animation = new SpriteAnimation(splitAnimDef.fps, _spriteBuffer.ToArray(), splitAnimDef.events, def.key);
                    }

                    break;

                default:
                    
                    break;
            }

            return _spriteBuffer;
        }
    }
}
