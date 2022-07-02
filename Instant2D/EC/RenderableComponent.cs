using Instant2D.Graphics;
using Instant2D.Utils.Math;
using Microsoft.Xna.Framework;
using System;
using System.Runtime.CompilerServices;

namespace Instant2D.EC {
    public abstract class RenderableComponent : Component, IComparable<RenderableComponent> {
        SceneRenderLayer _layer;

        // those are internal so I can acces them easier inside SceneLayers
        internal Material? _material;
        internal float _depth;
        internal int _z;

        public Color Color = Color.White;

        /// <summary>
        /// Material which this component will set before calling <see cref="Draw"/>.
        /// </summary>
        public Material Material { 
            get => _material ?? Material.Default; 
            set {
                // if material's the same, no need to reassign it
                if (_material == value)
                    return;

                _material = value;

                // mark the order dirty 
                if (_layer != null)
                    _layer._orderDirty = true;
            }
        }

        /// <summary>
        /// The layer this component resides on.
        /// </summary>
        public SceneRenderLayer RenderLayer {
            get => _layer;
            set {
                if (_layer == value)
                    return;

                // remove the object from the previous layer
                // (if it exists)
                _layer?.Objects.Remove(this);
                _layer = value;

                if (_layer != null) {
                    // add the object and update the order
                    _layer.Objects.Add(this);
                    _layer._orderDirty = true;
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
                    _layer._orderDirty = true;
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
                    _layer._orderDirty = true;
                }
            }
        }

        /// <summary>
        /// Gets this component's bounds used for culling. By default, it is a 64x64 box wrapped around the center of the Entity.
        /// </summary>
        public virtual RectangleF Bounds {
            get => new(Entity.Transform.Position + new Vector2(-32), new(64));
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

            // compare Materials as the last resort...
            return _material.GetHashCode().CompareTo(other._material.GetHashCode());
        }

        #region Setters

        /// <inheritdoc cref="Z"/>
        public RenderableComponent SetZ(int z) {
            Z = z;
            return this;
        }

        /// <inheritdoc cref="Depth"/>
        public RenderableComponent SetDepth(float depth) {
            Depth = depth;
            return this;
        }

        /// <summary> Attempts to search the SceneRenderLayer by name. </summary>
        public RenderableComponent SetRenderLayer(string name) {
            for (var i = 0; i < Scene.RenderLayers.Count; i++) {
                var layer = Scene.RenderLayers[i];

                // search the layer by name
                if (layer.Name == name) {
                    RenderLayer = layer;
                    return this;
                }
            }

            return this;
        }

        /// <inheritdoc cref="RenderLayer"/>
        public RenderableComponent SetRenderLayer(SceneRenderLayer layer) {
            RenderLayer = layer;
            return this;
        }

        #endregion

        /// <summary>
        /// The drawing function. Note that before this, <see cref="IDrawingBackend.Push(in Material, Microsoft.Xna.Framework.Matrix)"/>
        /// is called with <see cref="Material"/> and <see cref="ICamera.TransformMatrix"/>. <br/>
        /// Use Push/Pop functions if you happen to need to change the Material mid-rendering.
        /// </summary>
        public abstract void Draw(IDrawingBackend drawing, CameraComponent camera);
    }
}
