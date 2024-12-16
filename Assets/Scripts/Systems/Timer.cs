using EditorAttributes;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable, System.Obsolete]
public struct Timer_Old
{
    public float value;
    public float lowerEdge;
    public float higherEdge;
    public delegate void Delegate(); 
    public Delegate action;
    private bool active;

    public Timer_Old(float higherEdge, Delegate action = null)
    {
        value = 0;
        this.lowerEdge = 0;
        this.higherEdge = higherEdge;
        this.action = action;
        active = true;
    }
    public Timer_Old(float lowerEdge, float higherEdge, Delegate action = null)
    {
        value = 0;
        this.lowerEdge = lowerEdge;
        this.higherEdge = higherEdge;
        this.action = action;
        active = true;
    }
    public Timer_Old(float begin, float lowerEdge, float higherEdge, Delegate action = null)
    {
        value = begin;
        this.lowerEdge = lowerEdge;
        this.higherEdge = higherEdge;
        this.action = action;
        active = true;
    }

    public static implicit operator bool(Timer_Old timer) => timer.active;
    public static implicit operator float(Timer_Old timer) => timer.value;

    public static Timer_Old operator +(Timer_Old timer, float value)
    {
        if (!timer) return timer;
        timer.value += value;
        if(timer.value >= timer.higherEdge)
        {
            timer.action?.Invoke();
            timer.value = timer.value - timer.higherEdge + timer.lowerEdge;
        }
        return timer;
    }
    public static Timer_Old operator -(Timer_Old timer, float value)
    {
        if (!timer) return timer;
        timer.value -= value;
        if (timer.value <= timer.lowerEdge)
        {
            timer.action?.Invoke();
            timer.value += timer.value + timer.higherEdge - timer.lowerEdge;
        }
        return timer;
    }

    public bool Increment(float amount, Delegate action = null)
    {
        if(!this) return false;
        value += amount;
        bool act = false;
        if (amount>0 && value >= higherEdge)
        {
            value = value - higherEdge + lowerEdge;
            act = true;
        }
        if (amount<0 &&value <= lowerEdge)
        {
            value += value + higherEdge - lowerEdge;
            act = true;
        }
        if (act) (action ?? this.action)?.Invoke();
        return act;
    }

    public static bool Time(ref float time, float amount, float higherEdge, float lowerEdge = 0f)
    {
        time += amount;
        if (amount > 0 && time >= higherEdge)
        {
            time = time - higherEdge + lowerEdge;
            return true;
        }
        else if (amount < 0 && time <= lowerEdge)
        {
            time += time + higherEdge - lowerEdge;
            return true;
        }
        else return false;
    }
}

namespace Timer
{

    [System.Serializable]
    public struct Loop
    {
        [SerializeField] public float rate;
        [SerializeField, DisableInEditMode, DisableInPlayMode] public float current;
        [HideInInspector] public bool disabled;

        public Loop(float rate, bool disable = false)
        {
            this.rate = rate;
            current = 0f;
            disabled = disable;
        }

        public void Tick(Action callback)
        {
            if (disabled) return;
            current += Time.deltaTime;
            if(current > rate)
            {
                current %= rate;
                callback?.Invoke();
            }
        }
    }

    [System.Serializable]
    public struct OneTime
    {
        [SerializeField] public float length;
        [SerializeField, DisableInEditMode, DisableInPlayMode] public float current;
        [HideInInspector] public bool running;

        public OneTime(float rate, bool activate = false)
        {
            this.length = rate;
            current = 0f;
            running = false;
            if (activate) Begin();
        }

        public void Begin()
        {
            current = 0f;
            running = true;
        }

        public void Tick(Action callback)
        {
            if (!running) return;
            current += Time.deltaTime;
            if (current > length)
            {

                running = false;
                callback?.Invoke();
            }
        }
    }

}