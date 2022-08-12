namespace Instant2D.Verlets {
    /// <summary>
    /// Represents a Verlet constraint applied between instances of <see cref="VerletParticle"/>.
    /// </summary>
    public interface IVerletConstraint {
        /// <summary>
        /// Perform needed actions to solve this constraint.
        /// </summary>
        void Solve();
    }
}
