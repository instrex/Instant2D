﻿using Instant2D.Audio;
using Instant2D.Core;
using Instant2D.EC.Events;
using Instant2D.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.EC.Rendering {
    public class RenderLayer {
        /// <summary>
        /// Name of the master layer that will combine each other layer into a single texture for the scene. <br/>
        /// This layer will be automatically managed, and you won't be able manipulate it in any way aside from adding post-processing effects.
        /// </summary>
        public const string MASTER_LAYER_NAME = "master";

        RenderLayer[] _masteredLayers;
        RenderTarget2D _renderTarget;

        internal bool _drawOrderDirty;
        internal bool _useRenderTarget;

        public RenderLayer(Scene scene, string name) {
            Scene = scene;
            Name = name;

            // subscribe to SceneResolutionChangedEvent event
            Scene.Events.Subscribe<SceneResolutionChangedEvent>(ResizeRenderTarget);
        }

        /// <summary>
        /// The RenderTarget this layer renders onto. May be passed manually or created automatically.
        /// </summary>
        public RenderTarget2D RenderTarget {
            get => _renderTarget;
        }

        public readonly Scene Scene;

        /// <summary>
        /// The name of this layer.
        /// </summary>
        public string Name;

        /// <summary>
        /// List of renderable objects this layer houses.
        /// </summary>
        public readonly List<RenderableComponent> Objects = new();

        /// <summary>
        /// When <see langword="true"/>, the layer will be drawn. If you're looking to disable the layer but keep updating RenderTarget, see <see cref="ShouldPresent"/>.
        /// </summary>
        public bool Active = true;

        /// <summary>
        /// When <see langword="true"/>, the layer will be rendered onto the screen on 'Present' phase.
        /// </summary>
        public bool ShouldPresent = true;

        /// <summary>
        /// Color to which the <see cref="RenderTarget"/> will be cleared. Has no effect if this layer doesn't use it.
        /// </summary>
        public Color BackgroundColor = Color.Transparent;

        /// <summary>
        /// Individual camera for this layer. If this isn't set, <see cref="Scene.Camera"/> will be used. <br/>
        /// Can be used to render user interface in screen-space if you pass in another camera that doesn't move.
        /// </summary>
        public CameraComponent Camera;

        #region Setters

        /// <summary>
        /// Sets the flag to use RenderTarget for this layer. Note that if this layer uses post-processing, this should be set to <see langword="true"/>.
        /// </summary>
        public RenderLayer SetUseRenderTarget(bool useRenderTarget) {
            _useRenderTarget = useRenderTarget;
            return this;
        }

        /// <summary>
        /// Sets current layer to master provided <paramref name="layers"/>. This means that their contents will be drawn inside this layer before rendering all the objects. <br/>
        /// You can use this to apply post-processing effects to multiple layers at once. <br/>
        /// This method automatically sets <see cref="ShouldPresent"/> on provided layers to <see langword="false"/>.
        /// </summary>
        public RenderLayer SetMasteredLayers(params RenderLayer[] layers) {
            _masteredLayers = layers;
            
            // mastered layers should not be presented to the screen
            for (var i = 0; i < _masteredLayers.Length; i++) {
                _masteredLayers[i].ShouldPresent = false;
            }

            return this;
        }

        #endregion

        void ResizeRenderTarget(SceneResolutionChangedEvent ev) {
            // don't do anything if RT size haven't changed or we don't use RT at all
            if (!_useRenderTarget || ev.PreviousResolution.renderTargetSize == ev.Resolution.renderTargetSize) {
                return;
            }

            _renderTarget?.Dispose();
            _renderTarget = new RenderTarget2D(InstantGame.Instance.GraphicsDevice, ev.Resolution.renderTargetSize.X, ev.Resolution.renderTargetSize.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void DrawObjects() { 
            // no one's there
            if (Objects.Count == 0) {
                return;
            }

            // sort the objects when needed
            if (_drawOrderDirty) {
                _drawOrderDirty = false;
                Objects.Sort();
            }

            var drawing = GraphicsManager.Backend;

            // we need to force update the camera for the frame
            var camera = Camera ?? Scene.Camera;
            camera.ForceUpdate();

            // cache for culling
            var bounds = camera.Bounds;

            Material currentMaterial = null;

            // draw all the non-culled objects
            for (var i = 0; i < Objects.Count; i++) {
                var obj = Objects[i];

                // check if renderer is in bounds of camera
                if (!(obj.IsVisible = bounds.Intersects(obj.Bounds)))
                    continue;

                if (currentMaterial != obj.Material) {
                    // swap the material when needed
                    drawing.Push(currentMaterial = obj.Material, camera.TransformMatrix);
                }

                obj.Draw(drawing, camera);
            }

            if (currentMaterial != null) {
                drawing.Pop();
            }
        }

        /// <summary>
        /// Renders the contents of this layer.
        /// </summary>
        public void Prepare() {
            if (_useRenderTarget && _renderTarget == null) {
                // initialize the RenderTarget when needed
                _renderTarget = new RenderTarget2D(InstantGame.Instance.GraphicsDevice, Scene.Resolution.renderTargetSize.X, Scene.Resolution.renderTargetSize.Y);
            }

            // prepare the mastered layers
            if (_masteredLayers != null) {
                for (var i = 0; i < _masteredLayers.Length; i++) {
                    _masteredLayers[i].Prepare();
                }
            }

            // we don't have to render anything in there since we're not using RTs
            if (!_useRenderTarget)
                return;

            var gd = InstantGame.Instance.GraphicsDevice;
            gd.SetRenderTarget(_renderTarget);
            gd.Clear(BackgroundColor);

            // render all the mastered layers
            if (_masteredLayers != null) {
                for (var i = 0; i < _masteredLayers.Length; i++) {
                    _masteredLayers[i].Present(GraphicsManager.Backend);
                }
            }

            // render all of the objects into the texture
            DrawObjects();
        }

        /// <summary>
        /// Presents the layer to the screen. If it uses RenderTarget, all the drawing work would be done via <see cref="Draw"/>, and this step is just rendering the RenderTarget onto the screen.
        /// </summary>
        public void Present(IDrawingBackend drawing) {
            if (_useRenderTarget) {
                // present the rendertexture to the screen and call it a day
                drawing.DrawTexture(_renderTarget, Vector2.Zero, Color.White, 0, Vector2.One, Vector2.Zero);
                return;
            }

            // imitate the background color clear
            drawing.DrawRectangle(new(Vector2.Zero, Scene.Resolution.renderTargetSize.ToVector2()), BackgroundColor);

            // else, draw the objects as usual
            DrawObjects();
        }
    }
}
