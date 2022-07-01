using Instant2D.Graphics;
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
    public class CameraComponent : Component, ICamera {
        Matrix2D _transformMatrix = Matrix2D.Identity, _inverseTransformMatrix = Matrix2D.Identity;
        RectangleF _bounds = RectangleF.Empty;
        Matrix _projectionMatrix;
        Vector2 _origin;

        // filth
        bool _matricesDirty = true, _boundsDirty = true, _projectionDirty = true;

        public RectangleF Bounds {
            get {
                CalculateMatrices();
                if (_boundsDirty) {
                    var topLeft = ScreenToWorldPosition(Vector2.Zero);
                    var bottomRight = ScreenToWorldPosition(Scene.Resolution.renderTargetSize.ToVector2() * Scene.Resolution.scaleFactor);

                    // TODO: handle rotations
                    _bounds.Width = bottomRight.X - topLeft.X;
                    _bounds.Width = bottomRight.Y - topLeft.Y;
                    _bounds.Position = topLeft;

                    _boundsDirty = false;
                }

                return _bounds;
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

            Matrix2D temp;

            _transformMatrix = Matrix2D.CreateTranslation(-Entity.Transform.Position.Floor());

            // TODO: add camera zoom

            // apply the rotation if parent entity is rotated
            if (Entity.Transform.Rotation != 0) {
                Matrix2D.CreateRotation(Entity.Transform.Rotation, out temp);
                Matrix2D.Multiply(ref _transformMatrix, ref temp, out _transformMatrix);
            }

            // offset the matrix by origin
            Matrix2D.CreateTranslation(ref _origin, out temp);
            Matrix2D.Multiply(ref _transformMatrix, ref temp, out _transformMatrix);

            // inverse the matrix as well
            Matrix2D.Invert(ref _transformMatrix, out _inverseTransformMatrix);

            // unset the filth
            _matricesDirty = false;
            _boundsDirty = true;
        }

        #endregion

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

        public override void OnTransformUpdated(Transform.ComponentType components) {
            _matricesDirty = true;
            _boundsDirty = true;
        }

        public override void Initialize() {
            _origin = new(Scene.Resolution.renderTargetSize.X / 2, Scene.Resolution.renderTargetSize.Y / 2);
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
