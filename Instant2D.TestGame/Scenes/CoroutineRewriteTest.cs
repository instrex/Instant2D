using Instant2D;
using Instant2D.Coroutines;
using Instant2D.EC;
using Instant2D.EC.Rendering;
using Instant2D.Graphics;
using Instant2D.Input;
using Microsoft.Xna.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.TestGame.Scenes {
    public class CoroutineRewriteTest : Scene {
        public override void Initialize() {
            base.Initialize();

            InstantApp.Instance.TargetElapsedTime = TimeSpan.FromSeconds(1 / 165f);

            var logo = CreateEntity("logo", Vector2.Zero)
                .AddComponent<SpriteComponent>()
                .SetSprite(Assets.Get<Sprite>("sprites/logo"));

            for (var i = 0; i < 30; i++) {
                CreateEntity($"logo_{i}", new Vector2(Random.Shared.NextFloat(-320, 320), Random.Shared.NextFloat(-240, 240)))
                    .AddComponent<SpriteComponent>()
                    .SetSprite(Assets.Get<Sprite>("sprites/logo"));
            }

            CoroutineManager.Schedule(0.5f, () => {
                //RenderLayers[0].BackgroundColor = Color.Cyan;
            });
        }

        Coroutine _dvdMovement;
        Coroutine _longCoroutine;

        public override void Update() {
            base.Update();

            if (InputManager.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.E)) {
                _dvdMovement?.Stop();
                _dvdMovement = CoroutineManager.Run(MoveLikeDVD(FindEntityByName("logo")), FindEntityByName("logo"))
                    .SetCompletionHandler(this, c => c.GetContext<CoroutineRewriteTest>()._longCoroutine = null)
                    ;
            }

            if (InputManager.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.Q)) {
                _longCoroutine?.Stop();
                _longCoroutine = CoroutineManager.Run(LongCoroutine());
            }

            if (InputManager.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.D1)) {
                TimeScale = 1f;
            }

            if (InputManager.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.D2)) {
                TimeScale = 0.5f;
            }

            if (InputManager.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.D3)) {
                TimeScale = 2f;
            }

            if (InputManager.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.R))
                SceneManager.Switch<CoroutineRewriteTest>();
        }

        IEnumerator LongCoroutine() {
            var layer = GetLayer<EntityLayer>("default");

            var f = 0f;
            while (f < 1f) {
                f = MathHelper.Clamp(f + TimeManager.DeltaTime, 0, 1);
                layer.BackgroundColor = Color.Lerp(Color.Black, Color.DarkCyan, f);
                yield return null;
            }

            yield return new WaitForSeconds(2.0f);

            while (f >= 0f) {
                f = MathHelper.Clamp(f - TimeManager.DeltaTime, 0, 1);
                layer.BackgroundColor = Color.Lerp(Color.Black, Color.DarkCyan, f);
                yield return null;
            }
        }

        IEnumerator MoveLikeDVD(Entity entity) {
            var velocity = new Vector2(1, 1);
            while (true) {
                var bounds = Camera.Bounds;
                var selfBounds = entity.GetComponent<SpriteComponent>().Bounds;

                entity.Position += velocity;
                
                if (entity.Position.X - selfBounds.Width / 2 < bounds.Left || entity.Position.X + selfBounds.Width / 2 > bounds.Right) {
                    velocity.X *= -1;
                }

                if (entity.Position.Y - selfBounds.Height / 2 < bounds.Top || entity.Position.Y + selfBounds.Height / 2 > bounds.Bottom) {
                    velocity.Y *= -1;
                }

                yield return new WaitForFixedUpdate(false);
            }
        }

        IEnumerator CoroutineTest(Entity entity) {
            var time = 0f;

            while (true) {
                time += TimeManager.DeltaTime;

                entity.GetComponent<SpriteComponent>().Color = Color.Lerp(Color.Red, Color.Blue, 0.5f + MathF.Sin(time * 4) * 0.5f);
                entity.Transform.Position = Camera.MouseToWorldPosition();
                entity.Transform.Rotation = MathF.Cos(time) * 0.1f;

                if (time > 20)
                    yield break;

                yield return null;
            }
            
        }
    }
}
