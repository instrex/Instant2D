using Instant2D.Modules;
using Microsoft.Xna.Framework.Input;

namespace Instant2D.Input;

/// <summary>
/// Provides an easy interface over keyboard states.
/// </summary>
public class KeyboardInput : IGameSystem {
    static KeyboardState _prevKeyState, _currentKeyState;

    /// <summary> Is true when the key was just pressed this frame. </summary>
    public static bool IsKeyPressed(Keys key) => InstantApp.Instance.IsActive && _prevKeyState.IsKeyUp(key) && _currentKeyState.IsKeyDown(key);

    /// <summary> Is true for a frame after the key was released. </summary>
    public static bool IsKeyReleased(Keys key) => InstantApp.Instance.IsActive && _prevKeyState.IsKeyDown(key) && !_currentKeyState.IsKeyDown(key);

    /// <summary> Is true when the key is held. </summary>
    public static bool IsKeyDown(Keys key) => InstantApp.Instance.IsActive && _currentKeyState.IsKeyDown(key);

    float IGameSystem.UpdateOrder => float.MinValue + 0.5f;
    void IGameSystem.Initialize(InstantApp app) { }
    void IGameSystem.Update(InstantApp app, float deltaTime) {
        _prevKeyState = _currentKeyState;
        _currentKeyState = Keyboard.GetState();
    }
}
