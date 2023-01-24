using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.Utils {
    /// <summary>
    /// Presents an easy way to avoid excess allocations. Types may implement <see cref="IPooledInstance"/> in order to get reset before returning. <br/>
    /// Will gradually expand to accomodate more and more instances.
    /// </summary>
    public class Pool<T> : IInstancePool<T> where T: new() {
        static Pool<T> _sharedPool;

        /// <summary>
        /// Shared pool instance.
        /// </summary>
        public static Pool<T> Shared {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get {
                _sharedPool ??= new();
                return _sharedPool;
            }
        }

        public Func<IInstancePool<T>, T> InstanceConstructor { get; init; }
        public int Capacity { get; private set; }

        readonly Stack<T> _items;
        public Pool(int capacity = 24, Func<IInstancePool<T>, T> instanceConstructor = default) {
            _items = new(capacity);

            InstanceConstructor = instanceConstructor;
            
            if (capacity > 0) {
                // create initial instances
                Expand(capacity);
            }
        }

        /// <summary>
        /// Allocate more instances for later use.
        /// </summary>
        public void Expand(int instancesToAdd) {
            for (var i = 0; i < instancesToAdd; i++) {
                var item = InstanceConstructor != null ? InstanceConstructor(this) : new T();
                _items.Push(item);
                Capacity++;
            }
        }

        public T Get() {
            if (_items.TryPop(out var instance))
                return instance;

            Expand(Capacity);
            return _items.Pop();
        }

        public void Return(T obj) {
            if (obj is null) {
                // TODO: replace with a logger warning? maybe only throw in debug mode
                throw new InvalidOperationException($"Attempted to return a null value into the pool.");
            }

            _items.Push(obj);

            // reset the pooled object
            if (obj is IPooledInstance resettable)
                resettable.Reset();
        }

        /// <summary>
        /// Return an instance to the pool and null out the reference.
        /// </summary>
        public void Return(ref T obj) {
            Return(obj);
            obj = default;
        }
    }
}
