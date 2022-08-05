using Instant2D.Audio;
using Instant2D.Core;
using Instant2D.EC;
using Instant2D.Input;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.TestGame.Scenes {
    public class AudioTest : Scene {
        public override void Initialize() {
            
        }

        StreamingAudioInstance _audioInstance;

        public override void Update() {
            base.Update();

            if (InputManager.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.E)) {
                if (_audioInstance != null) {
                    _audioInstance.Dispose();
                }

                using var stream = AssetManager.Instance.OpenStream("music/stage_1.ogg");
                _audioInstance = new StreamingAudioInstance(stream, InstantGame.Instance.GetSystem<AudioManager>());
                _audioInstance.Play(true);

                _audioInstance.Position = 0;
            }

            if (_audioInstance != null) {
                if (InputManager.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.Q)) {
                    _audioInstance.Position = 2f;
                }

                if (InputManager.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.W)) {
                    _audioInstance.Dispose();
                    _audioInstance = null;
                }

                if (InputManager.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.D1)) {
                    _audioInstance.Pitch = Random.Shared.NextFloat(-1f, 1f);
                    _audioInstance.Pan = Random.Shared.NextFloat(-1f, 1f);
                }

                if (InputManager.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.R)) {
                    switch (_audioInstance.PlaybackState) {
                        case PlaybackState.Playing:
                            _audioInstance.Pause();
                            break;

                        case PlaybackState.Paused:
                            _audioInstance.Play(_audioInstance.IsLooping);
                            break;
                    }
                }
            }
        }

        public override void Render(IDrawingBackend drawing) {
            base.Render(drawing);

            if (_audioInstance != null) {
                drawing.DrawString($"Position: {_audioInstance.Position}sec", new(5), Color.White, Vector2.One, 0);
            }
        }
    }
}
