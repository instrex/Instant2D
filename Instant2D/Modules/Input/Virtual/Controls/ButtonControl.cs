using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;

namespace Instant2D.Input.Virtual;

public class ButtonControl : IVirtualControl {
    public interface IButtonInput {
        bool IsPressed();
        bool IsReleased();
        bool IsDown();
    }

    public readonly List<IButtonInput> Inputs = new(6);
    float _bufferDuration, _bufferTimer;

    // cached values
    bool _isPressed, _isReleased, _isDown;

    public bool IsDown => _isDown;
    public bool IsReleased => _isReleased;
    public bool IsPressed => _isPressed;

    /// <summary>
    /// Allows you to set up input buffering. <see cref="IsPressed"/> will remain <see langword="true"/> for a short duration after button has been released. <br/>
    /// This allows to make some events requiring precise timing a bit more forgiving. For example, allowing player to press jump before touching the ground.
    /// </summary>
    public ButtonControl SetBufferDuration(float duration) {
        _bufferDuration = duration;
        return this;
    }

    public ButtonControl AddInput(IButtonInput input) {
        Inputs.Add(input);
        return this;
    }

    public ButtonControl AddKeyboardKey(Keys key) => AddInput(new KeyboardKey { Key = key });
    public ButtonControl AddGamepadButton(Buttons button, int playerIndex = 0) => AddInput(new GamepadButton { Button = button, PlayerIndex = playerIndex });
    public ButtonControl AddMouseButton(MouseInput.Button button) => AddInput(new MouseButton { Button = button });

    void IVirtualControl.Reset() {
        _isPressed = _isReleased = _isDown = false;
        _bufferTimer = 0;
    }

    void IVirtualControl.Update(float dt) {
        _isReleased = _isDown = false;
        _isPressed = _bufferTimer > 0;
        if (_bufferTimer > 0) {
            _bufferTimer -= dt;
        }

        for (var i = 0; i < Inputs.Count; i++) {
            var input = Inputs[i];
            if (!_isReleased && input.IsReleased())
                _isReleased = true;

            if (!_isDown && input.IsDown())
                _isDown = true;

            if (!_isPressed && input.IsPressed()) {
                _isPressed = true;
                if (_bufferDuration > 0) {
                    // buffer the pressed event
                    _bufferTimer = _bufferDuration;
                }
            }
        }
    }

    public class KeyboardKey : IButtonInput {
        public Keys Key { get; set; }

        bool IButtonInput.IsReleased() => KeyboardInput.IsKeyReleased(Key);
        bool IButtonInput.IsPressed() => KeyboardInput.IsKeyPressed(Key);
        bool IButtonInput.IsDown() => KeyboardInput.IsKeyDown(Key);
    }

    public class GamepadButton : IButtonInput {
        public Buttons Button { get; set; }
        public int PlayerIndex { get; set; }

        bool IButtonInput.IsDown() => GamePadInput.Controllers[PlayerIndex].IsButtonDown(Button);
        bool IButtonInput.IsPressed() => GamePadInput.Controllers[PlayerIndex].IsButtonPressed(Button);
        bool IButtonInput.IsReleased() => GamePadInput.Controllers[PlayerIndex].IsButtonReleased(Button);
    }

    public class MouseButton : IButtonInput {
        public MouseInput.Button Button { get; set; }

        bool IButtonInput.IsReleased() => MouseInput.IsReleased(Button);
        bool IButtonInput.IsPressed() => MouseInput.IsPressed(Button);
        bool IButtonInput.IsDown() => MouseInput.IsDown(Button);
    }
}
