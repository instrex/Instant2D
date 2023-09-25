using Instant2D.EC.Events;
using Instant2D.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;

namespace Instant2D.EC.Rendering {
    public enum RenderTargetLayerBehavior {
        /// <summary>
        /// Layer will draw Content into the RenderTarget, then use this RenderTarget to present without redrawing content for the second time.
        /// </summary>
        PresentRenderTexture,

        /// <summary>
        /// Layer will draw Content into the RenderTarget, then draw it again it when presenting.
        /// </summary>
        PresentContent,
    }

    /// <summary>
    /// Render layer used to capture contents of another render layer into a RenderTarget.
    /// </summary>
    public class RenderTargetLayer : INestedRenderLayer, IDisposable {
        RenderTarget2D _renderTarget;
        bool _initialized;

        /// <summary>
        /// Read-only access to the RenderTarget this layer uses.
        /// </summary>
        /// <remarks>Avoid storing long lived references to this, as it may change with the game resolution. </remarks>
        public RenderTarget2D RenderTarget => _renderTarget;

        /// <summary>
        /// Tint applied when presenting the RenderTarget to the screen.
        /// </summary>
        public Color PresentColor = Color.White;

        /// <summary>
        /// Color used to clear the target.
        /// </summary>
        public Color ClearColor = Color.Transparent;

        /// <summary>
        /// Parameters for when this layer calls Present.
        /// </summary>
        public RenderTargetLayerBehavior PresentBehavior = RenderTargetLayerBehavior.PresentRenderTexture;

        /// <inheritdoc cref="PresentColor"/>
        public RenderTargetLayer SetColor(Color color) {
            PresentColor = color;
            return this;
        }

        /// <inheritdoc cref="RenderTargetLayerBehavior.PresentContent"/>
        public RenderTargetLayer SetPresentContent() {
            PresentBehavior = RenderTargetLayerBehavior.PresentContent;
            return this;
        }

        public IRenderLayer Content { get; set; }

        // IRenderLayer impl
        public bool IsActive { get; set; }
        public bool ShouldPresent { get; set; }
        public Scene Scene { get; init; }
        public float Order { get; init; }
        public string Name { get; init; }

        public virtual void Prepare() {
            if (!_initialized) {
                Scene.Events.Subscribe<SceneResolutionChangedEvent>(ResizeRenderTarget);
                _initialized = true;
            }

            if (Content == null)
                return;

            if (_renderTarget == null) 
                CreateRenderTarget();

            var gd = InstantApp.Instance.GraphicsDevice;
            //var oldRTs = gd.GetRenderTargets();

            // set the RT and clear it
            gd.SetRenderTarget(_renderTarget);
            gd.Clear(ClearColor);

            GraphicsManager.Context.Begin(Material.Opaque, Matrix.Identity);

            // draw the content onto RT
            Content.Present(GraphicsManager.Context);

            GraphicsManager.Context.End();

            // reset the RTs
            //gd.SetRenderTargets(oldRTs);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void CreateRenderTarget() => _renderTarget = new RenderTarget2D(InstantApp.Instance.GraphicsDevice, Scene.Resolution.renderTargetSize.X, Scene.Resolution.renderTargetSize.Y, false, InstantApp.Instance.GraphicsDevice.PresentationParameters.BackBufferFormat, DepthFormat.None);

        void ResizeRenderTarget(SceneResolutionChangedEvent ev) {
            if (_renderTarget == null || ev.PreviousResolution.renderTargetSize == ev.Resolution.renderTargetSize)
                return;

            // dispose of the old RT
            _renderTarget.Dispose();
            _renderTarget = null;

            // make new
            CreateRenderTarget();
        }

        public virtual void Present(DrawingContext drawing) {
            if (Content == null || _renderTarget == null)
                return;

            if (PresentBehavior == RenderTargetLayerBehavior.PresentContent) {
                Content.Present(GraphicsManager.Context);
                return;
            }

            drawing.DrawTexture(_renderTarget, Vector2.Zero, null, PresentColor, 0, Vector2.Zero, Vector2.One);
        }

        public void Dispose() {
            if (_renderTarget != null) {
                ((IDisposable)_renderTarget).Dispose();
                _renderTarget = null;
            }

            GC.SuppressFinalize(this);
        }
    }
}
