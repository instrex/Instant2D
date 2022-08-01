using Instant2D.Core;
using Instant2D.Graphics;
using Instant2D.Input;
using Instant2D.Utils;
using Instant2D.Coroutines;
using Instant2D.Utils.Math;
using Instant2D.Utils.ResolutionScaling;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Instant2D.Collisions;
using Instant2D.EC.Components;
using Instant2D.EC.Events;
using Instant2D.Assets;
using Instant2D.Assets.Containers;

namespace Instant2D.EC {
    public abstract class Scene : ICoroutineTarget {
        internal readonly List<Entity> _entities = new(128);
        readonly List<SceneRenderLayer> _layers = new(12);
        RenderTarget2D _sceneTarget, _tempTarget;
        bool _isInitialized, _isCleanedUp;
        bool _debugRender;
        Point _sceneSize;

        /// <summary>
        /// Represents collection of RenderLayers this scene will use.
        /// </summary>
        public IReadOnlyList<SceneRenderLayer> RenderLayers => _layers;

        /// <summary>
        /// Returns all entities attached to this scene.
        /// </summary>
        public IReadOnlyList<Entity> Entities => _entities;

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
        /// Amount of time that has passed since beginning this scene, taking <see cref="TimeScale"/> into account.
        /// </summary>
        public float TotalTime;

        /// <summary>
        /// Default RenderLayer used for components with nothing specified.
        /// </summary>
        public SceneRenderLayer DefaultRenderLayer;

        /// <summary>
        /// Camera used to render this scene.
        /// </summary>
        public CameraComponent Camera;

        /// <summary>
        /// Optional collision manager for this scene, must be initialized before using <see cref="CollisionComponent"/>.
        /// </summary>
        public SpatialHash<CollisionComponent> Collisions;

        /// <summary>
        /// Special events that may trigger throughout scene's lifetime. You can even define your own.
        /// </summary>
        public readonly EventBus Events = new();

        /// <summary>
        /// Scaled resolution used for this scene. If <see cref="SceneManager.ResolutionScaler"/> is null, returns the whole screen.
        /// </summary>
        public ScaledResolution Resolution;

        /// <summary>
        /// Returns transformation matrix used to convert scene -> screen coordinates. 
        /// May be useful if you want to overlay some information on top of entities, similar to how DebugRender works.
        /// </summary>
        public Matrix2D SceneToScreenTransform {
            get {
                var matrix = Camera.TransformMatrix;

                // apply resolution scale
                Matrix2D.Multiply(ref matrix, Resolution.scaleFactor, out matrix);

                // move by resolution offset
                return Matrix2D.Multiply(matrix, Matrix2D.CreateTranslation(Resolution.offset));
            }
        }

        /// <summary>
        /// Creates an entity and automatically adds it onto the scene.
        /// </summary>
        public Entity CreateEntity(string name, Vector2 position = default) {
            var entity = StaticPool<Entity>.Get();
            entity.Transform.Position = position;
            entity.Name = name;
            entity.Scene = this;

            return entity;
        }

        /// <summary>
        /// Attaches an existing entity onto this scene.
        /// </summary>
        public Entity AddEntity(Entity entity) {
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

            // set the default layer to first one
            DefaultRenderLayer ??= layer;

            return layer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void InternalUpdate(GameTime time) {
            // initialize the scene first
            if (!_isInitialized) {
                _isInitialized = true;
                if (Camera is null) {
                    Camera = CreateEntity("camera", Vector2.Zero)
                        .AddComponent<CameraComponent>();
                }

                Initialize();

                // initialize RTs for newly added layers
                ResizeRenderTargets(Resolution);
            }

            TotalTime += (float)time.ElapsedGameTime.TotalSeconds * TimeScale;

            // switch debug render
            if (InputManager.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.OemTilde)) {
                _debugRender = !_debugRender;
            }

            if (!IsActive)
                return;

            // update entities before anything else
            for (var i = 0; i < _entities.Count; i++) {
                _entities[i].Update();
            }

            // invoke the update callback
            Update();
        }

