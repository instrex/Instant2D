namespace Instant2D.Utils {
    /// <summary>
    /// Provides a static wrapper over <see cref="Pool{T}"/>.
    /// </summary>
    public static class StaticPool<T> where T: new() {
        static readonly Pool<T> _pool = new();

        /// <inheritdoc cref="Pool{T}.Get"/>
        public static T Get() => _pool.Get();

        /// <inheritdoc cref="Pool{T}.Return(T)"/>
        public static void Return(T obj) => _pool.Return(obj);

        /// <inheritdoc cref="Pool{T}.Expand(int)"/>
        public static void PreHeat(int objectsToAdd = -1) => _pool.Expand(objectsToAdd);
    }
}
