﻿using Instant2D.Collision;
using Instant2D.Collision.Shapes;
using Instant2D.EC.Collisions;
using Instant2D.Graphics;
using Instant2D.Utils;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.EC.Components {
    /// <summary>
    /// Base class for all collider components. Contains useful methods for overlap checking and moving with callback support.
    /// </summary>
    public abstract class CollisionComponent : Component, ICollider<CollisionComponent> {
        [Flags]
        public enum ColliderTransformBehaviour {
            None,

            /// <summary>
            /// Collider will be scaled.
            /// </summary>
            Scale = 1,

            /// <summary>
            /// Collider will rotate.
            /// </summary>
            Rotate = 2,

            /// <summary>
            /// Collider will be scaled and rotated.
            /// </summary>
            ScaleAndRotate = Scale | Rotate
        }

        int _transformBehaviour = (int)ColliderTransformBehaviour.ScaleAndRotate;

        // trigger stuff
        HashSet<CollisionComponent> _contactTriggers, _tempTriggerSet;
        internal List<ITriggerCallbacksHandler> _triggerHandlers;

        /// <summary>
        /// Anchor point of this collider from x=0, y=0 (top-left) to x=1, y=1 (bottom-right).
        /// </summary>
        protected Vector2 _origin = new(0.5f);

        /// <summary>
        /// Absolute translation applied to collider position. Scales / rotates with Transform when <see cref="ShouldScaleWithTransform"/> / <see cref="ShouldRotateWithTransform"/> is set.
        /// </summary>
        protected Vector2 _offset;

        /// <summary>
        /// Indicate whether or not collider size was explicitly provided by user. If <see langword="true"/>, autosizing will take place. <br/>
        /// You should manually this to <see langword="true"/> when user requests changes to the size.
        /// </summary>
        protected bool _wasSizeSet;

        /// <summary>
        /// If <see langword="true"/>, this collider will not be considered solid and instead cause trigger callbacks to be emitted by other colliders when moving.
        /// </summary>
        public bool IsTrigger;

        /// <summary>
        /// Layer mask used to determine if object of different layers collide with each other.
        /// </summary>
        public int CollidesWithMask { get; set; } = -1;

        /// <summary>
        /// Layer mask used to determine this object's collision layer, meaning it can be shifted/unshifted to the <see cref="CollidesWithMask"/>. <br/>
        /// This could contain more than 1 flag for better flexibility. Use <see cref="IntFlags"/> extensions for more convenience when working with bit operations.
        /// </summary>
        public int LayerMask { get; set; } = IntFlags.SetFlagExclusive(0);

        /// <summary>
        /// Collision shape used for this collider. Avoid modifying any values of it directly.
        /// </summary>
        public ICollisionShape Shape { get; protected set; }

        /// <summary>
        /// The region at which this collider resides in spatial hash.
        /// </summary>
        public Rectangle SpatialHashRegion { get; set; }

        /// <summary>
        /// Anchor point of this collider. Defaults to the center of collider (0.5, 0.5).
        /// </summary>
        public Vector2 Origin {
            get => _origin;
            set {
                _origin = value;

                // recalculate internal values
                UpdateCollider();
            }
        }

        /// <inheritdoc cref="_offset"/>
        public Vector2 Offset {
            get => _offset;
            set {
                _offset = value;

                // recalculate internal values
                UpdateCollider();
            }
        }

        /// <summary>
        /// Defines how collider reacts to transform changes. By default, it will scale and rotate accordingly. <br/>
        /// Can be disabled to improve performance and stability by switching to optimized collision handling for boxes (AABB).
        /// </summary>
        public ColliderTransformBehaviour TransformBehaviour {
            get => (ColliderTransformBehaviour)_transformBehaviour;
            set {
                _transformBehaviour = (int)value;
                UpdateCollider();
            }
        }

        /// <summary>
        /// When <see langword="true"/>, shape of the collider will be modified based on Entity's <see cref="Transform{T}.Scale"/>.
        /// </summary>
        public bool ShouldScaleWithTransform {
            get => IntFlags.IsFlagSet(_transformBehaviour, (int)ColliderTransformBehaviour.Scale);
            set {
                if (value) IntFlags.SetFlag(ref _transformBehaviour, (int)ColliderTransformBehaviour.Scale);
                else IntFlags.RemoveFlag(ref _transformBehaviour, (int)ColliderTransformBehaviour.Scale);

                UpdateCollider();
            }
        }

        /// <summary>
        /// When <see langword="true"/>, shape of the collider will be modified based on Entity's <see cref="Transform{T}.Rotation"/>. <br/>
        /// Could be set to <see langword="false"/> for box colliders to reduce the overhead introduced by rotation calculation.
        /// </summary>
        public bool ShouldRotateWithTransform {
            get => IntFlags.IsFlagSet(_transformBehaviour, (int)ColliderTransformBehaviour.Rotate);
            set {
                if (value) IntFlags.SetFlag(ref _transformBehaviour, (int)ColliderTransformBehaviour.Rotate);
                else IntFlags.RemoveFlag(ref _transformBehaviour, (int)ColliderTransformBehaviour.Rotate);

                UpdateCollider();
            }
        }

        public override void Initialize() {
            // check BaseCollider
            if (Shape is null) {
                InstantApp.Logger.Error($"Shape of {GetType().Name} wasn't initialized, disabling.");
                Entity.RemoveComponent(this);
                return;
            }

            // initialize the collision manager and notify the user
            // in case they would want to tweak the chunk size
            if (Scene.Collisions is null) {
                InstantApp.Logger.Info($"Collider added to a scene without Collisions intitialized, defaulting to SpatialHash with grid size of {SpatialHash<CollisionComponent>.DEFAULT_CHUNK_SIZE}.");
                Scene.Collisions = new();
            }
        }

        public override void OnRemovedFromEntity() {
            Scene.Collisions.RemoveCollider(this);

            // dispose of handlers
            if (_triggerHandlers != null) {
                _triggerHandlers.Pool();
                _triggerHandlers = null;
            }
        }

        public override void OnEnabled() {
            // register collider when this component is enabled
            UpdateCollider();
        }

        public override void OnDisabled() { 
            // deregister collider when this component is disabled
            Scene.Collisions.RemoveCollider(this);
        }

        public override void OnTransformUpdated(TransformComponentType components) {
            if (!IsActive)
                return;

            // update components when needed
            if ((components & TransformComponentType.Position) != 0 ||
                (ShouldScaleWithTransform && (components & TransformComponentType.Scale) != 0) ||
                (ShouldRotateWithTransform && (components & TransformComponentType.Rotation) != 0))
                UpdateCollider();
        }

        /// <summary>
        /// Move the collider in spatial hash and apply necessary shape values.
        /// </summary>
        public virtual void UpdateCollider() {
            // remove and readd the collider into the hash
            Scene.Collisions.RemoveCollider(this);
            Scene.Collisions.AddCollider(this);
        }

        /// <summary>
        /// Calculates movement of the object based on <paramref name="velocity"/>. If it collides into something, <paramref name="hits"/> will be populated with collision data,
        /// as well as <paramref name="velocity"/> will be recalculated to prevent the collision. <br/> 
        /// You'll have to return the <paramref name="hits"/> list back into the pool using <c><paramref name="hits"/>.Pool()</c> and move the entity by <paramref name="velocity"/> yourself.
        /// </summary>
        public bool CalculateMovementCollisions(ref Vector2 velocity, out List<CollisionResult<CollisionComponent>> hits) {
            hits = null;

            // generate bounds updated by the movement
            var bounds = Shape.Bounds;
            bounds.Position += velocity;

            // do a broad sweep to find all the potential collisions
            if (Scene.Collisions.Broadphase(bounds, out var colliders, CollidesWithMask)) {
                for (var i = 0; i < colliders.Count; i++) {
                    var other = colliders[i];

                    // check for collision, skipping self and triggers
                    if (other != this && CollidesWith(other, velocity, out var hit)) {
                        if (!other.IsTrigger) velocity -= hit.PenetrationVector;

                        // add the hit to hits array
                        hits ??= ListPool<CollisionResult<CollisionComponent>>.Rent();
                        hits.Add(hit);
                    }
                }

                colliders.Pool();
            }

            // return true only if we did something
            return hits != null;
        }

        public void Move(Vector2 velocity) {
            if (CalculateMovementCollisions(ref velocity, out var hits))
                hits.Pool();

            Transform.Position += velocity;
        }

        #region Collision Methods

        public bool CollidesWith(CollisionComponent other, out CollisionResult<CollisionComponent> result) {
            if (Shape.CollidesWith(other.Shape, out var normal, out var penetrationVector)) {
                result = new(this, other, normal, penetrationVector);
                return true;
            }

            result = default;
            return false;
        }

        public bool CollidesWithLine(Vector2 start, Vector2 end, out LineCastResult<CollisionComponent> result) {
            if (Shape.CollidesWithLine(start, end, out var distance, out var intersection, out var normal)) {
                result = new(this, start, end, distance, intersection, normal);
                return true;
            }

            result = default;
            return false;
        }

        /// <summary>
        /// Checks of two collision components could collider with <paramref name="velocity"/> applied to them.
        /// </summary>
        public bool CollidesWith(CollisionComponent other, Vector2 velocity, out CollisionResult<CollisionComponent> result) {
            var oldPosition = Shape.Position;
            Shape.Position += velocity;

            // offset the BaseCollider and check for collision, then reset
            var hit = CollidesWith(other, out result);
            Shape.Position = oldPosition;

            return hit;
        }

        /// <inheritdoc cref="CollidesWithAny(out CollisionResult{CollisionComponent}, int, Vector2)"/>
        public bool CollidesWithAny(int layerMask = -1, Vector2 velocity = default, bool ignoreTriggers = true) => CollidesWithAny(out _, layerMask, velocity, ignoreTriggers);

        /// <summary>
        /// Checks for any collisions using specified <paramref name="layerMask"/> and optional <paramref name="velocity"/>.
        /// </summary>
        public bool CollidesWithAny(out CollisionResult<CollisionComponent> collision, int layerMask = -1, Vector2 velocity = default, bool ignoreTriggers = true) {
            var oldPosition = Shape.Position;
            Shape.Position += velocity;

            // broadphase potential hits and check more precisely
            if (Scene.Collisions.Broadphase(Shape.Bounds, out var colliders, layerMask)) {
                foreach (var potential in colliders) {
                    if (potential != this && CollidesWith(potential, out collision)) {
                        if (ignoreTriggers && potential.IsTrigger)
                            continue;

                        Shape.Position = oldPosition;
                        return true;
                    }
                }

                colliders.Pool();
            }

            // revert to previous position
            Shape.Position = oldPosition;

            collision = default;
            return false;
        }

        /// <summary>
        /// Checks if this collider overlaps another Entity. <see cref="Entity.GetComponents{T}"/> is used to find all of the colliders. <br/>
        /// If <paramref name="preciseCheck"/> is set to <see langword="false"/>, only the bounds check will take place.
        /// </summary>
        public bool Overlaps(Entity other, bool preciseCheck = true) {
            foreach (var collider in other.GetComponents<CollisionComponent>()) {
                // break early if overlap is found
                if (Overlaps(collider, preciseCheck)) {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if this collider overlaps <paramref name="other"/>. If <paramref name="preciseCheck"/> is set to <see langword="false"/>, only the bounds check will take place.
        /// </summary>
        public bool Overlaps(CollisionComponent other, bool preciseCheck = true) {
            if (!preciseCheck) {
                // simply check bounds for intersections, that's it
                return Shape.Bounds.Contains(other.Shape.Bounds);
            }

            // go for more precise approach and call stuff
            return Shape.CheckOverlap(other.Shape);
        }

        #endregion

        #region Trigger Handling

        void TriggerCallbacks(CollisionComponent trigger, bool isEntering) {
            if (_triggerHandlers == null)
                return;

            for (var i = 0; i < _triggerHandlers.Count; i++) {
                if (isEntering) {
                    _triggerHandlers[i].OnTriggerEnter(this, trigger);
                } else {
                    _triggerHandlers[i].OnTriggerExit(this, trigger);
                }
            }
        }

        /// <summary>
        /// Update method for the triggers. Will push the events to all <see cref="ITriggerCallbacksHandler"/> attached to this collider and triggers.
        /// </summary>
        public void UpdateTriggers() {
            // initialize the sets
            if (_tempTriggerSet == null) {
                _contactTriggers = [];
                _tempTriggerSet = [];
            }

            // scan nearby area for overlapping triggers
            if (Scene.Collisions.Broadphase(Shape.Bounds, out var colliders, CollidesWithMask)) {
                for (var i = 0; i < colliders.Count; i++) {
                    var trigger = colliders[i];

                    // either object has to be the trigger
                    // I'm not sure how useful is it to test two triggers colliding though
                    if (!IsTrigger && !trigger.IsTrigger)
                        continue;

                    if (Shape.CheckOverlap(trigger.Shape)) {
                        // OnTriggerEnter
                        if (_contactTriggers.Add(trigger)) {
                            TriggerCallbacks(trigger, true);
                            trigger.TriggerCallbacks(this, true);
                        }

                        // store handled triggers for later
                        _tempTriggerSet.Add(trigger);
                    }
                }

                colliders.Pool();
            }
            
            // call exit events
            foreach (var trigger in _contactTriggers) {
                // OnTriggerExit
                if (!_tempTriggerSet.Contains(trigger)) {
                    TriggerCallbacks(trigger, false);
                    trigger.TriggerCallbacks(this, false);
                    _contactTriggers.Remove(trigger);
                }
            }

            _tempTriggerSet.Clear();
        }

        /// <summary>
        /// Clears all triggers this object is in contact with. Will trigger all events as if the object just entered next time <see cref="UpdateTriggers"/> is called.
        /// </summary>
        public void ReleaseTriggers() {
            if (_contactTriggers is null)
                return;

            foreach (var trigger in _contactTriggers) {
                TriggerCallbacks(trigger, false);
                trigger.TriggerCallbacks(this, false);
            }

            _contactTriggers.Clear();
        }

        #endregion

        public override string ToString() => $"{GetType().Name} ({Entity})";
    }
}
