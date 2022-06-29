namespace Instant2D.EC {
    public interface IUpdatableComponent {
        bool IsActive { get; }

        /// <summary>
        /// Called each frame while <see cref="IsActive"/> is <see langword="true"/>.
        /// </summary>
        void Update();
    }
}
