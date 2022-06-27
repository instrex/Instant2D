using Instant2D.Core;
using Instant2D.Graphics;
using Instant2D.Utils;
using Instant2D.Utils.ResolutionScaling;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.EC {
    /// <summary>
    /// The meat of entity system. 
    /// </summary>
    public class SceneManager : SubSystem {
        public static SceneManager Instance { get; set; }

        ScaledResolution _resolution;
        Scene _current;


        /// <summary>
        /// Resolution scaler which will apply to all scenes this SceneManager uses. May be null.
        /// </summary>
        public IResolutionScaler ResolutionScaler { 
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get; set; 
        }

        public T SetResolutionScaler<T>() where T : IResolutionScaler, new() {
            var scaler = new T();
            ResolutionScaler = scaler;

            return scaler;
        }

        public Scene Current {
            get => _current;
            set {
                _current = value;
                if (_current != null) {
                    _current.Resolution = _resolution;
                }
            }
        }

        public override void Initialize() {
            Instance = this;
            ShouldUpdate = true;

            if (!InstantGame.Instance.TryGetSystem<GraphicsManager>(out _)) {
                InstantGame.Instance.Logger.Warning("SceneManager requires GraphicsManager system to be added, initializing...");
                InstantGame.Instance.AddSystem<GraphicsManager>();
            }

            // setup the client size change callback for resizing RTs and stuff
            InstantGame.Instance.Window.ClientSizeChanged += UpdateResolution;

            // initialize the resolution
            var screenSize = new Point(Game.GraphicsDevice.Viewport.Width, Game.GraphicsDevice.Viewport.Height);
            _resolution = ResolutionScaler?.Calculate(screenSize) ?? new ScaledResolution { scaleFactor = 1.0f, renderTargetSize = screenSize };
        }

        public override void Update(GameTime time) {
            _current?.InternalUpdate();
        }

        private void UpdateResolution(object sender, EventArgs e) {
            var screenSize = new Point(Game.GraphicsDevice.Viewport.Width, Game.GraphicsDevice.Viewport.Height);
            _resolution = ResolutionScaler?.Calculate(screenSize) ?? new ScaledResolution { scaleFactor = 1.0f, renderTargetSize = screenSize };
            _current?.ResizeRenderTargets(_resolution);
        }
    }
}
 