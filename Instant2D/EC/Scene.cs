using Instant2D.Core;
using Instant2D.Graphics;
using Instant2D.Input;
using Instant2D.Utils;
using Instant2D.Coroutines;
using Instant2D.Utils.Math;
using Instant2D.Utils.ResolutionScaling;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Instant2D.Collisions;
using Instant2D.EC.Components;
using Instant2D.EC.Events;
using Instant2D.Assets;
using Instant2D.Assets.Containers;
using Instant2D.Audio;
using System.Text;
using Instant2D.EC.Rendering;

namespace Instant2D.EC {
    public abstract class Scene : ICoroutineTarget {
        internal readonly List<Entity> _entities = new(128);
        RenderTarget2D _sceneTarget, _tempTarget;
        bool _isInitialized, _isCleanedUp;
        bool _debugRender;

        readonly List<RenderLayer> _layers = new(12);
        RenderLayer _masterLayer;

        /// <summary>
        /// Represents collection of RenderLayers this scene will use.
        /// </summary>
        public IReadOnlyList<RenderLayer> RenderLayers => _layers;

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
        /// The listener object to use with spatial sounds. By default, it is assigned to <see cref="Camera"/>'s Entity.
        /// </summary>
        public Entity Listener;

        /// <summary>
        /// Default RenderLayer used for components with nothing specified.
        /// </summary>
        public RenderLayer DefaultRenderLayer;

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

        #region Layers 

        /// <summary>
        /// Create and register <see cref="SceneRenderLayer"/> for rendering objects or mastering other render layers.
        /// </summary>
        public RenderLayer AddRenderLayer(string name) {
            var layer = new RenderLayer(this, name);
            _layers.Add(layer);

            // set the default layer to first one
            DefaultRenderLayer ??= layer;

            return layer;
        }

        /// <summary>
        /// Adds a master layer to this scene. This layer should not contain any objects and instead be used to draw each other layer, optionally applying post-processing effects. <br/>
        /// Should be the last layer in hierarchy, is automatically added when you add post-processing effects to the scene.
        /// </summary>
        public RenderLayer AddMasterLayer(string name) {
            var layer = new RenderLayer(this, name);
            layer.SetMasteredLayers(_layers.ToArray());

            // if there's already a master layer defined,
            // remove it and dispose of it (brutal)
            if (_masterLayer != null) {
                _layers.Remove(_masterLayer);
                _masterLayer.Dispose();
            }

            // set the reference for later use
            _masterLayer = layer;
            _layers.Add(layer);

            return layer;
        }

