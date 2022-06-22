using Instant2D.Utils;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Instant2D.EC {
    public class Scene {
        internal readonly List<Entity> _entities = new(128);

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
            // update entities before anything else
            for (var i = 0; i < _entities.Count; i++) {
                _entities[i].Update();
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
