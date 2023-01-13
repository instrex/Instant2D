using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.Utils {
    /// <summary>
    /// Presents an easy way to avoid excess allocations. Types may implement <see cref="IPooled"/> in order to get reset before returning.
    /// </summary>
    public class Pool<T> where T: new() {
        /// <summary>
        /// Shared pool instance.
        /// </summary>
        public static readonly Pool<T> Shared = new();

        readonly Queue<T> _items;
        int _capacity;

        public Pool(int capacity = 24) {
            _capacity = capacity;
            _items = new(capacity);
            Expand(capacity);
        }

        /// <summary>
        /// Allocate more instances for later use. If <c>-1</c> is passed as <paramref name="objectsToAdd"/>, buffer size will be doubled.
        /// </summary>
        public void Expand(int objectsToAdd = -1) {
            if (objectsToAdd == -1) {
                objectsToAdd = _capacity;
            }

            for (var i = 0; i < objectsToAdd; i++) {
                var item = new T();
                _items.Enqueue(item);
            }

            _capacity += objectsToAdd;
        }

        /// <summary>
        /// Get a free reference from the pool, possibly resizing and allocating more if none is available. 
        /// If <typeparamref name="T"/> implements <see cref="IPooled"/>, the <see cref="IPooled.Reset"/> is called.
        /// </summary>
        /// <returns></returns>
        public T Get() {
            if (_items.Count == 0)
                Expand();

            return _items.Dequeue();
        }

        /// <summary>
        /// Return an instance to the pool.
        /// </summary>
        public void Return(T obj) {
            if (obj is null) {
                // TODO: replace with a logger warning? maybe only throw in debug mode
                throw new InvalidOperationException($"Attempted to return a null value into the pool.");
            }

            _items.Enqueue(obj);

            // reset the pooled object
            if (obj is IPooled resettable)
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
