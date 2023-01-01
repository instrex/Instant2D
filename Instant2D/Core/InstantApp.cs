using Instant2D.Assets;
using Instant2D.Audio;
using Instant2D.Coroutines;
using Instant2D.Diagnostics;
using Instant2D.Graphics;
using Instant2D.Input;
using Instant2D.Utils;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D {
    public abstract class InstantApp : Game {
        public static InstantApp Instance { get; private set; }

        // GameSystem loading
        internal readonly List<GameSystem> 
            _GameSystems = new(16),
            _updatableSystems = new(8),
            _renderableSystems = new(8);

        internal ILogger _logger;
        internal bool _systemOrderDirty;
        bool _initialized;

        // fps stuff
        TimeSpan _fpsTimer;
        int _framesPerSecond;
        int _fpsCounter;

        // window
        string _title;

        /// <summary>
        /// Gets or sets the window's title taking FPS counter into account.
        /// </summary>
        public string Title {
            get {
                _title ??= AppDomain.CurrentDomain.FriendlyName;
                return _title;
            }

            set {
                Window.Title = value;
                _title = value;
            }
        }

        /// <summary>
        /// Current number of frames per second.
        /// </summary>
        public int FPS => _framesPerSecond;

        public void SetTargetFramerate(int framesPerSecond) {
            if (framesPerSecond == -1) {
                IsFixedTimeStep = false;
                return;
            }

            TargetElapsedTime = TimeSpan.FromSeconds(1.0f / framesPerSecond);
            IsFixedTimeStep = true;
        }

        public GraphicsDeviceManager GraphicsDeviceManager { get; private set; }

        public InstantApp() {
            Instance = this;

            GraphicsDeviceManager = new GraphicsDeviceManager(this);
            IsMouseVisible = true;
        }

        #region System Management

        /// <summary>
        /// Logger implementation used for this game.
        /// </summary>
        public static ILogger Logger {
            get {
                if (Instance._logger == null) {
                    var defaultLogger = new DefaultLogger();
                    Instance._logger = defaultLogger;
                }

                return Instance._logger;
            }

            set => Instance._logger = value;
        }

        public T AddSystem<T>(Action<T> initializer = default) where T: GameSystem, new() {
            var instance = new T { Game = this };
            initializer?.Invoke(instance);
            _GameSystems.Add(instance);

            // if the game has already been initialized,
            // initialize this system as well
            if (_initialized) {
                instance.Initialize();
            }

            return instance;
        }

        /// <summary>
        /// Attempts to get a system.
        /// </summary>
        public bool TryGetSystem<T>(out T system) where T: GameSystem {
            for (var i = 0; i < _GameSystems.Count; i++) {
                if (_GameSystems[i] is T foundSystem) {
                    system = foundSystem;
                    return true;
                }
            }

            system = default;
            return false;
        }

        /// <summary>
        /// Gets the system, throwing an exception if it doesn't exist.
        /// </summary>
        public T GetSystem<T>() where T : GameSystem {
            for (var i = 0; i < _GameSystems.Count; i++) {
                if (_GameSystems[i] is T foundSystem) {
                    return foundSystem;
                }
            }

            throw new InvalidOperationException($"{GetType().Name} has no {typeof(T).Name} attached.");
        }

        #endregion

        #region Game Lifecycle

        /// <summary>
        /// Setup all of the systems in there using <see cref="AddSystem{T}(Action{T})"/>.
        /// </summary>
        protected virtual void SetupSystems() { }

        /// <summary>
        /// Called after each system has been initialized.
        /// </summary>
        protected virtual new void Initialize() { }

        #endregion

        /// <summary>
        /// Setups <see cref="InputManager"/>, <see cref="CoroutineManager"/>, <see cref="GraphicsManager"/> and <see cref="AudioManager"/>.
        /// </summary>
        protected void SetupDefaultSystems() {
            AddSystem<InputManager>();
            AddSystem<CoroutineManager>();
            AddSystem<GraphicsManager>();
            AddSystem<AudioManager>();
        }

        protected sealed override void LoadContent() {
            base.LoadContent();

            // save the original title
            if (_title == null) {
                _title = AppDomain.CurrentDomain.FriendlyName;
            }

            AddSystem<TimeManager>();

            SetupSystems();

            // initialize systems in order of definition
            _initialized = true;
            foreach (var system in _GameSystems.ToList()) {
                system.Initialize();
            }

            // setup file logging
            if (_logger is DefaultLogger defaultLogger)
                defaultLogger.SetOutputFile(".log");

            // then, sort them for later update tasks
            _GameSystems.Sort();

            Initialize();
        }

        protected override void Update(GameTime gameTime) {
            // sort the systems when needed
            if (_systemOrderDirty) {
                _systemOrderDirty = false;
                _updatableSystems.Sort();
            }

            // update the GameSystems
            for (var i = 0; i < _updatableSystems.Count; i++) {
                var system = _updatableSystems[i];
                system.Update(gameTime);
            }
        }

        static readonly TimeSpan _oneSecond = TimeSpan.FromSeconds(1);

        protected override void OnExiting(object sender, EventArgs args) {
            _logger?.Close();

            base.OnExiting(sender, args);
        }

        protected override void Draw(GameTime gameTime) {
            // calculate FPS 
            _fpsCounter++;
            _fpsTimer += gameTime.ElapsedGameTime;
            if (_fpsTimer >= _oneSecond) {
                Window.Title = $"{_title} [{_fpsCounter} FPS, {GC.GetTotalMemory(false) / 1048576f:F1} MB]";

                // reset the FPS metrics
                _framesPerSecond = _fpsCounter;
                _fpsTimer -= _oneSecond;
                _fpsCounter = 0;
            }

            // draw systems
            for (var i = 0; i < _renderableSystems.Count; i++) {
                var system = _renderableSystems[i];
                system.Render(gameTime);
            }
        }
    }
}
