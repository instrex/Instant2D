using Instant2D.Modules;
using System.Collections.Generic;
using System.Linq;

namespace Instant2D.Input.Virtual;

/// <summary>
/// Advanced input processing system, which allows to have multiple control schemes, keyboard/mouse and gamepad support at the same time. <br/>
/// Create a class implementing <see cref="IControlScheme"/> and call <see cref="SetScheme{T}"/> to enable it.
/// </summary>
public class ControlManager : IGameSystem {
    static IVirtualControl[] _controls;

    /// <summary>
    /// Currently active Control scheme.
    /// </summary>
    public static IControlScheme ActiveScheme { get; private set; }

    /// <summary>
    /// Swaps control schemes, resetting previous (when present).
    /// </summary>
    public static void SetScheme<T>() where T: IControlScheme, new() {
        if (_controls != null) {
            // reset previous controls so they don't give false inputs
            for (var i = 0; i < _controls.Length; i++) {
                _controls[i].Reset();
            }
        }

        ActiveScheme = new T();
        _controls = ActiveScheme
            .GetControls()
            .ToArray();
    }

    float IGameSystem.UpdateOrder => float.MinValue + 1.0f;
    void IGameSystem.Initialize(InstantApp app) { }
    void IGameSystem.Update(InstantApp app, float deltaTime) {
        if (_controls is null)
            return;

        for (var i = 0; i < _controls.Length; i++) {
            _controls[i].Update(deltaTime);
        }
    }
}