using Instant2D.Assets;
using Instant2D.Assets.Containers;
using Instant2D.Coroutines;
using Instant2D.EC;
using Instant2D.EC.Events;
using Instant2D.Graphics;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.TestGame.Scenes {
    public class LoaderTest : Scene {
        public class CustomLoader : IAssetLoader, ILazyAssetLoader, IHotReloader {
            public IEnumerable<Asset> Load(AssetManager assets) {
                // list all assets but not load them yet
                // loading everything at once is not ideal, expecially for bigger games
                foreach (var file in assets.EnumerateFiles("levels", "*.txt")) {
                    yield return new LazyAsset<MyLevel>(file.Replace(".txt", ""), this);
                }
            }

            // instead, we'll handle loading assets on-demand
            // that is, when user asks for an asset, it will be quickly loaded before they notice it's missing
            public void LoadOnDemand(LazyAsset asset) {
                // read asset
                using var stream = AssetManager.Instance.OpenStream(asset.Key + ".txt");
                using var reader = new StreamReader(stream);
                var levelString = reader.ReadToEnd().Split('\n', StringSplitOptions.TrimEntries);

                // create level
                var level = new MyLevel { Key = asset.Key };
                var width = levelString.Max(str => str.Length);
                var height = levelString.Length;
                level.Tiles = new char[width, height];

                // load tiles
                for(var y = 0; y < height; y++) {
                    for(var x = 0; x < width; x++) {
                        var row = levelString[y];

                        if (row.Length <= x)
                            continue;

                        level.Tiles[x, y] = levelString[y][x];
                    }
                }

                // update asset's content
                var levelAsset = asset as LazyAsset<MyLevel>;
                levelAsset.Content = level;
            }

            // optionally you can implement IHotReloader
            // which allows you to reload asset on the fly when they change, awesome!
            IEnumerable<string> IHotReloader.WatcherPatterns { get; } = new[] { "*.txt" };
            bool IHotReloader.TryReload(string assetKey, out IEnumerable<Asset> updatedAssets) {
                // this is not our problem
                if (!assetKey.StartsWith("levels/")) {
                    updatedAssets = null;
                    return false;
                }

                // it's very easy in this case, as we just need to unload the old asset to refresh
                var levelAsset = AssetManager.Instance.GetContainer(assetKey.Replace(".txt", "")) as LazyAsset<MyLevel>;
                levelAsset.Unload();

                updatedAssets = new[] { levelAsset };

                return true;
            }
        }

        public record MyLevel {
            public string Key;
            public char[,] Tiles;
        }

        public override void Initialize() {
            CreateLayer("default").BackgroundColor = Color.Black;

            Camera.Entity.SetPosition(Resolution.renderTargetSize.ToVector2() * 0.5f);

            CreateEntity("level_container");

            SetLevel(AssetManager.Instance.Get<MyLevel>("levels/beach"));

            Events.Subscribe<AssetReloadedEvent>(ev => {
                if (ev.UpdatedAsset is IAssetContainer<MyLevel> container && _currentLevel?.Key == container.Content.Key) {
                    SetLevel(container.Content);
                    return true;
                }

                return false;
            });
        }

        // cute little animation that happens when you reload the level
        static System.Collections.IEnumerator TileAnimation(Entity tile, float delay) {
            yield return delay;

            var f = 0f;
            while (f <= 1f) {
                f += TimeManager.TimeDelta * 4;
                tile.SetScale(MathHelper.Clamp(f, 0, 1) * 8);
                yield return null;
            }
        }

        MyLevel _currentLevel;

        void SetLevel(MyLevel level) {
            // clear previous level
            var entity = FindEntityByName("level_container");
            for (var i = 0; i < entity.ChildrenCount; i++) {
                entity[i].Destroy();
            }

            // map level tiles to colors
            for (var x = 0; x < level.Tiles.GetLength(0); x++) {
                for (var y = 0; y < level.Tiles.GetLength(1); y++) {
                    var tileEntity = CreateEntity($"tile_{x}_{y}")
                        .SetParent(entity)
                        .SetLocalPosition(new Vector2(x * 8, y * 8))
                        .SetScale(0f);

                    // add renderer
                    tileEntity.AddComponent<SpriteComponent>()
                        .SetSprite(GraphicsManager.Pixel)
                        .SetColor(level.Tiles[x, y] switch {
                            'G' => new(56, 153, 82),
                            'W' => new(86, 156, 224),
                            'S' => new(224, 198, 83),
                            _ => Color.Pink
                        });

                    // do a cute animation
                    tileEntity.RunCoroutine(TileAnimation(tileEntity, (x + y) * 0.025f));
                }
            }

            // set current level
            _currentLevel = level;
        }
    }
}
