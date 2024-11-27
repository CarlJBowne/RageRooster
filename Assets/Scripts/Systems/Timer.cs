using System;
using System.Collections.Generic;

[Serializable]
public struct Timer
{
    public float value;
    public float lowerEdge;
    public float higherEdge;
    public delegate void Delegate(); 
    public Delegate action;
    private bool active;

    public Timer(float higherEdge, Delegate action = null)
    {
        value = 0;
        this.lowerEdge = 0;
        this.higherEdge = higherEdge;
        this.action = action;
        active = true;
    }
    public Timer(float lowerEdge, float higherEdge, Delegate action = null)
    {
        value = 0;
        this.lowerEdge = lowerEdge;
        this.higherEdge = higherEdge;
        this.action = action;
        active = true;
    }
    public Timer(float begin, float lowerEdge, float higherEdge, Delegate action = null)
    {
        value = begin;
        this.lowerEdge = lowerEdge;
        this.higherEdge = higherEdge;
        this.action = action;
        active = true;
    }

    public static implicit operator bool(Timer timer) => timer.active;
    public static implicit operator float(Timer timer) => timer.value;

    public static Timer operator +(Timer timer, float value)
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
    public static Timer operator -(Timer timer, float value)
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
        if (amount>0 && value >= higherEdge) value = value - higherEdge + lowerEdge;
        if (amount<0 &&value <= lowerEdge) value += value + higherEdge - lowerEdge;
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
