using Instant2D.EC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.Coroutines {
    /// <summary>
    /// Will be called by scenes or objects depending on the timescale and <paramref name="IgnoreEntityTimeScale"/> parameter. <br/>
    /// In case of <paramref name="IgnoreEntityTimeScale"/> being <see langword="true"/>, awaiter will use Scene's timescale instead.
    /// </summary>
    public record struct WaitForFixedUpdate(bool IgnoreEntityTimeScale = false) {
        internal int _beganAtFixedUpdate;
        internal Entity _entity;
    }

    /// <summary>
    /// Wait for specified amount of time in seconds. Used timescale will depend on coroutine's target, set <paramref name="IgnoreTimescale"/> to <see langword="false"/> in order to ignore that behaviour.
    /// </summary>
    public record struct WaitForSeconds(float Duration, bool IgnoreTimescale = false);

    /// <summary>
    /// Wait until another coroutine finished execution.
    /// </summary>
    public record struct WaitForCoroutine(Coroutine Coroutine);

    /// <summary>
    /// Wait until next frame. Use <see cref="WaitForFixedUpdate"/> or apply <see cref="TimeManager.DeltaTime"/> for framerate-dependent actions.
    /// </summary>
    public record struct WaitForUpdate();
}
