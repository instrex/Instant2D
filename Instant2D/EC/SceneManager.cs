using Instant2D.Coroutines;
using Instant2D.Graphics;
using Instant2D.Modules;
using Instant2D.Utils;
using Instant2D.Utils.ResolutionScaling;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.EC;

/// <summary>
/// The brain of entity system. 
/// </summary>
public class SceneManager : IGameSystem, IRenderableGameSystem {
    public static SceneManager Instance { get; set; }

    CoroutineManager _coroutineManager;

    InstantApp _attachedApp;
    ScaledResolution _resolution;
    Scene _current, _next;

    /// <summary>
    /// When <see langword="true"/>, game will not be updated when minimized.
    /// </summary>
    public bool PauseOnFocusLost { get; set; } = true;

    /// <summary>
    /// Makes it possible to use WaitForSceneEvent inside coroutines. <br/>
    /// Defaults to <see langword="true"/>, may be disabled if you control it manually.
    /// </summary>
    public bool TickCoroutineEvents { get; set; } = true;

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

    /// <summary>
    /// Currently displayed and updated scene.
    /// </summary>
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
    /// Switches current scene to a new instance created by parameterless constructor.
    /// </summary>
    public static T Switch<T>() where T : Scene, new() {
        var scene = new T();
        Switch(scene);
        return scene;
    }

    /// <summary>
    /// Swtiches current scene to <paramref name="scene"/>.
    /// </summary>
    public static void Switch<T>(T scene) where T: Scene {
        InstantApp.Logger.Info($"Loading scene '{scene.GetType()}'...");
        Instance.Current = scene;
    }

    internal void OnClientSizeChanged(object sender, EventArgs e) {
        var screenSize = new Point(_attachedApp.GraphicsDevice.Viewport.Width, _attachedApp.GraphicsDevice.Viewport.Height);
        _resolution = ResolutionScaler?.Calculate(screenSize) ?? new ScaledResolution { scaleFactor = 1.0f, renderTargetSize = screenSize };
        _current?.ResizeRenderTargets(_resolution);
    }

    float IGameSystem.UpdateOrder { get; }
    float IRenderableGameSystem.PresentOrder { get; }
    void IGameSystem.Initialize(InstantApp app) {
        Instance = this;

        // check if GraphicsManager is added
        if (app.GetModule<GraphicsManager>() is null) {
            InstantApp.Logger.Info("SceneManager requires GraphicsManager system to be added, initializing default...");
            app.AddModule<GraphicsManager>();
        }

        // get coroutine module for tick synchronization
        _coroutineManager = app.GetModule<CoroutineManager>();  
        if (_coroutineManager != null) {
            _coroutineManager.IsManuallyControlled = true;
        }

        // setup the client size change callback for resizing RTs and stuff
        app.Window.ClientSizeChanged += OnClientSizeChanged;
        _attachedApp = app;

        // initialize the resolution
        var screenSize = new Point(app.GraphicsDevice.Viewport.Width, app.GraphicsDevice.Viewport.Height);
        _resolution = ResolutionScaler?.Calculate(screenSize) ?? new ScaledResolution { scaleFactor = 1.0f, renderTargetSize = screenSize };
    }

    void IGameSystem.Update(InstantApp app, float deltaTime) {
        if (PauseOnFocusLost && !app.IsActive)
            return;

        _current?.InternalUpdate(deltaTime);
        _coroutineManager?.Tick();

        if (_next != null) {
            _current.Cleanup();
            _current = _next;
            _next = null;
        }
    }

    void IRenderableGameSystem.Present(InstantApp app) {
        _current?.InternalRender();
    }
}
