namespace Instant2D.EC {
    public interface IFixedUpdate {
        bool IsActive { get; }

        Entity Entity { get; }

        /// <summary>
        /// Called in a period configured by <see cref="Scene.FixedTimeStep"/> while <see cref="IsActive"/> is <see langword="true"/>. 
        /// </summary>
        void FixedUpdate();
    }
}
