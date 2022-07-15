using Instant2D.Coroutines;
using Instant2D.EC;
using Instant2D.Input;
using Instant2D.Utils;
using Microsoft.Xna.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.TestGame.Scenes {
    public class CoroutineTest : Scene {
        public override void Initialize() {
            CreateLayer("default").BackgroundColor = Color.DarkCyan;

            Camera.Entity.SetPosition(new(160, 80));

            CreateEntity("wawa_1", Vector2.Zero)
                .SetScale(0.1f)
                .AddComponent(new SpriteComponent {
                    Sprite = AssetManager.Instance.Get<Sprite>("sprites/wawa"),
                    Z = 100
                });
        }

        CoroutineInstance _runningCoroutine;
        public override void Update() {
            if (InputManager.LeftMousePressed) {
                // interrupt currently running coroutine
                if (_runningCoroutine?.IsRunning == true)
                    _runningCoroutine.Stop();

                var entity = FindEntityByName("wawa_1");
                _runningCoroutine = entity.RunCoroutine(Coroutine(entity, Random.Shared.NextRectanglePoint(new(0, 0, 320, 160))), wasStopped => {
                    Logger.WriteLine(wasStopped ? "Coroutine was stopped." : "Coroutine has finished.");
                });
            }
        }

        static IEnumerator Coroutine(Entity entity, Vector2 position) {
            var dir = entity.Transform.Position.DirectionTo(position, 8);
            while (Vector2.Distance(entity.Transform.Position, position) > 15) {
                // move and rotate towards the position
                entity.Transform.Rotation += Math.Sign(dir.X) * 0.5f;
                entity.Transform.Position += dir;

                // wait till the next frame
                yield return null;
            }
        }
    }
}
