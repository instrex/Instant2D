using Instant2D.EC;
using Instant2D.Modules;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Instant2D.Input;

/// <summary>
/// Provides an easy interface over mouse states.
/// </summary>
public class MouseInput : IGameSystem {
    public enum Button {
        Left,
        Middle,
        Right
    }

    static MouseState _prevMouseState, _currentMouseState;
    static Vector2 _rawMousePosition, _scaledMousePosition;
    static float _mouseWheelDelta;
    static bool _shouldScaleMouse;

    /// <summary> Gets scaled mouse position relative to the current scene. If <see cref="SceneManager"/> isn't added, returns <see cref="RawPosition"/> instead. </summary>
    public static Vector2 Position => _scaledMousePosition;

    /// <summary> Raw mouse position relative to the game window. </summary>
    public static Vector2 RawPosition => _rawMousePosition;

    /// <summary> Is true when the left mouse button was pressed this frame. </summary>
    public static bool LeftPressed => InstantApp.Instance.IsActive && _currentMouseState.LeftButton == ButtonState.Pressed && _prevMouseState.LeftButton == ButtonState.Released;

    /// <summary> Is true when the left mouse button was released this frame. </summary>
    public static bool LeftReleased => InstantApp.Instance.IsActive && _currentMouseState.LeftButton == ButtonState.Released && _prevMouseState.LeftButton == ButtonState.Pressed;

    /// <summary> Is true when the left mouse button is down. </summary>
    public static bool LeftDown => InstantApp.Instance.IsActive && _currentMouseState.LeftButton == ButtonState.Pressed;

    /// <summary> Is true when the right mouse button was pressed this frame. </summary>
    public static bool RightPressed => InstantApp.Instance.IsActive && _currentMouseState.RightButton == ButtonState.Pressed && _prevMouseState.RightButton == ButtonState.Released;

    /// <summary> Is true when the right mouse button was released this frame. </summary>
    public static bool RightReleased => InstantApp.Instance.IsActive && _currentMouseState.RightButton == ButtonState.Released && _prevMouseState.RightButton == ButtonState.Pressed;

    /// <summary> Is true when the right mouse button is down. </summary>
    public static bool RightDown => InstantApp.Instance.IsActive && _currentMouseState.RightButton == ButtonState.Pressed;

    /// <summary> Is true when the middle mouse button was pressed this frame. </summary>
    public static bool MiddlePressed => InstantApp.Instance.IsActive && _currentMouseState.MiddleButton == ButtonState.Pressed && _prevMouseState.MiddleButton == ButtonState.Released;

    /// <summary> Is true when the middle mouse button was released this frame. </summary>
    public static bool MiddleReleased => InstantApp.Instance.IsActive && _currentMouseState.MiddleButton == ButtonState.Released && _prevMouseState.MiddleButton == ButtonState.Pressed;

    /// <summary> Is true when the middle mouse button is down. </summary>
    public static bool MiddleDown => InstantApp.Instance.IsActive && _currentMouseState.MiddleButton == ButtonState.Pressed;

    /// <summary> How much the mouse wheel has moved during this frame. </summary>
    public static float WheelDelta => _mouseWheelDelta;

    /// <summary>
    /// Checks if provided mouse button is pressed.
    /// </summary>
    public static bool IsPressed(Button button) => 
        button switch {
            Button.Left => LeftPressed,
            Button.Right => RightPressed,
            Button.Middle => MiddlePressed,
            _ => false
        };

    /// <summary>
    /// Checks if provided mouse button is down.
    /// </summary>
    public static bool IsDown(Button button) =>
        button switch {
            Button.Left => LeftDown,
            Button.Right => RightDown,
            Button.Middle => MiddleDown,
            _ => false
        };

    /// <summary>
    /// Checks if provided mouse button is down.
    /// </summary>
    public static bool IsReleased(Button button) =>
        button switch {
            Button.Left => LeftReleased,
            Button.Right => RightReleased,
            Button.Middle => MiddleReleased,
            _ => false
        };

    float IGameSystem.UpdateOrder => float.MinValue + 0.5f;
    void IGameSystem.Initialize(InstantApp app) {
        // scale mouse position only when scene manager is present (almost always lol)
        _shouldScaleMouse = app.GetModule<SceneManager>() is not null;
    }

    void IGameSystem.Update(InstantApp app, float deltaTime) {
        // move the states to previous
        _prevMouseState = _currentMouseState;

        // get current states
        _currentMouseState = Mouse.GetState();

        // record mouse state
        _rawMousePosition = new(_currentMouseState.X, _currentMouseState.Y);
        _scaledMousePosition = _rawMousePosition;

        // calculate mouse wheel delta
        _mouseWheelDelta = _prevMouseState.ScrollWheelValue - _currentMouseState.ScrollWheelValue;

        // optionally scale it to the scene
        if (_shouldScaleMouse && SceneManager.Instance.Current is Scene currentScene) {
            var resolution = currentScene.Resolution;
            _scaledMousePosition = (_rawMousePosition - resolution.offset) / resolution.scaleFactor;
        }
    }
}
