using Instant2D.EC;
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
    public interface ITransformCallbacksHandler {
        void OnTransformUpdated(TransformComponentType components);
    }

    public record struct TransformData(Vector2 Position, Vector2 Scale, float Rotation);

    [Flags]
    public enum TransformComponentType {
        None = 0,
        Position = 1,
        Scale = 2,
        Rotation = 4,
        All = Position | Scale | Rotation
    }

    /// <summary> 
    /// Represents position, scale and rotation as well as object hierarchy. <br/> 
    /// <typeparamref name="T"/> can optionally implement <see cref="ITransformCallbacksHandler"/> to receive transform events.
    /// </summary>
    public class Transform<T> : IPooled {
        /// <summary>
        /// An entity that will receive <see cref="Component.OnTransformUpdated(TransformComponentType)"/> events.
        /// </summary>
        public T Entity;

        Vector2 _localPosition, _position, _localScale = Vector2.One, _scale = Vector2.One;
        float _localRotation, _rotation;

        List<Transform<T>> _children;
        Transform<T> _parent;

        Matrix2D _localTransform, _worldTransform = Matrix2D.Identity;
        Matrix2D _translationMatrix, _rotationMatrix, _scaleMatrix;
        Matrix2D _worldToLocalTransform = Matrix2D.Identity;
        TransformComponentType _matricesDirty, _localMatricesDirty;

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

        #region Children

        /// <summary>
        /// Parent of this instance, in case you're working with entities set children/parents through entity methods, not this one!
        /// </summary>
        public Transform<T> Parent {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _parent;
            set {
                // stop if nothing changes
                if (_parent == value)
                    return;

                // update transform buffers
                _parent?._children?.Remove(this);
                value?.AddChild(this);

                // assign new parent and reset the position
                if ((_parent = value) != null)
                    Position = Vector2.Zero;

                MarkDirty(TransformComponentType.Position);
            }
        }

        /// <summary> Count of all the children this transform houses. </summary>
        public int ChildrenCount {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _children?.Count ?? 0;
        } 

        /// <summary> Gets an indexed child transform. Use <see cref="ChildrenCount"/> to determine the count of children and possibly enumerate over this. </summary>
        public Transform<T> this[int index] {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get {
                // no children?
                if (_children == null)
                    return null;

                // bruh moment
                if (index < 0 || index >= _children.Count)
                    return null;

                return _children[index];
            }
        }

        // add a child and lazily initialize the list
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void AddChild(Transform<T> transform) {
            if (_children == null)
                _children = new(8);

            _children.Add(transform);
        }

        #endregion

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
                _localMatricesDirty = TransformComponentType.All;
                MarkDirty(TransformComponentType.Position);
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
                _localMatricesDirty = TransformComponentType.All;
                MarkDirty(TransformComponentType.Rotation);
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
                _localMatricesDirty = TransformComponentType.All;
                MarkDirty(TransformComponentType.Scale);
            }
        }

        #endregion

        /// <summary>
        /// Quick retrieval of transform components such as Position, Scale and Rotation.
        /// </summary>
        public TransformData Data {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(Position, Scale, Rotation);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => (Position, Scale, Rotation) = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void CalculateTransform() {
            if (_matricesDirty == TransformComponentType.None)
                return;

            _parent?.CalculateTransform();
            if (_localMatricesDirty != TransformComponentType.None) {
                if ((_localMatricesDirty & TransformComponentType.Position) != 0) {
                    Matrix2D.CreateTranslation(_localPosition.X, _localPosition.Y, out _translationMatrix);
                    _localMatricesDirty &= ~TransformComponentType.Position;
                }

                if ((_localMatricesDirty & TransformComponentType.Rotation) != 0) {
                    Matrix2D.CreateRotation(_localRotation, out _rotationMatrix);
                    _localMatricesDirty &= ~TransformComponentType.Rotation;
                }

                if ((_localMatricesDirty & TransformComponentType.Scale) != 0) {
                    Matrix2D.CreateScale(_localScale.X, _localScale.Y, out _scaleMatrix);
                    _localMatricesDirty &= ~TransformComponentType.Scale;
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
            _matricesDirty = TransformComponentType.None;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void ApplyTransform(ref Vector2 position, ref Matrix2D matrix, out Vector2 result) {
            var x = (position.X * matrix.M11) + (position.Y * matrix.M21) + matrix.M31;
            var y = (position.X * matrix.M12) + (position.Y * matrix.M22) + matrix.M32;
            result.X = x;
            result.Y = y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void MarkDirty(TransformComponentType flags = TransformComponentType.All) {
            _matricesDirty |= flags;

            // call the transform callback if entity is there
            if (Entity is ITransformCallbacksHandler handler)
                handler.OnTransformUpdated(_matricesDirty);

            // pass the dirt onto our children
            if (_children != null)
                for (var i = 0; i < _children.Count; i++) {
                    _children[i].MarkDirty(flags);
                }
        }

        public void Reset() {
            Entity = default;
            _localPosition = default;
            _position = default;
            _scale = Vector2.One;
            _localScale = Vector2.One;
            _localRotation = 0;
            _rotation = 0;
            _children?.Clear();
            _parent = null;
            _matricesDirty = TransformComponentType.All;
            _localMatricesDirty = TransformComponentType.All;
        }
    }
}
