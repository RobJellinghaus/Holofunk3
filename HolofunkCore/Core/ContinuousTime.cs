/// Copyright (c) 2021 by Rob Jellinghaus.  All rights reserved.

namespace Holofunk.Core
{
    /// <summary>
    /// A continous distance between two Times.
    /// </summary>
    /// <typeparam name="TTime"></typeparam>
    public struct ContinuousDuration<TTime>
    {
        readonly float _duration;

        public ContinuousDuration(float duration)
        {
            _duration = duration;
        }

        public override string ToString()
        {
            return $"CD[{_duration}]";
        }

        public static explicit operator float(ContinuousDuration<TTime> duration)
        {
            return duration._duration;
        }

        public static implicit operator ContinuousDuration<TTime>(float value)
        {
            return new ContinuousDuration<TTime>(value);
        }

        public static ContinuousDuration<TTime> operator *(ContinuousDuration<TTime> duration, float value)
        {
            return new ContinuousDuration<TTime>(value * duration._duration);
        }
        public static ContinuousDuration<TTime> operator *(float value, ContinuousDuration<TTime> duration)
        {
            return new ContinuousDuration<TTime>(value * duration._duration);
        }
    }
}
