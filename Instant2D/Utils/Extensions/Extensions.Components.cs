using Instant2D.EC.Components;
using Instant2D.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Instant2D.EC {
    public static class Extensions {
        #region Renderable Component

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

        #endregion

        #region Sprite Renderer

        /// <inheritdoc cref="SpriteComponent.FlipY"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T SetFlipY<T>(this T spriteRenderer, bool flipY) where T: SpriteComponent {
            spriteRenderer.FlipY = flipY;
            return spriteRenderer;
        }

        /// <inheritdoc cref="SpriteComponent.FlipX"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T SetFlipX<T>(this T spriteRenderer, bool flipX) where T : SpriteComponent {
            spriteRenderer.FlipX = flipX;
            return spriteRenderer;
        }

        /// <inheritdoc cref="SpriteComponent.Sprite"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T SetSprite<T>(this T spriteRenderer, Sprite sprite) where T : SpriteComponent {
            spriteRenderer.Sprite = sprite;
            return spriteRenderer;
        }

        /// <inheritdoc cref="SpriteComponent.SpriteEffects"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T SetSpriteEffects<T>(this T spriteRenderer, SpriteEffects spriteEffects) where T : SpriteComponent {
            spriteRenderer.SpriteEffects = spriteEffects;
            return spriteRenderer;
        }

        #endregion

        #region Collision Component

        /// <inheritdoc cref="CollisionComponent.CollidesWith"/>
        public static T SetCollidesWith<T>(this T collisionComponent, int collidesWith) where T: CollisionComponent {
            collisionComponent.CollidesWith = collidesWith;
            return collisionComponent;
        }

        /// <inheritdoc cref="CollisionComponent.CollisionLayer"/>
        public static T SetCollisionLayer<T>(this T collisionComponent, int collisionLayer) where T : CollisionComponent {
            collisionComponent.CollisionLayer = collisionLayer;
            return collisionComponent;
        }

        #endregion
    }
}
