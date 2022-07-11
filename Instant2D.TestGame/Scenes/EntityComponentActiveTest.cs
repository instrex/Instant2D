﻿using Instant2D.EC;
using Instant2D.EC.Components;
using Instant2D.Utils.Coroutines;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.TestGame.Scenes {
    // A test scene for checking how Entity.IsActive and Component.IsActive work
    class EntityComponentActiveTest : Scene {
        class SineWave : Component, IUpdatableComponent {
            float _totalTime = 0;
            public void Update() {
                _totalTime += TimeManager.TimeDelta * Entity.TimeScale * 2;
                Entity.Transform.Position = Entity.Transform.Position with { Y = Scene.Resolution.renderTargetSize.Y * MathF.Sin(_totalTime) * 0.25f };
            }
        }

        public override void Initialize() {
            CreateLayer("default").BackgroundColor = Color.DarkCyan;

            var cat = AssetManager.Instance.Get<Sprite>("sprites/wawa");

            for (var i = 0; i < 3; i++) {
                var obj = CreateEntity($"obj_{i}", new Vector2(-120 + 120 * i, 0))
                    .SetScale(0.1f)
                    .AddComponent<SineWave>()
                    .AddComponent<SpriteComponent>()
                    .SetSprite(cat);

                CreateEntity($"text_{i}", new Vector2(-120 + 120 * i, Resolution.renderTargetSize.Y * -0.25f))
                    .AddComponent<TextComponent>()
                    .SetContent(i switch {
                        0 => "SpriteComponent.IsActive",
                        1 => "Entity.IsActive",
                        2 => "SineWave.IsActive",
                        _ => "???"
                    });

                switch (i) {
                    case 0:
                        // disable just the renderer
                        obj.Entity.Schedule(0.5f, _ => obj.IsActive = !obj.IsActive)
                            .SetRepeat();

                        break;

                    case 1:
                        // disable the whole enitty
                        obj.Entity.Schedule(0.5f, _ => obj.Entity.IsActive = !obj.Entity.IsActive)
                            .SetRepeat();

                        break;

                    case 2:
                        // disable the moving component
                        var comp = obj.Entity.GetComponent<SineWave>();
                        obj.Entity.Schedule(0.5f, _ => comp.IsActive = !comp.IsActive)
                            .SetRepeat();

                        break;
                }
            }
        }
    }
}
