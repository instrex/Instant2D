using Instant2D.Audio;
using Instant2D.Core;
using Instant2D.EC;
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
        public override void Initialize() {
            
        }

        StreamingAudioInstance _audioInstance;
        StaticAudioInstance _staticInstance;

        public override void Update() {
            base.Update();

            if (_staticInstance != null) {
                if (_staticInstance.PlaybackState != PlaybackState.Playing) {
                    Logger.WriteLine("Instance was pooled");
                    _staticInstance.Pool();
                    _staticInstance = null;
                }
            }

            if (InputManager.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.D2)) {
                _staticInstance = AssetManager.Instance.Get<Sound>("sfx/hat_1").CreateStaticInstance()
                    .SetPitch(1f);
                _staticInstance.Play();
            }

            if (InputManager.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.E)) {
                if (_audioInstance != null) {
                    _audioInstance.Dispose();
                }

                _audioInstance = AssetManager.Instance.Get<Sound>("music/stage_1").CreateStreamingInstance();
                _audioInstance.Play(true);
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

                if (InputManager.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.S)) {
                    if (_audioInstance.PlaybackState == PlaybackState.Playing) {
                        _audioInstance.Stop();
                    } else _audioInstance.Play(_audioInstance.IsLooping);
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
                var audioPosition = _audioInstance.Position;
                drawing.DrawString($"Position: {audioPosition}sec", new(5), Color.White, Vector2.One, 0);

                var progressBar = new RectangleF(5, 32, 320, 8);
                drawing.DrawRectangle(progressBar, Color.Gray);
                drawing.DrawRectangle(progressBar with { Size = progressBar.Size * new Vector2(audioPosition / _audioInstance.Length, 1) }, Color.Cyan);

                if (progressBar.Contains(InputManager.RawMousePosition)) {
                    var f = (InputManager.RawMousePosition.X - progressBar.Left) / progressBar.Width;
                    drawing.DrawPoint(new Vector2(progressBar.Left + f * progressBar.Width, progressBar.Center.Y), Color.LightCyan, 12);

                    if (InputManager.LeftMousePressed) {
                        
                        _audioInstance.Seek(f * _audioInstance.Length);
                    }
                }
            }
        }
    }
}
