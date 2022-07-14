using Instant2D.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Instant2D.EC {
    public static class Extensions {
        /// <inheritdoc cref="RenderableComponent.Material"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T SetMaterial<T>(this T renderableComponent, Material material) where T: RenderableComponent {
            renderableComponent.Material = material;
            return renderableComponent;
        }

        /// <inheritdoc cref="RenderableComponent.Color"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T SetColor<T>(this T renderableComponent, Color color) where T : RenderableComponent {
            renderableComponent.Color = color;
            return renderableComponent;
        }

        /// <inheritdoc cref="RenderableComponent.Depth"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T SetDepth<T>(this T renderableComponent, float depth) where T : RenderableComponent {
            renderableComponent.Depth = depth;
            return renderableComponent;
        }

        /// <inheritdoc cref="RenderableComponent.Z"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T SetZ<T>(this T renderableComponent, int z) where T : RenderableComponent {
            renderableComponent.Z = z;
            return renderableComponent;
        }

        /// <inheritdoc cref="RenderableComponent.RenderLayer"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T SetRenderLayer<T>(this T renderableComponent, SceneRenderLayer renderLayer) where T : RenderableComponent {
            renderableComponent.RenderLayer = renderLayer;
            return renderableComponent;
        }

        /// <inheritdoc cref="RenderableComponent.RenderLayer"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T SetRenderLayer<T>(this T renderableComponent, string layerName) where T : RenderableComponent {
            for (var i = 0; i < renderableComponent.Scene.RenderLayers.Count; i++) {
                var layer = renderableComponent.Scene.RenderLayers[i];

                // search the layer by name
                if (layer.Name == layerName) {
                    renderableComponent.RenderLayer = layer;
                    return renderableComponent;
                }
            }

            return renderableComponent;
        }
    }
}
