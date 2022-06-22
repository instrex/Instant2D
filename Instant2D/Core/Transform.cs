using Instant2D.Utils;
using Instant2D.Utils.Math;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D {
    /// <summary>
    /// Represents position, scale and rotation as well as object hierarchy. 
    /// Thank you, Nez!
    /// </summary>
    public class Transform : IResettable {
        [Flags]
        public enum ComponentType {
            Clean = 0,
            Position = 1, 
            Scale = 2,
            Rotation = 4,
            All = Position | Scale | Rotation
        }

        Vector2 _localPosition, _position, _localScale = Vector2.One, _scale = Vector2.One;
        float _localRotation, _rotation;

        readonly List<Transform> _children = new();
        Transform _parent;

        Matrix2D _localTransform, _worldTransform = Matrix2D.Identity;
        Matrix2D _translationMatrix, _rotationMatrix, _scaleMatrix;
        Matrix2D _worldToLocalTransform = Matrix2D.Identity;
        ComponentType _matricesDirty, _localMatricesDirty;

        bool _worldToLocalDirty;
        public Matrix2D WorldToLocalTransform {
            get {
                if (_worldToLocalDirty) {
                    if (_parent == null) {
                        _worldToLocalTransform = Matrix2D.Identity;
                    } else {
                        _parent.CalculateTransform();
                        Matrix2D.Invert(ref _parent._worldTransform, out _worldToLocalTransform);
                    }

                    _worldToLocalDirty = false;
                }

                return _worldToLocalTransform;
            }
        }

        public Transform Parent {
            get => _parent;
            set {
                if (_parent == value)
                    return;

                _parent?._children.Remove(this);
                value?._children.Add(this);

                _parent = value;

                if (_parent != null)
                    Position = Vector2.Zero;

                MarkDirty(ComponentType.Position);
            }
        }

        public IReadOnlyList<Transform> Children => _children;

        #region Components

        public Vector2 Position {
            get {
                CalculateTransform();
                return _position;
            }
            
            set {
                if (_position == value)
                    return;

                _position = value;
                LocalPosition = _parent != null ? Vector2.Transform(_position, WorldToLocalTransform) : _position;
            }
        }

        public Vector2 LocalPosition {
            get {
                CalculateTransform();
                return _localPosition;
            }

            set {
                if (value == _localPosition)
                    return;

                _localPosition = value;
                _localMatricesDirty = ComponentType.All;
                MarkDirty(ComponentType.Position);
            }
        }

        public float Rotation {
            get {
                CalculateTransform();
                return _rotation;
            } 
            set {
                _rotation = value;
                LocalRotation = _parent != null ? _parent.Rotation + value : value;
            }
            
        }

        public float LocalRotation {
            get {
                CalculateTransform();
                return _localRotation;
            }

            set {
                _localRotation = value;
                _localMatricesDirty = ComponentType.All;
                MarkDirty(ComponentType.Rotation);
            }
        }

        public Vector2 Scale {
            get {
                CalculateTransform();
                return _scale;
            }
            set {
                if (_scale == value)
                    return;

                _scale = value;
                LocalScale = _parent != null ? _scale / _parent._scale : _scale;
            }
        }
        
        public Vector2 LocalScale {
            get {
                CalculateTransform();
                return _localScale;
            }
            set {
                _localScale = value;
                _localMatricesDirty = ComponentType.All;
                MarkDirty(ComponentType.Scale);
            }
        }

        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void CalculateTransform() {
            if (_matricesDirty == ComponentType.Clean)
                return;

            _parent?.CalculateTransform();
            if (_localMatricesDirty != ComponentType.Clean) {
                if ((_localMatricesDirty & ComponentType.Position) != 0) {
                    Matrix2D.CreateTranslation(_localPosition.X, _localPosition.Y, out _translationMatrix);
                    _localMatricesDirty &= ~ComponentType.Position;
                }

                if ((_localMatricesDirty & ComponentType.Rotation) != 0) {
                    Matrix2D.CreateRotation(_localRotation, out _rotationMatrix);
                    _localMatricesDirty &= ~ComponentType.Rotation;
                }

                if ((_localMatricesDirty & ComponentType.Scale) != 0) {
                    Matrix2D.CreateScale(_localScale.X, _localScale.Y, out _scaleMatrix);
                    _localMatricesDirty &= ~ComponentType.Scale;
                }

                Matrix2D.Multiply(ref _scaleMatrix, ref _rotationMatrix, out _localTransform);
                Matrix2D.Multiply(ref _localTransform, ref _translationMatrix, out _localTransform);

                if (_parent == null) {
                    _worldTransform = _localTransform;
                    _rotation = _localRotation;
                    _scale = _localScale;
                }
            }

            if (_parent != null) {
                Matrix2D.Multiply(ref _localTransform, ref _parent._worldTransform, out _worldTransform);
                _rotation = _localRotation + _parent.Rotation;
                _scale = _parent._scale * _localScale;

                _parent.CalculateTransform();
                ApplyTransform(ref _localPosition, ref _parent._worldTransform, out _position);
            } else {
                _position = _localPosition;
            }

            _worldToLocalDirty = true;
            _matricesDirty = ComponentType.Clean;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void ApplyTransform(ref Vector2 position, ref Matrix2D matrix, out Vector2 result) {
            var x = (position.X * matrix.M11) + (position.Y * matrix.M21) + matrix.M31;
            var y = (position.X * matrix.M12) + (position.Y * matrix.M22) + matrix.M32;
            result.X = x;
            result.Y = y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void MarkDirty(ComponentType flags = ComponentType.All) {
            _matricesDirty |= flags;
            for (var i = 0; i < _children.Count; i++) {
                _children[i].MarkDirty(flags);
            }
        }

        public void Reset() {
            _localPosition = default;
            _position = default;
            _scale = Vector2.One;
            _rotation = 0;
            _children.Clear();
            _parent = null;
        }
    }
}
