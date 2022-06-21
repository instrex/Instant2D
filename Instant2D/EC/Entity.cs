using Instant2D.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.EC {
    /// <summary>
    /// Represents an entity in the game world that may have components attached to it.
    /// </summary>
    public sealed class Entity {
        static uint _entityIdCounter;

        /// <summary>
        /// Unique number identifier for this entity.
        /// </summary>
        public readonly uint Id = _entityIdCounter++;

        /// <summary>
        /// Spatial information of this entity.
        /// </summary>
        public readonly Transform Transform = new();

        /// <summary>
        /// The scene this Entity exists on.
        /// </summary>
        public Scene Scene {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _scene; 
            set {
                // if previous scene is not null and not the same as the one being assigned,
                // deregister components from all scene systems
                if (_scene != null && _scene != value) {
                    UnregisterComponents();
                }

                // if the new scene is not the same, assign it and register components
                if (_scene != value) {
                    _scene = value;
                    if (_scene != null) {
                        RegisterComponents();
                    }
                }
            }
        }

        readonly List<Component> _components = new(16);
        Scene _scene;

        #region Components

        /// <summary>
        /// Provides read-only access to all components attached to this entity.
        /// </summary>
        public IReadOnlyList<Component> Components => _components;

        static class ComponentPoolingData<T> where T: Component {
            static bool? _shouldPool;
            public static bool ShouldPool {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get {
                    // check if type implements IResettable
                    if (_shouldPool is not bool shouldPool) {
                        var isResettable = typeof(T).IsAssignableFrom(typeof(IResettable));
                        _shouldPool = isResettable;

                        return isResettable;
                    }

                    return shouldPool;
                }
            }
        }

        void RegisterComponents() {
            // TODO: register components for rendering, physics etc
        }

        void UnregisterComponents() {
            // TODO: remove components from rendering, physics etc
        }

        /// <summary>
        /// When the component type implements <see cref="IResettable"/>, takes existing instance from pool and adds it. If not, a new instance is created.
        /// </summary>
        public T AddComponent<T>() where T : Component, new() => AddComponent(ComponentPoolingData<T>.ShouldPool ? StaticPool<T>.Get() : new T());

        /// <summary>
        /// Attaches the provided component instance to this entity.
        /// </summary>
        public T AddComponent<T>(T component) where T: Component {
            component.AttachToEntity(this);
            if (_scene != null) {
                RegisterComponents();
            }

            return component;
        }

        /// <summary>
        /// Attempts to find a component of given type.
        /// </summary>
        public bool TryGetComponent<T>(out T component) where T: Component {
            for (var i = 0; i < _components.Count; i++) {
                if (_components[i] is T foundComponent) {
                    component = foundComponent;
                    return true;
                }
            }

            component = default;
            return false;
        }

        /// <summary>
        /// Attempts to find a component of given type, returning <see langword="null"/> if unsuccessful.
        /// </summary>
        public T GetComponent<T>() where T: Component => TryGetComponent<T>(out var component) ? component : default;

        #endregion
    }

    public abstract class Component {
        /// <summary>
        /// Entity this component is attached to.
        /// </summary>
        public Entity Entity {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get; private set; 
        }

        public void AttachToEntity(Entity entity) {
            Entity = entity;
        }
    }

    public class Scene {

    }
}
