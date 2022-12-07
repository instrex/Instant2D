namespace Instant2D.Coroutines {
    /// <summary>
    /// Marking object with this interface will allow you to attach coroutines to it, inheriting its properties and being able to stop coroutines by target. <br/>
    /// If you're implementing this interface on your own entities, make sure to call <see cref="CoroutineManager.StopAll(ICoroutineTarget)"/> when yout target is destroyed or it goes inactive.
    /// </summary>
    public interface ICoroutineTarget {
        /// <summary>
        /// Timescale used for coroutines. Will affect <see cref="WaitForSeconds"/> and <see cref="WaitForFixedUpdate"/>.
        /// </summary>
        float TimeScale { get; }
    }
}
