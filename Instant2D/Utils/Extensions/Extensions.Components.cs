using Instant2D.EC.Components;
using Instant2D.Graphics;
using Instant2D.Utils;
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

        /// <inheritdoc cref="CollisionComponent.CollidesWithMask"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T SetCollidesWith<T>(this T collisionComponent, int collidesWithMask) where T: CollisionComponent {
            collisionComponent.CollidesWithMask = collidesWithMask;
            return collisionComponent;
        }

        /// <inheritdoc cref="CollisionComponent.LayerMask"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T SetLayerMask<T>(this T collisionComponent, int layerMask) where T : CollisionComponent {
            collisionComponent.LayerMask = layerMask;
            return collisionComponent;
        }

        /// <summary>
        /// Set collision layer as unshifted flag. Modifies <see cref="CollisionComponent.LayerMask"/> using <see cref="IntFlags.SetFlagExclusive(int, bool)"/>. <br/>
        /// You can set multiple flags by modifying the <see cref="CollisionComponent.LayerMask"/> manually.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T SetCollisionLayer<T>(this T collisionComponent, int unshiftedCollisionFlag) where T : CollisionComponent {
            collisionComponent.LayerMask = IntFlags.SetFlagExclusive(unshiftedCollisionFlag);
            return collisionComponent;
        }

        /// <summary>
        /// Adds an unshifted collision flag to <see cref="CollisionComponent.CollidesWithMask"/> using <see cref="IntFlags.SetFlag(int, int)"/>. <br/>
        /// This means that this collider will react to objects with the provided flag set in their <see cref="CollisionComponent.LayerMask"/>. <br/>
        /// If <see cref="CollisionComponent.CollidesWithMask"/> was <c>-1</c> before calling this, the object will collide <b>only</b> with the specified layer.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T AddCollisionFlag<T>(this T collisionComponent, int unshiftedCollisionFlag) where T : CollisionComponent {
            collisionComponent.CollidesWithMask = collisionComponent.CollidesWithMask == -1 ?
                IntFlags.SetFlagExclusive(unshiftedCollisionFlag) :
                IntFlags.SetFlag(collisionComponent.CollidesWithMask, unshiftedCollisionFlag);
            return collisionComponent;
        }

        /// <summary>
        /// Removes an unshifted collision flag from <see cref="CollisionComponent.CollidesWithMask"/> using <see cref="IntFlags.RemoveFlag(int, int)"/>. <br/>
        /// This means that this collider will no longer react to objects with the provided flag set in their <see cref="CollisionComponent.LayerMask"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T RemoveCollisionFlag<T>(this T collisionComponent, int unshiftedCollisionFlag) where T : CollisionComponent {
            collisionComponent.CollidesWithMask = IntFlags.RemoveFlag(collisionComponent.CollidesWithMask, unshiftedCollisionFlag);
            return collisionComponent;
        }

        /// <inheritdoc cref="CollisionComponent.ShouldRotateWithTransform"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T SetShouldRotateWithTransform<T>(this T collisionComponent, bool shouldRotateWithTransform) where T : CollisionComponent {
            collisionComponent.ShouldRotateWithTransform = shouldRotateWithTransform;
            return collisionComponent;
        }

        /// <inheritdoc cref="CollisionComponent.ShouldScaleWithTransform"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T SetShouldScaleWithTransform<T>(this T collisionComponent, bool shouldScaleWithTransform) where T : CollisionComponent {
            collisionComponent.ShouldScaleWithTransform = shouldScaleWithTransform;
            return collisionComponent;
        }

        /// <summary>
        /// Attempts to automatically determine collider size using renderables attached to entity.
        /// </summary>
        public static T AutoResize<T>(this T collisionComponent) where T : CollisionComponent {
            // use renderables when possible
            if (collisionComponent.Entity.TryGetComponent<RenderableComponent>(out var renderableComponent))
                collisionComponent.AutoSize(renderableComponent.Bounds);

            return collisionComponent;
        }

        #endregion
    }
}
