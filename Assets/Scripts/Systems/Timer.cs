using System;
using System.Collections.Generic;

[Serializable]
public class Timer
{
    public float value;
    public float lowerEdge;
    public float higherEdge;
    public delegate void Delegate(); 
    public Delegate action;

    public Timer(float higherEdge, Delegate action = null) => new Timer(0, 0, higherEdge, action);
    public Timer(float lowerEdge, float higherEdge, Delegate action = null) => new Timer(lowerEdge, lowerEdge, higherEdge, action);
    public Timer(float begin, float lowerEdge, float higherEdge, Delegate action = null)
    {
        value = begin;
        this.lowerEdge = lowerEdge;
        this.higherEdge = higherEdge;
        this.action = action;
    }
    public Timer() { }

    public static Timer New(float higherEdge, Delegate action = null) => New(0, 0, higherEdge, action);
    public static Timer New(float lowerEdge, float higherEdge, Delegate action = null) => New(lowerEdge, lowerEdge, higherEdge, action);
    public static Timer New(float begin, float lowerEdge, float higherEdge, Delegate action = null)
    {
        Timer T = new();
        T.value = begin;
        T.lowerEdge = lowerEdge;
        T.higherEdge = higherEdge;
        T.action = action;
        return T;
    }



    public static implicit operator float(Timer timer) => timer.value;

    public static Timer operator +(Timer timer, float value)
    {
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
        timer.value -= value;
        if (timer.value <= timer.lowerEdge)
        {
            timer.action?.Invoke();
            timer.value += timer.value + timer.higherEdge - timer.lowerEdge;
        }
        return timer;
    }

    public void Increment(float amount, Delegate action = null)
    {
        value += amount;
        if (amount>0 && value >= higherEdge)
        {
            (action ?? this.action)?.Invoke();
            value = value - higherEdge + lowerEdge;
        }
        if (amount<0 &&value <= lowerEdge)
        {
            (action ?? this.action)?.Invoke();
            value += value + higherEdge - lowerEdge;
        }
    }
}
