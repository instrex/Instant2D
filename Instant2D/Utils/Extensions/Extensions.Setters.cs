using Instant2D.Audio;
using Instant2D.EC.Collisions;
using Instant2D.EC.Components;
using Instant2D.EC.Rendering;
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
    // have to put some methods here because they clash with each other in Extensions.cs class
    public static class RenderableComponentExtensions {
        /// <inheritdoc cref="Component.IsActive"/>
        public static T SetActive<T>(this T component, bool isActive) where T : Component {
            component.IsActive = isActive;
            return component;
        }

        /// <inheritdoc cref="RenderableComponent.Material"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T SetMaterial<T>(this T renderableComponent, Material material) where T : RenderableComponent {
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

        /// <inheritdoc cref="RenderableComponent.Offset"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T SetOffset<T>(this T renderableComponent, Vector2 offset) where T : RenderableComponent {
            renderableComponent.Offset = offset;
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
        public static T SetRenderLayer<T>(this T renderableComponent, EntityLayer renderLayer) where T : RenderableComponent {
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
                    renderableComponent.RenderLayer = layer switch {
                        EntityLayer entityLayer => entityLayer,
                        INestedRenderLayer nestedLayer when nestedLayer.Content is EntityLayer nestedEntityLayer => nestedEntityLayer,
                        _ => throw new InvalidOperationException($"Cannot add renderable component to a non-entity layer of type '{layer.GetType().Name}'"),
                    };

                    return renderableComponent;
                }
            }

            throw new InvalidOperationException($"Invalid render layer '{layerName}'");
        }
    }

    public static class Extensions {
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

        /// <inheritdoc cref="CollisionComponent.Origin"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T SetOrigin<T>(this T collisionComponent, Vector2 origin) where T : CollisionComponent {
            collisionComponent.Origin = origin;
            return collisionComponent;
        }

        /// <inheritdoc cref="CollisionComponent.Offset"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T SetOffset<T>(this T collisionComponent, Vector2 offset) where T : CollisionComponent {
            collisionComponent.Offset = offset;
            return collisionComponent;
        }

        /// <inheritdoc cref="CollisionComponent.IsTrigger"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T SetIsTrigger<T>(this T collisionComponent, bool isTrigger = true) where T : CollisionComponent {
            collisionComponent.IsTrigger = isTrigger;
            return collisionComponent;
        }

        /// <summary>
        /// Adds specified <paramref name="triggerHandler"/> to the collection of handlers. Provided object will now react to events after <see cref="CollisionComponent.UpdateTriggers"/> is called.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T AddTriggerHandler<T>(this T collisionComponent, ITriggerCallbacksHandler triggerHandler) where T : CollisionComponent {
            // initialize a new list when needed
            collisionComponent._triggerHandlers ??= ListPool<ITriggerCallbacksHandler>.Get();
            collisionComponent._triggerHandlers.Add(triggerHandler);
            return collisionComponent;
        }

        /// <summary>
        /// Removes the <paramref name="triggerHandler"/> from the collection of handlers and frees the list if there's nothing left.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T RemoveTriggerHandler<T>(this T collisionComponent, ITriggerCallbacksHandler triggerHandler) where T : CollisionComponent {
            if (collisionComponent._triggerHandlers == null)
                return collisionComponent;

            // free the list if it's the last thing
            if (collisionComponent._triggerHandlers.Remove(triggerHandler) && collisionComponent._triggerHandlers.Count == 0) {
                collisionComponent._triggerHandlers.Pool();
                collisionComponent._triggerHandlers = null;
            }

            return collisionComponent;
        }

        #endregion

        #region Audio

        /// <inheritdoc cref="AudioInstance.Volume"/>
        public static T SetVolume<T>(this T audio, float volume) where T: AudioInstance {
            audio.Volume = volume;
            return audio;
        }

        /// <inheritdoc cref="AudioInstance.Pitch"/>
        public static T SetPitch<T>(this T audio, float pitch) where T : AudioInstance {
            audio.Pitch = pitch;
            return audio;
        }

        /// <inheritdoc cref="AudioInstance.Pan"/>
        public static T SetPan<T>(this T audio, float pan) where T : AudioInstance {
            audio.Pan = pan;
            return audio;
        }

        #endregion

        #region Render Layers

        /// <inheritdoc cref="IRenderLayer.IsActive"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T SetActive<T>(this T layer, bool isActive) where T : IRenderLayer {
            layer.IsActive = isActive;
            return layer;
        }

        /// <inheritdoc cref="IRenderLayer.ShouldPresent"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T SetShouldPresent<T>(this T layer, bool shouldPresent) where T : IRenderLayer {
            layer.ShouldPresent = shouldPresent;
            return layer;
        }

        #endregion

        #region Entity Layers

        /// <summary>
        /// Creates a new entity named <paramref name="newCameraName"/> and attaches a <see cref="CameraComponent"/> to it.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T SetCamera<T>(this T layer, string newCameraName) where T : EntityLayer {
            layer.Camera = layer.Scene.CreateEntity(newCameraName).AddComponent<CameraComponent>();
            return layer;
        }

        /// <inheritdoc cref="EntityLayer.Camera"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T SetCamera<T>(this T layer, CameraComponent camera) where T : EntityLayer {
            layer.Camera = camera;
            return layer;
        }

        /// <inheritdoc cref="EntityLayer.BackgroundColor"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T SetBackgroundColor<T>(this T layer, Color backgroundColor) where T : EntityLayer {
            layer.BackgroundColor = backgroundColor;
            return layer;
        }

        /// <summary>
        /// Includes an entity tag for rendering. If no tags were previously set, RenderLayer will only render objects with this tag exclusively.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T IncludeEntityTag<T>(this T layer, int unshiftedFlag) where T : EntityLayer {
            layer.IncludeEntityTags = layer.IncludeEntityTags == -1 ?
                IntFlags.SetFlagExclusive(unshiftedFlag) :
                IntFlags.SetFlag(layer.IncludeEntityTags, unshiftedFlag);

            return layer;
        }

        /// <summary>
        /// Excludes an entity tag from rendering.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T ExcludeEntityTag<T>(this T layer, int unshiftedFlag) where T : EntityLayer {
            layer.IncludeEntityTags = layer.IncludeEntityTags.RemoveFlag(unshiftedFlag);
            return layer;
        }

        #endregion

        #region Nested Layers

        ///// <summary>
        ///// Sets the internal layer to provided render layer instance.
        ///// </summary>
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static T SetContent<T>(this T layer, IRenderLayer content) where T : INestedRenderLayer {
        //    layer.Content = content;
        //    return layer;
        //}

        /// <summary>
        /// Sets and applies <paramref name="initializer"/> to a new render layer instance.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TSelf SetContent<TSelf, TValue>(this TSelf layer, Action<TValue> initializer = default)
            where TSelf : INestedRenderLayer
            where TValue : IRenderLayer, new() {

            var content = new TValue() {
                Scene = layer.Scene,
                IsActive = true,
                ShouldPresent = true
            };

            // invoke initializer and save the layer
            initializer?.Invoke(content);
            layer.Content = content;

            return layer;
        }

        #endregion
    }
}
