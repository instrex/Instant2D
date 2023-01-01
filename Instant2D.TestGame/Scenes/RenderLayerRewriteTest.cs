using Instant2D;
using Instant2D.EC;
using Instant2D.EC.Rendering;
using Instant2D.Graphics;
using Instant2D.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.TestGame.Scenes {
    public class RenderLayerRewriteTest : Scene {
        class OutlineLayer : RenderTargetLayer {
            public override void Present(DrawingContext drawing) {
                Content.Present(drawing);

                drawing.Push(Material.Default with { BlendState = BlendState.Additive}, Matrix.Identity);

                var rotation = Scene.TotalTime.ToVector2() * (1 + 4 * MathF.Sin(Scene.TotalTime * 2));
                drawing.DrawTexture(RenderTarget, rotation, null, Color.White with { A = 255 / 4 }, 0, Vector2.Zero, Vector2.One);
                drawing.DrawTexture(RenderTarget, rotation.RotatedBy(MathHelper.PiOver2), null, Color.White with { A = 255 / 4}, 0, Vector2.Zero, Vector2.One);
                drawing.DrawTexture(RenderTarget, rotation.RotatedBy(MathHelper.Pi), null, Color.White with { A = 255 / 4 }, 0, Vector2.Zero, Vector2.One);
                drawing.DrawTexture(RenderTarget, rotation.RotatedBy(MathHelper.Pi + MathHelper.PiOver2), null, Color.White with { A = 255 / 4 }, 0, Vector2.Zero, Vector2.One);

                drawing.Pop();
            }
        }

        public override void Initialize() {
            // 1. setup the layer hierarchy first

            AddLayer<EntityLayer>(1, "sky")
                .SetBackgroundColor(Color.DarkCyan)
                .SetCamera("static_camera");

            AddLayer<EntityLayer>(3, "parallax_50")
                .SetCamera("parallax_camera_50");

            // mastering layer for background elements
            AddLayer<MasteringLayer>(5, "background")
                .SetLayerRange(0.0f, 5.0f);

            AddLayer<OutlineLayer>(7, "walls")
                .SetContent((EntityLayer _) => { });

            AddLayer<OutlineLayer>(10, "objects")
                .SetContent((EntityLayer _) => { });

            // mastering layer for in-game objects
            AddLayer<MasteringLayer>(15, "world")
                .SetLayerRange(5.0f, 15.0f);

            AddLayer<EntityLayer>(50, "ui")
                .SetCamera("ui_camera");

            // 2. now create dummy objects to represent layers

            CreateEntity("dummy_sky")
                .AddComponent<SpriteComponent>()
                .SetSprite(Assets.Get<Sprite>("sprites/renderlayer_test/sky"))
                .SetRenderLayer("sky")
                ;

            CreateEntity("dummy_parallax")
               .AddComponent<SpriteComponent>()
               .SetSprite(Assets.Get<Sprite>("sprites/renderlayer_test/parallax_50"))
               .SetRenderLayer("parallax_50")
               ;

            CreateEntity("dummy_walls")
               .AddComponent<SpriteComponent>()
               .SetSprite(Assets.Get<Sprite>("sprites/renderlayer_test/walls"))
               .SetRenderLayer("walls");

            CreateEntity("dummy_objects")
               .AddComponent<SpriteComponent>()
               .SetSprite(Assets.Get<Sprite>("sprites/renderlayer_test/objects"))
               .SetRenderLayer("objects");

            CreateEntity("dummy_ui")
               .AddComponent<SpriteComponent>()
               .SetSprite(Assets.Get<Sprite>("sprites/renderlayer_test/ui"))
               .SetRenderLayer("ui");
        }

        public override void Render() {
            base.Render();

            GraphicsManager.Context.Begin(Material.Default, Matrix.Identity);

            var yOff = Resolution.renderTargetSize.Y / 4;

            for (var i = 0; i < RenderLayers.Count; i++) {
                var layer = RenderLayers[i];

                var rtPos = new Vector2(0, yOff * i);
                var rect = new RectangleF(rtPos, Resolution.renderTargetSize.ToVector2() / 4);

                var rt = layer switch {
                    RenderTargetLayer { RenderTarget: RenderTarget2D renderTarget } => renderTarget,
                    MasteringLayer { RenderTarget: RenderTarget2D masteringRt } => masteringRt,
                    _ => null
                };

                GraphicsManager.Context.DrawRectangle(rect, rt == null ? Color.Black : Color.Magenta);

                if (rt != null) {
                    GraphicsManager.Context.DrawTexture(rt, rtPos, null, Color.White, 0, Vector2.Zero, Vector2.One / 4);

                    // draw big preview
                    if (rect.Contains(InputManager.RawMousePosition)) {
                        GraphicsManager.Context.DrawRectangle(new RectangleF(new Vector2(Resolution.renderTargetSize.X * 0.25f + 12, 4), Resolution.renderTargetSize.ToVector2()), Color.Magenta);
                        GraphicsManager.Context.DrawTexture(rt, new Vector2(Resolution.renderTargetSize.X * 0.25f + 12, 4), null, Color.White, 0, Vector2.Zero, Vector2.One);
                        GraphicsManager.Context.DrawRectangle(new RectangleF(new Vector2(Resolution.renderTargetSize.X * 0.25f + 12, 4), Resolution.renderTargetSize.ToVector2()), Color.Transparent, Color.Black, 4);
                    }
                } else {
                    GraphicsManager.Context.DrawLine(rect.TopLeft, rect.BottomRight, Color.Red, 2);
                    GraphicsManager.Context.DrawLine(rect.TopRight, rect.BottomLeft, Color.Red, 2);
                }

                GraphicsManager.Context.DrawRectangle(rect, Color.Transparent, Color.Black, 2);

                GraphicsManager.Context.DrawString($"#{layer.Order:F1} {layer.Name}", rtPos + new Vector2(4), Color.White, Vector2.One, 0, drawOutline: true);
                GraphicsManager.Context.DrawString($"({layer.GetType().Name})", rtPos + new Vector2(4, 14), Color.Gray, Vector2.One, 0, drawOutline: true);
            }

            GraphicsManager.Context.End();
        }

        Vector2 _cameraOrigin;
        Color _targetBgColor = Color.White;
        Color _targetWorldTint = Color.White;

        public override void Update() {
            if (InputManager.LeftMouseDown) {
                _cameraOrigin = Camera.MouseToWorldPosition();
            }

            var position = _cameraOrigin + new Vector2(MathF.Sin(TotalTime * 2), MathF.Cos(TotalTime * 4)) * 16;
            Camera.Transform.Position = position * 0.5f;
            Camera.Transform.Rotation = MathF.Sin(TotalTime * 0.5f) * 0.05f;
            FindEntityByName("parallax_camera_50").Transform.Position = position * 0.25f;
            FindEntityByName("parallax_camera_50").Transform.Rotation = MathF.Sin(TotalTime * 0.5f) * 0.025f;

            // restart the scene
            if (InputManager.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.R)) {
                SceneManager.Switch<RenderLayerRewriteTest>();
            }

            // return
            if (InputManager.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.Escape)) {
                SceneManager.Switch<Game.MainScene>();
            }

            // darken the background
            if (InputManager.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.E)) {
                _targetBgColor = Color.Lerp(Color.White, Color.Black, 0.75f);
            }

            // undarken the background
            if (InputManager.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.Q)) {
                _targetBgColor = Color.White;
            }

            var bg = GetLayer<MasteringLayer>("background");
            bg.Color = Color.Lerp(bg.Color, _targetBgColor, 0.1f);

            // whiten the world
            if (InputManager.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.D1)) {
                _targetWorldTint = Color.White;
            }

            // redden the world
            if (InputManager.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.D2)) {
                _targetWorldTint = Color.Red;
            }

            // green the world
            if (InputManager.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.D3)) {
                _targetWorldTint = Color.Green;
            }

            // blue the world
            if (InputManager.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.D4)) {
                _targetWorldTint = Color.Blue;
            }

            var world = GetLayer<MasteringLayer>("world");
            world.Color = Color.Lerp(world.Color, _targetWorldTint, 0.1f);
        }
    }
}
