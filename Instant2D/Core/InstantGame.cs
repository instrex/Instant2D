using Instant2D.Assets;
using Instant2D.Audio;
using Instant2D.Coroutines;
using Instant2D.Graphics;
using Instant2D.Input;
using Instant2D.Utils;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.Core {
    public abstract class InstantGame : Game {
        public static InstantGame Instance { get; private set; }

        // subsystem loading
        internal readonly List<SubSystem> 
            _subSystems = new(16),
            _updatableSystems = new(8),
            _renderableSystems = new(8);

        internal Logger _logger;
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
                if (_title == null) {
                    _title = AppDomain.CurrentDomain.FriendlyName;
                }

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

        public GraphicsDeviceManager GraphicsDeviceManager { get; private set; }

        public InstantGame() {
            Instance = this;

            GraphicsDeviceManager = new GraphicsDeviceManager(this);
            IsMouseVisible = true;
        }

        #region System Management

        public Logger Logger {
            get {
                if (_logger == null)
                    _logger = AddSystem<Logger>();

                return _logger;
            }
        }

        public T AddSystem<T>(Action<T> initializer = default) where T: SubSystem, new() {
            var instance = new T { Game = this };
            initializer?.Invoke(instance);
            _subSystems.Add(instance);

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
        public bool TryGetSystem<T>(out T system) where T: SubSystem {
            for (var i = 0; i < _subSystems.Count; i++) {
                if (_subSystems[i] is T foundSystem) {
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
        public T GetSystem<T>() where T : SubSystem {
            for (var i = 0; i < _subSystems.Count; i++) {
                if (_subSystems[i] is T foundSystem) {
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
            foreach (var system in _subSystems.ToList()) {
                system.Initialize();
            }

            // then, sort them for later update tasks
            _subSystems.Sort();

            Initialize();
        }

        protected override void Update(GameTime gameTime) {
            // sort the systems when needed
            if (_systemOrderDirty) {
                _systemOrderDirty = false;
                _updatableSystems.Sort();
            }

            // update the subsystems
            for (var i = 0; i < _updatableSystems.Count; i++) {
                var system = _updatableSystems[i];
                system.Update(gameTime);
            }
        }

        static readonly TimeSpan _oneSecond = TimeSpan.FromSeconds(1);

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
