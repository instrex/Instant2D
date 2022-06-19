using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Instant2D;
using Instant2D.Assets.Loaders;
using Instant2D.Core;
using Instant2D.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Instant2D.TestGame {
    public class Game : InstantGame {
        protected override void SetupSystems() {
            AddSystem<AssetManager>(assets => {
                assets.AddLoader<DevSpriteLoader>()
                    .SetSpritesheetMode()
                    ;
            });

            AddSystem<GraphicsManager>();
        }

        protected override void LoadContent() {
            base.LoadContent();
        }

        protected override void Draw(GameTime gameTime) {
            base.Draw(gameTime);

            GraphicsDevice.Clear(Color.Red);
            
            var drawing = GraphicsManager.Instance.Backend;

            drawing.Push(Material.Default);

            drawing.Draw(
                AssetManager.Instance.Get<Sprite>("logo"),
                new Vector2(50),
                Color.White,
                0,
                1f
            );

            drawing.Push(Material.Default with { BlendState = BlendState.Additive });

            drawing.Draw(
                AssetManager.Instance.Get<Sprite>("logo"),
                new Vector2(50),
                Color.White,
                0,
                1.2f
            );

            drawing.Pop();

            drawing.Pop();


        }
    }
}
