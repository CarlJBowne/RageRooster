using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class Timer
{
    public float value;
    public readonly float lowerEdge;
    public readonly float higherEdge;
    private readonly Action action;

    public Timer(float higherEdge, Action action = null) => new Timer(0, 0, higherEdge, action);
    public Timer(float lowerEdge, float higherEdge, Action action = null) => new Timer(lowerEdge, lowerEdge, higherEdge, action);
    public Timer(float begin, float lowerEdge, float higherEdge, Action action = null)
    {
        value = begin;
        this.lowerEdge = lowerEdge;
        this.higherEdge = higherEdge;
        this.action = action;
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

    public void Increment(float value, Action action = null)
    {
        this.value += value;
        if (value>0 && this.value >= this.higherEdge)
        {
            action?.Invoke();
            this.value = this.value - this.higherEdge + this.lowerEdge;
        }
        if (value<0 &&this.value <= this.lowerEdge)
        {
            action?.Invoke();
            this.value += this.value + this.higherEdge - this.lowerEdge;
        }
    }
}
