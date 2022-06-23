using Instant2D.Core;
using Instant2D.Graphics;
using Instant2D.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Instant2D.EC {
    public class Scene {
        internal readonly List<Entity> _entities = new(128);
        readonly List<SceneRenderLayer> _layers = new(12);
        RenderTarget2D _sceneTarget, _tempTarget;

        /// <summary>
        /// Represents collection of RenderLayers this scene will use.
        /// </summary>
        public IReadOnlyList<SceneRenderLayer> RenderLayers => _layers;

        /// <summary>
        /// When <see langword="true"/>, Scene's entities will be updated. Otherwise, they will be drawn but not updated.
        /// </summary>
        public bool IsActive;

        /// <summary>
        /// When <see langword="true"/>, Scene's RenderLayers and renderable components will be drawn each frame.
        /// </summary>
        public bool IsVisible;

        /// <summary>
        /// Camera used to render this scene.
        /// </summary>
        public ICamera Camera;

        /// <summary>
        /// Creates an entity and automatically adds it onto the scene.
        /// </summary>
        public Entity CreateEntity(string name, Vector2 position) {
            var entity = StaticPool<Entity>.Get();
            entity.Transform.Position = position;
            entity.Name = name;
            entity.Scene = this;

            return entity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void InternalUpdate() {
            if (IsActive) {
                // update entities before anything else
                for (var i = 0; i < _entities.Count; i++) {
                    _entities[i].Update();
                }
            }

            // we only do drawing from there
            if (!IsVisible) {
                return;
            }

            var gd = InstantGame.Instance.GraphicsDevice;
            for (var i = 0; i < _layers.Count; i++) {
                var layer = _layers[i];

                gd.SetRenderTarget(layer._renderTarget);

                layer.InternalDraw();
            }
        }

        #region Scene Lifecycle

        /// <summary>
        /// Called each frame when this scene is in focus.
        /// </summary>
        public virtual void Update() { }

        #endregion
    }
}
