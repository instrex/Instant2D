﻿using Instant2D.Assets;
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

        public GraphicsDeviceManager GraphicsDeviceManager { get; private set; }

        public InstantGame() {
            GraphicsDeviceManager = new GraphicsDeviceManager(this);

            IsMouseVisible = true;
            Instance = this;
        }

        Logger _logger;
        public Logger Logger {
            get {
                if (_logger == null)
                    _logger = new();

                return _logger;
            }
        }

        readonly List<SubSystem> _subSystems = new(16);
        readonly List<SubSystem> _updatableSystems = new(16);
        public InstantGame AddSystem<T>(Action<T> initializer = default) where T: SubSystem, new() {
            var instance = new T { Game = this };
            initializer?.Invoke(instance);
            _subSystems.Add(instance);

            return this;
        }

        internal void UpdateSystem(SubSystem system) {
            _updatableSystems.Add(system);
        }

        /// <summary>
        /// Setup all of the systems in there using <see cref="AddSystem{T}(Action{T})"/>.
        /// </summary>
        protected virtual void SetupSystems() { }

        protected override void Initialize() {
            base.Initialize();

            
        }

        protected override void LoadContent() {
            base.LoadContent();

            AddSystem<TimeManager>();

            SetupSystems();

            // initialize systems in order of definition
            foreach (var system in _subSystems) {
                system.Initialize();
            }

            // then, sort them for later update tasks
            _subSystems.Sort();
        }

        protected override void Update(GameTime gameTime) {
            base.Update(gameTime);

            // update the subsystems
            for (var i = _updatableSystems.Count - 1; i >= 0; i--) {
                var system = _updatableSystems[i];
                system.Update(gameTime);

                // remove the system if it's no longer updatable
                if (!system.ShouldUpdate) {
                    _updatableSystems.RemoveAt(i);
                }
            }
        }
    }
}
