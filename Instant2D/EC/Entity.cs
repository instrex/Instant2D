using Instant2D.Utils;
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
using System.Runtime.InteropServices;
using Instant2D.Coroutines;

namespace Instant2D.EC {
    /// <summary>
    /// Represents an entity in the game world that may have components attached to it.
    /// </summary>
    /// <remarks>
    /// NOTE: preferably, avoid creating instances of this class using constructor, instead use <see cref="StaticPool{T}"/> to access pooled instances. <br/>
    /// This will allow future <see cref="Scene.CreateEntity(string, Vector2)"/> calls utilize already allocated entities, instead of creating new ones each time. <br/>
    /// Same applies to components implementing <see cref="IPooledInstance"/> interface.
    /// </remarks>
    public sealed class Entity : IPooledInstance, ITransformCallbacksHandler, ICoroutineTarget {
        static uint _entityIdCounter;

        readonly List<Component> _components = new(16);
        internal List<IUpdate> _updatedComponents;
        internal List<IFixedUpdate> _fixedUpdateComponents;
        internal List<ILateUpdate> _lateUpdateComponents;
        internal float _timestepCounter, _timescale = 1.0f;
        internal TransformData _lastTransformState;
        bool _shouldDestroy;
        Scene _scene;

        /// <summary>
        /// Is set to <see langword="true"/> before this entity's first update cycle is ran.
        /// </summary>
        public bool IsInitialized { get; private set; }

        /// <summary> Unique number identifier for this entity. </summary>
        public readonly uint Id = _entityIdCounter++;

        /// <summary> Spatial information of this entity. </summary>
        public readonly Transform<Entity> Transform;

        /// <summary> Name assigned to this entity. </summary>
        public string Name;

        /// <summary>
        /// Transform components used for frame interpolation when <see cref="InterpolateTransform"/> is set.
        /// </summary>
        public TransformData TransformState;

        /// <summary>
        /// When <see langword="true"/>, <see cref="TransformState"/> will interpolate based on <see cref="AlphaFrameTime"/>. Useful when the game is running in higher framerate than the fixed update ticks. <br/>
        /// This effect is purely visual and won't affect <see cref="Transform"/> directly. Make sure to use <see cref="TransformState"/> in your render logic if you want to make use of this feature.
        /// </summary>
        public bool InterpolateTransform = true;

        /// <summary>
        /// Tags of this entity. Use <see cref="IntFlags"/> functions.
        /// </summary>
        public int Tags = 0;

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
                InstantApp.Logger.Warn($"Tried to add 'null' as '{Name}'s child.");
                return other;
            }

            // assign parent and scene
            other.Parent = this;
            other.Scene = Scene;

