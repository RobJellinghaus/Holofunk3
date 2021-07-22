
using System;
// Copyright by Rob Jellinghaus. All rights reserved.
namespace Holofunk.Core
{
    public struct Option<T>
    {
        readonly bool _hasValue;
        readonly T _value;

        public static Option<T> None
        {
            get { return default(Option<T>); }
        }

        public Option(T value)
        {
            _hasValue = true;
            _value = value;
        }

        public static implicit operator Option<T>(T value)
        {
            return new Option<T>(value);
        }

        /// <summary>
        /// Is this optional value equal to the other optional value, given the comparison (if they both have values)?
        /// </summary>
        public bool IsEqualTo(Option<T> other, Func<T, T, bool> comparison)
        {
            return HasValue == other.HasValue
                && (!HasValue || comparison(Value, other.Value));
        }

        public T Value
        {
            get
            {
                Contract.Requires(_hasValue, "m_hasValue");
                return _value;
            }
        }

        public bool HasValue => _hasValue;

        public T GetValueOrDefault(T defaultValue)
        {
            if (HasValue)
            {
                return Value;
            }
            else
            {
                return defaultValue;
            }
        }
    }
}
