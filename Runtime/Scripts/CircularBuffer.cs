using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CognitiveVR
{
    public class CircularBuffer<T> : IEnumerable, IEnumerable<T>
    {
        private T[] _buffer;
        private int _head;
        private int _tail;

        public CircularBuffer(int capacity)
        {
            _buffer = new T[capacity];
            _head = 0;
        }

        public int Count { get; private set; }

        public int Capacity
        {
            get { return _buffer.Length; }
        }

        /// <summary>
        /// called after a change to an item (in place) to increase tail and count
        /// </summary>
        public void Update()
        {
            _head = (_head + 1) % _buffer.Length;
            if (Count == _buffer.Length)
                _tail = (_tail + 1) % _buffer.Length;
            else
                Count++;
        }

        public int TailIndex
        {
            get { return _tail; }
        }
        public int HeadIndex
        {
            get { return _head; }
        }

        public T this[int index]
        {
            get
            {
                if (index < 0)
                {
                    return _buffer[_buffer.Length + index];
                }
                return _buffer[(_tail + index) % _buffer.Length];
            }
            set
            {
                _buffer[(_tail + index) % _buffer.Length] = value;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            if (Count == 0 || _buffer.Length == 0)
                yield break;

            for (var i = 0; i < Count; i++)
                yield return this[i];
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}