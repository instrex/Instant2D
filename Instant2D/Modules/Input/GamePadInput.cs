using Instant2D.Modules;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.Input;

public class GamePadInput : IGameSystem {
    public class GamePadWrapper {
        PlayerIndex _index;
        public GamePadWrapper(PlayerIndex index) {
            _index = index;
        }

        GamePadState _prevState, _currentState;
        internal void Update(PlayerIndex index) {
            _prevState = _currentState;
            _currentState = GamePad.GetState(index, GamePadDeadZone.IndependentAxes);
            if (!_currentState.IsConnected) {
                return;
            }
        }

        /// <inheritdoc cref="GamePadState.IsConnected"/>
        public bool IsConnected => _currentState.IsConnected;

        /// <summary>
        /// Checks if controller button is current down.
        /// </summary>
        public bool IsButtonDown(Buttons button) => _currentState.IsButtonDown(button);

        /// <summary>
        /// Checks if controller button was down previous frame, but was released.
        /// </summary>
        public bool IsButtonReleased(Buttons button) => _currentState.IsButtonUp(button) && _prevState.IsButtonDown(button);

        /// <summary>
        /// Checks if controller button was just pressed.
        /// </summary>
        public bool IsButtonPressed(Buttons button) => _currentState.IsButtonDown(button) && _prevState.IsButtonUp(button);

        /// <summary>
        /// Gets value of the left stick.
        /// </summary>
        public Vector2 LeftStick => _currentState.ThumbSticks.Left;

        /// <summary>
        /// Gets value of the right stick.
        /// </summary>
        public Vector2 RightStick => _currentState.ThumbSticks.Right;

        /// <summary>
        /// Gets left trigger value.
        /// </summary>
        public float LeftTrigger => _currentState.Triggers.Left;

        /// <summary>
        /// Gets right trigger value.
        /// </summary>
        public float RightTrigger => _currentState.Triggers.Right;

        /// <summary>
        /// Checks if controller supports gyro.
        /// </summary>
        public bool HasGyro => GamePad.GetCapabilities(_index).HasGyroEXT;

        /// <summary>
        /// Gets the value of gyro, if it's supported.
        /// </summary>
        public Vector3 Gyro {
            get {
                GamePad.GetGyroEXT(_index, out var gyro);
                return gyro;
            }
        }

        /// <summary>
        /// Checks if controller supports accelerometer.
        /// </summary>
        public bool HasAccelerometer => GamePad.GetCapabilities(_index).HasAccelerometerEXT;

        /// <summary>
        /// Gets the value of accelerometer, if it's supported.
        /// </summary>
        public Vector3 Accelerometer {
            get {
                GamePad.GetAccelerometerEXT(_index, out var accelerometer);
                return accelerometer;
            }
        }
    }

    const int MaxGamepads = 4;

    /// <summary>
    /// Contains extended data about each controller.
    /// </summary>
    public static readonly GamePadWrapper[] Controllers = new GamePadWrapper[MaxGamepads];

    /// <summary>
    /// Gets default gamepad instance. Use it if you don't care about multiple controllers, but make sure to <see langword="null"/> check it!
    /// </summary>
    public static GamePadWrapper Default { get; private set; }

    float IGameSystem.UpdateOrder => float.MinValue + 0.5f;
    void IGameSystem.Initialize(InstantApp app) { 
        for (var i = 0; i < MaxGamepads; i++) {
            Controllers[i] = new((PlayerIndex)i);
        }
    }

    void IGameSystem.Update(InstantApp app, float deltaTime) {
        Default = default;
        for (var i = 0; i < MaxGamepads; i++) {
            Controllers[i].Update((PlayerIndex)i);

            // register first connect gamepad as default
            if (Default is null && Controllers[i].IsConnected) {
                Default = Controllers[i];
            }
        }
    }
}
