using Instant2D.Audio;
using Instant2D.Core;
using Instant2D.EC.Events;
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

namespace Instant2D.EC.Rendering {
    public class RenderLayer : IDisposable {
        /// <summary>
        /// Name of the master layer that will combine each other layer into a single texture for the scene. <br/>
        /// This layer will be automatically managed, and you won't be able manipulate it in any way aside from adding post-processing effects.
        /// </summary>
        public const string MASTER_LAYER_NAME = "master";

        protected RenderLayer[] _masteredLayers;
        protected RenderTarget2D _renderTarget;

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
        public List<RenderableComponent> Objects = new();

        /// <summary>
        /// When <see langword="true"/>, the layer will be drawn. If you're looking to disable the layer from appearing on-screen but want to keep updating RenderTarget, see <see cref="ShouldPresent"/>.
        /// </summary>
        public bool Active = true;

        /// <summary>
        /// When <see langword="true"/>, the layer will be rendered onto the screen.
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

        /// <summary>
        /// A mask used to determine if entity should not be rendered by this layer. By default, none of the entities are excluded.
        /// </summary>
        public int ExcludeEntitiesWithTag = 0;

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

        /// <inheritdoc cref="BackgroundColor"/>
        public RenderLayer SetBackgroundColor(Color backgroundColor) {
            BackgroundColor = backgroundColor;
            return this;
        }

        /// <inheritdoc cref="Camera"/>
        public RenderLayer SetCamera(CameraComponent camera) {
            Camera = camera;
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
        protected void DrawObjects() { 
            // no one's there
            if (Objects.Count == 0) {
                return;
            }

            // sort the objects when needed
            if (_drawOrderDirty) {
                _drawOrderDirty = false;
                Objects.Sort();
            }

            var drawing = GraphicsManager.Context;

            // we need to force update the camera for the frame
            var camera = Camera ?? Scene.Camera;
            camera.ForceUpdate();

            // cache for culling
            var bounds = camera.Bounds;

            Material currentMaterial = null;

            // draw all the non-culled objects
            for (var i = 0; i < Objects.Count; i++) {
                var obj = Objects[i];

                // skip excluded entities
                if (ExcludeEntitiesWithTag != 0 && ExcludeEntitiesWithTag.IsFlagSet(obj.Entity.Tags, false))
                    continue;

                // check if renderer is in bounds of camera
                if (!(obj.IsVisible = bounds.Intersects(obj.Bounds)))
                    continue;

                if (currentMaterial != obj.Material) {
                    if (currentMaterial != null)
                        drawing.Pop();

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
        /// Disposes of internal render target (if not null).
        /// </summary>
        public void Dispose() {
            if (_renderTarget != null) {
                ((IDisposable)_renderTarget).Dispose();
            }
            
            // idk why this is needed
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Renders the contents of this layer.
        /// </summary>
        public virtual void Prepare() {
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
                    _masteredLayers[i].Present(GraphicsManager.Context);
                }
            }

            // render all of the objects into the texture
            DrawObjects();
        }

        /// <summary>
        /// Presents the layer to the screen. If it uses RenderTarget, all the drawing work would be done via <see cref="Draw"/>, and this step is just rendering the RenderTarget onto the screen.
        /// </summary>
        public virtual void Present(DrawingContext drawing) {
            if (_useRenderTarget) {
                // present the rendertexture to the screen and call it a day
                drawing.DrawTexture(_renderTarget, Vector2.Zero, null, Color.White, 0, Vector2.Zero, Vector2.One);
                return;
            }

            // imitate the background color clear
            drawing.DrawRectangle(new(Vector2.Zero, Scene.Resolution.renderTargetSize.ToVector2()), BackgroundColor);

            // else, draw the objects as usual
            DrawObjects();
        }
    }
}
