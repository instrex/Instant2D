namespace Instant2D.EC {
    public interface ILateUpdate {
        bool IsActive { get; }

        Entity Entity { get; }

        /// <summary>
        /// Called when all of the entities have been updated, just before rendering the scene.
        /// </summary>
        void LateUpdate(float dt);
    }
}
