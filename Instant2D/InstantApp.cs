﻿using Instant2D.Assets;
using Instant2D.Coroutines;
using Instant2D.Diagnostics;
using Instant2D.Graphics;
using Instant2D.Modules;
using Instant2D.Input;
using Instant2D.Utils;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Instant2D.EC;

namespace Instant2D;

public abstract class InstantApp : Game {
    /// <summary>
    /// Currently running InstantApp.
    /// </summary>
    public static InstantApp Instance { get; private set; }

    readonly List<IRenderableGameSystem> _renderableModules = new(8);
    readonly List<IGameSystem> _modules = new(16);

    string _defaultTitle;
    ILogger _logger;

    public GraphicsDeviceManager GraphicsDeviceManager { get; private set; }

    /// <summary>
    /// Gets or sets the game's base title. <br/>
    /// Allows systems like <see cref="PerformanceCounter"/> to modify the title with necessary information.
    /// </summary>
    public string DefaultTitle {
        get => _defaultTitle;
        set => Window.Title = _defaultTitle = value;
    }

    public InstantApp() {
        GraphicsDeviceManager = new GraphicsDeviceManager(this);
        IsMouseVisible = true;
        IsFixedTimeStep = false;
        Instance = this;
    }

    #region System Management

    /// <summary>
    /// Logger implementation used for this app.
    /// </summary>
    public static ILogger Logger {
        get {
            // initialize default logger on-demand
            return Instance._logger ??= Instance.AddModule<DefaultLogger>(logger => {
                logger.SetOutputFile(".log");
            });
        }

        set => Instance._logger = value;
    }

    /// <summary>
    /// Register game system using default constructor.
    /// </summary>
    /// <param name="initializer"> Optional initializer delegate. </param>
    public T AddModule<T>(Action<T> initializer = default) where T : IGameSystem, new() => AddModule(new T(), initializer);

    /// <summary>
    /// Register game system.
    /// </summary>
    /// <param name="initializer"> Optional initializer delegate. </param>
    public T AddModule<T>(T system, Action<T> initializer = default) where T : IGameSystem {
        initializer?.Invoke(system);
        _modules.Add(system);

        // register renderable systems
        if (system is IRenderableGameSystem renderableGameSystem) {
            _renderableModules.Add(renderableGameSystem);
        }

        return system;
    }

    /// <summary>
    /// Adds a module if it wasn't present.
    /// </summary>
    public T AddModuleIfMissing<T>() where T: IGameSystem, new() {
        return GetModule<T>() ?? AddModule<T>();
    }

    /// <summary>
    /// Attempts to find game system of type <typeparamref name="T"/>. Returns <see langword="null"/> if not found.
    /// </summary>
    public T GetModule<T>() where T : IGameSystem => (T)_modules.Find(m => m is T);

    /// <summary>
    /// Attempts to remove module of type <typeparamref name="T"/>. Return <see langword="true"/> on success.
    /// </summary>
    public bool RemoveModule<T>() where T : IGameSystem => _modules.Remove(_modules.Find(m => m is T));

    /// <summary>
    /// Triggers module reinitialization. Should be used after you modify existing modules at runtime.
    /// </summary>
    public void InitializeModules() {
        _modules.Sort();
        foreach (var module in _modules.ToList()) {
            module.Initialize(this);
        }
    }

    /// <summary>
    /// Adds commonly used systems to the app, such as:
    /// <list type="bullet">
    /// <item> <see cref="GraphicsManager"/> is required for any rendering done in the app. </item>
    /// <item> <see cref="KeyboardInput"/>, <see cref="MouseInput"/> for basic input processing; </item>
    /// <item> <see cref="CoroutineManager"/> for yieldable, continuous functions, scheduling timers and more; </item>
    /// <item> <see cref="AudioManager"/> for handling sounds and music in streaming/immediate modes; </item>
    /// </list>
    /// </summary>
    protected void AddDefaultModules() {
        AddModule<GraphicsManager>();
        AddModule<KeyboardInput>();
        AddModule<MouseInput>();
        AddModule<CoroutineManager>();
    }

    #endregion

    /// <summary>
    /// Sets window size.
    /// </summary>
    public void SetWindowSize(int width, int height) {
        GraphicsDeviceManager.PreferredBackBufferWidth = width;
        GraphicsDeviceManager.PreferredBackBufferHeight = height;
        GraphicsDeviceManager.ApplyChanges();

        // trigger scene resolution calculations
        SceneManager.Instance.OnClientSizeChanged(null, null);
    }

    /// <summary>
    /// Sets target framerate. Pass <c>-1</c> to set fps to uncapped. <br/>
    /// Defaults to uncapped framerate.
    /// </summary>
    public void SetTargetFrameRate(int fps = -1) {
        if (fps == -1) {
            IsFixedTimeStep = false;
            return;
        }

        // set target framerate
        TargetElapsedTime = TimeSpan.FromSeconds(1.0f / fps);
        IsFixedTimeStep = true;
    }

    /// <summary>
    /// Called after each system has been initialized and the game is ready to run.
    /// </summary>
    protected virtual new void Initialize() { }

    protected sealed override void LoadContent() {
        DefaultTitle = AppDomain.CurrentDomain.FriendlyName;
        AddModule<PerformanceCounter>(counter => counter.DisplayInTitle = true);
        AddModule<Time>();

        // trigger module init
        InitializeModules();
        Initialize();
    }

    protected override void Update(GameTime gameTime) {
        var deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // update modules
        for (var i = 0; i < _modules.Count; i++) {
            var system = _modules[i];
            system.Update(this, deltaTime);
        }

        // update internals
        FrameworkDispatcher.Update();
    }

    protected override void Draw(GameTime gameTime) {
        // present modules
        for (var i = 0; i < _renderableModules.Count; i++) {
            var system = _renderableModules[i];
            system.Present(this);
        }
    }
}
