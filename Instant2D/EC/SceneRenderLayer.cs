﻿using Instant2D.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Instant2D.EC {
    /// <summary>
    /// A batch of <see cref="RenderableComponent"/>s used to better organize rendering and entity sorting.
    /// </summary>
    public class SceneRenderLayer {
        /// <summary>
        /// All of the objects queued up for rendering on this layer. 
        /// This field is automatically updated/sorted.
        /// </summary>
        protected internal List<RenderableComponent> Objects = new(128);

        /// <summary>
        /// An identifier of this Layer.
        /// </summary>
        public string Name {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get; init; 
        }

        internal RenderTarget2D _renderTarget;
        internal bool _orderDirty;
        internal Scene _scene;
        Material _currentMaterial;

        /// <summary>
        /// The camera this layer uses. If it's <see langword="null"/>, Scene's camera will be used.
        /// </summary>
        public ICamera Camera;

        /// <summary>
        /// A color which the RenderTarget will be cleared to. Defaults to <see cref="Color.Transparent"/>.
        /// </summary>
        public Color BackgroundColor = Color.Transparent;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void InternalDraw() {
            // first, sort the objects if needed
            if (_orderDirty) {
                _orderDirty = false;
                Objects.Sort();
            }

            var drawing = GraphicsManager.Backend;
            var camera = Camera ?? _scene.Camera;
            var bounds = camera.Bounds;
            var cullingEnabled = bounds == default;

            // begin the batch
            drawing.Push(Material.Default, camera.TransformMatrix);

            // draw everything
            for (var i = 0; i < Objects.Count; i++) {
                var obj = Objects[i];

                if (cullingEnabled) {
                    // don't draw the objects outside view
                    if (!bounds.Contains(obj.Entity.Transform.Position))
                        continue;
                }

                // swap the material when needed
                if (obj._material is { } material && material != _currentMaterial) {
                    _currentMaterial = material;
                    drawing.Push(material);
                }

                obj.Draw(drawing, camera);
            }

            // flush the batch
            drawing.Pop(true);
        }
    }
}