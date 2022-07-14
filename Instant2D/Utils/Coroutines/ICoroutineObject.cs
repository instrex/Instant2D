namespace Instant2D.Coroutines {
    /// <summary>
    /// Represents base interface for all coroutine objects, such as <see cref="TimerInstance"/> or <see cref="CoroutineInstance"/>. <br/> 
    /// Exposes <see cref="IsRunning"/> property, which can help determine if the object has finished executing, 
    /// as well as <see cref="Stop"/> method and <see cref="Target"/>.
    /// </summary>
    public interface ICoroutineObject {
        /// <summary>
        /// Whether this coroutine finished execution or not.
        /// </summary>
        public bool IsRunning { get; }

        /// <summary>
        /// The optional target used for stopping coroutine prematurely and getting the individual timescale.
        /// </summary>
        public ICoroutineTarget Target { get; }

        /// <summary>
        /// End execution of this coroutine.
        /// </summary>
        public void Stop();
    }
}
