using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.Utils {
    public class FastList<T> : IList<T>, IResettable {
        const int DEFAULT_CAPACITY = 16;

        T[] _buffer;
        int _length;

        public FastList() : this(DEFAULT_CAPACITY) { }
        public FastList(int capacity) {
            _buffer = new T[capacity];
        }

        public FastList(IEnumerable<T> collection) : this(collection.TryGetNonEnumeratedCount(out var count) ? count : DEFAULT_CAPACITY) {

        }

        public T this[int index] {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _buffer[index];

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _buffer[index] = value;
        }

        public int Count {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _length;
        }

        public bool IsReadOnly => false;

        public void Add(T item) {
            // resize when at the limit
            if (_length == _buffer.Length) {
                Array.Resize(ref _buffer, _length * 2);
            }

            _buffer[_length++] = item;
        }

        /// <summary>
        /// Clears the buffer and resets the length.
        /// </summary>
        public void Clear() {
            Array.Clear(_buffer, 0, _length);
            _length = 0;
        }

        /// <summary>
        /// Resets the length, but doesn't clear the buffer.
        /// </summary>
        public void Reset() {
            _length = 0;
        }

        public bool Contains(T item) {
            var comparer = EqualityComparer<T>.Default;
            for (var i = 0; i < _length; i++) {
                if (comparer.Equals(_buffer[i], item))
                    return true;
            }

            return false;
        }

        public void CopyTo(T[] array, int arrayIndex) {
            throw new NotImplementedException();
        }

        public int IndexOf(T item) {
            throw new NotImplementedException();
        }

        public void Insert(int index, T item) {
            throw new NotImplementedException();
        }

        public bool Remove(T item) {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index) {
            throw new NotImplementedException();
        }

        public IEnumerator<T> GetEnumerator() {
            throw new NotImplementedException("FastList does not support enumeration yet.");
        }

        IEnumerator IEnumerable.GetEnumerator() {
            throw new NotImplementedException("FastList does not support enumeration yet.");
        }

    }
}
