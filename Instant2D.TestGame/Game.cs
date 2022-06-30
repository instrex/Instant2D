﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Instant2D.Assets.Loaders;
using Instant2D.Core;
using Instant2D.EC;
using Instant2D.EC.Components;
using Instant2D.Graphics;
using Instant2D.Input;
using Instant2D.Utils;
using Instant2D.Utils.Math;
using Instant2D.Utils.ResolutionScaling;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Instant2D.TestGame {
    public class Game : InstantGame {
        protected override void SetupSystems() {
            AddSystem<AssetManager>(assets => {
                assets.AddLoader<DevSpriteLoader>()
                    //.SetSpritesheetMode()
                    ;
            });

            AddSystem<InputManager>();
            AddSystem<GraphicsManager>();
            AddSystem<SceneManager>(scene => {
                scene.SetResolutionScaler<DefaultResolutionScaler>()
                    .SetDesignResolution(640, 360)
                    .SetPixelPerfect()
                    .SetDisplayMode(DefaultResolutionScaler.ScreenDisplayMode.ShowAll);
            });
        }

        class MoveOnTouchComponent : Component, IUpdatableComponent {
            public void Update() {
                Entity.Transform.Position = InputManager.MousePosition;
                Entity.Transform.Rotation += 0.1f;

                // rotate wawas
                for (var i = 0; i < Entity.ChildrenCount; i++) {
                    var entity = Entity[i];
                    entity.Transform.LocalRotation += i % 2 == 0 ? -0.1f : 0.1f;
                    entity.Transform.LocalPosition = entity.Transform.LocalPosition.SafeNormalize() * (500 - 250 * MathF.Sin((TimeManager.TotalTime + i) * 4));
                }
            }
        }

        SoundEffect _soundEffect;
        protected override void LoadContent() {
            base.LoadContent();
            
            // OGG loading test using FAudio
            unsafe {
                var data = File.ReadAllBytes(@"C:\Users\instrex\Desktop\OneShot.ogg");
                fixed (byte* ptr = data) {
                    var ogg = FAudio.stb_vorbis_open_memory((IntPtr)ptr, data.Length, out var err, IntPtr.Zero);
                    var info = FAudio.stb_vorbis_get_info(ogg);
                    var sampleCount = FAudio.stb_vorbis_stream_length_in_samples(ogg);

                    // allocate sample buffers 
                    var buffer = new byte[sampleCount * 2 * info.channels];
                    var samples = new float[buffer.Length / 2];
                    _ = FAudio.stb_vorbis_get_samples_float_interleaved(ogg, info.channels, samples, samples.Length);

                    // convert float to byte[] PCM data
                    for (var i = 0; i < samples.Length; i++) {
                        var val = (short)(samples[i] * short.MaxValue);
                        buffer[i * 2] = (byte)val;
                        buffer[i * 2 + 1] = (byte)(val >> 8);
                    }

                    // create the sound effect
                    _soundEffect = new SoundEffect(buffer, (int)info.sample_rate, (AudioChannels)info.channels);

                    // kill stream
                    FAudio.stb_vorbis_close(ogg);
                }
            }

            _soundEffect.Play();

            Window.AllowUserResizing = true;

            SceneManager.Instance.Current = new SimpleScene {
                Camera = new ScreenSpaceCameraComponent(),
                
                OnInitialize = scene => {
                    // setup layers
                    var bg = scene.CreateLayer("background");
                    bg.BackgroundColor = Color.DarkGreen;

                    // create scaling test
                    scene.CreateEntity("scaling-test", Vector2.Zero)
                        .AddComponent<SpriteRenderer>()
                        .SetSprite(AssetManager.Instance.Get<Sprite>("scaling_test"))
                        .SetRenderLayer("background")
                        .SetDepth(1.0f);

                    // create funny renderer
                    var wawaCat = scene.CreateEntity("wawa-cat", Vector2.Zero)
                        .SetLocalScale(0.25f);

                    wawaCat.AddComponent<SpriteRenderer>()
                        .SetSprite(AssetManager.Instance.Get<Sprite>("wawa"))
                        .SetRenderLayer("background");

                    wawaCat.AddComponent<MoveOnTouchComponent>();

                    // create Mini Wawas
                    for (var i = 0; i < 8; i++) {
                        var wawa = scene.CreateEntity($"mini-wawa-{i}", Vector2.Zero)
                            .SetParent(wawaCat)
                            .SetLocalPosition(new Vector2(250, 0).RotatedBy(i * MathHelper.PiOver4))
                            .SetLocalScale(0.5f)
                            .AddComponent(new SpriteRenderer {
                                Sprite = AssetManager.Instance.Get<Sprite>("wawa"),
                                RenderLayer = bg,
                                Depth = 0.5f
                            });
                    }
                },

                OnUpdate = scene => {

                }
            };
        }

        protected override void Draw(GameTime gameTime) {
            base.Draw(gameTime);

            var drawing = GraphicsManager.Backend;
            drawing.Push(Material.Default);

            var scaledRes = SceneManager.Instance.Current.Resolution;
            drawing.Draw(GraphicsManager.Pixel, scaledRes.offset, Color.Red * 0.2f, 0, scaledRes.renderTargetSize.ToVector2());

            drawing.Pop(true);
        }
    }
}
