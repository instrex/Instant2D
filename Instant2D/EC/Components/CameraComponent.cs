using Instant2D.Graphics;
using Instant2D.Input;
using Instant2D.Utils;
using Instant2D.Utils.Math;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.EC {
    /// <summary>  </summary>
    public class CameraComponent : Component {
        Matrix2D _transformMatrix = Matrix2D.Identity, _inverseTransformMatrix = Matrix2D.Identity, _temp;
        RectangleF _bounds = RectangleF.Empty;
        Matrix _projectionMatrix;
        float _zoom = 1.0f;
        Vector2 _origin;

        // filth
        bool _matricesDirty = true, _boundsDirty = true, _projectionDirty = true;

        public RectangleF Bounds {
            get {
                CalculateMatrices();
                if (_boundsDirty) {
                    _boundsDirty = false;

                    var topLeft = ScreenToWorldPosition(Vector2.Zero);
                    var bottomRight = ScreenToWorldPosition(Scene.Resolution.renderTargetSize.ToVector2());

                    // if rotation is zero, no further action is needed
                    if (Transform.Rotation == 0) {
                        _bounds.Width = bottomRight.X - topLeft.X;
                        _bounds.Height = bottomRight.Y - topLeft.Y;
                        _bounds.Position = topLeft;

                        return _bounds;
                    }

                    var topRight = ScreenToWorldPosition(new Vector2(Scene.Resolution.renderTargetSize.X, 0));
                    var bottomLeft = ScreenToWorldPosition(new Vector2(0, Scene.Resolution.renderTargetSize.Y));
                    _bounds = RectangleF.FromCoordinates(topLeft, topRight, bottomLeft, bottomRight);
                }

                return _bounds;
            }
        }

        public float Zoom {
            get => _zoom;
            set {
                if (_zoom == value)
                    return;

                if (value <= 0) {
                    Logger.WriteLine($"Attempted to set zoom value of '{Entity.Name}' to a value less than zero ({value:F2}). Pls don't.", Logger.Severity.Warning);
                    return;
                }

                _matricesDirty = true;
                _zoom = value;
            }
        }

        #region Matrices

        /// <summary> Transform matrix that's used to convert world coordinates to screen. </summary>
        public Matrix2D TransformMatrix {
            get {
                if (_matricesDirty) {
                    CalculateMatrices();
                }

                return _transformMatrix;
            }
        }

        /// <summary> Transform matrix that's used to convert screen coordinates to world. </summary>
        public Matrix2D InverseTransformMatrix {
            get {
                if (_matricesDirty) {
                    CalculateMatrices();
                }

                return _inverseTransformMatrix;
            }
        }

        /// <summary> Camera projection matrix. </summary>
        public Matrix ProjectionMatrix {
            get {
                if (_projectionDirty) {
                    Matrix.CreateOrthographicOffCenter(0, Scene.Resolution.Width, Scene.Resolution.Height, 0, 0, -1, out _projectionMatrix);
                    _projectionDirty = false;
                }

                return _projectionMatrix;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void CalculateMatrices() {
            if (!_matricesDirty)
                return;

            _transformMatrix = Matrix2D.CreateTranslation(-Entity.Transform.Position.Floor() + _origin);

            // apply the zoom when needed
            if (_zoom != 1.0f) {
                Matrix2D.CreateScale(_zoom, out _temp);
                Matrix2D.Multiply(ref _transformMatrix, ref _temp, out _transformMatrix);
            }

            // apply the rotation if parent entity is rotated
            if (Entity.Transform.Rotation != 0) {
                Matrix2D.CreateRotation(Entity.Transform.Rotation, out _temp);
                Matrix2D.Multiply(ref _transformMatrix, ref _temp, out _transformMatrix);
            }

            // offset the matrix by origin
            Matrix2D.CreateTranslation(ref _origin, out _temp);
            Matrix2D.Multiply(ref _transformMatrix, ref _temp, out _transformMatrix);

            // invert the matrix as well
            Matrix2D.Invert(ref _transformMatrix, out _inverseTransformMatrix);

            // unset the filth
            _matricesDirty = false;
            _boundsDirty = true;
        }

        #endregion

        public Vector2 MouseToWorldPosition() => ScreenToWorldPosition(InputManager.MousePosition);

        public Vector2 ScreenToWorldPosition(Vector2 screenPosition) {
            CalculateMatrices();
            VectorUtils.Transform(ref screenPosition, ref _inverseTransformMatrix, out var result);

            return result;
        }

        public Vector2 WorldToScreenPosition(Vector2 worldPosition) {
            CalculateMatrices();
            VectorUtils.Transform(ref worldPosition, ref _transformMatrix, out var result);

            return result;
        }

        public override void OnTransformUpdated(TransformComponentType components) {
            _matricesDirty = true;
            _boundsDirty = true;
        }

        /// <summary> Forces the camera matrices and bounds to be rebuilt. </summary>
        public void ForceUpdate() {
            _matricesDirty = true;
        }

        // happens when the window is resized,
        // we should recalculate bounds/projection
        // and set the origin accordingly
        internal void OnClientSizeChanged() {
            _projectionDirty = true;
            _matricesDirty = true;
            _boundsDirty = true;

            var old = _origin;

            // calculate new origin and offset the Entity to compensate resize effect
            _origin = new(Scene.Resolution.renderTargetSize.X / 2, Scene.Resolution.renderTargetSize.Y / 2);
            Entity.Transform.LocalPosition += _origin - old;
        }
    }
}
