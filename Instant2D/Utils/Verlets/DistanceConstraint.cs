using Microsoft.Xna.Framework;

namespace Instant2D.Verlets {
    public enum DistanceConstraintType {
        /// <summary>
        /// The constraint will always attempt to maintain specified distance.
        /// </summary>
        Fixed,

        /// <summary>
        /// The constraint will attempt to maintain the specified distance only if other particle is closer than distance.
        /// </summary>
        PushAway,
    }

    public record struct DistanceConstraint : IVerletConstraint {
        /// <summary>
        /// Component of this constraint.
        /// </summary>
        public VerletParticle A, B;

        /// <summary>
        /// The resting distance between components.
        /// </summary>
        public float Distance;

        /// <summary>
        /// Controls the springyness/rigidness of this constraint.
        /// </summary>
        public float Stiffness;

        /// <summary>
        /// The way this constraint should be solved.
        /// </summary>
        public DistanceConstraintType Type;

        public DistanceConstraint(VerletParticle a, VerletParticle b, float distance = float.NaN, float stiffness = 1.0f, DistanceConstraintType type = DistanceConstraintType.Fixed) {
            A = a;
            B = b;
            Stiffness = stiffness;
            Type = type;

            // automatically assign distance if no value is passed
            if (float.IsNaN(distance)) {
                distance = Vector2.Distance(a.Position, b.Position);
            }

            Distance = distance;
        }

        public void Solve() {
            var dir = A.Position - B.Position;
            var dist = dir.Length();

            // find the distance ratio + L
            var diff = (Distance - dist) / dist;

            // don't solve the constraint if its set to just push away
            if (diff < 0 && Type == DistanceConstraintType.PushAway)
                return;

            // inverse the mass quantities
            var invMassA = 1f / A.Mass;
            var invMassB = 1f / B.Mass;
            var scalarP1 = (invMassA / (invMassA + invMassB)) * Stiffness;
            var scalarP2 = Stiffness - scalarP1;

            // push/pull vertices
            A.Position += dir * scalarP1 * diff;
            B.Position -= dir * scalarP2 * diff;
        }
    }
}
