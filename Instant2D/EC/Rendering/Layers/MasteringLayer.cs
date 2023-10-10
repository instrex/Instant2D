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

        // probably a bad idea, but two RTs should be able to be shared between all layers
        static RenderTarget2D _ppTarget, _ppSwapTarget;

        bool _initialized, _useRenderTarget;
        List<PostProcessor> _postProcessors;
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

        public IReadOnlyList<PostProcessor> PostProcessors => _postProcessors;

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
        /// Forces this mastering layer to render into its <see cref="RenderTarget"/>. By default, it tries to avoid creating new RTs for layers that don't need them. <br/>
        /// Cases in which RenderTarget is instantiated: using <see cref="SetColor(Color)"/> or <see cref="AddPostProcessor{T}(T)"/>.
        /// </summary>
        public MasteringLayer ForceUseRenderTarget() {
            _useRenderTarget = true;
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
        RenderTarget2D CreateRenderTarget() => new(InstantApp.Instance.GraphicsDevice, Scene.Resolution.renderTargetSize.X, Scene.Resolution.renderTargetSize.Y, false, 
            InstantApp.Instance.GraphicsDevice.PresentationParameters.BackBufferFormat, DepthFormat.None);

        void ResizeRT(ref RenderTarget2D renderTarget, int width, int height) {
            if (renderTarget.Width == width && renderTarget.Height == height)
                return;

            renderTarget.Dispose();
            renderTarget = CreateRenderTarget();
        }

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
            if (ev.PreviousResolution.renderTargetSize == ev.Resolution.renderTargetSize)
                return;

            var (width, height) = (ev.Resolution.renderTargetSize.X, ev.Resolution.renderTargetSize.Y);

            if (_useRenderTarget && _renderTarget != null) {
                ResizeRT(ref _renderTarget, width, height);
            }

            if (_ppTarget != null) {
                ResizeRT(ref _ppTarget, width, height);
            }

            if (_ppSwapTarget != null) {
                ResizeRT(ref _ppSwapTarget, width, height);
            }

            if (_postProcessors != null) {
                // notify post processors of resolution changes
                for (var i = 0; i < _postProcessors.Count; i++) {
                    var processor = _postProcessors[i];
                    processor.OnResolutionChanged(ev.Resolution, ev.PreviousResolution);
                }
            }
        }

        public void Prepare() {
            if (!_initialized) {
                _initialized = true;

                // subscribe to scene events
                Scene.Events.Subscribe<SceneResolutionChangedEvent>(ResizeRenderTarget);
            }

            if (_masteredLayers.Count == 0)
                CollectLayers();

            if (!_useRenderTarget) 
                return;

            var layersSpan = CollectionsMarshal.AsSpan(_masteredLayers);
            _renderTarget ??= CreateRenderTarget();

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

            if (_postProcessors != null) 
                ApplyPostProcessing();
        }

        #region Post-processing

        /// <summary>
        /// Adds a post processor to this layer. Will automatically enable using RenderTarget.
        /// </summary>
        public MasteringLayer AddPostProcessor<T>(T postProcessor) where T: PostProcessor {
            postProcessor.Parent = this;

            // init the list on demand
            _postProcessors ??= new();
            _postProcessors.Add(postProcessor);

            // sort post processors by order
            _postProcessors.Sort((a, b) => a.Order.CompareTo(b.Order));

            // init any resolution-based things
            postProcessor.OnResolutionChanged(Scene.Resolution, default);

            // post processors operate on RenderTargets, so.. yeah
            _useRenderTarget = true;

            return this;
        }

        /// <inheritdoc cref="AddPostProcessor{T}(T)"/>
        public MasteringLayer AddPostProcessor<T>(float order) where T : PostProcessor, new() {
            var postProcessor = new T { Parent = this, Order = order };
            return AddPostProcessor(postProcessor);
        }

        void ApplyPostProcessing() {
            _ppTarget ??= CreateRenderTarget();
            _ppSwapTarget ??= CreateRenderTarget();

            var activeRtCounter = 0;
            for (var i = 0; i < _postProcessors.Count; i++) {
                var pp = _postProcessors[i];

                if (!pp.IsActive)
                    continue;

                var (source, destination) = (
                    i == 0 ? _renderTarget : (activeRtCounter % 2 == 0 ? _ppTarget : _ppSwapTarget),
                    activeRtCounter % 2 == 1 ? _ppTarget : _ppSwapTarget
                );

                activeRtCounter++;

                // apply post processing
                pp.Apply(source, destination);
            }

            InstantApp.Instance.GraphicsDevice.SetRenderTarget(_renderTarget);
            GraphicsManager.Context.Begin(Material.Opaque, Matrix.Identity);

            GraphicsManager.Context.DrawTexture(activeRtCounter % 2 == 0 ? _ppTarget : _ppSwapTarget, Vector2.Zero, null, _color, 0, Vector2.Zero, Vector2.One);

            GraphicsManager.Context.End();

            if (activeRtCounter % 2 == 1) {
                // if we end up on ppTarget, swap rts
                (_renderTarget, _ppTarget) = (_ppTarget, _renderTarget);
            }
        }

        #endregion

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

            if (_ppTarget != null) {
                ((IDisposable)_ppTarget).Dispose();
                _ppTarget = null;
            }

            GC.SuppressFinalize(this);
        }
    }
}
