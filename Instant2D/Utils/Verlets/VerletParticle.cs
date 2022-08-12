using Microsoft.Xna.Framework;

namespace Instant2D.Verlets {
    public record VerletParticle {
        /// <summary>
        /// Position of this particle in world space.
        /// </summary>
        public Vector2 Position;

        /// <summary>
        /// Position of this particle before the latest update.
        /// </summary>
        public Vector2 OldPosition;

        /// <summary>
        /// Acceleration of this particle.
        /// </summary>
        public Vector2 Velocity;

        /// <summary>
        /// Mass of this particle, used by constraints for calculating force.
        /// </summary>
        public float Mass = 1.0f;

        /// <summary>
        /// If not <see langword="null"/>, pinned position of this particle.
        /// </summary>
        public Vector2? Pin;

        public VerletParticle(Vector2 position = default, bool shouldPin = false, float mass = 1.0f) {
            Position = position;
            Mass = mass;
            if (shouldPin) {
                Pin = position;
            }
        }
    }
}
