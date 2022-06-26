using Instant2D.Core;
using Instant2D.Graphics;
using Instant2D.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace Instant2D.EC {
    public abstract class Scene {
        internal readonly List<Entity> _entities = new(128);
        readonly List<SceneRenderLayer> _layers = new(12);
        RenderTarget2D _sceneTarget, _tempTarget;
        bool _isInitialized;
        Point _sceneSize;

        /// <summary>
        /// Represents collection of RenderLayers this scene will use.
        /// </summary>
        public IReadOnlyList<SceneRenderLayer> RenderLayers => _layers;

        /// <summary>
        /// When <see langword="true"/>, Scene's entities will be updated. Otherwise, they will be drawn but not updated.
        /// </summary>
        public bool IsActive = true;

        /// <summary>
        /// When <see langword="true"/>, Scene's RenderLayers and renderable components will be drawn each frame.
        /// </summary>
        public bool IsVisible = true;

        /// <summary>
        /// The rate at which entities will update. Note that this doesn't change anything by itself, 
        /// any time-sensitive components should make use of this variable in order to make it useful.
        /// </summary>
        public float TimeScale = 1.0f;

        /// <summary>
        /// Camera used to render this scene.
        /// </summary>
        public ICamera Camera;

        /// <summary>
        /// Creates an entity and automatically adds it onto the scene.
        /// </summary>
        public Entity CreateEntity(string name, Vector2 position) {
            var entity = StaticPool<Entity>.Get();
            entity.Transform.Position = position;
            entity.Name = name;
            entity.Scene = this;

            return entity;
        }

        /// <inheritdoc cref="CreateLayer{T}(string)"/>
        public SceneRenderLayer CreateLayer(string name) => CreateLayer<SceneRenderLayer>(name);

        /// <summary>
        /// Create and register <see cref="SceneRenderLayer"/> for rendering objects.
        /// </summary>
        public T CreateLayer<T>(string name) where T: SceneRenderLayer, new() {
            var layer = new T() { Name = name, _scene = this };
            _layers.Add(layer);

            return layer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void InternalUpdate() {
            // initialize the scene first
            if (!_isInitialized) {
                _isInitialized = true;
                Initialize();

                // initialize RTs for newly added layers
                ResizeRenderTargets();
            }

            if (IsActive) {
                // update entities before anything else
                for (var i = 0; i < _entities.Count; i++) {
                    _entities[i].Update();
                }

                // invoke the update callback
                Update();
            }

            // we only do drawing from there
            if (!IsVisible) {
                return;
            }

            // draw each layer into its own RT
            var gd = InstantGame.Instance.GraphicsDevice;
            for (var i = 0; i < _layers.Count; i++) {
                var layer = _layers[i];

                // set the RT and clear it
                gd.SetRenderTarget(layer._renderTarget);
                gd.Clear(layer.BackgroundColor);

                layer.InternalDraw();
            }

            gd.SetRenderTarget(null);
            gd.Clear(Color.Transparent);

            // draw the layers onto the backbuffer
            var drawing = GraphicsManager.Instance.Backend;

            drawing.Push(Material.Default);
            for (var i = 0; i < _layers.Count; i++) {
                drawing.Draw(new Sprite(_layers[i]._renderTarget, new(0, 0, _sceneSize.X, _sceneSize.Y), Vector2.Zero), Vector2.Zero, Color.White);
            }

            drawing.Pop(true);
        }

        internal void ResizeRenderTargets() {
            var gd = InstantGame.Instance.GraphicsDevice;

            // get Scene size
            var (width, height) = (gd.Viewport.Width, gd.Viewport.Height);
            _sceneSize = new(width, height);

            // dispose of the existing RTs
            _sceneTarget?.Dispose();
            _tempTarget?.Dispose();

            // allocate new scene targets
            _sceneTarget = new RenderTarget2D(gd, width, height);
            _tempTarget = new RenderTarget2D(gd, width, height);

            // reallocate new RTs for layers
            for (var i = 0; i < _layers.Count; i++) {
                _layers[i]._renderTarget?.Dispose();
                _layers[i]._renderTarget = new RenderTarget2D(gd, width, height);
            }
        }

        #region Scene Lifecycle

        /// <summary>
        /// Called when this Scene begins.
        /// </summary>
        public virtual void Initialize() { }

        /// <summary>
        /// Called each frame when this scene is in focus and all components have already been updated this frame.
        /// </summary>
        public virtual void Update() { }

        #endregion
    }

    /// <summary>
    /// A simple implementation of <see cref="Scene"/> which uses callbacks for control.
    /// </summary>
    public class SimpleScene : Scene {
        /// <summary>
        /// Called each frame when the Scene is active.
        /// </summary>
        public Action<Scene> OnUpdate;

        /// <summary>
        /// Called when the Scene begins.
        /// </summary>
        public Action<Scene> OnInitialize;

        public override void Initialize() {
            OnInitialize?.Invoke(this);
        }

        public override void Update() {
            OnUpdate?.Invoke(this);
        }
    }
}
