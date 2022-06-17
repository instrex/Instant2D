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

        Logger _logger;
        public Logger Logger {
            get {
                if (_logger == null)
                    _logger = new();

                return _logger;
            }
        }

        readonly List<SubSystem> _subSystems = new(16);
        public InstantGame AddSystem<T>(Action<T> initializer = default) where T: SubSystem, new() {
            var instance = new T();
            initializer?.Invoke(instance);
            _subSystems.Add(instance);
            _subSystems.Sort();

            return this;
        }

        protected virtual void SetupSystems() { }

        protected override void Initialize() {
            base.Initialize();

            SetupSystems();

            // set the singleton
            Instance = this;
            foreach (var system in _subSystems) {
                system.Initialize();
            }
        }
    }

    public class TestGame : InstantGame {
        protected override void SetupSystems() {
            AddSystem<AssetManager>(assets => {
                
            });

        }
    }
}
