using Instant2D.EC;
using Instant2D.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.TestGame.Scenes {
    /// <summary>
    /// Represents a Verlet constraint applied between instances of <see cref="VerletParticle"/>.
    /// </summary>
    public interface IVerletConstraint {
        /// <summary>
        /// Perform needed actions to solve this constraint.
        /// </summary>
        void Solve();
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

        public void Solve() {
            var dir = A.Position - B.Position;
            var dist = dir.Length();

            // find the distance ratio + L
            var diff = (Distance - dist) / dist;

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
    }

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

    public class VerletTest : Scene {
        class ChainRenderer : RenderableComponent, IUpdatableComponent {
            public VerletObject Verlet;
            public Vector2 PassiveMovement;

            public void Update() {
                Verlet.Update(PassiveMovement);
            }

            protected override void RecalculateBounds(ref RectangleF bounds) => bounds = Camera.Bounds;

            public override void Draw(IDrawingBackend drawing, CameraComponent camera) {
                for (int i = 1; i < Verlet.Particles.Count; i++) {
                    //VerletParticle particle = Verlet.Particles[i];
                    //drawing.DrawPoint(particle.Position, Color, 8);

                    drawing.DrawLine(Verlet.Particles[i - 1].Position, Verlet.Particles[i].Position, Color.White, 4);
                }
            }
        }

        public VerletObject Verlet;

        public override void Initialize() {
            Verlet = new VerletObject {
                Particles = {
                    new VerletParticle { Pin = Vector2.Zero }
                }
            };

            for (var i = 1; i < 24; i++) {
                var particle = new VerletParticle() { Position = new Vector2(i * 50, 0) };
                Verlet.Particles.Add(particle);

                Verlet.Constraints.Add(new DistanceConstraint {
                    A = Verlet.Particles[i - 1],
                    B = particle,
                    Distance = 12f,
                    Stiffness = 1f
                });
            }

            //Verlet.Constraints.Add(new DistanceConstraint {
            //    A = Verlet.Particles[0],
            //    B = Verlet.Particles[1],
            //    Distance = 24f,
            //    Stiffness = 1f
            //});

            CreateLayer("default");

            CreateEntity("verlet-test", Vector2.Zero)
                .AddComponent(new ChainRenderer {
                    Verlet = Verlet,
                    PassiveMovement = new Vector2(0, 0f)
                });
        }

        public override void Update() {
            if (InputManager.LeftMouseDown) {
                Verlet.Particles[0].Pin = Camera.MouseToWorldPosition();
            }
        }
    }
}
