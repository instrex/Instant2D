using Instant2D.Graphics;
using Instant2D.Utils;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Instant2D.EC.Rendering {
    /// <summary>
    /// Render layer used to present <see cref="RenderableComponent"/>s.
    /// </summary>
    public class EntityLayer : IRenderLayer {
        /// <summary>
        /// List of components this layer should render.
        /// </summary>
        public List<RenderableComponent> Components = new();

        /// <summary>
        /// A mask used to determine if entity should be rendered by this layer. By default, all of the entities are included.
        /// </summary>
        public int IncludeEntityTags = -1;

        /// <summary>
        /// Camera this layer uses. If <see langword="null"/>, <see cref="Scene.Camera"/> will be used instead.
        /// </summary>
        public CameraComponent Camera;

        /// <summary>
        /// When <see langword="true"/>, components outside of <see cref="CameraComponent.Bounds"/> will not be rendered.
        /// </summary>
        public bool EnableCulling = true;

        /// <summary>
        /// Optional color used to fill the space underneath the objects.
        /// </summary>
        public Color? BackgroundColor;

        internal bool _drawOrderDirty = true;

        /// <summary>
        /// Custom predicate function used to determine if object should be rendered or not.
        /// </summary>
        protected virtual bool ShouldDrawObject(RenderableComponent component) => true;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SortComponents() {
            _drawOrderDirty = false;
            Components.Sort();
        }

        // IRenderLayer impl
        public bool IsActive { get; set; }
        public bool ShouldPresent { get; set; }
        public Scene Scene { get; init; }
        public float Order { get; init; }
        public string Name { get; init; }

        public virtual void Prepare() { 
            // no preparation required
        }

        public virtual void Present(DrawingContext drawing) {
            if (BackgroundColor is Color bgColor)
                drawing.DrawRectangle(new(Vector2.Zero, Scene.Resolution.renderTargetSize.ToVector2()), bgColor);

            if (Components == null || Components.Count == 0)
                return;

            // sort components on-demand
            if (_drawOrderDirty)
                SortComponents();

            // prepare camera
            var camera = Camera ?? Scene.Camera;
            camera.ForceUpdate();

            var bounds = camera.Bounds;

            Material material = default;
            foreach (var component in CollectionsMarshal.AsSpan(Components)) {
                // cull the objects outside the camera view
                if (EnableCulling && !component.DisableCulling && !(component.IsVisible = bounds.Intersects(component.Bounds)))
                    continue;

                // skip entities with excluded tags
                if (IncludeEntityTags != -1 && component.Entity.Tags != 0 && !IncludeEntityTags.IsFlagSet(component.Entity.Tags, false))
                    continue;

                // run custom conditions
                if (!ShouldDrawObject(component))
                    continue;

                if (material != component.Material) {
                    if (material != null)
                        drawing.Pop();

                    // restart the batch when new material appears
                    drawing.Push(material = component.Material, camera.TransformMatrix);
                }

                // finally, present the object
                component.Draw(drawing, camera);
            }

            // reset the batch
            if (material != null)
                drawing.Pop();
        }
    }
}
