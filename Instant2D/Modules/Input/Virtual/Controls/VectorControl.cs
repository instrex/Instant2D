using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace Instant2D.Input.Virtual;

public class VectorControl : IVirtualControl {
    public interface IVectorInput {
        Vector2 GetValue();
    }

    public readonly List<IVectorInput> Inputs = new(6);
    public Vector2 Value { get; private set; }

    public VectorControl AddInput(IVectorInput input) {
        Inputs.Add(input);
        return this;
    }

    public VectorControl AddGamepadStick(bool rightStick = false, Vector2? deadzones = default, bool invertHorizontally = false, bool invertVertically = false, int playerIndex = 0)
        => AddInput(new GamepadStick { 
            RightStick = rightStick,  
            Deadzones = deadzones ?? new Vector2(0.1f),
            InvertHorizontally = invertHorizontally,
            InvertVertically = invertVertically,
            PlayerIndex = playerIndex,
        });

    public VectorControl AddKeyboardKeys(Keys up, Keys left, Keys down, Keys right) =>
        AddInput(new KeyboardKeys { Up = up, Left = left, Down = down, Right = right });

    void IVirtualControl.Reset() {
        Value = default;
    }

    void IVirtualControl.Update(float dt) {
        Value = default;
        for (var i = 0; i < Inputs.Count; i++) {
            var input = Inputs[i].GetValue();
            if (input != Vector2.Zero) {
                Value = input;
                break;
            }
        }
    }

    public class GamepadStick : IVectorInput {
        public bool RightStick { get; set; }
        public Vector2 Deadzones { get; set; }
        public bool InvertHorizontally { get; set; }
        public bool InvertVertically { get; set; }
        public int PlayerIndex { get; set; }

        Vector2 IVectorInput.GetValue() {
            if (GamePadInput.Controllers[PlayerIndex] is not GamePadInput.GamePadWrapper controller)
                return Vector2.Zero;

            var value = RightStick ? controller.RightStick : controller.LeftStick;

            // apply deadzones
            if (MathF.Abs(value.X) < Deadzones.X) value.X = 0;
            if (MathF.Abs(value.Y) < Deadzones.Y) value.Y = 0;

            // apply inversions
            if (InvertHorizontally) value.X *= -1;
            if (InvertVertically) value.Y *= -1;

            return value;
        }
    }

    public class KeyboardKeys : IVectorInput {
        public Keys Up { get; set; }
        public Keys Left { get; set; }
        public Keys Down { get; set; }
        public Keys Right { get; set; }

        Vector2 IVectorInput.GetValue() {
            var value = Vector2.Zero;
            if (KeyboardInput.IsKeyDown(Left)) value.X -= 1;
            if (KeyboardInput.IsKeyDown(Right)) value.X += 1;
            if (KeyboardInput.IsKeyDown(Up)) value.Y -= 1;
            if (KeyboardInput.IsKeyDown(Down)) value.Y += 1;

            return value;
        }
    }
}
