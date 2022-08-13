using Instant2D.Core;
using Instant2D.Utils;
using Instant2D.Coroutines;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
    public sealed class Entity : IPooled, ITransformCallbacksHandler, ICoroutineTarget {
        static uint _entityIdCounter;

        readonly List<Component> _components = new(16);
        internal List<IUpdatableComponent> _updatedComponents;
        float? _overrideTimeScale;
        bool _shouldDestroy, _isInitialized;
        Scene _scene;

        /// <summary> Unique number identifier for this entity. </summary>
        public readonly uint Id = _entityIdCounter++;

        /// <summary> Spatial information of this entity. </summary>
        public readonly Transform<Entity> Transform;

        /// <summary> Name assigned to this entity. </summary>
        public string Name;

        public Entity() {
            Transform = new() { Entity = this };
        }

        #region Children

        /// <summary> Handles assigning parent entities. </summary>
        public Entity Parent {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Transform.Parent?.Entity;
            set {
                if (Transform.Parent == value?.Transform)
                    return;

                // pass to the internal transform
                Transform.Parent = value.Transform;
            }
        }

        /// <summary> Gets this entity's child using <paramref name="index"/>. </summary>
        public Entity this[int index] {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Transform[index]?.Entity;
        }

        /// <summary> Attempts to find the child entity using <see cref="Name"/>. </summary>
        public Entity this[string name] {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get {
                for (var i = 0; i < Transform.ChildrenCount; i++) {
                    var child = Transform[i];
                    if (child.Entity.Name == name)
                        return child.Entity;
                }

                return null;
            }
        }

        /// <summary> Amount of children entities this parent has. Use <see cref="this[int]"/> to access them. </summary>
        public int ChildrenCount {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Transform.ChildrenCount;
        }

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

        internal bool _isActive = true;

        public bool IsActive {
            get => _isActive;
            set {
                if (_isActive == value)
                    return;

                _isActive = value;

                // update components activity
                for (var i = 0; i < _components.Count; i++) {
                    var comp = _components[i];

                    if (!_isActive) {
                        // save the previous state
                        var isActive = comp._isActive;
                        comp.IsActive = _isActive;
                        comp._isActive = isActive;

                        continue;
                    }

                    comp.IsActive = comp._isActive;
                }

                // apply to children as well
                for (var i = 0; i < ChildrenCount; i++) {
                    this[i].IsActive = _isActive;
                }
            }
        }

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

        /// <summary> 
        /// Allows objects to have individual timescales. In case it's not set, will return either the parent timescale (if defined) or a global scene timescale. <br/>
        /// Set this to <see cref="float.NaN"/> to undo the timescale override and use <see cref="Scene.TimeScale"/>.
        /// </summary>
        public float TimeScale {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _overrideTimeScale is float ts ? ts : (Parent?.TimeScale ?? Scene.TimeScale);
            set {
                if (float.IsNaN(value)) {
                    _overrideTimeScale = null;
                    return;
                }

                _overrideTimeScale = value;
            }
        }

        /// <summary> <see langword="true"/> if this entity was destroyed and detached from its scene. </summary>
        public bool IsDestroyed {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get; private set;
        }

        public void OnTransformUpdated(TransformComponentType components) {
            for (var i = 0; i < _components.Count; i++) {
                _components[i].OnTransformUpdated(components);
            }
        }

        public void Destroy() {
            _shouldDestroy = true;
        }

        #region Components

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void DetachRenderableComponents() {
            for (var i = 0; i < _components.Count; i++) {
                // null out their RenderLayer so they're automatically removed
                if (_components[i] is RenderableComponent renderable) {
                    renderable.RenderLayer = default;
                }
            }
        }

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

            // add component to the list
            _components.Add(component);

            // register updatable components
            if (component is IUpdatableComponent updatable) {
                // allocate the updated component buffer when needed
                if (_updatedComponents == null) {
                    _updatedComponents = new List<IUpdatableComponent>(8);
                }

                _updatedComponents.Add(updatable);
            }

            if (_isActive) {
                // finally call Component.OnEnabled
                // to simulate it being enabled
                component.OnEnabled();
            }

            if (_isInitialized) {
                // call post initialize immediately
                // when active and already initialized
                component.PostInitialize();
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
        /// Enumerates all components of provided type <typeparamref name="T"/>.
        /// </summary>
        public IEnumerable<T> GetComponents<T>() where T: Component {
            for (var i = 0; i < _components.Count; i++) {
                if (_components[i] is T foundComponent) {
                    yield return foundComponent;
                }
            }
        } 

        /// <summary>
        /// Attempts to remove a component of type, and returns it on success. If removal didn't succeed, returns <see langword="null"/>.
        /// </summary>
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

        /// <summary>
        /// Attempts to detach specified component. Will return <see langword="true"/> if it exists and was successfully removed, or <see langword="false"/> if otherwise.
        /// </summary>
        public bool RemoveComponent<T>(T component) where T: Component {
            for (var i = 0; i < _components.Count; i++) {
                if (_components[i] == component) {
                    _components.RemoveAt(i);
                    if (component is IUpdatableComponent updatable) {
                        _updatedComponents?.Remove(updatable);
                    }

                    // run the callback
                    component.OnRemovedFromEntity();
                    return true;
                }
            }

            return false;
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

        /// <inheritdoc cref="Transform.Position"/>
        public Entity SetPosition(Vector2 position) {
            Transform.Position = position;
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
        public Entity SetScale(float scale) {
            Transform.Scale = new(scale);
            return this;
        }

        /// <inheritdoc cref="Transform.Scale"/>
        public Entity SetLocalScale(float scale) {
            Transform.LocalScale = new(scale);
            return this;
        }

        /// <inheritdoc cref="Transform.Scale"/>
        public Entity SetLocalScale(Vector2 scale) {
            Transform.LocalScale = scale;
            return this;
        }

        /// <inheritdoc cref="Transform.Rotation"/>
        public Entity SetRotation(float rotation) {
            Transform.Rotation = rotation;
            return this;
        }

        /// <inheritdoc cref="Transform.LocalRotation"/>
        public Entity SetLocalRotation(float rotation) {
            Transform.LocalRotation = rotation;
            return this;
        }

        #endregion

        public void Update() {
            if (!_isInitialized) {
                // call post initialize on all components
                for (var i = 0; i < _components.Count; i++) {
                    _components[i].PostInitialize();
                }

                _isInitialized = true;
            }

            if (_shouldDestroy) {
                ImmediateDestroy();
                return;
            }

            if (!_isActive)
                return;

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

        internal void ImmediateDestroy() {
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
            for (var i = 0; i < ChildrenCount; i++) {
                this[i].Destroy();
            }

            // detach from the scene
            IsDestroyed = true;
            Scene = null;

            // put the entity into the pool for reuse
            StaticPool<Entity>.Return(this);

            // release all the coroutines attached to this entity
            CoroutineManager.StopByTarget(this);
        }

        // IPooled impl
        void IPooled.Reset() {
            IsDestroyed = false;
            Name = null;

            // reset the transform and reassign entity
            Transform.Reset();
            Transform.Entity = this;

            // reset othet fields
            _isInitialized = false;
            _overrideTimeScale = null;
            _updatedComponents?.Clear();
            _components.Clear();
            _shouldDestroy = false;
            _scene = null;
        }

        // ICoroutineTarget impl
        bool ICoroutineTarget.IsActive => !_shouldDestroy && !IsDestroyed;
        float ICoroutineTarget.TimeScale => TimeScale;
    }
}
