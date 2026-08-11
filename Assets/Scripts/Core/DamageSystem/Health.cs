using EditorAttributes;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;
using UnityEngine.Serialization;
using SLS.GeneralUtilities.StatObjects;
using SLS.GeneralUtilities.EventTickets;





#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// A Component managing the Health of an entity, whether that be Player, Enemy, or Destructible Object.
/// </summary>

public class Health : MonoBehaviour, IDamagable
{
    #region Real Values / Initial Cofig
    [SerializeField, Title("Initial Health")] protected int _current;
    [SerializeField, Title("Max Health")] protected int _max;
    [SerializeField, Title("Min Health")] protected int _min;

    public UltEvents.UltEvent<int> damageEvent = new();
    public UltEvents.UltEvent depleteEvent = new();
    public UltEvents.UltEvent<int> healEvent = new();
    [SerializeField] protected Attack.TagSet immuneTags = new();
    #endregion

    #region Primary Get Setters
    public virtual int Current
    {
        get => _current;
        set
        {
            int oldValue = _current;
            _current = value;
            if (_current.CompareTo(_min) < 0) _current = _min;
            if (_current.CompareTo(_max) > 0) _current = _max;
            if (!_current.Equals(oldValue)) OnValueChanged?.Invoke(_current);
        }
    }
    public virtual void SetValue(int value) => Current = value;
    public virtual int Max
    {
        get => _max;
        set
        {
            int oldValue = _max;
            _max = value;
            if (!_max.Equals(oldValue)) OnMaxChanged?.Invoke(_max);
            Current = Current;
        }
    }
    public virtual void SetMax(int value) => Max = value;
    public virtual int Min
    {
        get => _min;
        set
        {
            int oldValue = _min;
            _min = value;
            if (!_min.Equals(oldValue)) OnMinChanged?.Invoke(_min);
            Current = Current;
        }
    }
    public virtual void SetMin(int value) => Min = value;
    #endregion

    #region Callbacks
    [SerializeField] public UltEvents.UltEvent<int> OnValueChanged;
    [SerializeField] public UltEvents.UltEvent<int> OnMaxChanged;
    [SerializeField] public UltEvents.UltEvent<int> OnMinChanged;
    #endregion

    #region Helpers
    public virtual void InstantFill() => Current = Max;
    public virtual void InstantDeplete() => Current = Min;
    public virtual void SetMaxAndFill(int value)
    {
        int preMax = Max;
        int preVal = Current;

        _max = value;
        _current = value;

        if (!preMax.Equals(_max)) OnMaxChanged?.Invoke(value);
        if (!preVal.Equals(_current)) OnMinChanged?.Invoke(value);
    }

    public virtual float Percentage => (float)Current / (float)Max;
    #endregion

    #region Operators
    public static implicit operator int(Health s) => s != null ? s.Current : default;
    public static bool operator ==(Health l, int r) => l != null && l.Current.Equals(r);
    public static bool operator !=(Health l, int r) => !(l == r);

    public static Health operator +(Health l, int r)
    {
        l.Current += r;
        return l;
    }
    public static Health operator -(Health l, int r)
    {
        l.Current -= r;
        return l;
    }
    public static Health operator *(Health l, int r)
    {
        l.Current *= r;
        return l;
    }
    public static Health operator /(Health l, int r)
    {
        l.Current /= r;
        return l;
    }
    public static Health operator %(Health l, int r)
    {
        l.Current %= r;
        return l;
    }

    /// <summary>
    /// Psudeo Assignment operator. Assigns the value on the right to the value.
    /// </summary>
    public static Health operator &(Health l, int r)
    {
        l.Current = r;
        return l;
    }
    /// <summary>
    /// Psudeo Assignment operator. Assigns the object on the right's value to the value.
    /// </summary>
    public static Health operator &(Health l, IntStat r)
    {
        l.Current = r.Value;
        return l;
    }

    #endregion

    #region Other Data

    public override bool Equals(object obj) => obj is int objT
        ? Current.Equals(objT)
        : base.Equals(obj);
    public override int GetHashCode() => _current.GetHashCode();

    protected List<EventTicket> events = new();
    protected virtual IntStat MaxSourceStat => null;
    protected bool damagable = true;

    #endregion


    #region Object Functionality
    protected virtual void Awake()
    {
        if (MaxSourceStat != null)
        {
            Max = MaxSourceStat;
            events.Add(MaxSourceStat.Subscribe(SetMax));
        }
        InstantFill();
    }
    protected virtual void OnEnable() => events.SubscribeAll();
    protected virtual void OnDisable() => events.UnSubscribeAll();
    protected virtual void OnDestroy() => events.UnSubscribeAll();
    public virtual void Destroy() => Destroy(gameObject);
    #endregion

    #region Damage Functionality
    public bool Damage(Attack attack)
    {
        if (!damagable || attack.amount < 1 || immuneTags.ContainsAnyFrom(attack.tags) || !OverrideDamageable(attack))
            return false;
        OverrideDamageValue(ref attack);
        if (!damagable || attack.amount < 1 || immuneTags.ContainsAnyFrom(attack.tags) || !OverrideDamageable(attack))
            return false;

        int prevValue = Current;
        Current -= attack.amount;

        if (Current < prevValue) OnDamage(attack);
        if (Current == 0) OnDeplete(attack);

        return true;
    }

    public bool Heal(int amount)
    {
        if (amount < 1 || Current >= Max) return false;

        int prevValue = Current;
        Current += amount;

        if (Current > prevValue) OnHeal(Current - prevValue);

        return true;
    }

    protected virtual void OnDamage(Attack attack) => damageEvent?.Invoke(attack.amount);
    protected virtual void OnDeplete(Attack attack) => depleteEvent?.Invoke();
    protected virtual void OnHeal(int amount) => healEvent?.Invoke(amount);


    /// <summary>
    /// Overrides whether this thing can be damaged under certain conditions
    /// </summary>
    /// <param name="attack">The attack fed in.</param>
    /// <returns>Whether the attack successfully connects.</returns>
	protected virtual bool OverrideDamageable(Attack attack) { return true; }
    protected virtual void OverrideDamageValue(ref Attack attack) { }



    #endregion
}