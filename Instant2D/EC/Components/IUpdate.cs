namespace Instant2D.EC {
    public interface IUpdate {
        bool IsActive { get; }

        Entity Entity { get; }

        /// <summary>
        /// Called each frame while <see cref="IsActive"/> is <see langword="true"/>.
        /// </summary>
        void Update(float dt);
    }
}
