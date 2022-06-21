using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Instant2D;
using Instant2D.Assets.Loaders;
using Instant2D.Core;
using Instant2D.Graphics;
using Instant2D.Utils;
using Instant2D.Utils.Math;
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

        protected override void Update(GameTime gameTime) {
            base.Update(gameTime);


        }

        class TestCamera : ICamera {
            Matrix2D _matrix = Matrix2D.Identity;
            Vector2 _pos;
            public Vector2 Position {
                get => _pos;
                set {
                    _pos = value.Round();
                    Matrix2D.CreateTranslation(ref _pos, out _matrix);
                }
            }

            public float Scale {
                set {
                    _matrix *= Matrix2D.CreateScale(value);
                }
            }

            public Matrix2D TransformMatrix => _matrix;

            public Vector2 ScreenToWorldPosition(Vector2 screenPosition) {
                throw new NotImplementedException();
            }

            public Vector2 WorldToScreenPosition(Vector2 worldPosition) {
                throw new NotImplementedException();
            }
        }

        TestCamera _funnyCamera = new();
        protected override void Draw(GameTime gameTime) {
            base.Draw(gameTime);

            _funnyCamera.Position = new Vector2(MathF.Sin(TimeManager.TotalTime) * 12, MathF.Cos(TimeManager.TotalTime * 2) * 12);
            _funnyCamera.Scale = 1f + 0.75f * MathF.Sin(TimeManager.TotalTime * 5);

            GraphicsDevice.Clear(Color.Red);
            
            var drawing = GraphicsManager.Instance.Backend;

            drawing.Push(Material.Default, _funnyCamera.TransformMatrix);

            var logo = AssetManager.Instance.Get<Sprite>("logo");
            drawing.Draw(
                logo,
                new Vector2(50),
                Color.White,
                0,
                1f
            );

            drawing.DrawAnimation(
                AssetManager.Instance.Get<SpriteAnimation>("explosion"),
                new Vector2(140),
                Color.Black,
                0,
                Vector2.One
            );

            drawing.Push(Material.Default with { BlendState = BlendState.Additive });

            for (var i = 0; i < 6; i++) {
                drawing.Draw(
                    logo,
                    new Vector2(50),
                    Color.White * (1f - i / 6f) * (0.75f + 0.25f * MathF.Sin(TimeManager.TotalTime)),
                    0,
                    1f + 0.1f * i
                );
            }
            

            drawing.Pop(true);
        }
    }
}
