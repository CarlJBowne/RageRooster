using System;
using System.Collections;
using UnityEngine;

public class EntityStunner : MonoBehaviour
{
    public Behaviour[] disableComponents;
    public float defaultStunDuration = -1;
    public Action onUnStun;

    public new bool enabled
    {
        get => base.enabled;
        set
        {
            if(disableComponents.Length > 0)
                for (int i = 0; i < disableComponents.Length; i++)
                    if (disableComponents[i] != null) 
                        disableComponents[i].enabled = !value;
                
            targetUnStunTime = defaultStunDuration > 0 && value 
                ? Time.time + defaultStunDuration 
                : -1;

            base.enabled = value;
        }
    }

    private float targetUnStunTime = -1;

    public void Stun(float duration = -1)
    {
        enabled = true;
        targetUnStunTime = Time.time + duration;
    }
    public void Unstun() => enabled = false;

    public void ExtendStun(float time)
    {
        if (!enabled) return;
        targetUnStunTime += time;
    }

    private void Update()
    {
        if (targetUnStunTime > 0 && Time.time >= targetUnStunTime)
        {
            enabled = false;
            onUnStun?.Invoke();
        }
            
    }

    public void Reset() => enabled = false;
    public void Awake() => enabled = false;
}