            return other;
        }

        /// <summary> Shortcut for creating an entity using <see cref="Scene.CreateEntity(string, Vector2)"/> and setting its <see cref="Parent"/> to this. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Entity AddChild(string name) => AddChild(_scene?.CreateEntity(name, Vector2.Zero) ?? Pool<Entity>.Shared.Rent());

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
        /// Individual timescale of this object. Will be multiplied by <see cref="Scene.TimeScale"/>.
        /// </summary>
        public float TimeScale {
            get => _timescale;
            set {
                _timescale = value;
                if (_timescale == 1.0f) {
                    // reset the individual timestep counter
                    // when timescale is back to 1.0
                    _timestepCounter = 0f;
                }

                // set timescales for children
                for(var i = 0; i < ChildrenCount; ++i) {
                    this[i].TimeScale = value;
                }
            }
        }

        /// <summary>
        /// Used for interpolation between FixedUpdate frames, a value in range of 0.0 - 1.0. <br/>
        /// TODO: come up with a better name for this... ?
        /// </summary>
        public float AlphaFrameTime;

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

        //static class ComponentPoolingData<T> where T: Component {
        //    static bool? _shouldPool;
        //    public static bool ShouldPool {
        //        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        //        get {
        //            // check if type implements IResettable
        //            if (_shouldPool is not bool shouldPool) {
        //                var isResettable = typeof(T).IsAssignableFrom(typeof(IPooled));
        //                _shouldPool = isResettable;

        //                return isResettable;
        //            }

        //            return shouldPool;
        //        }
        //    }
        //}

        /// <summary>
        /// When the component type implements <see cref="IPooled"/>, takes existing instance from pool and adds it. If not, a new instance is created.
        /// </summary>
        public T AddComponent<T>() where T : Component, new() => AddComponent(new T());

        /// <summary>
        /// Attaches the provided component instance to this entity.
        /// </summary>
        public T AddComponent<T>(T component) where T: Component {
            component.Entity = this;
            component.Initialize();

            // add component to the list
            _components.Add(component);

            // register updatable components
            if (component is IUpdate updatable) {
                // allocate the updated component buffer when needed
                _updatedComponents ??= ListPool<IUpdate>.Get();
                _updatedComponents.Add(updatable);
            }

            // register fixed update components
            if (component is IFixedUpdate fixedUpdatable) {
                _fixedUpdateComponents ??= ListPool<IFixedUpdate>.Get();
                _fixedUpdateComponents.Add(fixedUpdatable);
            }

            // register late update components
            if (component is ILateUpdate lateUpdatable) {
                _lateUpdateComponents ??= ListPool<ILateUpdate>.Get();
                _lateUpdateComponents.Add(lateUpdatable);
            }

            if (_isActive) {
                // finally call Component.OnEnabled
                // to simulate it being enabled
                component.OnEnabled();
            }

            if (IsInitialized) {
                // call post initialize immediately
                // when active and already initialized
                component.PostInitialize();
            }

            return component;
        }

        /// <summary>
        /// Attempts to find a component of given type.
        /// </summary>
        public bool TryGetComponent<T>(out T component) {
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
        public T GetComponent<T>() => TryGetComponent<T>(out var component) ? component : default;

        /// <summary>
        /// Enumerates all components of provided type <typeparamref name="T"/>.
        /// </summary>
        public IEnumerable<T> GetComponents<T>() {
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
                    RemoveComponent(foundComponent);
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

                    if (component is IUpdate updatable) 
                        _updatedComponents?.Remove(updatable);
                    
                    if (component is IFixedUpdate fixedUpdatable)
                        _fixedUpdateComponents?.Remove(fixedUpdatable);

                    if (component is ILateUpdate lateUpdatable)
                        _lateUpdateComponents?.Remove(lateUpdatable);

                    // run the callback
                    component.OnRemovedFromEntity();
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region Setters

        /// <summary>
        /// Tags this entity with specified unshifted flag.
        /// </summary>
        public Entity AddTag(int tag) {
            Tags = Tags.SetFlag(tag);
            return this;
        }

        /// <summary>
        /// Removes unshifted flag from this entity's tag .
        /// </summary>
        public Entity RemoveTag(int tag) {
            Tags = Tags.RemoveFlag(tag);
            return this;
        }

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
        public Vector2 Position {
            get => Transform.Position;
            set => SetPosition(value);
        }

        /// <inheritdoc cref="Transform.LocalPosition"/>
        public Vector2 LocalPosition {
            get => Transform.LocalPosition;
            set => SetLocalPosition(value);
        }

        /// <inheritdoc cref="Transform.Scale"/>
        public Vector2 Scale {
            get => Transform.Scale;
            set => SetScale(value);
        }

        /// <inheritdoc cref="Transform.Scale"/>
        public Vector2 LocalScale {
            get => Transform.LocalScale;
            set => SetLocalScale(value);
        }

        /// <inheritdoc cref="Transform.Rotation"/>
        public float Rotation {
            get => Transform.Rotation;
            set => SetRotation(value);
        }

        /// <inheritdoc cref="Transform.LocalRotation"/>
        public float LocalRotation {
            get => Transform.LocalRotation;
            set => SetLocalRotation(value);
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

        /// <inheritdoc cref="IsActive"/>
        public Entity SetActive(bool isActive) {
            IsActive = isActive;
            return this;
        }

        /// <inheritdoc cref="TimeScale"/>
        public Entity SetTimeScale(float timeScale = 1.0f, bool applyToChildren = true) {
            TimeScale = timeScale;

            return this;
        }

        #endregion

        /// <summary>
        /// Sets transform data for previous and present frames. You shouldn't really call this unless you know what you're doing.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetTransformState(in TransformData data) {
            _lastTransformState = data;
            TransformState = data;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void PreUpdate() {
            if (!IsInitialized) {
                // call post initialize on all components
                for (var i = 0; i < _components.Count; i++) {
                    _components[i].PostInitialize();
                }

                // set initial transform state
                SetTransformState(Transform.Data);

                IsInitialized = true;
            }

            if (_shouldDestroy) {
                ImmediateDestroy();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void UpdateComponents(float dt) {
            if (_updatedComponents != null) {
                foreach (var comp in CollectionsMarshal.AsSpan(_updatedComponents)) {
                    if (comp.IsActive) comp.Update(dt);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void FixedUpdateGlobal(int updateCount, bool useChildren = false) {
            if (_fixedUpdateComponents != null && updateCount > 0) {
                // do several updates at once to reduce looping overhead
                foreach (var comp in CollectionsMarshal.AsSpan(_fixedUpdateComponents)) {
                    for (var i = 0; i < updateCount; i++) {
                        comp.FixedUpdate();
                    }
                }
            }

            if (useChildren) {
                for (var i = 0; i < ChildrenCount; i++) {
                    this[i].FixedUpdateGlobal(updateCount, useChildren);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void FixedUpdateCustom(float dt) {
            // parents handle their children themselves
            if (Parent != null) return;

            var fixedUpdateCount = 0;
            _timestepCounter += dt * _timescale * Scene.TimeScale;

            // find out needed amount of updates
            while (_timestepCounter >= Scene.FixedTimeStep) {
                _timestepCounter -= Scene.FixedTimeStep;
                fixedUpdateCount++;
            }

            if (fixedUpdateCount > 0) {
                // save previous state data first
                _lastTransformState = Transform.Data;
                for (var i = 0; i < ChildrenCount; i++) {
                    this[i]._lastTransformState = this[i].Transform.Data;
                }

                // invoke the big callback
                FixedUpdateGlobal(fixedUpdateCount, true);

                // call the tick method when there are entity-blocked coroutines
                //if (CoroutineManager._anyEntityBlockedCoroutines)
                //    for (var i = 0; i < fixedUpdateCount; i++) {
                //        CoroutineManager.TickFixedUpdate(Scene);
                //    }
            }

            // set AlphaFrameTime on self and all children
            AlphaFrameTime = _timestepCounter / Scene.FixedTimeStep;
            for (var i = 0; i < ChildrenCount; i++) {
                this[i].AlphaFrameTime = AlphaFrameTime;
            }
        }

        internal void LateUpdate(float dt) {
            // perform transform interpolation
            if (InterpolateTransform) {
                var (lastPos, lastScale, lastRot) = _lastTransformState;
                TransformState = new(
                    Vector2.Lerp(lastPos, Transform.Position, AlphaFrameTime),
                    Vector2.Lerp(lastScale, Transform.Scale, AlphaFrameTime),
                    VectorUtils.LerpAngle(lastRot, Transform.Rotation, AlphaFrameTime)
                );
            } else TransformState = Transform.Data;

            if (_lateUpdateComponents != null) {
                foreach (var comp in CollectionsMarshal.AsSpan(_lateUpdateComponents)) {
                    if (comp.IsActive) comp.LateUpdate(dt);
                }
            }
        }

        internal void ImmediateDestroy() {
            // set this immediately so components know
            // why they're being removed
            IsDestroyed = true;

            // release all the coroutines attached to this entity
            CoroutineManager.StopByTarget(this);

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
            _fixedUpdateComponents?.Clear();
            _lateUpdateComponents?.Clear();
            _updatedComponents?.Clear();
            _components.Clear();

            // destroy children
            for (var i = 0; i < ChildrenCount; i++) {
                this[i].Destroy();
            }

            // detach from the scene
            Scene = null;

            // put the entity into the pool for reuse
            Pool<Entity>.Shared.Return(this);
        }

        public override string ToString() => $"{Name} #{Id}";

        // IPooled impl
        void IPooledInstance.Reset() {
            IsDestroyed = false;
            AlphaFrameTime = 0f;
            TimeScale = 1.0f;
            Name = null;
            Tags = 0;

            _timestepCounter = 0;

            // reset the transform and reassign entity
            Transform.Reset();
            Transform.Entity = this;

            // reset other fields
            IsInitialized = false;
            _updatedComponents?.Pool();
            _updatedComponents = null;
            _fixedUpdateComponents?.Pool();
            _fixedUpdateComponents = null;
            _lateUpdateComponents?.Pool();
            _lateUpdateComponents = null;
            _components.Clear();
            _shouldDestroy = false;
            _isActive = true;
            _scene = null;
        }

        // ICoroutineTarget impl
        float ICoroutineTarget.TimeScale => TimeScale * (Scene?.TimeScale ?? 1.0f);
    }
}
