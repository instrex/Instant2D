using Instant2D.Core;
using Instant2D.EC;
using Instant2D.Utils.ResolutionScaling;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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

        static bool _isFocused;
        static KeyboardState _prevKeyState, _currentKeyState;
        static MouseState _prevMouseState, _currentMouseState;
        static Vector2 _rawMousePosition, _scaledMousePosition;
        static float _mouseWheelDelta;
        static bool _shouldScaleMouse;

        /// <summary> Is true when the key was just pressed this frame. </summary>
        public static bool IsKeyPressed(Keys key) => _isFocused && _prevKeyState.IsKeyUp(key) && _currentKeyState.IsKeyDown(key);

        /// <summary> Is true for a frame after the key was released. </summary>
        public static bool IsKeyReleased(Keys key) => _isFocused && _prevKeyState.IsKeyDown(key) && !_currentKeyState.IsKeyDown(key);

        /// <summary> Is true when the key is held. </summary>
        public static bool IsKeyDown(Keys key) => _isFocused && _currentKeyState.IsKeyDown(key);

        #region Mouse Helpers

        /// <summary> Gets scaled mouse position relative to the current scene, or if <see cref="SceneManager"/> isn't added, <see cref="RawMousePosition"/>. </summary>
        public static Vector2 MousePosition => _scaledMousePosition;

        /// <summary> Raw mouse position relative to the game window. </summary>
        public static Vector2 RawMousePosition => _rawMousePosition;

        /// <summary> Is true when the left mouse button was pressed this frame. </summary>
        public static bool LeftMousePressed => _isFocused && _currentMouseState.LeftButton == ButtonState.Pressed && _prevMouseState.LeftButton == ButtonState.Released;

        /// <summary> Is true when the left mouse button was released this frame. </summary>
        public static bool LeftMouseReleased => _isFocused && _currentMouseState.LeftButton == ButtonState.Released && _prevMouseState.LeftButton == ButtonState.Pressed;

        /// <summary> Is true when the left mouse button is down. </summary>
        public static bool LeftMouseDown => _isFocused && _currentMouseState.LeftButton == ButtonState.Pressed;

        /// <summary> Is true when the right mouse button was pressed this frame. </summary>
        public static bool RightMousePressed => _isFocused && _currentMouseState.RightButton == ButtonState.Pressed && _prevMouseState.RightButton == ButtonState.Released;

        /// <summary> Is true when the right mouse button was released this frame. </summary>
        public static bool RightMouseReleased => _isFocused && _currentMouseState.RightButton == ButtonState.Released && _prevMouseState.RightButton == ButtonState.Pressed;

        /// <summary> Is true when the right mouse button is down. </summary>
        public static bool RightMouseDown => _isFocused && _currentMouseState.RightButton == ButtonState.Pressed;
        
        /// <summary> Is true when the middle mouse button was pressed this frame. </summary>
        public static bool MiddleMousePressed => _isFocused && _currentMouseState.MiddleButton == ButtonState.Pressed && _prevMouseState.MiddleButton == ButtonState.Released;

        /// <summary> Is true when the middle mouse button was released this frame. </summary>
        public static bool MiddleMouseReleased => _isFocused && _currentMouseState.MiddleButton == ButtonState.Released && _prevMouseState.MiddleButton == ButtonState.Pressed;

        /// <summary> Is true when the middle mouse button is down. </summary>
        public static bool MiddleMouseDown => _isFocused && _currentMouseState.MiddleButton == ButtonState.Pressed;

        /// <summary> How much the mouse wheel has moved during this frame. </summary>
        public static float MouseWheelDelta => _mouseWheelDelta;

        #endregion

        /// <summary>
        /// Handles mouse and keyboard states, as well as position scaling and other stuff. 
        /// Abstracted away so you can manually call it instead of <see cref="Update(GameTime)"/> when required.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StaticUpdate(GameTime time) {
            _isFocused = InstantGame.Instance.IsActive;

            // move the states to previous
            _prevMouseState = _currentMouseState;
            _prevKeyState = _currentKeyState;

            // get current states
            _currentKeyState = Keyboard.GetState();
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

        public override void Update(GameTime time) => StaticUpdate(time);
    }
}
