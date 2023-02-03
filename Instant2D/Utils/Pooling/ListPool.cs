using System.Collections.Generic;

namespace Instant2D.Utils {
    /// <summary>
    /// Static utility pool for lists.
    /// </summary>
    public static class ListPool<T> {
        static readonly Pool<List<T>> _internalPool = new(3);

        /// <inheritdoc cref="Pool{T}.Rent"/>
        public static List<T> Get() => _internalPool.Rent();

        /// <inheritdoc cref="Pool{T}.Return(T)"/>
        public static void Return(List<T> obj) {
            _internalPool.Return(obj);
            obj.Clear();
        } 
    }
}
