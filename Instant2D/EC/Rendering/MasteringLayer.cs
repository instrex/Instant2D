using Instant2D.EC.Events;
using Instant2D.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Instant2D.EC.Rendering {
    /// <summary>
    /// Render layer that combines several other layers into one. <br/>
    /// TODO: May be used to apply post-processing effects or global tint, in which case all the layers would be rendered into a RenderTarget before presenting.
    /// </summary>
    public class MasteringLayer : IRenderLayer, IDisposable {
        record struct FloatRange(float Min, float Max);

        readonly List<IRenderLayer> _masteredLayers = new();

        bool _initialized, _useRenderTarget;
        RenderTarget2D _renderTarget;
        Color _color = Color.White;
        FloatRange? _layerRange;

        public RenderTarget2D RenderTarget => _renderTarget;

        /// <summary>
        /// Color used when presenting mastered layers to the screen. Setting this to <see langword="true"/> will automatically cause this layer to use RenderTarget.
        /// </summary>
        public Color Color {
            get => _color;
            set {
                if (_color != value) {
                    _useRenderTarget = true;
                    _color = value;
                }
            }
        }

        public Color BackgroundColor = Color.Transparent;

        /// <inheritdoc cref="Color"/>
        public MasteringLayer SetColor(Color color) {
            Color = color;
            return this;
        }

        /// <inheritdoc cref="BackgroundColor"/>
        public MasteringLayer SetBackgroundColor(Color color) {
            BackgroundColor = color;
            return this;
        }

        /// <summary>
        /// Sets <see cref="IRenderLayer.Order"/> range of layers this one should master. 
        /// </summary>
        public MasteringLayer SetLayerRange(float min, float max) {
            _layerRange = new(min, max);
            return this;
        }

        /// <summary>
        /// Setups layer to master by name.
        /// </summary>
        public MasteringLayer AddMasteredLayersByName(params string[] names) {
            for (var i = 0; i < names.Length; i++) {
                var layer = Scene.GetLayer(names[i]);

                if (layer == null) {
                    InstantApp.Logger.Warn($"Layer '{names[i]}' wasn't found.");
                    continue;
                }

                layer.ShouldPresent = false;
                _masteredLayers.Add(layer);
            }

            return this;
        }

        // IRenderLayer impl
        public float Order { get; init; }
        public string Name { get; init; }
        public bool IsActive { get; set; }
        public bool ShouldPresent { get; set; }
        public Scene Scene { get; init; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void CreateRenderTarget() => _renderTarget = new RenderTarget2D(InstantApp.Instance.GraphicsDevice, Scene.Resolution.renderTargetSize.X, Scene.Resolution.renderTargetSize.Y, false, 
            InstantApp.Instance.GraphicsDevice.PresentationParameters.BackBufferFormat, DepthFormat.None);

        void CollectLayers() {
            if (_layerRange is FloatRange range) {
                _masteredLayers.Clear();

                // collect layers based on Order range
                foreach (var layer in Scene.RenderLayers.Where(layer => layer.Order >= range.Min && layer.Order < range.Max)) {
                    layer.ShouldPresent = false;
                    _masteredLayers.Add(layer);
                }
                
                // sort layers
                _masteredLayers.Sort();

                return;
            }
        }

        void ResizeRenderTarget(SceneResolutionChangedEvent ev) {
            if (!_useRenderTarget || _renderTarget == null || ev.PreviousResolution.renderTargetSize == ev.Resolution.renderTargetSize)
                return;

            // dispose of the old RT
            _renderTarget.Dispose();
            _renderTarget = null;

            // make new
            CreateRenderTarget();
        }

        public void Prepare() {
            if (!_initialized) {
                _initialized = true;

                // subscribe to scene events
                Scene.Events.Subscribe<SceneResolutionChangedEvent>(ResizeRenderTarget);
            }

            if (_masteredLayers.Count == 0)
                CollectLayers();


            //// prepare mastered layers
            //for (var i = 0; i < layersSpan.Length; i++) {
            //    layersSpan[i].Prepare();
            //}

            if (!_useRenderTarget) 
                return;

            var layersSpan = CollectionsMarshal.AsSpan(_masteredLayers);

            if (_renderTarget == null)
                CreateRenderTarget();

            // if RenderTarget is used, proceed to render contents into it
            var gd = InstantApp.Instance.GraphicsDevice;

            // set the RT and clear it
            gd.SetRenderTarget(_renderTarget);
            gd.Clear(BackgroundColor);

            GraphicsManager.Context.Begin(Material.Opaque, Matrix.Identity);

            // draw layers onto the RT
            for (var i = 0; i < layersSpan.Length; i++) {
                layersSpan[i].Present(GraphicsManager.Context);
            }

            GraphicsManager.Context.End();
        }

        public void Present(DrawingContext drawing) {
            if (_useRenderTarget) {
                // TODO: apply post-processing here
                // all the work has been done in Prepare already
                drawing.DrawTexture(_renderTarget, Vector2.Zero, null, _color, 0, Vector2.Zero, Vector2.One);

                return;
            }

            drawing.DrawRectangle(new(Vector2.Zero, Scene.Resolution.renderTargetSize.ToVector2()), BackgroundColor);

            var layersSpan = CollectionsMarshal.AsSpan(_masteredLayers);

            // present layers one by one
            for (var i = 0; i < layersSpan.Length; i++) {
                layersSpan[i].Present(GraphicsManager.Context);
            }
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
