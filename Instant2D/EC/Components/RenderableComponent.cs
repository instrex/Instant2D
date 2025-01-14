using Instant2D.EC.Rendering;
using Instant2D.Graphics;
using Instant2D.Utils;
using Microsoft.Xna.Framework;
using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace Instant2D.EC {
    public abstract class RenderableComponent : Component, IComparable<RenderableComponent> {
        EntityLayer _layer;

        // used for bounds calculations
        static Matrix2D _tempMatrix, _tempTransform;

        // those are internal so I can acces them easier inside SceneLayers
        internal Material _material = Material.AlphaBlend;
        internal float _depth;
        internal int _z;

        // bounds business
        protected bool _boundsDirty = true;
        RectangleF _bounds;

        /// <summary>
        /// When <see langword="true"/>, the object will not be culled when out of view.
        /// </summary>
        public bool DisableCulling;

        bool _isVisible = true;

        public Color Color = Color.White;

        /// <summary>
        /// An additional offset applied when rendering. Note that this doesn't rotate nor scale with <see cref="Entity.Transform"/>.
        /// </summary>
        public Vector2 Offset;

        /// <summary>
        /// Whether or not this RenderableComponent was visible during last render.
        /// </summary>
        public bool IsVisible {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _isVisible;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal set {
                if (_isVisible == value)
                    return;

                _isVisible = value;
                OnVisibilityChanged();
            } 
        }

        /// <summary>
        /// Material which this component will set before calling <see cref="Draw"/>.
        /// </summary>
        public Material Material { 
            get => _material; 
            set {
                // if material's the same, no need to reassign it
                if (_material == value)
                    return;

                _material = value;

                // mark the order dirty 
                if (_layer != null)
                    _layer._drawOrderDirty = true;
            }
        }

        /// <summary>
        /// The layer this component resides on.
        /// </summary>
        public EntityLayer RenderLayer {
            get => _layer;
            set {
                // remove the object from the previous layer
                // (if it exists)
                _layer?.Components.Remove(this);
                _layer = value;

                if (_layer != null && IsActive && Entity.IsActive) {
                    // add the object and update the order
                    _layer.Components.Add(this);
                    _layer._drawOrderDirty = true;
                }
            }
        }

        /// <summary>
        /// Z-index of this component in the layer space, used for sorting. Changing this will trigger a scene layer update. <br/>
        /// For even more sorting options, check <see cref="Depth"/>.
        /// </summary>
        public int Z {
            get => _z;
            set {
                if (_z == value)
                    return;

                _z = value;
                if (_layer != null) {
                    _layer._drawOrderDirty = true;
                }
            }
        }

        /// <summary>
        /// Depth of this component in the layer space (a value between 0f and 1f), used for sorting. Changing this will trigger a scene layer update. <br/>
        /// For even more sorting options, check <see cref="Z"/>.
        /// </summary>
        public float Depth {
            get => _depth;
            set {
                // clamp the value betwen 0.0f and 1.0f
                var clamped = Math.Clamp(value, 0f, 1f);

                if (_depth == clamped)
                    return;

                _depth = clamped;
                if (_layer != null) {
                    _layer._drawOrderDirty = true;
                }
            }
        }

        /// <summary>
        /// Gets this component's bounds used for culling. 
        /// </summary>
        public RectangleF Bounds {
            get {
                if (_boundsDirty) {
                    RecalculateBounds(ref _bounds);
                    _boundsDirty = false;
                }

                return _bounds;
            }
        }

        /// <summary>
        /// Get the camera this component will be displayed with. If <see cref="RenderLayer"/> doesn't have one defined, returns <see cref="Scene.Camera"/>.
        /// </summary>
        public CameraComponent Camera {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _layer?.Camera ?? Scene?.Camera;
        }

        /// <summary>
        /// Compares two renderable components based on:
        /// <list type="number">
        /// <item><see cref="Z"/> index</item>
        /// <item><see cref="Depth"/></item>
        /// <item>Equality of <see cref="Material"/></item>
        /// </list>
        /// </summary>
        public int CompareTo(RenderableComponent other) {
            var z = _z.CompareTo(other._z);

            // test Z first, if it's equal continue to depth testing
            if (z != 0) {
                return z;
            }
            
            // reverse the depth comparison so it's actually from 0 (nearest to screen) to 1 (furthest from screen)
            var depth = _depth.CompareTo(other._depth) * -1;

            // test depth second, if it's equal again go on with material testing
            if (depth != 0) {
                return depth;
            }

            var materials = _material.GetHashCode().CompareTo(other._material.GetHashCode());

            if (materials != 0) {
                return materials;
            }

            // compare entity ids as last resort
            return Entity.Id.CompareTo(other.Entity.Id);
        }

        public override void OnTransformUpdated(TransformComponentType components) {
            _boundsDirty = true;
        }

        public override void OnEnabled() {
            RenderLayer = _layer;
        }

        public override void OnDisabled() {
            var layer = _layer;

            // detach the component from the layer,
            // but keep the value to reattach it later
            RenderLayer = null;
            _layer = layer;
        }

        public override void OnRemovedFromEntity() {
            RenderLayer = null;
        }

        public override void PostInitialize() {
            if (_layer == null) {
                // assign default renderlayer if its null
                RenderLayer = Scene.DefaultRenderLayer;
            }
        }

        /// <summary>
        /// Helper method for calculating common renderable component bounds.
        /// </summary>
        public static RectangleF CalculateBounds(Vector2 position, Vector2 offset, Vector2 origin, Vector2 size, float rotation, Vector2 scale) {
            if (rotation == 0f) {
                return new(position + offset - origin * scale, size * scale);
            }

            var worldPosX = position.X + offset.X;
            var worldPosY = position.Y + offset.Y;

            Matrix2D.CreateTranslation(-worldPosX - origin.X, -worldPosY - origin.Y, out _tempTransform);
            Matrix2D.CreateScale(scale.X, scale.Y, out _tempMatrix); // scale ->
            Matrix2D.Multiply(ref _tempTransform, ref _tempMatrix, out _tempTransform);
            Matrix2D.CreateRotation(rotation, out _tempMatrix); // rotate ->
            Matrix2D.Multiply(ref _tempTransform, ref _tempMatrix, out _tempTransform);
            Matrix2D.CreateTranslation(worldPosX, worldPosY, out _tempMatrix); // translate back
            Matrix2D.Multiply(ref _tempTransform, ref _tempMatrix, out _tempTransform);

            // get four rectangle points to construct bounds
            var topLeft = new Vector2(worldPosX, worldPosY);
            var topRight = new Vector2(worldPosX + size.X, worldPosY);
            var bottomLeft = new Vector2(worldPosX, worldPosY + size.Y);
            var bottomRight = new Vector2(worldPosX + size.X, worldPosY + size.Y);

            // transform the corners
            VectorUtils.Transform(ref topLeft, ref _tempTransform, out topLeft);
            VectorUtils.Transform(ref topRight, ref _tempTransform, out topRight);
            VectorUtils.Transform(ref bottomLeft, ref _tempTransform, out bottomLeft);
            VectorUtils.Transform(ref bottomRight, ref _tempTransform, out bottomRight);

            // create the bounds
            return RectangleF.FromCoordinates(topLeft, topRight, bottomLeft, bottomRight);
        }

        /// <summary>
        /// Recalculate bounds used for camera culling. By default, it is a 64x64 box wrapped around the center of the Entity. <br/>
        /// When finished, <see cref="Bounds"/> value will be set to <paramref name="bounds"/>. This is only called when needed (see <see cref="_boundsDirty"/>).
        /// </summary>
        protected virtual void RecalculateBounds(ref RectangleF bounds) {
            bounds = new(Entity.Transform.Position + new Vector2(-32), new(64));
        }

        /// <summary>
        /// Is called whenever the object appears inside Camera bounds. <see cref="Bounds"/> property must be set and Camera should support culling for this to be called. <br/>
        /// Use <see cref="IsVisible"/> to determine current visibility.
        /// </summary>
        protected virtual void OnVisibilityChanged() { }

        /// <summary>
        /// The drawing function. Note that before this, <see cref="IDrawingBackend.Push(in Material, Microsoft.Xna.Framework.Matrix)"/>
        /// is called with <see cref="Material"/> and <see cref="ICamera.TransformMatrix"/>. <br/>
        /// Use Push/Pop functions if you happen to need to change the Material mid-rendering.
        /// </summary>
        public abstract void Draw(DrawingContext drawing, CameraComponent camera);
    }
}