        internal void InternalRender() {
            if (!IsVisible || !_isInitialized) {
                return;
            }
            
            // draw each layer into its own RT
            var gd = InstantGame.Instance.GraphicsDevice;
            for (var i = 0; i < _layers.Count; i++) {
                var layer = _layers[i];

                // set the RT and clear it
                gd.SetRenderTarget(layer._renderTarget);
                gd.Clear(layer.BackgroundColor);

                Camera.ForceUpdate();

                layer.InternalDraw();
            }

            gd.SetRenderTarget(null);
            gd.Clear(Color.Transparent);

            // draw the layers onto the backbuffer
            var drawing = GraphicsManager.Backend;

            drawing.Push(Material.Default);
            for (var i = 0; i < _layers.Count; i++) {
                var layer = _layers[i];
                drawing.Draw(new Sprite(
                    _layers[i]._renderTarget,
                    new(0, 0, _sceneSize.X, _sceneSize.Y), Vector2.Zero),
                    Resolution.offset,
                    layer.Color,
                    0,
                    Resolution.scaleFactor
                );
            }

            // invoke the user callback
            Render(drawing);

            // render some debug stuff
            if (_debugRender) {
                // render bounds & culling data
                drawing.Push(Material.Default, SceneToScreenTransform);

                for (var i = 0; i < _layers.Count; i++) {
                    var layer = _layers[i];
                    for (var k = 0; k < layer.Objects.Count; k++) {
                        var obj = layer.Objects[k];
                        var color = obj.IsVisible ? Color.Green : Color.Red;
                        var bounds = obj.Bounds with { Position = obj.Bounds.Position };

                        for (var j = 0; j < 4; j++) {
                            var offset = new Vector2(2, 0).RotatedBy(j * MathHelper.PiOver2);
                            drawing.DrawRectangle(bounds with { Position = bounds.Position + offset }, Color.Transparent, Color.Black, 2);
                        }

                        var innerColor = Color.Transparent;
                        if (bounds.Contains(Camera.ScreenToWorldPosition(InputManager.MousePosition))) {
                            innerColor = color * (0.25f + 0.125f * MathF.Sin(TimeManager.TotalTime * 4 + i));
                        }

                        drawing.DrawRectangle(bounds, innerColor, color, 2);
                        drawing.DrawPoint(obj.Entity.Transform.Position, Color.Black, 12);
                        drawing.DrawPoint(obj.Entity.Transform.Position, color, 8);
                        drawing.DrawString(obj.Entity.Name, new Vector2(bounds.Left, bounds.Top - 24), color, Vector2.One * 2, 0, drawOutline: true);
                    }
                }

                drawing.DrawPoint(Vector2.Zero, Color.Green, 16);

                drawing.Push(Material.Default, Matrix.Identity);

                // render layers preview
                for (var i = 0; i < _layers.Count; i++) {
                    var drawScale = 0.5f;
                    if (new RectangleF(16, 16 + _sceneSize.Y * 0.5f * i, _sceneSize.X * 0.5f, _sceneSize.Y * 0.5f).Contains(InputManager.RawMousePosition)) {
                        drawing.DrawRectangle(new(0, 0, Resolution.rawScreenSize.X, Resolution.rawScreenSize.Y), Color.Black * 0.5f);
                        drawScale = 2f;
                    }

                    drawing.DrawRectangle(new RectangleF(16, 16 + _sceneSize.Y * 0.5f * i, _sceneSize.X * drawScale, _sceneSize.Y * drawScale), Color.Black * 0.5f);

                    drawing.Draw(
                        new Sprite(_layers[i].RenderTarget, new(0, 0, _sceneSize.X, _sceneSize.Y), Vector2.Zero),
                        new Vector2(16, 16 + _sceneSize.Y * 0.5f * i), 
                        Color.White, 
                        0f,
                        drawScale
                    );

                    if (drawScale > 1f)
                        break;

                    drawing.DrawString($"#{i} '{_layers[i].Name}'", new Vector2(24 + _sceneSize.X * 0.5f, 18 + _sceneSize.Y * 0.5f * i).Round(),
                        Color.White, Vector2.One * 2f, 0, drawOutline: true);
                }
            }

            drawing.Pop(true);
        }

        internal void OnAssetsUpdated(IEnumerable<Asset> assets) {
            // loop over unhandled asset changes, thus giving freedom to modify the way assets are reloaded when needed
            foreach (var asset in assets.Where(asset => !Events.Raise(new AssetReloadedEvent { UpdatedAsset = asset }))) {
                switch (asset) {
                    // replace changed sprites inside SpriteRenderers
                    case IAssetContainer<Sprite> spriteAsset:
                        foreach (var spriteRenderer in FindComponentsOfType<SpriteComponent>().Where(s => s.Sprite.Key == spriteAsset.Content.Key))
                            spriteRenderer.SetSprite(spriteAsset.Content);

                        break;

                    // replace changed sprite animations inside SpriteAnimators
                    case IAssetContainer<SpriteAnimation> animationAsset:
                        foreach (var spriteRenderer in FindComponentsOfType<SpriteAnimationComponent>().Where(s => s.Animation.Key == animationAsset.Content.Key))
                            spriteRenderer.SetAnimation(animationAsset.Content);

                        break;
                }
            }
        }

        internal void Cleanup() {
            // destroy all entities before switching scenes
            for (var i = 0; i < _entities.Count; i++) {
                _entities[i].ImmediateDestroy();
            }

            // release all the coroutines attached to this scene
            CoroutineManager.StopByTarget(this);

            // farewell
            _isCleanedUp = true;
        }


        internal void ResizeRenderTargets(ScaledResolution resolution) {
            var gd = InstantGame.Instance.GraphicsDevice;

            var previous = Resolution;

            // resize RenderTargets when needed
            if (_sceneSize == default || previous.renderTargetSize != resolution.renderTargetSize) {
                // get Scene size
                var (width, height) = (resolution.renderTargetSize.X, resolution.renderTargetSize.Y);
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

            Resolution = resolution;

            // notify about the change
            Events.Raise(new SceneResolutionChangedEvent {
                PreviousResolution = previous,
                Resolution = resolution
            });
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

        /// <summary>
        /// Called after everything is rendered onto the screen.
        /// </summary>
        public virtual void Render(IDrawingBackend drawing) { }

        #endregion

        /// <summary>
        /// Attempts to find the first entity with component attached. Returns null if unsuccessful.
        /// </summary>
        public T FindComponentOfType<T>() where T : Component => FindComponentsOfType<T>().FirstOrDefault();

        /// <summary>
        /// Enumerates over all of the component instances in this scene.
        /// </summary>
        public IEnumerable<T> FindComponentsOfType<T>() where T: Component {
            for (var i = 0; i < _entities.Count; i++) {
                if (_entities[i].TryGetComponent<T>(out var component))
                    yield return component;
            }
        }

        /// <summary>
        /// Attempts to find an Entity by its name.
        /// </summary>
        public Entity FindEntityByName(string name) {
            for (var i = 0; i < _entities.Count; i++) {
                if (_entities[i].Name == name)
                    return _entities[i];
            }

            return null;
        }

        // ICoroutineTarget impl
        bool ICoroutineTarget.IsActive => !_isCleanedUp;
        float ICoroutineTarget.TimeScale => TimeScale;
    }
}
