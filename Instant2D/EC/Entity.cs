using Instant2D.Core;
using Instant2D.Utils;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Instant2D.EC {
    /// <summary>
    /// Represents an entity in the game world that may have components attached to it.
    /// </summary>
    /// <remarks>
    /// NOTE: preferably, avoid creating instances of this class using constructor, instead use <see cref="StaticPool{T}"/> to access pooled instances. <br/>
    /// This will allow future <see cref="Scene.CreateEntity(string, Vector2)"/> calls utilize already allocated entities, instead of creating new ones each time. <br/>
    /// Same applies to components implementing <see cref="IPooled"/> interface.
    /// </remarks>
    public sealed class Entity : IPooled, ITransformCallbacksHandler {
        static uint _entityIdCounter;

        /// <summary> Unique number identifier for this entity. </summary>
        public readonly uint Id = _entityIdCounter++;

        /// <summary> Spatial information of this entity. </summary>
        public readonly Transform Transform = new();

        /// <summary> Name assigned to this entity. </summary>
        public string Name;

        /// <summary> The scene this Entity exists on. </summary>
        public Scene Scene {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _scene; 
            set {
                // if previous scene is not null and not the same as the one being assigned,
                // deregister components from all scene systems
                if (_scene != null && _scene != value) {
                    _scene._entities.Remove(this);
                    DetachRenderableComponents();
                }

                // if the new scene is not the same, assign it and register components
                if (_scene != value) {
                    _scene = value;
                    if (_scene != null) {
                        _scene._entities.Add(this);
                    }
                }
            }
        }

        #region Children

        /// <summary> Handles assigning parent entities. </summary>
        /// <remarks> <b>NOTE: do not set the <see cref="Transform.Parent"/> manually</b>, as it will bypass registering the child for recursive destruction and activation!</remarks>
        public Entity Parent {
            get => _parent;
            set {
                if (_parent == value)
                    return;

                // do some stuff if parent is not null
                if (value != null) {
                    // set the transform parent and add this as a child
                    Transform.Parent = value.Transform;
                    value.CheckChildrenBuffer();
                    value._children.Add(this);
                }
                
                _parent = value;
            }
        }

        /// <summary> Gets this entity's child using <paramref name="index"/>. </summary>
        public Entity this[int index] {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _children == null ? null : (index >= 0 && _children.Count > index ? _children[index] : null);
        }

        /// <summary> Amount of children entities this parent has. Use <see cref="this[int]"/> to access them. </summary>
        public int ChildrenCount => _children?.Count ?? default;

        /// <summary> Adds the <paramref name="other"/> to the children list. </summary>
        public Entity AddChild(Entity other) {
            if (other == null) {
                InstantGame.Instance.Logger.Warning($"Tried to add 'null' as '{Name}'s child.");
                return other;
            }

            // assign parent and scene
            other.Parent = this;
            other.Scene = Scene;

            return other;
        }

        /// <summary> Shortcut for creating an entity using <see cref="Scene.CreateEntity(string, Vector2)"/> and setting its <see cref="Parent"/> to this. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Entity AddChild(string name) => AddChild(_scene?.CreateEntity(name, Vector2.Zero) ?? StaticPool<Entity>.Get());

        #endregion

        /// <summary> <see langword="true"/> if this entity was destroyed and detached from its scene. </summary>
        public bool IsDestroyed {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get; private set;
        }

        readonly List<Component> _components = new(16);
        internal List<IUpdatableComponent> _updatedComponents; 
        internal List<Entity> _children;
        Entity _parent;
        bool _shouldDestroy;
        Scene _scene;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void CheckChildrenBuffer() {
            // allocate the children buffer when needed
            if (_children == null) {
                _children = new List<Entity>(16);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void DetachRenderableComponents() {
            for (var i = 0; i < _components.Count; i++) {
                // null out their RenderLayer so they're automatically removed
                if (_components[i] is RenderableComponent renderable) {
                    renderable.RenderLayer = default;
                }
            }
        }

        public void OnTransformUpdated(Transform.ComponentType components) {
            for (var i = 0; i < _components.Count; i++) {
                _components[i].OnTransformUpdated(components);
            }
        }

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
                        var isResettable = typeof(T).IsAssignableFrom(typeof(IPooled));
                        _shouldPool = isResettable;

                        return isResettable;
                    }

                    return shouldPool;
                }
            }
        }

        /// <summary>
        /// When the component type implements <see cref="IPooled"/>, takes existing instance from pool and adds it. If not, a new instance is created.
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
                // allocate the updated component buffer when needed
                if (_updatedComponents == null) {
                    _updatedComponents = new List<IUpdatableComponent>(8);
                }

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
                    if (foundComponent is IUpdatableComponent updatable) {
                        _updatedComponents?.Remove(updatable);
                    }

                    // run the callback
                    foundComponent.OnRemovedFromEntity();
                    return foundComponent;
                }
            }

            return null;
        }

        #endregion

        #region Setters

        /// <inheritdoc cref="Scene"/>
        public Entity SetScene(Scene scene) {
            Scene = scene;
            return this;
        }

        /// <inheritdoc cref="Parent"/>
        public Entity SetParent(Entity other) {
            Parent = other;
            return this;
        }

        /// <inheritdoc cref="Transform.LocalPosition"/>
        public Entity SetLocalPosition(Vector2 position) {
            Transform.LocalPosition = position;
            return this;
        }

        /// <inheritdoc cref="Transform.Scale"/>
        public Entity SetScale(Vector2 scale) {
            Transform.Scale = scale;
            return this;
        }

        /// <inheritdoc cref="Transform.Scale"/>
        public Entity SetLocalScale(float scale) {
            Transform.LocalScale = new(scale);
            return this;
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
                _updatedComponents?.Clear();
                _components.Clear();

                // destroy children
                if (_children != null) {
                    for (var i = 0; i < _children.Count; i++) {
                        _children[i].Destroy();
                    }
                }

                // detach from the scene
                IsDestroyed = true;
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

        void IPooled.Reset() {
            IsDestroyed = false;
            Name = null;

            Transform.Reset();

            _updatedComponents?.Clear();
            _components.Clear();
            _shouldDestroy = false;
            _scene = null;
        }
    }
}
