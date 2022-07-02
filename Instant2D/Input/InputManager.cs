using Instant2D.Core;
using Instant2D.EC;
using Instant2D.Utils.ResolutionScaling;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.Input {
    /// <summary>
    /// Presents an easy way to check for input events, as well as, <i>eventually</i>, Virtual input.
    /// </summary>
    /// <remarks>
    /// NOTE: before accessing the static methods and properties there, make sure you add this system inside <see cref="InstantGame.SetupSystems"/>. 
    /// Otherwise, returned values will be nonsensical.
    /// </remarks>
    public class InputManager : SubSystem {
        public override void Initialize() {
            UpdateOrder = int.MinValue;
            IsUpdatable = true;

            // if SceneManager is there, enable mouse scaling relative to the scene
            _shouldScaleMouse = Game.TryGetSystem<SceneManager>(out _);
        }

        static KeyboardState _prevKeyState, _currentKeyState;
        static MouseState _prevMouseState, _currentMouseState;
        static Vector2 _rawMousePosition, _scaledMousePosition;
        static bool _shouldScaleMouse;

        /// <summary>
        /// Is true when the key was just pressed this frame.
        /// </summary>
        public static bool IsKeyPressed(Keys key) => _prevKeyState.IsKeyUp(key) && _currentKeyState.IsKeyDown(key);

        /// <summary>
        /// Is true for a frame after the key was released.
        /// </summary>
        public static bool IsKeyReleased(Keys key) => _prevKeyState.IsKeyDown(key) && !_currentKeyState.IsKeyDown(key);

        /// <summary>
        /// Is true when the key is held.
        /// </summary>
        public static bool IsKeyDown(Keys key) => _currentKeyState.IsKeyDown(key);

        /// <summary>
        /// Is true when the left mouse button was just pressed this frame.
        /// </summary>
        public static bool LeftMousePressed => _currentMouseState.LeftButton == ButtonState.Pressed && _prevMouseState.LeftButton == ButtonState.Released;

        /// <summary>
        /// Gets scaled mouse position relative to the current scene, or if <see cref="SceneManager"/> isn't added, <see cref="RawMousePosition"/>.
        /// </summary>
        public static Vector2 MousePosition => _shouldScaleMouse ? _scaledMousePosition : _rawMousePosition;

        /// <summary>
        /// Raw mouse position relative to the game window.
        /// </summary>
        public static Vector2 RawMousePosition => _rawMousePosition;

        public override void Update(GameTime time) {
            // move the states to previous
            _prevMouseState = _currentMouseState;
            _prevKeyState = _currentKeyState;

            // get current states
            _currentKeyState = Keyboard.GetState();
            _currentMouseState = Mouse.GetState();

            // record mouse state and optionally scale it to the scene
            _rawMousePosition = new(_currentMouseState.X, _currentMouseState.Y);
            if (_shouldScaleMouse) {
                var resolution = SceneManager.Instance.Current.Resolution;
                _scaledMousePosition = (_rawMousePosition - resolution.offset) / resolution.scaleFactor;
            }
        }
    }
}
