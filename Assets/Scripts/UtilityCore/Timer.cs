using EditorAttributes;
using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif


public static class Timer
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
            if (disabled || rate < 0) return;
            if (rate == 0) callback?.Invoke();
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

        public OneTime(float length, bool activate = false)
        {
            this.length = length;
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

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(Timer.Loop))]
public class LoopPropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // Retrieve the serialized fields
        SerializedProperty rateProperty = property.FindPropertyRelative("rate");
        SerializedProperty currentProperty = property.FindPropertyRelative("current");

        // Draw the label
        position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

        // Adjust the width for the fields
        float fieldWidth = position.width / (EditorApplication.isPlaying ? 2 : 1);
        Rect rateRect = new Rect(position.x, position.y, fieldWidth, position.height);

        // Draw the "rate" field
        EditorGUI.PropertyField(rateRect, rateProperty, GUIContent.none);

        if (EditorApplication.isPlaying)
        {
            // Draw the range slider for "current" if in play mode
            Rect sliderRect = new Rect(position.x + fieldWidth + 5, position.y, fieldWidth - 5, position.height);
            float rateValue = rateProperty.floatValue;
            float currentValue = currentProperty.floatValue;
            currentValue = EditorGUI.Slider(sliderRect, currentValue, 0f, rateValue);
            currentProperty.floatValue = currentValue;
        }

        EditorGUI.EndProperty();
    }
}
[CustomPropertyDrawer(typeof(Timer.OneTime))]
public class OneTimePropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // Retrieve the serialized fields
        SerializedProperty lengthProperty = property.FindPropertyRelative("length");
        SerializedProperty currentProperty = property.FindPropertyRelative("current");

        // Draw the label
        position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

        // Adjust the width for the fields
        float fieldWidth = position.width / (EditorApplication.isPlaying ? 2 : 1);
        Rect rateRect = new Rect(position.x, position.y, fieldWidth, position.height);

        // Draw the "rate" field
        EditorGUI.PropertyField(rateRect, lengthProperty, GUIContent.none);

        if (EditorApplication.isPlaying)
        {
            // Draw the range slider for "current" if in play mode
            Rect sliderRect = new Rect(position.x + fieldWidth + 5, position.y, fieldWidth - 5, position.height);
            float lengthValue = lengthProperty.floatValue;
            float currentValue = currentProperty.floatValue;
            currentValue = EditorGUI.Slider(sliderRect, currentValue, 0f, lengthValue);
            currentProperty.floatValue = currentValue;
        }

        EditorGUI.EndProperty();
    }
}

#endif

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
        if (timer.value >= timer.higherEdge)
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
        if (!this) return false;
        value += amount;
        bool act = false;
        if (amount > 0 && value >= higherEdge)
        {
            value = value - higherEdge + lowerEdge;
            act = true;
        }
        if (amount < 0 && value <= lowerEdge)
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
