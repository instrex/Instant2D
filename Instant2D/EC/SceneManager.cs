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
        Scene _current, _next;

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
            internal set {
                if (_current != null)
                    _next = value;

                else _current = value;
                if (value != null) {
                    value.Resolution = _resolution;
                }
            }
        }

        /// <summary>
        /// Swtiches current scene to a new instance created by parameterless constructor.
        /// </summary>
        public static void Switch<T>() where T : Scene, new() => Switch<T>(new());

        /// <summary>
        /// Swtiches current scene to <paramref name="scene"/>.
        /// </summary>
        public static void Switch<T>(T scene) where T: Scene {
            Logger.WriteLine($"Switching to scene '{scene.GetType()}'...");
            Instance.Current = scene;
        }

        public override void Initialize() {
            Instance = this;
            IsUpdatable = true;
            IsRenderable = true;

            if (!InstantGame.Instance.TryGetSystem<GraphicsManager>(out _)) {
                InstantGame.Instance.Logger.Warning("SceneManager requires GraphicsManager system to be added, initializing...");
                InstantGame.Instance.AddSystem<GraphicsManager>();
            }

            // setup the client size change callback for resizing RTs and stuff
            InstantGame.Instance.Window.ClientSizeChanged += OnClientSizeChanged;

            // initialize the resolution
            var screenSize = new Point(Game.GraphicsDevice.Viewport.Width, Game.GraphicsDevice.Viewport.Height);
            _resolution = ResolutionScaler?.Calculate(screenSize) ?? new ScaledResolution { scaleFactor = 1.0f, renderTargetSize = screenSize };
        }

        public override void Update(GameTime time) {
            _current?.InternalUpdate(time);
            if (_next != null) {
                _current.Cleanup();
                _current = _next;
                _next = null;
            }
        }

        public override void Render(GameTime time) {
            _current?.InternalRender();
        }

        void OnClientSizeChanged(object sender, EventArgs e) {
            var screenSize = new Point(Game.GraphicsDevice.Viewport.Width, Game.GraphicsDevice.Viewport.Height);
            _resolution = ResolutionScaler?.Calculate(screenSize) ?? new ScaledResolution { scaleFactor = 1.0f, renderTargetSize = screenSize };
            _current?.ResizeRenderTargets(_resolution);
        }
    }
}
 