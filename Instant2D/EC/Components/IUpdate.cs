namespace Instant2D.EC {
    public interface IUpdate {
        bool IsActive { get; }

        Entity Entity { get; }

        /// <summary>
        /// Called each frame while <see cref="IsActive"/> is <see langword="true"/>.
        /// </summary>
        void Update(float dt);
    }

    public interface IFixedUpdate {
        bool IsActive { get; }

        Entity Entity { get; }

        /// <summary>
        /// Called in a period configured by <see cref="Scene.FixedTimeStep"/> while <see cref="IsActive"/> is <see langword="true"/>. 
        /// </summary>
        void FixedUpdate();
    }

    public interface ILateUpdate {
        bool IsActive { get; }

        Entity Entity { get; }

        /// <summary>
        /// Called when all of the entities have been updated, just before rendering the scene.
        /// </summary>
        void LateUpdate(float dt);
    }
}