        /// <summary>
        /// Attempts to get a render layer with provided name.
        /// </summary>
        public RenderLayer GetRenderLayer(string name) {
            for (var i = 0; i < _layers.Count; i++) {
                if (_layers[i].Name == name) {
                    return _layers[i];
                }
            }

            return null;
        }

        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void InternalUpdate(GameTime time) {
            // initialize the scene first
            if (!_isInitialized) {
                _isInitialized = true;
                if (Camera is null) {
                    Camera = CreateEntity("camera", Vector2.Zero)
                        .AddComponent<CameraComponent>();

                    Listener = Camera;
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
            
            // prepare all of the layers before drawing them on-screen
            for (var i = 0; i < _layers.Count; i++) {
                var layer = _layers[i];

                // we prepare the layer for presentation there
                if (layer.Active && layer.ShouldPresent) {
                    layer.Prepare();
                }
            }

            // use Scene RT for flattening
            var gd = InstantGame.Instance.GraphicsDevice;
            gd.SetRenderTarget(_sceneTarget);
            gd.Clear(Color.Transparent);

            var drawing = GraphicsManager.Backend;
            drawing.Push(Material.Default);

            // draw the layers onto the RT
            for (var i = 0; i < _layers.Count; i++) {
                _layers[i].Present(drawing);
            }

            drawing.Pop(true);

            // now draw the flattened layer image to backbuffer
            gd.SetRenderTarget(null);
            gd.Clear(Color.Transparent);

            drawing.Push(Material.Default);

            drawing.DrawTexture(_sceneTarget, Resolution.offset, Color.White, 0, new(Resolution.scaleFactor), Vector2.Zero);

            // invoke the user callback
            Render(drawing);

            // render some debug stuff
            if (_debugRender) {
                DebugRender(drawing);
            }

            drawing.Pop(true);
        }

        StringBuilder _debugInfoText = new();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void DebugRender(IDrawingBackend drawing) {
            drawing.DrawString($"Collision Debug", new(10), Color.LightBlue, new Vector2(3), 0, drawOutline: true);

            _debugInfoText.Clear();
            CollisionsDebugLayer(drawing, _debugInfoText);

            drawing.Pop();

            drawing.DrawString(_debugInfoText.ToString(), new(14, 42), Color.LightBlue, new Vector2(2), 0, drawOutline: true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void CollisionsDebugLayer(IDrawingBackend drawing, StringBuilder info) {
            drawing.Push(Material.Default, SceneToScreenTransform);

            var shiftHeld = InputManager.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift);
            var mousePos = Camera.MouseToWorldPosition();
            var colliderIndex = 0;

            foreach (var collider in FindComponentsOfType<CollisionComponent>()) {
                var bounds = collider.BaseCollider.Bounds;

                // cull the off-screen colliders
                if (!Camera.Bounds.Intersects(bounds))
                    continue;

                if (bounds.Contains(mousePos)) {
                    info.AppendLine($"#{colliderIndex++} '{collider.Entity.Name}': {collider.GetType().Name}");

                    // visualize the bitmasks
                    for (var j = 0; j < 2; j++) {
                        var mask = j == 0 ? collider.LayerMask : collider.CollidesWithMask;
                        info.Append($" - {(j == 0 ? "LayerMask" : "CollidesWith")}: {mask} (Set Flags: ");

                        // don't list all flags
                        if (mask == -1) {
                            info.AppendLine("ALL)");
                            continue;
                        }

                        var begun = false;
                        for (var i = 0; i < 32; i++) {
                            if (IntFlags.IsFlagSet(mask, i)) {
                                if (!begun) begun = true;
                                else info.Append(", ");
                                info.Append(i);
                            }
                        }

                        info.AppendLine(")");
                    }

                    info.AppendLine($" - Origin: {collider.Origin}");

                    info.AppendLine();
                }

                // draw bounds
                drawing.DrawRectangle(bounds, Color.Transparent, new Color(1f, 1f, 1f, bounds.Contains(mousePos) ? 1f : 0.5f));

                // draw actual collider shape
                switch (collider) {
                    case CircleCollisionComponent circle:
                        drawing.DrawCircle(collider.Transform.Position, circle.Radius, Color.Red, resolution: 24);
                        break;
                }
            }
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

            // raise the cleanup event
            Events.Raise<SceneCleanupEvent>(default);

            // farewell
            _isCleanedUp = true;
        }

        internal void ResizeRenderTargets(ScaledResolution resolution) {
            var gd = InstantGame.Instance.GraphicsDevice;

            var previous = Resolution;

            // resize RenderTargets when needed
            if (_sceneTarget == null || previous.renderTargetSize != resolution.renderTargetSize) {
                // get Scene size
                var (width, height) = (resolution.renderTargetSize.X, resolution.renderTargetSize.Y);

                // dispose of the existing RTs
                _sceneTarget?.Dispose();
                _tempTarget?.Dispose();

                // allocate new scene targets
                _sceneTarget = new RenderTarget2D(gd, width, height);
                _tempTarget = new RenderTarget2D(gd, width, height);
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

        /// <summary>
        /// Plays one-shot sound effect and automatically recycles it with optional cool features: <list type="bullet">
        /// <item> You can position the sound in world-space using <paramref name="position"/> parameter. In order for it to work, <paramref name="rolloff"/> should not be <see langword="default"/>. </item>
        /// <item> 
        /// You can control the way audio rolloff is applied to this sound using <paramref name="rolloff"/> parameter. <br/>
        /// By default, depending on if <paramref name="position"/> is set, the sound will either use default rolloff of MaxDistance = 200 or not use it at all. <br/>
        /// Pass in <see cref="AudioRolloff.None"/> to make the sound global (or just dont pass <paramref name="position"/> lol, not much use for it in this case). 
        /// </item>
        /// <item> 
        /// <paramref name="volume"/>, <paramref name="pan"/> and <paramref name="pitch"/> can be used to adjust characteristics of the sound. <br/>
        /// Note that <paramref name="pan"/> will be overriden if AudioRolloff is enabled.
        /// </item>
        /// <item> 
        /// The sound may be attached to a moving entity (meaning it will continuosly update position) using <paramref name="followEntity"/> parameter. <br/>
        /// If <paramref name="followEntity"/> is destroyed during the sound playback, it will remain on the last position it had.
        /// </item>
        /// </list>
        /// </summary>
        public CoroutineInstance PlaySound(Sound sound, Vector2? position = default, AudioRolloff? rolloff = default, float volume = 1.0f, float pan = 0f, float pitch = 0f, Entity followEntity = default) {
            var instance = sound.CreateStaticInstance();
            instance.Pitch = pitch;
            instance.Pan = pan;

            // determine rolloff for this
            var actualRolloff = (position, rolloff) switch {
                // in case rolloff is explicitly provided, just use it
                (_, AudioRolloff setRolloff) => setRolloff,

                // if position is provided, but not the rolloff initialize the default instance
                (Vector2 setPosition, null) => new AudioRolloff(),

                // guh!
                _ => AudioRolloff.None
            };

            return this.RunCoroutine(
                AudioComponent.OneShotSound(instance, position, actualRolloff, volume, followEntity),
                _ => instance.Pool()
            );
        }

        // ICoroutineTarget impl
        bool ICoroutineTarget.IsActive => !_isCleanedUp;
        float ICoroutineTarget.TimeScale => TimeScale;
    }
}
