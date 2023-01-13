using Instant2D.Graphics;
using Instant2D.Input;
using Instant2D.Utils;
using Instant2D.Utils.Math;
using Instant2D.Utils.ResolutionScaling;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Instant2D.Collision;
using Instant2D.EC.Components;
using Instant2D.EC.Events;
using Instant2D.Assets;
using Instant2D.Assets.Containers;
using Instant2D.Audio;
using System.Text;
using Instant2D.EC.Rendering;
using System.IO;
using System.Runtime.InteropServices;
using Instant2D.Coroutines;

namespace Instant2D.EC {
    public abstract class Scene : ICoroutineTarget {
        internal readonly List<Entity> _entities = new(128);
        RenderTarget2D _sceneTarget, _tempTarget;
        float _fixedTimestepProgress;
        bool _isInitialized, _isCleanedUp;
        bool _debugRender;

        // how many fixed updates have been completed
        internal int _fixedUpdatesPassed;

        readonly List<IRenderLayer> _renderLayers = new(12);

        /// <summary>
        /// Represents collection of RenderLayers this scene will use.
        /// </summary>
        public IReadOnlyList<IRenderLayer> RenderLayers => _renderLayers;

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
        /// An interval at which components implementing <see cref="IFixedUpdate"/> step forward. Defaults to 1/60.
        /// </summary>
        public float FixedTimeStep = 1.0f / 60;

        /// <summary>
        /// Used for interpolation between FixedUpdate frames, a value in range of 0.0 - 1.0. <br/>
        /// TODO: come up with a better name for this... ?
        /// </summary>
        public float AlphaFrameTime;

        /// <summary>
        /// Amount of time that has passed since beginning this scene, taking <see cref="TimeScale"/> into account.
        /// </summary>
        public float TotalTime;

        /// <summary>
        /// The listener object to use with spatial sounds. By default, it is assigned to <see cref="Camera"/>'s Entity.
        /// </summary>
        public Entity Listener;

        /// <summary>
        /// Default Entity RenderLayer used for components with nothing specified.
        /// </summary>
        public EntityLayer DefaultRenderLayer;

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
            var entity = Pool<Entity>.Shared.Get();
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
        /// Create and register <typeparamref name="T"/>.
        /// </summary>
        public T AddLayer<T>(float order, string name) where T: IRenderLayer, new() {
            var layer = new T {
                Scene = this, Name = name, Order = order,
                ShouldPresent = true, IsActive = true,
            };

            // update the list and sort
            _renderLayers.Add(layer);
            _renderLayers.Sort();

            // set default render layer
            if (DefaultRenderLayer == null && layer is EntityLayer entityLayer)
                DefaultRenderLayer = entityLayer;

            return layer;
        }

        /// <summary>
        /// Create and register <typeparamref name="T"/> using automatically assigned Order.
        /// </summary>
        public T AddLayer<T>(string name) where T : IRenderLayer, new() 
            => AddLayer<T>(_renderLayers.Count == 0 ? 0 : _renderLayers.Max(l => l.Order) + 1, name);

        /// <summary>
        /// Attempts to find a render layer with provided name.
        /// </summary>
        public IRenderLayer GetLayer(string name) {
            for (var i = 0; i < _renderLayers.Count; i++) {
                if (_renderLayers[i].Name == name) {
                    return _renderLayers[i];
                }
            }

            return null;
        }

        /// <summary>
        /// Attempts to find a typed render layer with provided name.
        /// </summary>
        public T GetLayer<T>(string name, bool checkNestedLayers = true) where T: IRenderLayer {
            for (var i = 0; i < _renderLayers.Count; i++) {
                if (_renderLayers[i].Name == name) {
                    switch (_renderLayers[i]) {
                        case T typedLayer:
                            return typedLayer;

                        // check nested layers when prompted to
                        case INestedRenderLayer nestedLayer when checkNestedLayers && nestedLayer.Content is T nestedTypedLayer:
                            return nestedTypedLayer;
                    }
                }
            }

            return default;
        }

        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void InternalUpdate(GameTime time) {
            // initialize the scene first
            // TODO: move initialiation into SceneManager instead?
            if (!_isInitialized) {
                _isInitialized = true;

                // create a camera if haven't already
                if (Camera is null) {
                    Camera = CreateEntity("camera", Vector2.Zero)
                        .AddComponent<CameraComponent>();

                    Listener = Camera;
                }

                Initialize();

                // add default render layer if none was added
                if (_renderLayers.Count == 0) {
                    InstantApp.Logger.Info("No RenderLayers were added, automatically added 'default'.");
                    AddLayer<EntityLayer>(0, "default");
                }

                // initialize RTs for newly added layers
                ResizeRenderTargets(Resolution);
            }

            // call PreUpdate first to ensure that no destroyed or uninitialized components are updated
            for (var i = 0; i < _entities.Count; i++) {
                _entities[i].PreUpdate();
            }

            var dt = (float)time.ElapsedGameTime.TotalSeconds;
            TotalTime += dt * TimeScale;

            var fixedUpdateCount = 0;
            _fixedTimestepProgress += dt * TimeScale;

            // determine amount of fixed updates
            while (_fixedTimestepProgress >= FixedTimeStep) {
                _fixedTimestepProgress -= FixedTimeStep;
                fixedUpdateCount++;
            }

            // get alpha frame time for interpolations
            AlphaFrameTime = _fixedTimestepProgress / FixedTimeStep;

            // apply FixedUpdates
            var span = CollectionsMarshal.AsSpan(_entities);

            if (fixedUpdateCount > 0) {
                for (var i = 0; i < span.Length; i++) {
                    var entity = span[i];
                    if (entity._timescale == 1f) {
                        // set the lastTransformState for all entities first
                        entity._lastTransformState = entity.Transform.Data;
                    }
                }
            }
            
            // now loop over all entities and invoke FixedUpdates
            for (var i = 0; i < span.Length; i++) {
                var entity = span[i];
                if (entity._timescale == 1f) {
                    entity.FixedUpdateGlobal(fixedUpdateCount);
                    entity.AlphaFrameTime = AlphaFrameTime;
                } else {
                    entity.FixedUpdateCustom(dt);
                }
            }

            // call scene FixedUpdate
            for (var i = 0; i < fixedUpdateCount; i++) {
                FixedUpdate();

                // increment the fixedUpdate counter
                _fixedUpdatesPassed++;

                // tick all WaitForFixedUpdate coroutines
                // CoroutineManager.TickFixedUpdate(this);
            }

            // apply Updates
            foreach (var entity in span) {
                entity.UpdateComponents(dt);
            }

            // apply LateUpdates
            foreach (var entity in span) {
                entity.LateUpdate(dt);
            }

            // switch debug render
            // TODO: move to a component
            if (InputManager.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.OemTilde)) {
                _debugRender = !_debugRender;
            }

