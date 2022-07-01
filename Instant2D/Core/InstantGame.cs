using Instant2D.Assets;
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
        readonly List<SubSystem> _subSystems = new(16), _updatableSystems = new(16);
        internal Logger _logger;
        bool _initialized;

        // fps stuff
        TimeSpan _fpsTimer;
        int _fpsCounter;

        // window
        string _title;

        public GraphicsDeviceManager GraphicsDeviceManager { get; private set; }

        public InstantGame() {
            Instance = this;

            GraphicsDeviceManager = new GraphicsDeviceManager(this);
            IsMouseVisible = true;
        }

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

        internal void UpdateSystem(SubSystem system) {
            _updatableSystems.Add(system);
        }

        /// <summary>
        /// Setup all of the systems in there using <see cref="AddSystem{T}(Action{T})"/>.
        /// </summary>
        protected virtual void SetupSystems() { }

        protected override void LoadContent() {
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
        }

        protected override void Update(GameTime gameTime) {
            // calculate FPS 
            _fpsCounter++;
            _fpsTimer += gameTime.ElapsedGameTime;
            if (_fpsTimer >= TimeSpan.FromSeconds(1)) {
                Window.Title = $"{_title} [{_fpsCounter} FPS, {GC.GetTotalMemory(false) / 1048576f:F1} MB]";

                // reset the FPS metrics
                _fpsTimer -= TimeSpan.FromSeconds(1);
                _fpsCounter = 0;
            }

            // update the subsystems
            for (var i = _updatableSystems.Count - 1; i >= 0; i--) {
                var system = _updatableSystems[i];
                system.Update(gameTime);

                // remove the system if it's no longer updatable
                if (!system.ShouldUpdate) {
                    _updatableSystems.RemoveAt(i);
                }
            }

            base.Update(gameTime);
        }
    }
}
