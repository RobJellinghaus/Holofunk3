/// Copyright by Rob Jellinghaus.  All rights reserved.

using UnityEngine;

namespace Holofunk.Core
{
    /// <summary>Rolling buffer which can average a number of T's.</summary>
    /// <remarks>Parameterized with methods to handle summing / dividing the T's in question.</remarks>
    public abstract class Averager<T>
    {
        // the buffer of T's
        readonly T[] _storage;

        // have we filled the current storage?
        bool _storageFull;

        // what's the next index to be overwritten with the next datum?
        int _index;

        // the total
        T _total;

        // the current average, so we don't have race conditions about it
        T m_average;

        public Averager(int capacity)
        {
            _storage = new T[capacity];
        }

        /// <summary>Has this Averager got no data?</summary>
        public bool IsEmpty { get { return _index == 0 && !_storageFull; } }

        /// <summary>Update this Averager with another data point.</summary>
        public void Update(T nextT)
        {
            if (!IsValid(nextT)) {
                return;
            }

            if (_index == _storage.Length) {
                // might as well unconditionally set it, branching is more expensive
                _storageFull = true;
                _index = 0;
            }

            if (_storageFull) {
                _total = Subtract(_total, _storage[_index]);
            }
            _total = Add(_total, nextT);
            _storage[_index] = nextT;
            _index++;
            m_average = Divide(_total, _storageFull ? _storage.Length : _index);
        }

        /// <summary>Get the average; invalid if Average.IsEmpty.</summary>
        public T Average 
        { 
            get 
            {
                return m_average;
            } 
        }

        protected abstract bool IsValid(T t);
        protected abstract T Subtract(T total, T nextT);
        protected abstract T Add(T total, T nextT);
        protected abstract T Divide(T total, int count);
    }

    public class FloatAverager : Averager<float>
    {
        public FloatAverager(int capacity)
            : base(capacity)
        {
        }

        protected override bool IsValid(float t)
        {
            // semi-arbitrary, but intended to filter out infinities and other extreme bogosities
            return -100 < t && t < 2000;
        }

        protected override float Add(float total, float nextT)
        {
            return total + nextT;
        }

        protected override float Subtract(float total, float nextT)
        {
            return total - nextT;
        }

        protected override float Divide(float total, int count)
        {
            return total / count;
        }
    }

    public class Vector3Averager : Averager<Vector3>
    {
        public Vector3Averager(int capacity)
            : base(capacity)
        {
        }

        protected override bool IsValid(Vector3 t)
        {
            // semi-arbitrary, but intended to filter out infinities and other extreme bogosities
            return -100 < t.x && t.x < 2000 && -100 < t.y && t.y < 2000;
        }

        protected override Vector3 Add(Vector3 total, Vector3 nextT)
        {
            return total + nextT;
        }

        protected override Vector3 Subtract(Vector3 total, Vector3 nextT)
        {
            return total - nextT;
        }

        protected override Vector3 Divide(Vector3 total, int count)
        {
            return new Vector3(total.x / count, total.y / count, total.z / count);
        }
    }
}
