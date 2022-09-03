using Instant2D.Audio;
using Instant2D.Core;
using Instant2D.Coroutines;
using Instant2D.EC;
using Instant2D.EC.Components;
using Instant2D.Input;
using Instant2D.Utils;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.TestGame.Scenes {
    public class AudioTest : Scene {
        Entity _saul;

        public override void Initialize() {
            AddRenderLayer("default").BackgroundColor = Color.Black;

            _saul = CreateEntity("saul")
                .AddComponent(new SpriteComponent {
                    Sprite = AssetManager.Instance.Get<Sprite>("sprites/saul")
                });

            _saul.AddComponent(new AudioComponent {
                IsLooped = true,
                Sound = AssetManager.Instance.Get<Sound>("music/saul"),
                IsStreaming = true,
                Volume = 0.0f
            });

        }

        float _shake;

        public override void Update() {
            if (InputManager.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.A)) {
                _saul.Transform.Position += new Vector2(-10, 0);
            }

            if (InputManager.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.D)) {
                _saul.Transform.Position += new Vector2(10, 0);
            }

            if (InputManager.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.W)) {
                _saul.Transform.Position += new Vector2(0, -5);
            }

            if (InputManager.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.S)) {
                _saul.Transform.Position += new Vector2(0, 5);
            }

            if (InputManager.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.R)) {
                PlaySound(AssetManager.Instance.Get<Sound>("sfx/hat_2"), _saul.Transform.Position, followEntity: _saul);
                _shake = 32;
            }

            if (_shake > 0) {
                Camera.Transform.Position = Random.Shared.NextDirection(_shake);
                Camera.Transform.Rotation = Random.Shared.NextFloat(-_shake, _shake) * 0.01f;
                _shake -= 4;
            } else {
                Camera.Transform.Position = Vector2.Zero;
                Camera.Transform.Rotation = 0;
            }
        }

        public override void Render(IDrawingBackend drawing) {
            base.Render(drawing);
        }
    }
}
