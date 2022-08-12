using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace Instant2D.Verlets {
    /// <summary>
    /// Represents a verlet object constructed from <see cref="VerletParticle"/>s and <see cref="IVerletConstraint"/>s between them.
    /// </summary>
    public class VerletObject {
        /// <summary>
        /// List of particles this object has.
        /// </summary>
        public List<VerletParticle> Particles = new();

        public List<IVerletConstraint> Constraints = new();

        public Vector2 Friction = new(0.9f);

        public void Update(Vector2 passiveMovement = default) {
            // solve the constraints first
            for (var k = 0; k < Constraints.Count; k++) {
                Constraints[k].Solve();
            }

            for (var i = 0; i < Particles.Count; i++) {
                var particle = Particles[i];

                // we don't process pinned particles
                if (particle.Pin is Vector2 pinnedPosition) {
                    particle.Position = pinnedPosition;
                    continue;
                }

                particle.Velocity += passiveMovement;

                // calculate velocity based on previous position
                var velocity = (particle.Position - particle.OldPosition) * Friction;

                // apply Verlet Integration
                var updatedPosition = particle.Position + velocity + particle.Velocity * 0.5f;

                // reset the variables
                particle.OldPosition = particle.Position;
                particle.Position = updatedPosition;
                particle.Velocity = Vector2.Zero;
            }
        }
    }
}
