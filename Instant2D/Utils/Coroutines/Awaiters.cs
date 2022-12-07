using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.Coroutines {
    /// <summary>
    /// Will be called by scenes or objects depending on the timescale and <paramref name="UseObjectTimescale"/> parameter. <br/>
    /// This cannot be used outside of default EC framework.
    /// </summary>
    public record struct WaitForFixedUpdate(bool UseObjectTimescale = false);

    /// <summary>
    /// Wait for specified amount of time in seconds. Used timescale will depend on coroutine's target, set <paramref name="IgnoreTimescale"/> to <see langword="false"/> in order to ignore that behaviour.
    /// </summary>
    public record struct WaitForSeconds(float Duration, bool IgnoreTimescale = false);

    /// <summary>
    /// Wait until another coroutine finished execution.
    /// </summary>
    public record struct WaitForCoroutine(Coroutine Coroutine);

    /// <summary>
    /// Wait until next frame. Use <see cref="WaitForFixedUpdate"/> or apply <see cref="TimeManager.DeltaTime"/> for framerate-independent actions.
    /// </summary>
    public record struct WaitForUpdate();
}