            if (IsActive) 
                Update();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void InternalRender() {
            if (!IsVisible || !_isInitialized) {
                return;
            }

            Render();
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

                    // replace materials with reloaded effects
                    case IAssetContainer<Effect> effectAsset:
                        foreach (var renderable in Entities.SelectMany(e => e.Components.OfType<RenderableComponent>()).Where(r => r.Material != null && r.Material.Effect != null)) {
                            if (renderable.Material.Effect.Tag is string key && key == asset.Key)
                                renderable.Material = renderable.Material with { Effect = effectAsset.Content };
                        }

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
            CoroutineManager.Instance.StopAll(this);
            OnExiting();

            // raise the cleanup event
            Events.Raise<SceneCleanupEvent>(default);

            // farewell
            _isCleanedUp = true;
        }

        internal void ResizeRenderTargets(ScaledResolution resolution) {
            var gd = InstantApp.Instance.GraphicsDevice;

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
        /// Called each frame when <see cref="IsActive"/>. Make sure to include <c>base.Update();</c> in your override, or include your own entity update routine.
        /// </summary>
        public virtual void Update() {

        }

        /// <summary>
        /// Called according to <see cref="FixedTimeStep"/>.
        /// </summary>
        public virtual void FixedUpdate() {

        }

        /// <summary>
        /// Called on the first scene update. 
        /// </summary>
        public virtual void Initialize() {
            
        }

        /// <summary>
        /// Called when this scene unloads before loading the next one.
        /// </summary>
        public virtual void OnExiting() {

        }

        /// <summary>
        /// Called each frame when <see cref="IsVisible"/> is set and Scene is initialized. Make sure to include <c>base.Render();</c> in your override, or implement your own rendering routine.
        /// </summary>
        public virtual void Render() {
            var layersSpan = CollectionsMarshal.AsSpan(_renderLayers);

            // prepare all of the layers before drawing them on-screen
            for (var i = 0; i < layersSpan.Length; i++) {
                if (layersSpan[i].IsActive) {
                    layersSpan[i].Prepare();
                }
            }

            // use Scene RT for flattening
            var gd = InstantApp.Instance.GraphicsDevice;
            gd.SetRenderTarget(_sceneTarget);
            gd.Clear(Color.Transparent);

            var drawing = GraphicsManager.Context;
            drawing.Begin(Material.Opaque, Matrix.Identity);

            // draw the layers onto the RT
            for (var i = 0; i < layersSpan.Length; i++) {
                if (layersSpan[i].ShouldPresent)
                    layersSpan[i].Present(drawing);
            }

            drawing.End();

            // now draw the flattened layer image to backbuffer
            gd.SetRenderTarget(null);
            gd.Clear(Color.Transparent);

            drawing.Begin(Material.Opaque, Matrix.Identity);

            drawing.DrawTexture(_sceneTarget, Resolution.offset, null, Color.White, 0, Vector2.Zero, new(Resolution.scaleFactor));

            drawing.End();
        }

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
        public Coroutine PlaySound(Sound sound, Vector2? position = default, AudioRolloff? rolloff = default, float volume = 1.0f, float pan = 0f, float pitch = 0f, Entity followEntity = default) {
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

            // TODO: update to new coroutine system
            return null;
            //return this.RunCoroutine(
            //    AudioComponent.OneShotSound(instance, position, actualRolloff, volume, followEntity),
            //    (coroutine, _) => coroutine.Context<StaticAudioInstance>().Pool()
            //).SetContext(instance);
        }

        // simple assets shortcut to avoid some verbosity
        public static AssetManager Assets => AssetManager.Instance;

        // ICoroutineTarget impl
        float ICoroutineTarget.TimeScale => TimeScale;
    }
}
