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
using Instant2D.Utils.Coroutines;
using Instant2D.Utils.Math;
using Instant2D.Utils.ResolutionScaling;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using static System.Formats.Asn1.AsnWriter;

namespace Instant2D.TestGame {
    public class Game : InstantGame {
        protected override void SetupSystems() {
            AddSystem<AssetManager>(assets => {
                assets.SetupHotReload("./Instant2D.TestGame/Assets/");
                assets.AddLoader<DevSpriteLoader>();
            });

            AddSystem<InputManager>();
            AddSystem<CoroutineManager>();
            AddSystem<GraphicsManager>();
            AddSystem<SceneManager>(scene => {
                scene.SetResolutionScaler<DefaultResolutionScaler>()
                    .SetDesignResolution(640 / 2, 360 / 2)
                    .SetPixelPerfect()
                    .SetDisplayMode(DefaultResolutionScaler.ScreenDisplayMode.ShowAll);
            });
        }

        public class MainScene : Scene {
            IEnumerable<Type> _sceneTypes;

            public override void Initialize() {
                CreateLayer("default").BackgroundColor = Color.DarkCyan;

                // gather all scenes in this project
                _sceneTypes = typeof(MainScene).Assembly.GetTypes()
                    .Where(t => t != typeof(MainScene) && t.IsSubclassOf(typeof(Scene)));
            }

            public override void Render(IDrawingBackend drawing) {
                drawing.Push(Material.Default, Matrix2D.CreateScale(Resolution.scaleFactor));

                var y = 6;
                foreach (var type in _sceneTypes) {
                    var size = GraphicsManager.DefaultFont.MeasureString(type.Name);

                    var rect = new RectangleF(6, y, size.X + 8, 16);

                    var isHovered = rect.Contains(InputManager.MousePosition);
                    if (InputManager.LeftMousePressed && isHovered) {
                        SceneManager.Instance.Current = Activator.CreateInstance(type) as Scene;
                        return;
                    }

                    drawing.DrawRectangle(rect, isHovered ? Color.White : Color.Black);
                    drawing.DrawString(type.Name, new Vector2(10, y + 5), isHovered ? Color.Black : Color.White, Vector2.One, 0);

                    y += 18;
                }
            }
        }

        protected override void Initialize() {
            Window.AllowUserResizing = true;

            Title = "Instant2D Sample Projects";

            SceneManager.Instance.Current = new MainScene();
        }
    }
}
