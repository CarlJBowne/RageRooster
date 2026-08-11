using System;
using System.Collections.Generic;
using System.Text;
using SLS.GeneralUtilities.EventTickets;
using UnityEngine;

namespace SLS.GeneralUtilities.StatObjects
{
    [System.Serializable]
    public class StatObject<T> where T : struct, IComparable<T>
    {
        [SerializeField] protected T _value;

        public virtual T Value
        {
            get => _value;
            set
            {
                T oldValue = _value;
                _value = value;
                if (!_value.Equals(oldValue)) CallOnValueChanged(_value);
            }
        }
        public virtual void SetValue(T value) => Value = value;

        public event Action<T> OnValueChanged; [SerializeField] protected UltEvents.UltEvent<T> OnValueChangedUlt;
        protected void CallOnValueChanged(T v)
        {
            OnValueChanged?.Invoke(v);
            OnValueChangedUlt?.Invoke(v);
        }

        public static implicit operator T(StatObject<T> s) => s is null ? default : s.Value;
        public static bool operator ==(StatObject<T> l, T r) => l is not null && l.Value.Equals(r);
        public static bool operator !=(StatObject<T> l, T r) => !(l == r);

        public override bool Equals(object obj) => obj is T objT
            ? Value.Equals(objT)
            : base.Equals(obj);
        public override int GetHashCode() => _value.GetHashCode();
        public override string ToString() => $"Value:{_value}, {OnValueChanged?.GetInvocationList().Length ?? 0} Listeners";

        public static StatObject<T> operator &(StatObject<T> l, T r)
        {
            l.Value = r;
            return l;
        }
        public static StatObject<T> operator &(StatObject<T> l, StatObject<T> r)
        {
            l.Value = r.Value;
            return l;
        }

        public EventTicket Subscribe(Action<T> subscriber, bool subscribeNow = true) => 
            OnValueChanged.Subscribe(subscriber, subscribeNow);
    }

    public class StatObjectClamped<T> : StatObject<T> where T : struct, IComparable<T>
    {
        [SerializeField] protected T _max;
        [SerializeField] protected T _min;
        public override T Value
        {
            get => _value;
            set
            {
                T oldValue = _value;
                _value = value;
                if (_value.CompareTo(_min) < 0) _value = _min;
                if (_value.CompareTo(_max) > 0) _value = _max;
                if (!_value.Equals(oldValue)) CallOnValueChanged(_value);
            }
        }

        public event Action<T> OnMaxChanged; [SerializeField] protected UltEvents.UltEvent<T> OnMaxChangedUlt;
        protected void CallOnMaxChanged(T v)
        {
            OnMaxChanged?.Invoke(v);
            OnMaxChangedUlt?.Invoke(v);
        }
        public event Action<T> OnMinChanged; [SerializeField] protected UltEvents.UltEvent<T> OnMinChangedUlt;
        protected void CallOnMinChanged(T v)
        {
            OnMinChanged?.Invoke(v);
            OnMinChangedUlt?.Invoke(v);
        }

        public T Max
        {
            get => _max;
            set
            {
                T oldValue = _max;
                _max = value;
                if (!_max.Equals(oldValue)) CallOnMaxChanged(_max);
                Value = Value;
            }
        }
        public virtual void SetMax(T value) => Max = value;
        public T Min
        {
            get => _min;
            set
            {
                T oldValue = _min;
                _min = value;
                if (!_min.Equals(oldValue)) CallOnMinChanged(_min);
                Value = Value;
            }
        }
        public virtual void SetMin(T value) => Min = value;

        public virtual void InstantFill() => Value = Max;
        public virtual void InstantDeplete() => Value = Min;
        public virtual void SetMaxAndFill(T value)
        {
            T preMax = Max;
            T preVal = Value;

            _max = value;
            _value = value;

            if (!preMax.Equals(_max)) CallOnMaxChanged(value);
            if (!preVal.Equals(_value)) CallOnValueChanged(value);
        }

        public EventTicket SubscribeMax(Action<T> subscriber, bool subscribeNow = true) =>
            OnMaxChanged.Subscribe(subscriber, subscribeNow);
        public EventTicket SubscribeMin(Action<T> subscriber, bool subscribeNow = true) =>
            OnMinChanged.Subscribe(subscriber, subscribeNow);

    }

