using System;

namespace Instant2D.Utils {
    /// <summary>
    /// Interface for instance pooling systems.
    /// </summary>
    public interface IInstancePool<T> {
        /// <summary>
        /// Instance capacity of this pool.
        /// </summary>
        int Capacity { get; }

        /// <summary>
        /// Custom constructor for when the pool needs to be expanded.
        /// </summary>
        Func<IInstancePool<T>, T> InstanceConstructor { get; init; }

        /// <summary>
        /// Expands the pool by provided instance count.
        /// </summary>
        void Expand(int instancesToCreate);

        /// <summary>
        /// Obtain an instance from the pool.
        /// </summary>
        T Rent();

        /// <summary>
        /// Return an instance to the pool. <see cref="IPooledInstance.Reset"/> will be called when implemented.
        /// </summary>
        void Return(T instance);
    }
}
