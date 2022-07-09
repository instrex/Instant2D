namespace Instant2D.Utils.Coroutines {
    /// <summary>
    /// Convenience interface for all the possible timer/tween/coroutine targets. 
    /// Provides a way for them to know when the object is not active anymore and stop accordingly, preventing various unwanted scenarios. <br/> 
    /// For example, a tween trying to access a property of destroyed object (which could've been pooled afterwards).
    /// </summary>
    public interface ICoroutineTarget {
        /// <summary>
        /// Whether or not the current target is active. When <see langword="false"/>, coroutine will not be advanced and will stop.
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// TimeScale information for this target.
        /// </summary>
        float TimeScale { get; }
    }


}
