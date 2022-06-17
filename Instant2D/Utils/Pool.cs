using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.Utils {
    public class Pool<T> where T: new() {
        readonly Queue<T> _items;
        readonly int _initialCapacity;

        public Pool(int capacity = 100) {
            _initialCapacity = capacity;
            _items = new(capacity);
            Heat(capacity);
        }

        public void Heat(int objectsToAdd) {
            for (var i = 0; i < objectsToAdd; i++) {
                var item = new T();
                _items.Enqueue(item);
            }
        }

        public T Get() {
            if (_items.Count == 0)
                Heat(_initialCapacity);

            var obj = _items.Dequeue();

            if (obj is IResettable resettable)
                resettable.Reset();

            return obj;
        }

        public void Return(T obj) {
            _items.Enqueue(obj);
        }
    }

    public static class StaticPool<T> where T: new() {
        static readonly Pool<T> _pool = new();
        public static T Get() => _pool.Get();
        public static void Return(T obj) => _pool.Return(obj);
    }
}
