/// Copyright by Rob Jellinghaus.  All rights reserved.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Holofunk.Core
{
    /// <summary>Rolling buffer which tracks the element which occurs most.</summary>
    public class TopCounter<T>
    {
        /// <summary>
        /// The queue of T's.
        /// </summary>
        readonly Queue<T> _queue;

        /// <summary>
        /// The histogram, indexed by (int)T; recalculated on each queue update.
        /// </summary>
        readonly int[] _bins;

        /// <summary>
        /// The capacity.
        /// </summary>
        readonly int _capacity;

        readonly Func<T, int> _castToInt;
        readonly Func<int, T> _castToT;

        /// <summary>
        /// The current top value.
        /// </summary>
        Option<T> m_topValue;

        public TopCounter(int capacity, int maxValue, Func<T, int> castToInt, Func<int, T> castToT)
        {
            _queue = new Queue<T>(capacity);
            _bins = new int[maxValue + 1];
            _capacity = capacity;
            _castToInt = castToInt;
            _castToT = castToT;
        }
        
        public Option<T> TopValue { get { return m_topValue; } }

        /// <summary>Update this Averager with another data point.</summary>
        public void Update(T nextT)
        {
            if (_queue.Count == _capacity)
            {
                _bins[_castToInt(_queue.Dequeue())]--;
            }

            _queue.Enqueue(nextT);
            _bins[_castToInt(nextT)]++;

            // We track the newTopValue as an Option<int> rather than Option<T> to avoid having to cast while indexing into m_bins.
            Option<int> newTopValue = Option<int>.None;
            bool ambiguousTopValue = false;
            for (int i = 0; i < _bins.Length; i++)
            {
                if (_bins[i] > 0)
                {
                    if (!newTopValue.HasValue)
                    {
                        newTopValue = i;
                    }
                    else if (_bins[newTopValue.Value] == _bins[i])
                    {
                        ambiguousTopValue = true; // may already be true, which would be fine
                    }
                    else if (_bins[newTopValue.Value] < _bins[i])
                    {
                        newTopValue = i;
                        ambiguousTopValue = false;
                    }
                }
            }

            if (newTopValue.HasValue && !ambiguousTopValue)
            {
                m_topValue = _castToT(newTopValue.Value);
            }
            // otherwise deliberately leave m_topValue alone.
        }
    }
}
