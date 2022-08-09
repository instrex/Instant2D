using Microsoft.Xna.Framework;
using System;

namespace Instant2D.EC.Components {
    /// <summary>
    /// Settings for the audio rolloff effect. Used to calculate pan/volume for positional audio sources.
    /// </summary>
    public record struct AudioRolloff {
        public const float DEFAULT_MAX_DISTANCE = 400f;

        /// <summary>
        /// When <see langword="true"/>, <see cref="Calculate(Vector2, Vector2)"/> will be evaluated <see cref="MinDistance"/> and <see cref="MaxDistance"/>.
        /// </summary>
        public bool Enabled;

        /// <summary>
        /// Minimum distance at which the sound should be heard at maximum volume.
        /// </summary>
        public float MinDistance;

        /// <summary>
        /// Maximum distance at which sound should be heard at all.
        /// </summary>
        public float MaxDistance;

        public AudioRolloff() {
            MinDistance = 1;
            MaxDistance = DEFAULT_MAX_DISTANCE;
            Enabled = true;
        }

        // TODO: add logarithmic and custom rolloff support
        /// <summary>
        /// Calculate volume and pan based on <paramref name="listener"/> and <paramref name="position"/> of the sound. 
        /// </summary>
        public (float volume, float pan) Calculate(Vector2 listener, Vector2 position, float volumeScale = 1.0f) {
            var dist = Vector2.Distance(listener, position);
            var volume = 1f - (Math.Clamp(dist - MinDistance, 0, MaxDistance) / MaxDistance);
            var xDist = position.X - listener.X;
            return (volume * volumeScale, xDist / MaxDistance);
        }

        /// <summary>
        /// Audio rolloff functionality is disabled.
        /// </summary>
        public static AudioRolloff None => new AudioRolloff { Enabled = false };
    }
}