    public class IntStat : StatObject<int>
    {
        public static IntStat operator +(IntStat l, int r)
        {
            l.Value += r;
            return l;
        }
        public static IntStat operator -(IntStat l, int r)
        {
            l.Value -= r;
            return l;
        }
        public static IntStat operator *(IntStat l, int r)
        {
            l.Value *= r;
            return l;
        }
        public static IntStat operator /(IntStat l, int r)
        {
            l.Value /= r;
            return l;
        }
        public static IntStat operator %(IntStat l, int r)
        {
            l.Value %= r;
            return l;
        }

        /// <summary>
        /// Psudeo Assignment operator. Assigns the value on the right to the value.
        /// </summary>
        public static IntStat operator &(IntStat l, int r)
        {
            l.Value = r;
            return l;
        }
        /// <summary>
        /// Psudeo Assignment operator. Assigns the object on the right's value to the value.
        /// </summary>
        public static IntStat operator &(IntStat l, IntStat r)
        {
            l.Value = r.Value;
            return l;
        }
    }

    public class IntStatClamped : StatObjectClamped<int>
    {
        public static IntStatClamped operator +(IntStatClamped l, int r)
        {
            l.Value += r;
            return l;
        }
        public static IntStatClamped operator -(IntStatClamped l, int r)
        {
            l.Value -= r;
            return l;
        }
        public static IntStatClamped operator *(IntStatClamped l, int r)
        {
            l.Value *= r;
            return l;
        }
        public static IntStatClamped operator /(IntStatClamped l, int r)
        {
            l.Value /= r;
            return l;
        }
        public static IntStatClamped operator %(IntStatClamped l, int r)
        {
            l.Value %= r;
            return l;
        }

        /// <summary>
        /// Psudeo Assignment operator. Assigns the value on the right to the value.
        /// </summary>
        public static IntStatClamped operator &(IntStatClamped l, int r)
        {
            l.Value = r;
            return l;
        }
        /// <summary>
        /// Psudeo Assignment operator. Assigns the object on the right's value to the value.
        /// </summary>
        public static IntStatClamped operator &(IntStatClamped l, IntStat r)
        {
            l.Value = r.Value;
            return l;
        }
    }

    public class FloatStat : StatObject<float>
    {
        public static FloatStat operator +(FloatStat l, float r)
        {
            l.Value += r;
            return l;
        }
        public static FloatStat operator -(FloatStat l, float r)
        {
            l.Value -= r;
            return l;
        }
        public static FloatStat operator *(FloatStat l, float r)
        {
            l.Value *= r;
            return l;
        }
        public static FloatStat operator /(FloatStat l, float r)
        {
            l.Value /= r;
            return l;
        }
        public static FloatStat operator %(FloatStat l, float r)
        {
            l.Value %= r;
            return l;
        }

        /// <summary>
        /// Psudeo Assignment operator. Assigns the value on the right to the value.
        /// </summary>
        public static FloatStat operator &(FloatStat l, float r)
        {
            l.Value = r;
            return l;
        }
        /// <summary>
        /// Psudeo Assignment operator. Assigns the object on the right's value to the value.
        /// </summary>
        public static FloatStat operator &(FloatStat l, FloatStat r)
        {
            l.Value = r.Value;
            return l;
        }
    }

    public class FloatStatClamped : StatObjectClamped<float>
    {
        public static FloatStatClamped operator +(FloatStatClamped l, float r)
        {
            l.Value += r;
            return l;
        }
        public static FloatStatClamped operator -(FloatStatClamped l, float r)
        {
            l.Value -= r;
            return l;
        }
        public static FloatStatClamped operator *(FloatStatClamped l, float r)
        {
            l.Value *= r;
            return l;
        }
        public static FloatStatClamped operator /(FloatStatClamped l, float r)
        {
            l.Value /= r;
            return l;
        }
        public static FloatStatClamped operator %(FloatStatClamped l, float r)
        {
            l.Value %= r;
            return l;
        }

        /// <summary>
        /// Psudeo Assignment operator. Assigns the value on the right to the value.
        /// </summary>
        public static FloatStatClamped operator &(FloatStatClamped l, float r)
        {
            l.Value = r;
            return l;
        }
        /// <summary>
        /// Psudeo Assignment operator. Assigns the object on the right's value to the value.
        /// </summary>
        public static FloatStatClamped operator &(FloatStatClamped l, FloatStatClamped r)
        {
            l.Value = r.Value;
            return l;
        }
    }

}
