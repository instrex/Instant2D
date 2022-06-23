using Instant2D.Utils;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.EC {
    /// <summary>
    /// Represents an entity in the game world that may have components attached to it.
    /// </summary>
    public sealed class Entity : IResettable {
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
        /// Name assigned to this entity. Don't change.
        /// </summary>
        public string Name;

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
                    _scene._entities.Remove(this);
                    UnregisterComponents();
                }

                // if the new scene is not the same, assign it and register components
                if (_scene != value) {
                    _scene = value;
                    if (_scene != null) {
                        _scene._entities.Add(this);
                        RegisterComponents();
                    }
                }
            }
        }

        /// <summary>
        /// <see langword="true"/> if this entity was destroyed and detached from its scene.
        /// </summary>
        public bool IsDestroyed {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get; private set;
        }

        internal List<IUpdatableComponent> _updatedComponents = new(8); 
        readonly List<Component> _components = new(16);
        bool _shouldDestroy;
        Scene _scene;

        public void Destroy() {
            _shouldDestroy = true;
        }

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
            component.Entity = this;
            component.Initialize();

            // register updatable components
            if (component is IUpdatableComponent updatable) {
                _updatedComponents.Add(updatable);
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

        /// <summary>
        /// Attempts to remove a component of type, and returns it on success. If removal didn't succeed, returns <see langword="null"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T RemoveComponent<T>() where T: Component {
            for (var i = 0; i < _components.Count; i++) {
                if (_components[i] is T foundComponent) {
                    _components.RemoveAt(i);
                    if (foundComponent is IUpdatableComponent updatable)
                        _updatedComponents.Remove(updatable);

                    // run the callback
                    foundComponent.OnRemovedFromEntity();
                    return foundComponent;
                }
            }

            return null;
        }

        #endregion

        public void Update() {
            if (_shouldDestroy) {
                // notify components of death and detach
                for (var i = 0; i < _components.Count; i++) {
                    // null out RenderLayers so that renderable components
                    // are detached from them
                    if (_components[i] is RenderableComponent render) {
                        render.RenderLayer = null;
                    }

                    _components[i].OnRemovedFromEntity();
                    _components[i].Entity = null;
                }

                // clear components so no references remain
                _updatedComponents.Clear();
                _components.Clear();

                // detach from the scene
                Scene = null;
                return;
            }

            // update components
            if (_updatedComponents != null) {
                for (var i = 0; i < _updatedComponents.Count; i++) {
                    var comp = _updatedComponents[i];
                    if (comp.IsActive) {
                        _updatedComponents[i].Update();
                    }
                }
            }
        }

        void IResettable.Reset() {
            Transform.Reset();
            IsDestroyed = false;
            Name = null;
            _updatedComponents.Clear();
            _components.Clear();
            _shouldDestroy = false;
            _scene = null;
        }
    }
}
