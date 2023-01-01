using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D {
    public class TimeManager : GameSystem {
        /// <summary>
        /// Total time that has passed since beginning of the game.
        /// </summary>
        public static float TotalTime { get; private set; }

        /// <summary>
        /// Time that has passed since the last frame.
        /// </summary>
        public static float DeltaTime { get; private set; }

        /// <summary>
        /// Total number of frames passed.
        /// </summary>
        public static int FrameCount { get; private set; }

        public override void Initialize() {
            // time calculations should happen before anything else
            UpdateOrder = int.MinValue;
            IsUpdatable = true;
        }

        public override void Update(GameTime time) {
            var deltaTime = (float)time.ElapsedGameTime.TotalSeconds;
            TotalTime += deltaTime;
            DeltaTime = deltaTime;
            FrameCount++;
        }
    }
}
