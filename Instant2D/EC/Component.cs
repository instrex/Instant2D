using Instant2D.Utils;
using Instant2D.Utils.Coroutines;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Instant2D.EC {
    public abstract class Component : ICoroutineTarget {
        /// <summary>
        /// Entity this component is attached to.
        /// </summary>
        public Entity Entity {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get; internal set;
        }

        /// <summary>
        /// Attempts to grab the transform of <see cref="Entity"/> attached.
        /// </summary>
        public Transform<Entity> Transform {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Entity?.Transform;
        }

        /// <summary>
        /// Attempts to grab the scene of <see cref="Entity"/> attached.
        /// </summary>
        public Scene Scene {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Entity?.Scene;
        }

        internal bool _isActive = true;

        /// <summary>
        /// While true, <see cref="IUpdatableComponent.Update"/> will be run.
        /// </summary>
        public bool IsActive {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _isActive;
            set {
                // call appropriate callbacks
                if (_isActive = value)
                    OnEnabled();

                else OnDisabled();
            }
        }

        #region Component Lifecycle

        /// <summary>
        /// Called when the Component is added to Entity via <see cref="Entity.AddComponent{T}"/>.
        /// </summary>
        public virtual void Initialize() { }

        /// <summary>
        /// Called when this Component is detached from entity. Specifically called in following scenarios:
        /// <list type="bullet">
        /// <item> After <see cref="Entity.RemoveComponent{T}"/> is called. </item>
        /// <item> After <see cref="Entity"/> is destroyed via <see cref="Entity.Destroy"/>. </item>
        /// </list>
        /// You can still access <see cref="Entity"/> in there, but keep in mind it'll be gone the second this method ends.
        /// </summary> 
        public virtual void OnRemovedFromEntity() { }

        /// <summary>
        /// Called after <see cref="IsActive"/> set to <see langword="true"/>.
        /// </summary>
        public virtual void OnEnabled() { }

        /// <summary>
        /// Called after <see cref="IsActive"/> set to <see langword="false"/>.
        /// </summary>
        public virtual void OnDisabled() { }

        /// <summary>
        /// Is called each time <see cref="Entity.Transform"/> is updated.
        /// </summary>
        public virtual void OnTransformUpdated(TransformComponentType components) { }

        #endregion

        /// <inheritdoc cref="Entity.AddComponent{T}"/>
        public T AddComponent<T>() where T : Component, new() => Entity.AddComponent<T>();

        /// <inheritdoc cref="Entity.AddComponent{T}(T)"/>
        public T AddComponent<T>(T component) where T : Component => Entity.AddComponent(component);

        // ICoroutineTarget impl
        float ICoroutineTarget.TimeScale => Entity.TimeScale;
    }
}
